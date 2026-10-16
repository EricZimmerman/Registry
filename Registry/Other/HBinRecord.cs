using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Registry.Cells;
using Registry.Lists;
using Serilog;
using Serilog.Events;
using static Registry.Other.Helpers;

// namespaces...

namespace Registry.Other;

// public classes...
public class HBinRecord
{
    private readonly int _minorVersion;
    private readonly bool _recoverDeleted;

    private readonly RegistryHive _registryHive;
    private byte[] _rawBytes;

    // protected internal constructors...
    /// <summary>
    ///     Initializes a new instance of the <see cref="HBinRecord" /> class.
    ///     <remarks>Represents a Hive Bin Record</remarks>
    /// </summary>
    protected internal HBinRecord(byte[] rawBytes, long relativeOffset, int minorVersion, bool recoverDeleted,
        RegistryHive reg)
    {
        RelativeOffset = relativeOffset;

        _registryHive = reg;

        _recoverDeleted = recoverDeleted;

        _minorVersion = minorVersion;

        _rawBytes = rawBytes;

        Signature = Encoding.ASCII.GetString(rawBytes, 0, 4);

        var sig = BitConverter.ToInt32(rawBytes, 0);

        // Check.That(sig).IsEqualTo(HbinSignature);
        if (sig != HbinSignature) throw new Exception("Invalid hbin signature");

        //  reg.Logger.Trace("Got valid hbin signature for hbin at absolute offset 0x{0:X}", AbsoluteOffset);

        FileOffset = BitConverter.ToUInt32(rawBytes, 0x4);

        Size = BitConverter.ToUInt32(rawBytes, 0x8);

        Reserved = BitConverter.ToUInt32(rawBytes, 0xc);

        var ts = BitConverter.ToInt64(rawBytes, 0x14);

        try
        {
            var dt = DateTimeOffset.FromFileTime(ts).ToUniversalTime();

            if (dt.Year > 1601) LastWriteTimestamp = dt;
        }
        catch (Exception) //ncrunch: no coverage
        {
            //ncrunch: no coverage
            //very rarely you get a 'Not a valid Win32 FileTime' error, so trap it if thats the case
        } //ncrunch: no coverage

        Spare = BitConverter.ToUInt32(rawBytes, 0xc);
    }

    // public properties...
    /// <summary>
    ///     The offset to this record from the beginning of the hive, in bytes
    /// </summary>
    public long AbsoluteOffset => RelativeOffset + 4096;

    // public properties...
    /// <summary>
    ///     The relative offset to this record
    /// </summary>
    public uint FileOffset { get; }

    /// <summary>
    ///     The last write time of this key
    /// </summary>
    public DateTimeOffset? LastWriteTimestamp { get; }

    /// <summary>
    ///     The offset to this record as stored by other records
    ///     <remarks>This value will be 4096 bytes (the size of the regf header) less than the AbsoluteOffset</remarks>
    /// </summary>
    public long RelativeOffset { get; }

    public uint Reserved { get; }

    /// <summary>
    ///     The signature of the hbin record. Should always be "hbin"
    /// </summary>
    public string Signature { get; }

    /// <summary>
    ///     The size of the hive
    ///     <remarks>
    ///         This value will always be positive. See IsFree to determine whether or not this cell is in use (it has a
    ///         negative size)
    ///     </remarks>
    /// </summary>
    public uint Size { get; }

    public uint Spare { get; }

    public List<IRecordBase> Process()
    {
        var records = new List<IRecordBase>();

        //additional cell data starts 32 bytes (0x20) in
        var offsetInHbin = 0x20;

        _registryHive.TotalBytesRead += 0x20;

        while (offsetInHbin < Size)
        {
            var recordSize = BitConverter.ToUInt32(_rawBytes, offsetInHbin);

            var readSize = (int) recordSize;

            if (!_recoverDeleted && readSize > 0)
            {
                //since we do not want to get deleted stuff, if the cell size > 0, its free, so skip it
                offsetInHbin += readSize;
                continue;
            }

            // if we get a negative number here the record is allocated, but we cant read negative bytes, so get absolute value
            readSize = Math.Abs(readSize);

            if (readSize < 8 || offsetInHbin + (long) readSize > _rawBytes.Length)
            {
                //cells are at least 8 bytes (and 8 byte aligned), and in the hbin; anything else means this hbin is damaged here
                Log.Warning("Cell at absolute offset 0x{Offset:X} has an invalid size (0x{Size:X}). Skipping the rest of the hbin",
                    offsetInHbin + RelativeOffset + 0x1000, recordSize);
                RegistryHive.SoftParsingErrorsInternal += 1;
                break;
            }

            if (Log.IsEnabled(LogEventLevel.Verbose))
                Log.Verbose("Processing hbin at relative offset {RelativeOffset} (Absolute offset: {AbsoluteOffset})",
                    $"0x{RelativeOffset:X}", $"0x{AbsoluteOffset:X}");

            _registryHive.TotalBytesRead += readSize;

            var cellSignature2 = BitConverter.ToInt16(_rawBytes, offsetInHbin + 4);

            if (cellSignature2 == LkSignature && readSize < 0x50)
                //this is not an ln, its data, so update the sig
                cellSignature2 = 0x0;

            //nk and vk records read themselves from the hive, and data cells are only needed to recover deleted records,
            //so only the others are copied
            var needsCopy = cellSignature2 switch
            {
                NkSignature or VkSignature => false,
                LfSignature or LhSignature or LiSignature or RiSignature or DbSignature or LkSignature or SkSignature => true,
                _ => _recoverDeleted
            };

            if (needsCopy == false && cellSignature2 != NkSignature && cellSignature2 != VkSignature)
            {
                offsetInHbin += readSize;
                continue;
            }

            var rawRecord = needsCopy ? Helpers.Slice(_rawBytes, offsetInHbin, readSize) : null;
            var cellSignature = Encoding.ASCII.GetString(_rawBytes, offsetInHbin + 4, 2);

            ICellTemplate cellRecord = null;
            IListTemplate listRecord = null;
            DataNode dataRecord = null;

            try
            {
                switch (cellSignature2)
                {
                    case LfSignature:
                    case LhSignature:
                        listRecord = new LxListRecord(rawRecord, offsetInHbin + RelativeOffset);
                        break;

                    case LiSignature:
                        listRecord = new LiListRecord(rawRecord, offsetInHbin + RelativeOffset);
                        break;

                    case RiSignature:
                        listRecord = new RiListRecord(rawRecord, offsetInHbin + RelativeOffset);
                        break;

                    case DbSignature:
                        listRecord = new DbListRecord(rawRecord, offsetInHbin + RelativeOffset);
                        break;

                    case LkSignature:

                        cellRecord = new LkCellRecord(rawRecord, offsetInHbin + RelativeOffset);
                        break; //ncrunch: no coverage

                    case NkSignature:
                        if (readSize >= 0x30) // the minimum length for a recoverable record
                            cellRecord = new NkCellRecord(readSize, offsetInHbin + RelativeOffset,
                                _registryHive);

                        break;
                    case SkSignature:
                        if (readSize >= 0x14) // the minimum length for a recoverable record
                            cellRecord = new SkCellRecord(rawRecord, offsetInHbin + RelativeOffset);

                        break;

                    case VkSignature:
                        if (readSize >= 0x18) // the minimum length for a recoverable record
                            cellRecord = new VkCellRecord(readSize, offsetInHbin + RelativeOffset,
                                _minorVersion, _registryHive);

                        break;

                    default:
                        dataRecord = new DataNode(rawRecord, offsetInHbin + RelativeOffset);
                        break;
                }
            }
            catch (Exception ex)
            {
                //check size and see if its free. if so, dont worry about it. too small to be of value, but store it somewhere else?

                var size = BitConverter.ToInt32(_rawBytes, offsetInHbin);
                rawRecord ??= Helpers.Slice(_rawBytes, offsetInHbin, readSize);

                if (size < 0)
                {
                    //ncrunch: no coverage
                    RegistryHive.HardParsingErrorsInternal += 1; //ncrunch: no coverage

                    Log.Error( //ncrunch: no coverage     
                        ex,
                        "Hard error processing record with cell signature {CellSignature} at Absolute Offset: {Offset} with raw data: {RawRecord}",
                        cellSignature, $"0x{offsetInHbin + RelativeOffset + 4096:X}", BitConverter.ToString(rawRecord));

                    //TODO store it somewhere else as a placeholder if its in use. include relative offset and other critical stuff
                } //ncrunch: no coverage                     
                else
                {
                    Log.Warning(
                        "Soft error processing record with cell signature {CellSignature} at Absolute Offset: {Offset} with raw data: {RawRecord}",
                        cellSignature, $"0x{offsetInHbin + RelativeOffset + 4096:X}", BitConverter.ToString(rawRecord));
                    //This record is marked 'Free' so its not as important of an error
                    RegistryHive.SoftParsingErrorsInternal += 1;
                }
            }

            List<IRecordBase> carvedRecords = null;

            if (cellRecord != null)
            {
                if (cellRecord.IsFree)
                    carvedRecords = ExtractRecordsFromSlack(cellRecord.RawBytes, cellRecord.RelativeOffset);
                else
                    records.Add((IRecordBase) cellRecord);
            }

            if (listRecord != null)
            {
                if (_recoverDeleted)
                    carvedRecords = ExtractRecordsFromSlack(listRecord.RawBytes, listRecord.RelativeOffset);

                records.Add((IRecordBase) listRecord);
            }

            if (dataRecord != null && _recoverDeleted)
                carvedRecords = ExtractRecordsFromSlack(dataRecord.RawBytes, dataRecord.RelativeOffset);

            if (carvedRecords != null)
                if (carvedRecords.Count > 0)
                    records.AddRange(carvedRecords);

            offsetInHbin += readSize;
        }

        _rawBytes = null;
        return records;
    }

    private List<IRecordBase> ExtractRecordsFromSlack(byte[] remainingData, long relativeoffset)
    {
        var records = new List<IRecordBase>();

//            if (remainingData.Length == 4064 && _registryHive.HivePath.Contains("DeletedBags"))
//            {
//                Debug.WriteLine(1);
//            }

        var offsetList2 = new List<int>();

        byte[] raw = null;

        //   _registryHive.Logger.Trace("Looking for cell signatures at absolute offset 0x{0:X}",
        //          relativeoffset + 0x1000);

        for (var i = 5; i < remainingData.Length; i++)
            if (remainingData[i] == 0x6b) //6b == k
                if (remainingData[i - 1] == 0x6e || remainingData[i - 1] == 0x76) //6e = n, 76 = v
                    //if we are here we have a good signature, nk or vk
                    //check what is before that to see if its 0x00 or 0xFF
                    if (remainingData[i - 2] == 0x00 || remainingData[i - 2] == 0xFF)
                        //winner! since we initially hit on ZZ, subtract 5 to get to the beginning of the record, XX XX XX XX YY ZZ
                        offsetList2.Add(i - 5);

        //offsetList2 now has offset of every record signature we are interested in
        foreach (var i in offsetList2)
            try
            {
                var actualStart = i;

                var size = BitConverter.ToInt32(remainingData, actualStart);

                if (Math.Abs(size) <= 3)
                    //if its empty, move on
                    continue;

                var declared = Math.Abs(size);
                var pastCell = remainingData.Length - actualStart < declared;
                if (pastCell)
                {
                    //issue #20: a remnant whose size goes past the end of this cell. When a deleted key's cell was merged
                    //with the one before it, and the result split again to hold (say) a subkeys list, the remnant is in
                    //the slack of that list, and its data goes on in the hive past the list's cell. Read it from the hive
                    //(the records read their bytes from there), when it is in the file
                    var absolute = relativeoffset + actualStart + 0x1000;
                    if (absolute + declared > _registryHive.FileBytes.Length || declared > 0x10000)
                        continue;

                    raw = _registryHive.ReadBytesFromHive(absolute, declared);
                }
                else
                {
                    raw = Helpers.Slice(remainingData, actualStart, declared);
                }

                if (raw.Length < 6) continue;
                // since we need 4 bytes for the size and 2 for sig, if its smaller than 6, go to next one

                var sig2 = BitConverter.ToInt16(raw, 4);

                switch (sig2)
                {
                    case NkSignature:
                        //read past its cell (issue #20), it has to be a whole key node, name included; inside the cell, a
                        //remnant whose name is gone is kept ("(Unable to determine name)"), as it always was
                        if (raw.Length <= 0x30 || (pastCell && (raw.Length <= 0x50 || 0x50 + BitConverter.ToUInt16(raw, 0x4C) > raw.Length))) continue;

                        var nk = new NkCellRecord(raw.Length, relativeoffset + actualStart, _registryHive);
                        if (nk.LastWriteTimestamp.Year > 1900)
                            //              _registryHive.Logger.Trace("Found nk record in slack at absolute offset 0x{0:X}",
                            //                      relativeoffset + actualStart + 0x1000);
                            records.Add(nk);

                        break;
                    case VkSignature:
                        if (raw.Length < 0x18 || (pastCell && 0x18 + BitConverter.ToUInt16(raw, 0x6) > raw.Length))
                            //cant have a record shorter than this, even when no name is present
                            continue;

                        var vk = new VkCellRecord(raw.Length, relativeoffset + actualStart, _minorVersion,
                            _registryHive);
                        //      _registryHive.Logger.Trace("Found vk record in slack at absolute offset 0x{0:X}",
                        //              relativeoffset + actualStart + 0x1000);
                        records.Add(vk);
                        break;
                }
            }
            catch (Exception ex) //ncrunch: no coverage
            {
                //ncrunch: no coverage
                // this is a corrupted/unusable record
                Log.Debug( //ncrunch: no coverage
                    ex,
                    "When recovering from slack at absolute offset {Offset}, an error happened! raw Length: {RawLength}",
                    $"0x{relativeoffset + i + 0x1000:X8}", $"0x{raw?.Length:X}");

                RegistryHive.SoftParsingErrorsInternal += 1; //ncrunch: no coverage
            } //ncrunch: no coverage


        return records;
    }

    // public methods...
    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Size: 0x{Size:X}");
        sb.AppendLine($"Relative Offset: 0x{RelativeOffset:X}");
        sb.AppendLine($"Absolute Offset: 0x{AbsoluteOffset:X}");

        sb.AppendLine($"Signature: {Signature}");

        if (LastWriteTimestamp.HasValue) sb.AppendLine($"Last Write Timestamp: {LastWriteTimestamp}");

        sb.AppendLine();

        sb.AppendLine();
        sb.AppendLine($"File offset: 0x{FileOffset:X}");
        sb.AppendLine();

        sb.AppendLine($"Reserved: 0x{Reserved:X}");
        sb.AppendLine($"Spare: 0x{Spare:X}");

        return sb.ToString();
    }
}