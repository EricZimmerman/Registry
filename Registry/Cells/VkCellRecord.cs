﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Registry.Lists;
using Registry.Other;
using Serilog;

// namespaces...

namespace Registry.Cells;

// public classes...
// internal classes...
/// <summary>
///     <remarks>Represents a Key Value Record</remarks>
/// </summary>
public class VkCellRecord : ICellTemplate, IRecordBase
{
    // public enums...
    public enum DataTypeEnum
    {
        [Description("Binary data (any arbitrary data)")]
        RegBinary = 0x0003,

        [Description("A DWORD value, a 32-bit unsigned integer (little-endian)")]
        RegDword = 0x0004,

        [Description("A DWORD value, a 32-bit unsigned integer (big endian)")]
        RegDwordBigEndian = 0x0005,

        [Description(
            "An 'expandable' string value that can contain environment variables, normally stored and exposed in UTF-16LE"
        )]
        RegExpandSz = 0x0002,
        [Description("FILETIME data")] RegFileTime = 0x0010,

        [Description(
            "A resource descriptor (used by the Plug-n-Play hardware enumeration and configuration)")]
        RegFullResourceDescription
            = 0x0009,

        [Description(
            "A symbolic link (UNICODE) to another Registry key, specifying a root key and the path to the target key"
        )]
        RegLink = 0x0006,

        [Description(
            "A multi-string value, which is an ordered list of non-empty strings, normally stored and exposed in UTF-16LE, each one terminated by a NUL character"
        )]
        RegMultiSz = 0x0007,

        [Description("No type (the stored value, if any)")]
        RegNone = 0x0000,

        [Description("A QWORD value, a 64-bit integer (either big- or little-endian, or unspecified)")]
        RegQword =
            0x000B,

        [Description(
            "A resource list (used by the Plug-n-Play hardware enumeration and configuration)")]
        RegResourceList =
            0x0008,

        [Description("A resource requirements list (used by the Plug-n-Play hardware enumeration and configuration)"
        )]
        RegResourceRequirementsList = 0x000A,

        [Description("A string value, normally stored and exposed in UTF-16LE")]
        RegSz = 0x0001,
        [Description("Unknown data type")] RegUnknown = 999,

        [Description("UWP Byte")] RegUwpByte = 257,
        [Description("UWP Int16")] RegUwpInt16 = 258,
        [Description("UWP Uint16")] RegUwpUint16 = 259,
        [Description("UWP Int32")] RegUwpInt32 = 260,
        [Description("UWP Uint32")] RegUwpUint32 = 261,
        [Description("UWP Int64")] RegUwpInt64 = 262,
        [Description("UWP Uint64")] RegUwpUint64 = 263,
        [Description("UWP Single")] RegUwpSingle = 264,
        [Description("UWP Double")] RegUwpDouble = 265,
        [Description("UWP Char")] RegUwpChar = 266,
        [Description("UWP Boolean")] RegUwpBoolean = 267,
        [Description("UWP String")] RegUwpString = 268,
        [Description("UWP Composite Value")] RegUwpCompositeValue = 269,
        [Description("UWP DateTimeOffset")] RegUwpDateTimeOffset = 270,
        [Description("UWP TimeSpan")] RegUwpTimeSpan = 271,
        [Description("UWP Guid")] RegUwpGuid = 272,
        [Description("UWP Point")] RegUwpPoint = 273,
        [Description("UWP Size")] RegUwpSize = 274,
        [Description("UWP Rect")] RegUwpRect = 275,
        [Description("UWP Array of Bytes")] RegUwpArrayByte = 276,
        [Description("UWP Array of Int16")] RegUwpArrayInt16 = 277,
        [Description("UWP Array of Uint16")] RegUwpArrayUint16 = 278,
        [Description("UWP Array of Int32")] RegUwpArrayInt32 = 279,
        [Description("UWP Array of Uint32")] RegUwpArrayUint32 = 280,
        [Description("UWP Array of Int64")] RegUwpArrayInt64 = 281,
        [Description("UWP Array of Uint64")] RegUwpArrayUint64 = 282,
        [Description("UWP Array of Single")] RegUwpArraySingle = 283,
        [Description("UWP Array of Double")] RegUwpArrayDouble = 284,
        [Description("UWP Array of Char")] RegUwpArrayChar = 285,
        [Description("UWP Array of Boolean")] RegUwpArrayBoolean = 286,
        [Description("UWP Array of String")] RegUwpArrayString = 287,
        [Description("UWP Array of DateTimeOffset")] RegUwpArrayDateTimeOffset = 288,
        [Description("UWP Array of TimeSpan")] RegUwpArrayTimeSpan = 289,
        [Description("UWP Array of Guid")] RegUwpArrayGuid = 290,
        [Description("UWP Array of Point")] RegUwpArrayPoint = 291,
        [Description("UWP Array of Size")] RegUwpArraySize = 292,
        [Description("UWP Array of Rect")] RegUwpArrayRect = 293
    }

    private const uint DwordSignMask = 0x80000000;

    private const uint DevpropMaskType = 0x00000FFF;
    private readonly bool _dataIsResident;
    private readonly int _minorVersion;

    private readonly int _rawBytesLength;

    private readonly IRegistry _registryHive;

    private uint _dataLengthInternal;
    private int _internalDataOffset;

    // public constructors...
    /// <summary>
    ///     Initializes a new instance of the <see cref="VkCellRecord" /> class.
    /// </summary>
    public VkCellRecord(int recordSize, long relativeOffset, int minorVersion, IRegistry registryHive)
    {
        RelativeOffset = relativeOffset;

        _registryHive = registryHive;
        _minorVersion = minorVersion;

        _rawBytesLength = recordSize;

        DataOffsets = new HashSet<ulong>();

        OffsetToData = BitConverter.ToUInt32(RawBytes, 0x0c);

        _dataLengthInternal = DataLength;

        //if the high bit is set, data lives in the field used to typically hold the OffsetToData Value
        _dataIsResident = (_dataLengthInternal & DwordSignMask) == DwordSignMask;

        //this is used later to pull the data from the raw bytes. By setting this here we do not need a bunch of if/then stuff later
        _internalDataOffset = 4;

        if (_dataIsResident)
        {
            //normalize the data for future use
            _dataLengthInternal = _dataLengthInternal - 0x80000000;

            //A data size of 4 uses all 4 bytes of the data offset
            //A data size of 2 uses the last 2 bytes of the data offset (on a little-endian system)
            //A data size of 1 uses the last byte (on a little-endian system)
            //A data size of 0 represents that the value is not set (or NULL)

            _internalDataOffset = 0;
        }
        else if (IsFree)
        {
            //for free records, we need to do extra check to make sure non-resident data record has not been reallocated.
            //read the non-resident data in order to check size. if its negative, its in use, and therefore has been reused

            try
            {
                //while we could just look at DataBlockRaw, this is a lot more I/O, so just grab what we need here.
                var sizeBytes = _registryHive.ReadBytesFromHive(4096 + OffsetToData, 4);

                var dbSize = BitConverter.ToInt32(sizeBytes, 0);

                if (dbSize < 0)
                    //data block is in use somewhere else
                    DataRecordAllocated = true;
            }
            catch (Exception)
            {
                //crazy things can happen in IsFree records
            }
        }

        //force to a known datatype 
        var dataTypeInternal = DataTypeRaw;

        if (dataTypeInternal > (ulong) DataTypeEnum.RegUwpArrayRect) dataTypeInternal = 999;

        DataType = (DataTypeEnum) dataTypeInternal;
    }

    private byte[] DataBlockRaw
    {
        get
        {
            var dataBlockSize = 0;
            byte[] datablockRaw;

            if (_dataIsResident)
            {
                if (DataType == DataTypeEnum.RegDwordBigEndian)
                    //this is a special case where the data length shows up as 2, but a dword needs 4 bytes, so adjust
                    _dataLengthInternal = 4;
                //Since its resident, the data lives in the OffsetToData.

                datablockRaw = new ArraySegment<byte>(RawBytes, 0xC, (int) _dataLengthInternal).ToArray();

                //set our data length to what is available since its resident and unknown. it can be used for anything
                if (DataType == DataTypeEnum.RegUnknown) _dataLengthInternal = 4;
            }
            else
            {
                //We have to go look at the OffsetToData to see what we have so we can do the right thing
                //The first operations are always the same. Go get the length of the data cell, then see how big it is.

                var datablockSizeRaw = Array.Empty<byte>();

                if (IsFree)
                    try
                    {
                        datablockSizeRaw = _registryHive.ReadBytesFromHive(4096 + OffsetToData, 4);
                    }
                    catch (Exception)
                    {
                        //crazy things can happen in IsFree records
                    }
                else
                    datablockSizeRaw = _registryHive.ReadBytesFromHive(4096 + OffsetToData, 4);

                //add this offset so we can mark the data cells as referenced later
                DataOffsets.Add(OffsetToData);

                // in some rare cases the bytes returned from above are all zeros, so make sure we get something but all zeros
                if (datablockSizeRaw.Length == 4)
                    try
                    {
                        dataBlockSize = Math.Abs(BitConverter.ToInt32(datablockSizeRaw, 0));
                    }
                    catch (Exception)
                    {
                        // Console.WriteLine(e);
                        dataBlockSize = int.MaxValue;
                    }

                if (IsFree && dataBlockSize > DataLength * 100)
                    //safety net to avoid crazy large reads that just fail
                    //find out the next highest multiple of 8 based on DataLength for a best guess, with 32 extra bytes to spare
                    dataBlockSize = (int) (Math.Ceiling((double) DataLength / 8) * 8) + 32;

                if (IsFree && dataBlockSize == DataLength) dataBlockSize += 4;

                //The most common case is simply where the data we want lives at OffsetToData, so we just go get it
                //we know the offset to where the data lives, so grab bytes in order to get the size of the data *block* vs the size of the data in it
                if (IsFree)
                    try
                    {
                        datablockRaw = _registryHive.ReadBytesFromHive(4096 + OffsetToData, dataBlockSize);
                    }
                    catch (Exception)
                    {
                        //crazy things can happen in IsFree records
                        datablockRaw = Array.Empty<byte>();
                    }
                else
                    datablockRaw = _registryHive.ReadBytesFromHive(4096 + OffsetToData, dataBlockSize);

                //data block Raw now has our value AND slack space!
                //value is dataLengthInternal long. rest is slack

                //Some values are huge, so look for them and, if found, get the data into dataBlockRaw (but only for certain versions of hives)
                if (_dataLengthInternal > 16344 && _minorVersion > 3)
                {
                    // this is the BIG DATA case. here, we have to get the data pointed to by OffsetToData and process it to get to our (possibly fragmented) DataType data

                    datablockRaw = _registryHive.ReadBytesFromHive(4096 + OffsetToData, dataBlockSize);

                    var db = new DbListRecord(datablockRaw, 4096 + OffsetToData);

                    if (db.Size == 0)
                        //nothing to do
                        return Array.Empty<byte>();

                    // db now contains a pointer to where we can get db.NumberOfEntries offsets to our data and reassemble it

                    datablockSizeRaw = _registryHive.ReadBytesFromHive(4096 + db.OffsetToOffsets, 4);

                    try
                    {
                        dataBlockSize = BitConverter.ToInt32(datablockSizeRaw, 0);
                    }
                    catch (Exception e)
                    {
                        if (IsFree) return Array.Empty<byte>();

                        Log.Error(e, "Error getting data block: {Message}", e.Message);
                    }

                    datablockRaw = _registryHive.ReadBytesFromHive(4096 + db.OffsetToOffsets,
                        Math.Abs(dataBlockSize));

                    //data block Raw now contains our list of pointers to fragmented Data

                    //make a place to reassemble things
                    var bigDataRaw = new ArrayList((int) _dataLengthInternal);

                    for (var i = 1; i <= db.NumberOfEntries; i++)
                    {
                        // read the offset and go get that data. use i * 4 so we get 4, 8, 12, 16, etc
                        var os = BitConverter.ToUInt32(datablockRaw, i * 4);

                        // in order to accurately mark data cells as Referenced later, add these offsets to a list
                        DataOffsets.Add(os);

                        var tempDataBlockSizeRaw = _registryHive.ReadBytesFromHive(4096 + os, 4);

                        try
                        {
                            var tempdataBlockSize = BitConverter.ToInt32(tempDataBlockSizeRaw, 0);

                            //get our data block
                            var tempDataRaw =
                                _registryHive.ReadBytesFromHive(4096 + os, Math.Abs(tempdataBlockSize));

                            // since the data is prefixed with its length (4 bytes), skip that so we do not include it in the final data 
                            //we read 16344 bytes as the rest is padding and jacks things up if you use the whole range of bytes
                            bigDataRaw.AddRange(tempDataRaw.Skip(4).Take(16344).ToArray());
                        }
                        catch (Exception e)
                        {
                            if (IsFree) return Array.Empty<byte>();

                            Log.Error(e, "Error getting data block: {Message}", e.Message);
                        }
                    }

                    datablockRaw = (byte[]) bigDataRaw.ToArray(typeof(byte));

                    //reset this so slack calculation works
                    //dataBlockSize = data block Raw.Length;

                    //since dataBlockRaw doesn't have the size on it in this case, adjust internalDataOffset accordingly
                    _internalDataOffset = 0;
                }

                //Now that we are here the data we need to convert to our Values resides in data block Raw and is ready for more processing according to DataType
            }

            return datablockRaw;
        }
    }

    public byte[] Padding
    {
        get
        {
            var paddingOffset = 0x18 + NameLength;

            var paddingBlock = (int) Math.Ceiling((double) paddingOffset / 8);

            var actualPaddingOffset = paddingBlock * 8;

            var paddingLength = actualPaddingOffset - paddingOffset;

            if (paddingLength > 0 && paddingOffset + paddingLength <= RawBytes.Length)
                return new ArraySegment<byte>(RawBytes, paddingOffset, paddingLength).ToArray();

            return Array.Empty<byte>();
        }
    }

    /// <summary>
    ///     A list of offsets to data records.
    ///     <remarks>This is used to mark each Data record's IsReferenced property to true</remarks>
    /// </summary>
    public HashSet<ulong> DataOffsets { get; }

    // public properties...
    public uint DataLength => BitConverter.ToUInt32(RawBytes, 0x08);

    public DataTypeEnum DataType { get; set; }

    //we need to preserve the datatype as it exists (so we can see unsupported types easily)
    public uint DataTypeRaw => BitConverter.ToUInt32(RawBytes, 0x10) & DevpropMaskType;

    public ushort NameLength => BitConverter.ToUInt16(RawBytes, 0x06);

    //TODO IsTombstone?
    /// <summary>
    ///     Used to determine if the name is stored in ASCII (> 0) or Unicode (== 0)
    /// </summary>
    public ushort NamePresentFlag => BitConverter.ToUInt16(RawBytes, 0x14);

    /// <summary>
    ///     The relative offset to the data for this record. If the high bit is set, the data is resident in the offset itself.
    ///     <remarks>
    ///         When resident, this value will be similar to '0x80000002' or '0x80000004'. The actual length can be
    ///         determined by subtracting 0x80000000
    ///     </remarks>
    /// </summary>
    public uint OffsetToData { get; internal set; }

    /// <summary>
    ///     The normalized Value of this value record. This is what is visible under the 'Data' column in RegEdit
    /// </summary>
    public object ValueData
    {
        get
        {
            object val;
            var localDbl = Array.Empty<byte>();

            //var _logger = LogManager.GetLogger("FFFFFFF");
            try
            {
                val = DataBlockRaw;
                localDbl = (byte[]) val; // DataBlockRaw;

                //  _logger.Debug($"\r\n ValueName: {ValueName}   LocalDbl is null?: {localDBL == null}");
                //  _logger.Debug($"Local dbt len is {localDBL.Length}, _internalDoffset: {_internalDataOffset}, _datalenInternal: {_dataLengthInternal}, LocalDblBytes: {BitConverter.ToString(localDBL)} ");
                //  _logger.Debug($"Record is free: {IsFree}");

                if (IsFree)
                    // since its free but the data length is less than what we have, take what we do have and live with it
                    if (localDbl.Length < _dataLengthInternal)
                    {
                        //         _logger.Debug("In top return");

                        val = localDbl;
                        return val;
                    }

                //this is a failsafe for when IsFree == true. a lot of time the data is there, but if not, stick what we do have in the value and call it a day
                //       _logger.Debug($"In try loop. Data type is {DataType}");

                switch (DataType)
                {
                    case DataTypeEnum.RegFileTime:
                        var ts = BitConverter.ToUInt64(localDbl, _internalDataOffset);
                        val = DateTimeOffset.FromFileTime((long) ts).ToUniversalTime();
                        break;

                    case DataTypeEnum.RegExpandSz:
                    case DataTypeEnum.RegMultiSz:
                    case DataTypeEnum.RegSz:
                        var tempVal = Encoding.Unicode.GetString(localDbl, _internalDataOffset,
                            (int) _dataLengthInternal);
                        var nullIndex = tempVal.IndexOf("\0\0", StringComparison.Ordinal);
                        if (nullIndex > -1)
                        {
                            var baseString = tempVal.Substring(0, nullIndex);
                            var chunks = baseString.Split('\0');
                            val = string.Join(" ", chunks);
                        }
                        else
                        {
                            val = tempVal;
                        }

                        val = val.ToString().Trim('\0');
                        break;

                    case DataTypeEnum.RegNone: // spec says RegNone means "No defined data type", and not "no data"
                    case DataTypeEnum.RegBinary:
                    case DataTypeEnum.RegResourceRequirementsList:
                    case DataTypeEnum.RegResourceList:
                    case DataTypeEnum.RegFullResourceDescription:
                        val =
                            new ArraySegment<byte>(localDbl, _internalDataOffset,
                                    (int) _dataLengthInternal)
                                .ToArray();
                        break;

                    case DataTypeEnum.RegDword:
                        val = _dataLengthInternal == 4 ? BitConverter.ToUInt32(localDbl, 0) : 0;
                        break;

                    case DataTypeEnum.RegDwordBigEndian:
                        if (localDbl.Length > 0)
                        {
                            var reversedBlock = localDbl;

                            Array.Reverse(reversedBlock);

                            val = BitConverter.ToUInt32(reversedBlock, 0);
                        }

                        break;

                    case DataTypeEnum.RegQword:
                        val = _dataLengthInternal == 8 ? BitConverter.ToUInt64(localDbl, _internalDataOffset) : 0;
                        break;

                    case DataTypeEnum.RegLink:
                        val =
                            Encoding.Unicode.GetString(localDbl, _internalDataOffset, (int) _dataLengthInternal)
                                .Replace("\0", " ")
                                .Trim();
                        break;
                        
                    case DataTypeEnum.RegUwpByte:
                        val = $"0x{localDbl[_internalDataOffset]:X2}";
                        break;
                    case DataTypeEnum.RegUwpInt16:
                        val = BitConverter.ToInt16(localDbl, _internalDataOffset);
                        break;
                    case DataTypeEnum.RegUwpUint16:
                        val = BitConverter.ToUInt16(localDbl, _internalDataOffset);
                        break;
                    case DataTypeEnum.RegUwpInt32:
                        val = BitConverter.ToInt32(localDbl, _internalDataOffset);
                        break;
                    case DataTypeEnum.RegUwpUint32:
                        val = BitConverter.ToUInt32(localDbl, _internalDataOffset);
                        break;
                    case DataTypeEnum.RegUwpInt64:
                        val = BitConverter.ToInt64(localDbl, _internalDataOffset);
                        break;
                    case DataTypeEnum.RegUwpUint64:
                        val = BitConverter.ToUInt64(localDbl, _internalDataOffset);
                        break;
                    case DataTypeEnum.RegUwpSingle:
                        val = BitConverter.ToSingle(localDbl, _internalDataOffset);
                        break;
                    case DataTypeEnum.RegUwpDouble:
                        val = BitConverter.ToDouble(localDbl, _internalDataOffset);
                        break;
                    case DataTypeEnum.RegUwpChar:
                        val = BitConverter.ToChar(localDbl, _internalDataOffset);
                        break;
                    case DataTypeEnum.RegUwpBoolean:
                        val = BitConverter.ToBoolean(localDbl, _internalDataOffset);
                        break;
                    case DataTypeEnum.RegUwpString:
                        var end = _internalDataOffset;
                        while (end + 1 < localDbl.Length)
                        {
                            if (localDbl[end] == 0x00 && localDbl[end + 1] == 0x00)
                                break;
                            end += 2;
                        }

                        val = Encoding.Unicode.GetString(localDbl, _internalDataOffset, end - _internalDataOffset);
                        break;
                    case DataTypeEnum.RegUwpCompositeValue://TODO: Decipher this. For now we just output the raw bytes
                        var numRecs = (int)(_dataLengthInternal - 8) / 1;
                        var valComposite = new byte[numRecs];
                        Array.Copy(localDbl, _internalDataOffset, valComposite, 0, numRecs);

                        val = string.Format("[{0}]", string.Join(", ", valComposite.Select(b => $"0x{b:X2}")));
                        break;
                    case DataTypeEnum.RegUwpDateTimeOffset:
                        val = BitConverter.ToInt64(localDbl, _internalDataOffset);
                        break;
                    case DataTypeEnum.RegUwpTimeSpan:
                        val = new TimeSpan(BitConverter.ToInt64(localDbl, _internalDataOffset));
                        break;
                    case DataTypeEnum.RegUwpGuid:
                        var guidBytes = new byte[16];
                        Array.Copy(localDbl, _internalDataOffset, guidBytes, 0, 16);
                        val = new Guid(guidBytes);
                        break;
                    case DataTypeEnum.RegUwpPoint:
                        val = $"X: {BitConverter.ToSingle(localDbl, _internalDataOffset)}, Y: {BitConverter.ToSingle(localDbl, _internalDataOffset + 4)}";
                        break;
                    case DataTypeEnum.RegUwpSize:
                        val = $"Width: {BitConverter.ToSingle(localDbl, _internalDataOffset)}, Height: {BitConverter.ToSingle(localDbl, _internalDataOffset + 4)}";
                        break;
                    case DataTypeEnum.RegUwpRect:
                        val = $"X: {BitConverter.ToSingle(localDbl, _internalDataOffset)}, Y: {BitConverter.ToSingle(localDbl, _internalDataOffset + 4)}, Width: {BitConverter.ToSingle(localDbl, _internalDataOffset + 8)}, Height: {BitConverter.ToSingle(localDbl, _internalDataOffset + 12)}";
                        break;
                    case DataTypeEnum.RegUwpArrayByte:
                        numRecs = (int)(_dataLengthInternal - 8) / 1;
                        var valBytes = new byte[numRecs];
                        Array.Copy(localDbl, _internalDataOffset, valBytes, 0, numRecs);
                        val = string.Format("[{0}]", string.Join(", ", valBytes.Select(b => $"0x{b:X2}")));
                        break;
                    case DataTypeEnum.RegUwpArrayInt16:
                        numRecs = (int)(_dataLengthInternal - 8) / 2;
                        var valInt16 = new short[numRecs];
                        for (var i = 0; i < numRecs; i++)
                        {
                            valInt16[i] = BitConverter.ToInt16(localDbl, _internalDataOffset + (i * 2));
                        }
                        val = string.Format("[{0}]", string.Join(",", valInt16));
                        break;
                    case DataTypeEnum.RegUwpArrayUint16:
                        numRecs = (int)(_dataLengthInternal - 8) / 2;
                        var valUint16 = new ushort[numRecs];
                        for (var i = 0; i < numRecs; i++)
                        {
                            valUint16[i] = BitConverter.ToUInt16(localDbl, _internalDataOffset + (i * 2));
                        }
                        val = string.Format("[{0}]", string.Join(",", valUint16));
                        break;
                    case DataTypeEnum.RegUwpArrayInt32:
                        numRecs = (int)(_dataLengthInternal - 8) / 4;
                        var valInt32 = new int[numRecs];
                        for (var i = 0; i < numRecs; i++)
                        {
                            valInt32[i] = BitConverter.ToInt32(localDbl, _internalDataOffset + (i * 4));
                        }
                        val = string.Format("[{0}]", string.Join(",", valInt32));
                        break;
                    case DataTypeEnum.RegUwpArrayUint32:
                        numRecs = (int)(_dataLengthInternal - 8) / 4;
                        var valUint32 = new uint[numRecs];
                        for (var i = 0; i < numRecs; i++)
                        {
                            valUint32[i] = BitConverter.ToUInt32(localDbl, _internalDataOffset + (i * 4));
                        }
                        val = string.Format("[{0}]", string.Join(",", valUint32));
                        break;
                    case DataTypeEnum.RegUwpArrayInt64:
                        numRecs = (int)(_dataLengthInternal - 8) / 8;
                        var valInt64 = new long[numRecs];
                        for (var i = 0; i < numRecs; i++)
                        {
                            valInt64[i] = BitConverter.ToInt64(localDbl, _internalDataOffset + (i * 8));
                        }
                        val = string.Format("[{0}]", string.Join(",", valInt64));
                        break;
                    case DataTypeEnum.RegUwpArrayUint64:
                        numRecs = (int)(_dataLengthInternal - 8) / 8;
                        var valUint64 = new ulong[numRecs];
                        for (var i = 0; i < numRecs; i++)
                        {
                            valUint64[i] = BitConverter.ToUInt64(localDbl, _internalDataOffset + (i * 8));
                        }
                        val = string.Format("[{0}]", string.Join(",", valUint64));
                        break;
                    case DataTypeEnum.RegUwpArraySingle:
                        numRecs = (int)(_dataLengthInternal - 8) / 4;
                        var valSingle = new float[numRecs];
                        for (var i = 0; i < numRecs; i++)
                        {
                            valSingle[i] = BitConverter.ToSingle(localDbl, _internalDataOffset + (i * 4));
                        }
                        val = string.Format("[{0}]", string.Join(",", valSingle));
                        break;
                    case DataTypeEnum.RegUwpArrayDouble:
                        numRecs = (int)(_dataLengthInternal - 8) / 8;
                        var valDouble = new double[numRecs];
                        for (var i = 0; i < numRecs; i++)
                        {
                            valDouble[i] = BitConverter.ToDouble(localDbl, _internalDataOffset + (i * 8));
                        }
                        val = string.Format("[{0}]", string.Join(",", valDouble));
                        break;
                    case DataTypeEnum.RegUwpArrayChar:
                        numRecs = (int)(_dataLengthInternal - 8) / 2;
                        var valChar = new char[numRecs];
                        for (var i = 0; i < numRecs; i++)
                        {
                            valChar[i] = BitConverter.ToChar(localDbl, _internalDataOffset + (i * 2));
                        }
                        val = string.Format("[{0}]", string.Join(",", valChar));
                        break;
                    case DataTypeEnum.RegUwpArrayBoolean:
                        numRecs = (int)(_dataLengthInternal - 8);
                        var valBool = new bool[numRecs];
                        for (var i = 0; i < numRecs; i++)
                        {
                            valBool[i] = BitConverter.ToBoolean(localDbl, _internalDataOffset + i);
                        }
                        val = string.Format("[{0}]", string.Join(",", valBool));
                        break;
                    case DataTypeEnum.RegUwpArrayString:
                        var check = _internalDataOffset;
                        var elStrings = new List<string>();
                        var dataLen = _dataLengthInternal - 8;

                        while (check < dataLen)
                        {
                            var lengthString = BitConverter.ToInt32(localDbl, check);
                            if (check + 4 + lengthString > localDbl.Length)
                            {
                                // if data is truncated, get what we can
                                lengthString = localDbl.Length - (check + 4);
                                if (lengthString < 0)
                                {
                                    break;
                                }
                            }
                            var elementString = Encoding.Unicode.GetString(localDbl, check + 4, lengthString - 2);
                            elStrings.Add(elementString);
                            check = check + lengthString + 4;
                        }
                        val = string.Format("[{0}]", string.Join(",", elStrings));
                        break;
                    case DataTypeEnum.RegUwpArrayDateTimeOffset:
                        numRecs = (int)(_dataLengthInternal - 8) / 8;
                        var valDateTimeOffset = new long[numRecs];
                        for (var i = 0; i < numRecs; i++)
                        {
                            valDateTimeOffset[i] = BitConverter.ToInt64(localDbl, _internalDataOffset + (i * 8));
                        }
                        val = string.Format("[{0}]", string.Join(",", valDateTimeOffset));
                        break;
                    case DataTypeEnum.RegUwpArrayTimeSpan:
                        numRecs = (int)(_dataLengthInternal - 8) / 8;
                        var valTimeSpan = new TimeSpan[numRecs];
                        for (var i = 0; i < numRecs; i++)
                        {
                            valTimeSpan[i] = new TimeSpan(BitConverter.ToInt64(localDbl, _internalDataOffset + (i * 8)));
                        }
                        val = string.Format("[{0}]", string.Join(",", valTimeSpan));
                        break;
                    case DataTypeEnum.RegUwpArrayGuid:
                        numRecs = (int)(_dataLengthInternal - 8) / 16;
                        var valGUID = new Guid[numRecs];
                        for (var i = 0; i < numRecs; i++)
                        {
                            var gBytes = new byte[16];
                            Array.Copy(localDbl, _internalDataOffset + (i * 16), gBytes, 0, 16);
                            valGUID[i] = new Guid(gBytes);
                        }
                        val = string.Format("[{0}]", string.Join(",", valGUID));
                        break;
                    case DataTypeEnum.RegUwpArrayPoint:
                        numRecs = (int)(_dataLengthInternal - 8) / 8;
                        var valPoint = new List<string>();
                        for (var i = 0; i < numRecs; i++)
                        {
                            valPoint.Add($"{{X: {BitConverter.ToSingle(localDbl, _internalDataOffset + (i*8))}, Y: {BitConverter.ToSingle(localDbl, _internalDataOffset + (i * 8) + 4)}}}");
                        }
                        val = string.Format("[{0}]", string.Join(",", valPoint));
                        break;
                    case DataTypeEnum.RegUwpArraySize:
                        numRecs = (int)(_dataLengthInternal - 8) / 8;
                        var valSize = new List<string>();
                        for (var i = 0; i < numRecs; i++)
                        {
                            valSize.Add($"{{Width: {BitConverter.ToSingle(localDbl, _internalDataOffset + (i*8))}, Height: {BitConverter.ToSingle(localDbl, _internalDataOffset + (i * 8) + 4)}}}");
                        }
                        val = string.Format("[{0}]", string.Join(",", valSize));
                        break;
                    case DataTypeEnum.RegUwpArrayRect:
                        numRecs = (int)(_dataLengthInternal - 8) / 16;
                        var valRect = new List<string>();
                        for (var i = 0; i < numRecs; i++)
                        {
                            valRect.Add($"{{X: {BitConverter.ToSingle(localDbl, _internalDataOffset + (i*8))}, Y: {BitConverter.ToSingle(localDbl, _internalDataOffset + (i * 8) + 4)}, Width: {BitConverter.ToSingle(localDbl, _internalDataOffset + (i * 8) + 8)}, Height: {BitConverter.ToSingle(localDbl, _internalDataOffset + (i * 8) + 12)}}}");
                        }
                        val = string.Format("[{0}]", string.Join(",", valRect));
                        break;

                    case DataTypeEnum.RegUnknown:
                        val = localDbl;
                        break;

                    default:
                        DataType = DataTypeEnum.RegUnknown;
                        val = localDbl;
                        break;
                }
            }

            catch (Exception ex)
            {
                //  _logger.Debug($"error happened {ex.Message} {ex.StackTrace} Will throw? :{IsFree == false}");
                //if its a free record, errors are expected, but if not, throw so the issue can be addressed
                if (IsFree)
                {
                    val = localDbl;
                }
                else
                {
                    Log.Error(ex,
                        "Error getting ValueData for {ValueName}. Error message: {Message}", ValueName, ex.Message);
                    val = localDbl;
                    //throw;
                }
            }

            return val;
        }
    }

    /// <summary>
    ///     The raw contents of this value record's Value
    /// </summary>
    public byte[] ValueDataRaw
    {
        get
        {
            var ret = Array.Empty<byte>();

            if (_dataLengthInternal + _internalDataOffset > DataBlockRaw.Length)
            {
                //we don't have enough data to copy, so take what we can get

                if (DataBlockRaw.Length > 0)
                    try
                    {
                        return new ArraySegment<byte>(DataBlockRaw, _internalDataOffset,
                            DataBlockRaw.Length - _internalDataOffset).ToArray();
                    }
                    catch (Exception)
                    {
                        //In case it goes real sideways
                        return Array.Empty<byte>();
                    }
            }
            else
            {
                return new ArraySegment<byte>(DataBlockRaw, _internalDataOffset,
                    (int) _dataLengthInternal).ToArray();
            }

            if (IsFree) return Array.Empty<byte>();

            return ret;
        }
    }

    //The raw contents of this value record's slack space
    public byte[] ValueDataSlack
    {
        get
        {
            var dbRaw = DataBlockRaw;

            if (dbRaw.Length > _dataLengthInternal + _internalDataOffset)
            {
                var slackStart = (int) (_dataLengthInternal + _internalDataOffset);
                var slackLen = dbRaw.Length - slackStart;

                return
                    new ArraySegment<byte>(DataBlockRaw, slackStart, slackLen).ToArray();
            }

            return Array.Empty<byte>();
        }
    }

    /// <summary>
    ///     The name of the value. This is what is visible under the 'Name' column in RegEdit.
    /// </summary>
    public string ValueName
    {
        get
        {
            var valName = "(Unable to determine name)";

            if (NameLength == 0)
            {
                valName = "(default)";
            }
            else
            {
                if (NamePresentFlag > 0)
                {
                    if (IsFree)
                    {
                        //make sure we have enough data
                        if (RawBytes.Length >= NameLength + 0x18)
                            valName = CodePagesEncodingProvider.Instance.GetEncoding(1252).GetString(RawBytes, 0x18, NameLength);
                    }
                    else
                    {
                        valName = CodePagesEncodingProvider.Instance.GetEncoding(1252).GetString(RawBytes, 0x18, NameLength);
                    }
                }
                else
                {
                    // in very rare cases, the ValueName is in ascii even when it should be in Unicode.

                    var valString = BitConverter.ToString(RawBytes, 0x18, NameLength);

                    var foundMatch = Regex.IsMatch(valString, "[0-9A-Fa-f]{2}-[0]{2}-?", RegexOptions.Compiled);

                    if (foundMatch)
                        // we found what appears to be unicode
                        valName = Encoding.Unicode.GetString(RawBytes, 0x18, NameLength);
                }
            }

            return valName;
        }
    }

    /// <summary>
    ///     When true, the VK is free but the non-resident value data (in a data record) is in use elsewhere.
    ///     <remarks>
    ///         Useful to display a warning to end user so they know the data may not be correct since the record has been
    ///         reused by something else.
    ///     </remarks>
    /// </summary>
    public bool DataRecordAllocated { get; }

    // public properties...
    public long AbsoluteOffset => RelativeOffset + 4096;

    public bool IsFree => BitConverter.ToInt32(RawBytes, 0) > 0;

    public bool IsReferenced { get; internal set; }

    public byte[] RawBytes
    {
        get
        {
            var raw = _registryHive.ReadBytesFromHive(AbsoluteOffset, _rawBytesLength);

            return raw;
        }
    }

    public long RelativeOffset { get; }

    public string Signature => Encoding.ASCII.GetString(RawBytes, 4, 2);

    public int Size => BitConverter.ToInt32(RawBytes, 0);

    // public methods...
    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Size: 0x{Math.Abs(Size):X}");
        sb.AppendLine($"Relative Offset: 0x{RelativeOffset:X}");
        sb.AppendLine($"Absolute Offset: 0x{AbsoluteOffset:X}");
        sb.AppendLine($"Signature: {Signature}");

        sb.AppendLine($"Data Type raw: 0x{DataTypeRaw:X}");
        sb.AppendLine();
        sb.AppendLine($"Is Free: {IsFree}");

        sb.AppendLine();

        sb.AppendLine($"Data Length: 0x{DataLength:X}");
        sb.AppendLine($"Offset To Data: 0x{OffsetToData:X}");

        sb.AppendLine();

        sb.AppendLine($"Name Length: 0x{NameLength:X}");
        sb.AppendLine($"Name Present Flag: 0x{NamePresentFlag:X}");

        sb.AppendLine();

        if (Padding.Length > 0) sb.AppendLine($"Padding: {BitConverter.ToString(Padding)}");

        sb.AppendLine();

        sb.AppendLine($"Value Name: {ValueName}");
        sb.AppendLine($"Value Type: {DataType}");

        switch (DataType)
        {
            case DataTypeEnum.RegSz:
            case DataTypeEnum.RegExpandSz:
            case DataTypeEnum.RegMultiSz:
            case DataTypeEnum.RegLink:
                sb.AppendLine($"Value Data: {ValueData}");
                break;

            case DataTypeEnum.RegNone:
            case DataTypeEnum.RegBinary:
            case DataTypeEnum.RegResourceList:
            case DataTypeEnum.RegResourceRequirementsList:
            case DataTypeEnum.RegFullResourceDescription:
                sb.AppendLine($"Value Data: {BitConverter.ToString((byte[]) ValueData)}");
                break;

            case DataTypeEnum.RegFileTime:
                if (ValueData != null)
                {
                    var dto = (DateTimeOffset) ValueData;

                    sb.AppendLine($"Value Data: {dto}");
                }

                break;

            case DataTypeEnum.RegDwordBigEndian:
            case DataTypeEnum.RegDword:
            case DataTypeEnum.RegQword:
                sb.AppendLine($"Value Data: {ValueData:N}");
                break;
            default:
                sb.AppendLine($"Value Data: {BitConverter.ToString((byte[]) ValueData)}");
                break;
        }

        if (ValueDataSlack != null) sb.AppendLine($"Value Data Slack: {BitConverter.ToString(ValueDataSlack, 0)}");


        return sb.ToString();
    }
}