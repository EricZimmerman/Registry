using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Registry.Other;

namespace Registry.Test;

/// <summary>Builds primary files and transaction logs (new and old format) for replay tests</summary>
public static class LogBuilder
{
    private const ulong MarvinSeed = 0x82EF4D887A4E55C5;

    /// <summary>A base block (4096 bytes for a primary file, the first 512 for a log)</summary>
    public static byte[] BaseBlock(uint primarySequence, uint secondarySequence, uint fileType, uint hiveBinsDataSize, long lastWritten = 0x01D0000000000000, uint flags = 0, bool validChecksum = true)
    {
        var b = new byte[4096];
        Encoding.ASCII.GetBytes("regf").CopyTo(b, 0);
        BitConverter.GetBytes(primarySequence).CopyTo(b, 4);
        BitConverter.GetBytes(secondarySequence).CopyTo(b, 8);
        BitConverter.GetBytes(lastWritten).CopyTo(b, 12);
        BitConverter.GetBytes(1).CopyTo(b, 20);
        BitConverter.GetBytes(5).CopyTo(b, 24);
        BitConverter.GetBytes(fileType).CopyTo(b, 28);
        BitConverter.GetBytes(1).CopyTo(b, 32);
        BitConverter.GetBytes(0x20).CopyTo(b, 36);
        BitConverter.GetBytes(hiveBinsDataSize).CopyTo(b, 40);
        BitConverter.GetBytes(1).CopyTo(b, 44);
        Encoding.Unicode.GetBytes("\\SystemRoot\\TEST").CopyTo(b, 48);
        BitConverter.GetBytes(flags).CopyTo(b, 144);
        var checksum = LogReplay.Checksum(b);
        BitConverter.GetBytes(validChecksum ? checksum : checksum ^ 0x5A5A).CopyTo(b, 508);
        return b;
    }

    /// <summary>A hive bin of <paramref name="size" /> bytes at <paramref name="offset" />, filled with one free cell</summary>
    public static byte[] HiveBin(uint offset, uint size, byte fill)
    {
        var b = new byte[size];
        Encoding.ASCII.GetBytes("hbin").CopyTo(b, 0);
        BitConverter.GetBytes(offset).CopyTo(b, 4);
        BitConverter.GetBytes(size).CopyTo(b, 8);
        BitConverter.GetBytes((int)size - 0x20).CopyTo(b, 0x20);
        for (var i = 0x24; i < size; i++) b[i] = fill;
        return b;
    }

    /// <summary>A primary file: base block and hive bins of 4096 bytes, bin n filled with byte n + 1</summary>
    public static byte[] Primary(uint primarySequence, uint secondarySequence, int bins, bool validChecksum = true)
    {
        var ms = new MemoryStream();
        ms.Write(BaseBlock(primarySequence, secondarySequence, 0, (uint)(bins * 4096), validChecksum: validChecksum));
        for (var n = 0; n < bins; n++) ms.Write(HiveBin((uint)(n * 4096), 4096, (byte)(n + 1)));
        return ms.ToArray();
    }

    /// <summary>A new format log entry (HvLE) writing <paramref name="pages" /> (offsets relative to the hive bins data)</summary>
    public static byte[] Entry(uint sequence, uint hiveBinsDataSize, params (uint Offset, byte[] Data)[] pages)
    {
        var header = 40 + 8 * pages.Length;
        var size = header + pages.Sum(p => p.Data.Length);
        size = (size + 511) / 512 * 512;
        var b = new byte[size];
        Encoding.ASCII.GetBytes("HvLE").CopyTo(b, 0);
        BitConverter.GetBytes(size).CopyTo(b, 4);
        BitConverter.GetBytes(sequence).CopyTo(b, 12);
        BitConverter.GetBytes(hiveBinsDataSize).CopyTo(b, 16);
        BitConverter.GetBytes(pages.Length).CopyTo(b, 20);
        var at = header;
        for (var i = 0; i < pages.Length; i++)
        {
            BitConverter.GetBytes(pages[i].Offset).CopyTo(b, 40 + 8 * i);
            BitConverter.GetBytes(pages[i].Data.Length).CopyTo(b, 44 + 8 * i);
            pages[i].Data.CopyTo(b, at);
            at += pages[i].Data.Length;
        }

        BitConverter.GetBytes((ulong)Marvin.ComputeHash(ref b[40], b.Length - 40, MarvinSeed)).CopyTo(b, 24);
        BitConverter.GetBytes((ulong)Marvin.ComputeHash(ref b[0], 32, MarvinSeed)).CopyTo(b, 32);
        return b;
    }

    /// <summary>A new format log: a base block with <paramref name="sequence" />, then the entries</summary>
    public static byte[] NewLog(uint sequence, uint hiveBinsDataSize, params byte[][] entries)
    {
        var ms = new MemoryStream();
        ms.Write(BaseBlock(sequence, sequence, 6, hiveBinsDataSize), 0, 512);
        foreach (var e in entries) ms.Write(e);
        return ms.ToArray();
    }

    /// <summary>An old format log (DIRT): 512 byte pages of the hive bins data</summary>
    public static byte[] OldLog(uint sequence, uint hiveBinsDataSize, long lastWritten, params (int Page, byte[] Data)[] pages)
    {
        var ms = new MemoryStream();
        ms.Write(BaseBlock(sequence, sequence, 1, hiveBinsDataSize, lastWritten), 0, 512);
        var bitmap = new byte[hiveBinsDataSize / 4096];
        foreach (var (page, _) in pages) bitmap[page / 8] |= (byte)(1 << (page % 8));
        ms.Write(Encoding.ASCII.GetBytes("DIRT"));
        ms.Write(bitmap);
        while (ms.Length % 512 != 0) ms.WriteByte(0);
        foreach (var (_, data) in pages.OrderBy(t => t.Page)) ms.Write(data);
        return ms.ToArray();
    }

    public static byte[] Fill(int length, byte value) => Enumerable.Repeat(value, length).ToArray();

    public static List<TransactionLogFileInfo> Logs(params (string Name, byte[] Bytes)[] logs) =>
        logs.Select(t => new TransactionLogFileInfo(t.Name, t.Bytes)).ToList();
}
