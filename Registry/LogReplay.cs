using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Registry.Other;
using Serilog;

namespace Registry;

/// <summary>
///     Applies transaction logs to a dirty primary file, as described in Maxim Suhanov's Windows registry file format
///     specification (https://github.com/msuhanov/regf, CC BY 4.0), "Format of transaction log files", "Dirty state of a
///     hive" and "Multiple transaction log files":
///     <list type="bullet">
///         <item>
///             New format (*.LOG1/*.LOG2, Windows 8.1 and later): log entries ("HvLE") from the sequence number in the
///             log's base block on, each one more than the last; the first entry that is invalid (signature, size, hive
///             bins data size, hashes, sequence number) ends the log. Both logs are used, the one with earlier entries
///             first, then the other when it continues the sequence. A hive whose base block is invalid uses only the log
///             with the latest entries. The primary file grows to the hive bins data size of the entries applied.
///         </item>
///         <item>
///             Old format (Windows 8 and earlier): a dirty vector ("DIRT") of 512 byte pages, applied when the log is not
///             older than the primary file (by last written time); of two such logs, the newer one.
///         </item>
///     </list>
///     The recovered base block gets the last sequence number applied (as both sequence numbers), the hive bins data size
///     and flags of the last entry, and a new checksum
/// </summary>
internal static class LogReplay
{
    internal const int BaseBlockSize = 4096;
    private const int LogBaseBlockSize = 512;
    private const int HiveBinAlignment = 4096;
    private const ulong MarvinSeed = 0x82EF4D887A4E55C5;

    internal sealed class Result
    {
        public byte[] Bytes;
        public bool Recovered;
        public uint? LastSequenceNumber;
        public List<string> LogsApplied = new();
        public List<string> Messages = new();
    }

    /// <summary>A base block: the fields recovery uses</summary>
    internal sealed class BaseBlock
    {
        public BaseBlock(byte[] b, string name, bool primary)
        {
            Name = name;
            Bytes = b;
            Valid = b.Length >= 512 && BitConverter.ToUInt32(b, 508) == Checksum(b) && Encoding.ASCII.GetString(b, 0, 4) == "regf";
            PrimarySequence = BitConverter.ToUInt32(b, 4);
            SecondarySequence = BitConverter.ToUInt32(b, 8);
            FileType = BitConverter.ToUInt32(b, 28);
            Dirty = Valid == false || PrimarySequence != SecondarySequence;
            LastWritten = BitConverter.ToInt64(b, 12);
            RootCellOffset = BitConverter.ToUInt32(b, 36);
            HiveBinsDataSize = BitConverter.ToUInt32(b, 40);
            MinorVersion = BitConverter.ToUInt32(b, 24);
            Flags = b.Length >= 148 ? BitConverter.ToUInt32(b, 144) : 0;
            LastReorganized = b.Length >= 176 ? BitConverter.ToInt64(b, 168) : 0;

            if (primary && Valid == false)
            {
                //the base block can't be trusted: the hive bins data size from the file size, and the last written time
                //from the first hive bin
                HiveBinsDataSize = (uint)((b.Length - BaseBlockSize) / HiveBinAlignment * HiveBinAlignment);
                LastWritten = b.Length >= BaseBlockSize + 0x1C ? BitConverter.ToInt64(b, BaseBlockSize + 0x14) : 0;
            }
        }

        public string Name { get; }
        public byte[] Bytes { get; }
        public bool Valid { get; }
        public bool Dirty { get; }
        public uint PrimarySequence { get; }
        public uint SecondarySequence { get; }
        public uint FileType { get; }
        public long LastWritten { get; }
        public uint RootCellOffset { get; }
        public uint HiveBinsDataSize { get; }
        public uint MinorVersion { get; }
        public uint Flags { get; }
        public long LastReorganized { get; }
    }

    /// <summary>The XOR-32 checksum of the first 508 bytes (0 is written as 1, 0xFFFFFFFF as 0xFFFFFFFE)</summary>
    internal static uint Checksum(byte[] b)
    {
        uint x = 0;
        for (var i = 0; i < 508; i += 4)
        {
            x ^= BitConverter.ToUInt32(b, i);
        }

        return x switch
        {
            0 => 1,
            0xFFFFFFFF => 0xFFFFFFFE,
            _ => x
        };
    }

    internal sealed class State
    {
        internal BaseBlock Primary;
        public byte[] Bytes;
        public int ApplyCount;
        public uint? LastSequence;
        public uint HiveBinsDataSize;
        public uint Flags;
        internal BaseBlock OldLogBase;
        internal BaseBlock NewLogBase;
        public Result Result;

        /// <summary>Writes a page, growing the file when it is past the end</summary>
        public void Write(long offset, byte[] source, long sourceOffset, int length)
        {
            if (offset + length > Bytes.Length)
            {
                var grown = new byte[offset + length];
                Buffer.BlockCopy(Bytes, 0, grown, 0, Bytes.Length);
                Bytes = grown;
            }

            Buffer.BlockCopy(source, (int)sourceOffset, Bytes, (int)offset, length);
        }
    }

    /// <summary>A state for applying entries to <paramref name="hive" /> without the rest of recovery</summary>
    internal static State StateFor(byte[] hive)
    {
        var primary = new BaseBlock(hive, "primary", true);
        return new State { Primary = primary, Bytes = hive, HiveBinsDataSize = primary.HiveBinsDataSize, Flags = primary.Flags, Result = new Result { Bytes = hive } };
    }

    private sealed class NotEligibleException : Exception
    {
        public NotEligibleException(string message) : base(message)
        {
        }
    }

    /// <summary>Replays <paramref name="logs" /> (named *.LOG, *.LOG1, *.LOG2) over a copy of <paramref name="primary" /></summary>
    public static Result Replay(byte[] primary, IList<TransactionLogFileInfo> logs)
    {
        var result = new Result { Bytes = primary };
        var state = new State
        {
            Primary = new BaseBlock(primary, "primary", true),
            Bytes = (byte[])primary.Clone(),
            Result = result
        };
        state.HiveBinsDataSize = state.Primary.HiveBinsDataSize;
        state.Flags = state.Primary.Flags;

        if (state.Primary.Dirty == false)
        {
            result.Messages.Add("The hive is not dirty");
            return result;
        }

        TransactionLogFileInfo Named(string extension)
        {
            return logs.FirstOrDefault(t => t?.FileBytes is { Length: > 0 } && (t.FileName ?? string.Empty).EndsWith(extension, StringComparison.OrdinalIgnoreCase));
        }

        var log = Named(".LOG");
        var log1 = Named(".LOG1");
        var log2 = Named(".LOG2");

        //logs that aren't named like the hive's: the first two as LOG1 and LOG2
        var others = logs.Where(t => t?.FileBytes is { Length: > 0 } && t != log && t != log1 && t != log2).ToList();
        log1 ??= others.ElementAtOrDefault(0);
        log2 ??= others.ElementAtOrDefault(log1 == others.ElementAtOrDefault(0) ? 1 : 0);

        var newLog = IsNewLog(log, result);
        var newLog1 = IsNewLog(log1, result);
        var newLog2 = IsNewLog(log2, result);
        var oldLog = IsOldLog(log, result);
        var oldLog1 = IsOldLog(log1, result);
        var oldLog2 = IsOldLog(log2, result);

        try
        {
            //the new format and the dual logging scheme first
            if (newLog1 && newLog2)
            {
                ApplyNewLogs(state, log1, log2);
            }
            else if (newLog1 || newLog2)
            {
                ApplyNewLog(state, newLog1 ? log1 : log2);
            }
            else if (newLog)
            {
                ApplyNewLog(state, log);
            }
            else if (oldLog1 && oldLog2)
            {
                //the newer of the two
                var ts1 = new BaseBlock(log1.FileBytes, log1.FileName, false).LastWritten;
                var ts2 = new BaseBlock(log2.FileBytes, log2.FileName, false).LastWritten;
                ApplyOldLog(state, ts1 >= ts2 ? log1 : log2);
            }
            else if (oldLog1 || oldLog2)
            {
                ApplyOldLog(state, oldLog1 ? log1 : log2);
            }
            else if (oldLog)
            {
                ApplyOldLog(state, log);
            }
            else
            {
                result.Messages.Add("No transaction log that can be applied was found");
            }
        }
        catch (NotEligibleException e)
        {
            result.Messages.Add(e.Message);
        }

        if (state.ApplyCount == 0)
        {
            return result;
        }

        result.Recovered = true;
        result.LastSequenceNumber = state.LastSequence;
        result.Bytes = FinishBaseBlock(state);
        return result;
    }

    private static bool IsNewLog(TransactionLogFileInfo log, Result result)
    {
        if (log == null)
        {
            return false;
        }

        var b = log.FileBytes;
        if (b.Length <= LogBaseBlockSize + 40 || Encoding.ASCII.GetString(b, 0, 4) != "regf" || BitConverter.ToUInt32(b, 28) != 6)
        {
            return false;
        }

        var bb = new BaseBlock(b, log.FileName, false);
        if (bb.Dirty)
        {
            result.Messages.Add($"{Path.GetFileName(log.FileName)}: its base block is invalid or dirty, so it is not used");
            return false;
        }

        return true;
    }

    private static bool IsOldLog(TransactionLogFileInfo log, Result result)
    {
        if (log == null)
        {
            return false;
        }

        var b = log.FileBytes;
        if (b.Length < LogBaseBlockSize + 4 || Encoding.ASCII.GetString(b, 0, 4) != "regf")
        {
            return false;
        }

        var type = BitConverter.ToUInt32(b, 28);
        if (type != 1 && type != 2)
        {
            return false;
        }

        var bb = new BaseBlock(b, log.FileName, false);
        if (bb.Dirty)
        {
            result.Messages.Add($"{Path.GetFileName(log.FileName)}: its base block is invalid or dirty, so it is not used");
            return false;
        }

        return b.Length >= DirtyPagesStart(bb.HiveBinsDataSize) + 512 && Encoding.ASCII.GetString(b, LogBaseBlockSize, 4) == "DIRT";
    }

    /// <summary>Sequence number <paramref name="a" /> is earlier than <paramref name="b" /> (allowing for overflow)</summary>
    private static bool IsEarlier(uint a, uint b)
    {
        if (a >= b)
        {
            return a - b > 0x7FFFFFFF;
        }

        return b - a <= 0x7FFFFFFF;
    }

    private static void ApplyNewLogs(State state, TransactionLogFileInfo log1, TransactionLogFileInfo log2)
    {
        var seq1 = BitConverter.ToUInt32(log1.FileBytes, 4);
        var seq2 = BitConverter.ToUInt32(log2.FileBytes, 4);
        var (first, second) = IsEarlier(seq1, seq2) ? (log1, log2) : (log2, log1);

        if (state.Primary.Valid == false)
        {
            //only the log with the latest entries (as Windows does)
            ApplyNewLog(state, second);
            return;
        }

        try
        {
            ApplyNewLog(state, first);
        }
        catch (NotEligibleException e)
        {
            state.Result.Messages.Add(e.Message);
            ApplyNewLog(state, second);
            return;
        }

        if (state.LastSequence.HasValue && BitConverter.ToUInt32(second.FileBytes, 4) == unchecked(state.LastSequence.Value + 1))
        {
            ApplyNewLog(state, second);
        }
    }

    private static void ApplyNewLog(State state, TransactionLogFileInfo log)
    {
        var b = log.FileBytes;
        var bb = new BaseBlock(b, log.FileName, false);
        var name = Path.GetFileName(log.FileName);

        if (state.ApplyCount >= 2)
        {
            throw new NotEligibleException($"{name}: no more than two logs can be applied");
        }

        if (state.LastSequence.HasValue && IsEarlier(bb.PrimarySequence, unchecked(state.LastSequence.Value + 1)))
        {
            throw new NotEligibleException($"{name}: its entries are not after the last one applied");
        }

        if (state.Primary.Valid && IsEarlier(bb.PrimarySequence, state.Primary.SecondarySequence))
        {
            throw new NotEligibleException($"{name}: its entries (from sequence number {bb.PrimarySequence}) are older than the hive ({state.Primary.SecondarySequence})");
        }

        state.NewLogBase = bb;
        state.Flags = bb.Flags;

        var applied = ApplyEntries(state, b, bb.PrimarySequence, name);

        state.ApplyCount += 1;
        state.Result.LogsApplied.Add(log.FileName);
        state.Result.Messages.Add(applied > 0
            ? $"{name}: applied {applied:N0} log entries (sequence numbers {bb.PrimarySequence} to {state.LastSequence})"
            : $"{name}: no log entries to apply");
    }

    /// <summary>
    ///     Applies the log entries of a new format log from <paramref name="firstSequence" /> on, until one is invalid or out
    ///     of sequence; returns how many were applied
    /// </summary>
    internal static int ApplyEntries(State state, byte[] b, uint firstSequence, string name)
    {
        var expected = firstSequence;
        var position = (long)LogBaseBlockSize;
        var applied = 0;
        while (position + 40 <= b.Length)
        {
            var problem = InvalidEntry(b, position, expected);
            if (problem != null)
            {
                //a log holds older entries after the current ones, so this is how most logs end
                Log.Debug("{Log}: stopping at offset 0x{Position:X}: {Problem}", name, position, problem);
                break;
            }

            var size = BitConverter.ToUInt32(b, (int)position + 4);
            var count = BitConverter.ToUInt32(b, (int)position + 20);
            var pages = position + 40 + 8L * count;
            var delta = 0L;
            var refs = new List<(uint Offset, uint Size)>();
            for (var i = 0; i < count; i++)
            {
                refs.Add((BitConverter.ToUInt32(b, (int)(position + 40 + 8 * i)), BitConverter.ToUInt32(b, (int)(position + 44 + 8 * i))));
            }

            if (refs.Sum(r => (long)r.Size) + pages > b.Length)
            {
                state.Result.Messages.Add($"{name}: the dirty pages of the entry with sequence number {expected} are past the end of the log");
                break;
            }

            state.LastSequence = expected;
            state.Flags = (state.Flags & ~1u) | (BitConverter.ToUInt32(b, (int)position + 8) & 1);
            state.HiveBinsDataSize = BitConverter.ToUInt32(b, (int)position + 16);

            foreach (var r in refs)
            {
                state.Write(BaseBlockSize + (long)r.Offset, b, pages + delta, (int)r.Size);
                delta += r.Size;
            }

            applied += 1;
            position += size;
            expected = unchecked(expected + 1);
        }

        return applied;
    }

    /// <summary>Why the log entry at <paramref name="position" /> can't be applied, or null</summary>
    private static string InvalidEntry(byte[] b, long position, uint expected)
    {
        if (Encoding.ASCII.GetString(b, (int)position, 4) != "HvLE")
        {
            return "no log entry signature";
        }

        var size = BitConverter.ToUInt32(b, (int)position + 4);
        if (size < 512 || size % 512 != 0)
        {
            return $"invalid size 0x{size:X}";
        }

        if (position + size > b.Length)
        {
            return $"the entry (0x{size:X} bytes) is past the end of the log";
        }

        var hiveBinsDataSize = BitConverter.ToUInt32(b, (int)position + 16);
        if (hiveBinsDataSize < HiveBinAlignment || hiveBinsDataSize % HiveBinAlignment != 0)
        {
            return $"invalid hive bins data size 0x{hiveBinsDataSize:X}";
        }

        var count = BitConverter.ToUInt32(b, (int)position + 20);
        if (count == 0 || 40 + 8L * count > size)
        {
            return $"invalid dirty pages count {count}";
        }

        var hash1 = BitConverter.ToUInt64(b, (int)position + 24);
        var hash2 = BitConverter.ToUInt64(b, (int)position + 32);
        if ((ulong)Marvin.ComputeHash(ref b[position], 32, MarvinSeed) != hash2 ||
            (ulong)Marvin.ComputeHash(ref b[position + 40], (int)size - 40, MarvinSeed) != hash1)
        {
            return "invalid hashes";
        }

        var sequence = BitConverter.ToUInt32(b, (int)position + 12);
        return sequence == expected ? null : $"sequence number {sequence} where {expected} was expected";
    }

    private static long DirtyPagesStart(uint hiveBinsDataSize)
    {
        var unaligned = LogBaseBlockSize + 4L + hiveBinsDataSize / 4096;
        return (unaligned + 511) / 512 * 512;
    }

    private static void ApplyOldLog(State state, TransactionLogFileInfo log)
    {
        var b = log.FileBytes;
        var bb = new BaseBlock(b, log.FileName, false);
        var name = Path.GetFileName(log.FileName);

        if (bb.LastWritten < state.Primary.LastWritten)
        {
            throw new NotEligibleException($"{name}: it is older than the hive");
        }

        state.OldLogBase = bb;
        state.HiveBinsDataSize = bb.HiveBinsDataSize;
        state.Flags = bb.Flags;

        var bitmapLength = bb.HiveBinsDataSize / 4096;
        var pages = DirtyPagesStart(bb.HiveBinsDataSize);
        var n = 0L;
        var applied = 0;
        for (var bit = 0L; bit < bitmapLength * 8L; bit++)
        {
            if (((b[LogBaseBlockSize + 4 + bit / 8] >> (int)(bit % 8)) & 1) == 0)
            {
                continue;
            }

            var from = pages + n * 512;
            if (from + 512 > b.Length)
            {
                state.Result.Messages.Add($"{name}: dirty page {n} is past the end of the log");
                break;
            }

            state.Write(BaseBlockSize + bit * 512, b, from, 512);
            n += 1;
            applied += 1;
        }

        state.ApplyCount += 1;
        state.Result.LogsApplied.Add(log.FileName);
        state.Result.Messages.Add($"{name}: applied {applied:N0} dirty pages (old format)");
    }

    /// <summary>
    ///     The primary file's base block when it is valid, else the log's; with the sequence numbers, hive bins data size
    ///     and flags recovery ended with, file type 0, and its checksum
    /// </summary>
    private static byte[] FinishBaseBlock(State state)
    {
        var bytes = state.Bytes;
        var baseBlock = new byte[BaseBlockSize];
        if (state.Primary.Valid)
        {
            Buffer.BlockCopy(bytes, 0, baseBlock, 0, BaseBlockSize);
        }
        else
        {
            var source = (state.OldLogBase ?? state.NewLogBase).Bytes;
            Buffer.BlockCopy(source, 0, baseBlock, 0, LogBaseBlockSize);
        }

        var sequence = state.LastSequence ?? BitConverter.ToUInt32(baseBlock, 4);
        BitConverter.GetBytes(sequence).CopyTo(baseBlock, 4);
        BitConverter.GetBytes(sequence).CopyTo(baseBlock, 8);
        BitConverter.GetBytes(state.HiveBinsDataSize).CopyTo(baseBlock, 40);
        BitConverter.GetBytes(state.Flags).CopyTo(baseBlock, 144);
        BitConverter.GetBytes(0u).CopyTo(baseBlock, 28);
        BitConverter.GetBytes(Checksum(baseBlock)).CopyTo(baseBlock, 508);

        Buffer.BlockCopy(baseBlock, 0, bytes, 0, BaseBlockSize);
        return bytes;
    }
}
