using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NFluent;
using Registry.Lists;
using Registry.Other;

// namespaces...
namespace Registry.Cells
{
    // public classes...
    // internal classes...
    /// <summary>
    /// <remarks>Represents a Key Value Record</remarks>
    /// </summary>
    public class VKCellRecord : ICellTemplate
    {
        // private fields...
        private readonly int _size;

        private const uint DEVPROP_MASK_TYPE = 0x00000FFF;

        // public constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="VKCellRecord"/> class.
        /// </summary>
        public VKCellRecord(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;
   


            RawBytes = rawBytes;

            _size = BitConverter.ToInt32(rawBytes, 0);

            IsFree = _size > 0;

            Signature = Encoding.ASCII.GetString(rawBytes, 4, 2);

            Check.That(Signature).IsEqualTo("vk");

            DataOffets = new List<ulong>();

          

                //TODO whats the minimum where this works? look for exceptions when free and support this better
                //if (IsFree)
                //{
                //    return;
                //}

                NameLength = BitConverter.ToUInt16(rawBytes, 0x06);
            DataLength = BitConverter.ToUInt32(rawBytes, 0x08);

            var dataLengthInternal = DataLength;

            //if the high bit is set, data lives in the field used to typically hold the OffsetToData Value
            var dataIsResident = Convert.ToString(dataLengthInternal, 2).PadLeft(32, '0').StartsWith("1");

            //this is used later to pull the data from the raw bytes. By setting this here we do not need a bunch of if/then stuff later
            var internalDataOffset = 4;

            if (dataIsResident)
            {
                //normalize the data for future use
                dataLengthInternal = dataLengthInternal - 0x80000000;

                //A data size of 4 uses all 4 bytes of the data offset
                //A data size of 2 uses the last 2 bytes of the data offset (on a little-endian system)
                //A data size of 1 uses the last byte (on a little-endian system)
                //A data size of 0 represents that the value is not set (or NULL)

                //if (dataLengthInternal == 1)
                //    Debug.Write("dataLengthInternal == 1");

                internalDataOffset = 0;
            }


            OffsetToData = BitConverter.ToUInt32(rawBytes, 0x0c);

            //we need to preserve the datatype as it exists (so we can see unsupported types easily)
            DataTypeRaw = BitConverter.ToUInt32(rawBytes, 0x10) & DEVPROP_MASK_TYPE;

            //force to a known datatype 
            var dataTypeInternal = DataTypeRaw;

            if (dataTypeInternal > (ulong)DataTypeEnum.RegFileTime)
            {
                dataTypeInternal = 999;
            }

            DataType = (DataTypeEnum)dataTypeInternal;

            NamePresentFlag = BitConverter.ToUInt16(rawBytes, 0x14);
            Unknown = BitConverter.ToUInt16(rawBytes, 0x16);

            if (NameLength == 0)
            {
                ValueName = "(default)";
            }
            else
            {
                if (NamePresentFlag > 0)
                {
                    if (IsFree)
                    {
                        //make sure we have enough data
                        if (rawBytes.Length >= NameLength + 0x18)
                        {
                            ValueName = Encoding.ASCII.GetString(rawBytes, 0x18, NameLength);
                        }
                        else
                        {
                            ValueName = "(Unable to determine name)";
                        }
                            
                    }
                    else
                    {
                        ValueName = Encoding.ASCII.GetString(rawBytes, 0x18, NameLength);
                    }
                     
                }
                else
                {
                    // in very rare cases, the ValueName is in ascii even when it should be in Unicode.
                    var valString = BitConverter.ToString(rawBytes, 0x18, NameLength);

                    bool foundMatch = false;
                    try
                    {
                        foundMatch = Regex.IsMatch(valString, "[0-9A-Fa-f]{2}-[0]{2}-?"); // look for hex chars followed by 00 separators
                    }
                    catch (ArgumentException)
                    {
                        // Syntax error in the regular expression
                    }

                    if (foundMatch)
                    {
                        // we found what appears to be unicode
                        ValueName = Encoding.Unicode.GetString(rawBytes, 0x18, NameLength);
                    }
                    else
                    {
                        ValueName = Encoding.ASCII.GetString(rawBytes, 0x18, NameLength);
                    }
                }
            }



            byte[] datablockRaw;
            var dataBlockSize = 0;

            if (dataIsResident)
            {
                //test validation for making sure we are taking the "right" side of the 4 bytes (which is really the left side)
                //if (dataLengthInternal < 4 && dataLengthInternal != 0 && rawBytes[0xc] != 0x00)
                //{
                //    var testing = new byte[4];
                //    Array.Copy(rawBytes, 0xc, testing, 0, 4);
                //    Debug.WriteLine("Testing resident data. Type: {2}, data type raw: 0x{3:X}, Data len: {0}, bytes: {1}", dataLengthInternal, BitConverter.ToString(testing, 0),DataType, DataTypeRaw);
                //}

                if (DataType == DataTypeEnum.RegDwordBigEndian)
                {
                    //this is a special case where the data length shows up as 2, but a dword needs 4 bytes, so adjust
                    dataLengthInternal = 4;
                }
                //Since its resident, the data lives in the OffsetToData.
                datablockRaw = new byte[dataLengthInternal];

                //make a copy for processing below
                //Array.Copy(rawBytes, 0xc, datablockRaw, 0, 4); org
                Array.Copy(rawBytes, 0xc, datablockRaw, 0, dataLengthInternal);

                //set our data length to what is available since its resident and unknown. it can be used for anything
                if (DataType == DataTypeEnum.RegUnknown)
                {
                    dataLengthInternal = 4;
                }
            }
            else
            {
                //We have to go look at the OffsetToData to see what we have so we can do the right thing

                //The first operations are always the same. Go get the length of the data cell, then see how big it is.

                var datablockSizeRaw = new byte[0];

                if (IsFree)
                {
                    try
                    {
                        datablockSizeRaw = RegistryHive.ReadBytesFromHive(4096 + OffsetToData, 4);
                    }
                    catch (Exception)
                    {
                        //crazy things can happen in IsFree records
                    }
                }
                else
                {
                    datablockSizeRaw = RegistryHive.ReadBytesFromHive(4096 + OffsetToData, 4);
                }


                    

                //add this offset so we can mark the data cells as referenced later
                DataOffets.Add(OffsetToData);

                // in some rare cases the bytes returned from above are all zeros, so make sure we get something but all zeros
                if (datablockSizeRaw.Length == 4)
                {
                    dataBlockSize = BitConverter.ToInt32(datablockSizeRaw, 0);
                }


                if (IsFree && Math.Abs(dataBlockSize) > DataLength*10)
                {
                    //safety net to avoid crazy large reads that just fail
                    //find out the next highest multiple of 8 based on DataLength for a best guess
                    dataBlockSize = (int)(Math.Ceiling(((double)DataLength / 8)) * 8);
                   // dataBlockSize = (int) (DataLength * 2);
                }

                    //The most common case is simply where the data we want lives at OffsetToData, so we just go get it

                    //sanity check the length. if its crazy big, make it managable
                    if (dataBlockSize < -2147483640)
                    {
                        dataBlockSize = dataBlockSize - -2147483648;
                    }


                //  Debug.WriteLine("Datablock size for vk at rel offset 0x{0:x} (IsResident: {1}): 0x{2:X}", relativeOffset, dataIsResident, dataBlockSize);

                //we know the offset to where the data lives, so grab bytes in order to get the size of the data *block* vs the size of the data in it
                if (IsFree)
                {
                    try
                    {
                        datablockRaw = RegistryHive.ReadBytesFromHive(4096 + OffsetToData, Math.Abs(dataBlockSize));
                    }
                    catch (Exception)
                    {
                        //crazy things can happen in IsFree records
                        datablockRaw = new byte[0];
                       
                        
                    }
                }
                else
                {
                    datablockRaw = RegistryHive.ReadBytesFromHive(4096 + OffsetToData, Math.Abs(dataBlockSize));
                }
                    

                //datablockRaw now has our value AND slack space!
                //value is dataLengthInternal long. rest is slack

                //Some values are huge, so look for them and, if found, get the data into dataBlockRaw (but only for certain versions of hives)
                if (dataLengthInternal > 16344 && (RegistryHive.Header.MajorVersion == 1 && RegistryHive.Header.MinorVersion > 3))
                {
                    // this is the BIG DATA case. here, we have to get the data pointed to by OffsetToData and process it to get to our (possibly fragmented) DataType data

                    datablockRaw = RegistryHive.ReadBytesFromHive(4096 + OffsetToData, Math.Abs(dataBlockSize));

                    var db = new DBListRecord(datablockRaw, 4096 + OffsetToData);

                    // db now contains a pointer to where we can get db.NumberOfEntries offsets to our data and reassemble it

                    datablockSizeRaw = RegistryHive.ReadBytesFromHive(4096 + db.OffsetToOffsets, 4);
                    dataBlockSize = BitConverter.ToInt32(datablockSizeRaw, 0);

                    datablockRaw = RegistryHive.ReadBytesFromHive(4096 + db.OffsetToOffsets, Math.Abs(dataBlockSize));

                    //datablockRaw now contains our list of pointers to fragmented Data

                    //make a place to reassemble things
                    var bigDataRaw = new ArrayList((int)dataLengthInternal);

                    for (var i = 1; i <= db.NumberOfEntries; i++)
                    {
                        // read the offset and go get that data. use i * 4 so we get 4, 8, 12, 16, etc
                        var os = BitConverter.ToUInt32(datablockRaw, i * 4);

                        // in order to accurately mark data cells as Referenced later, add these offsets to a list
                        DataOffets.Add(os);

                        var tempDataBlockSizeRaw = RegistryHive.ReadBytesFromHive(4096 + os, 4);
                        var tempdataBlockSize = BitConverter.ToInt32(tempDataBlockSizeRaw, 0);

                        //get our data block
                        var tempDataRaw = RegistryHive.ReadBytesFromHive(4096 + os, Math.Abs(tempdataBlockSize));

                        // since the data is prefixed with its length (4 bytes), skip that so we do not include it in the final data 
                        //we read 16344 bytes as the rest is padding and jacks things up if you use the whole range of bytes
                        bigDataRaw.AddRange(tempDataRaw.Skip(4).Take(16344).ToArray());
                    }

                    datablockRaw = (byte[])bigDataRaw.ToArray(typeof(byte)) ;

                    //reset this so slack calculation works
                    dataBlockSize = datablockRaw.Length;

                    //since dataBlockRaw doesnt have the size on it in this case, adjust internalDataOffset accordingly
                    internalDataOffset = 0;
                }

                //Now that we are here the data we need to convert to our Values resides in datablockRaw and is ready for more processing according to DataType
            }


            //Testing trap
            //if (DataTypeRaw == 1 && AbsoluteOffset == 0x00000000000fd410)
            //{
            //    Debug.Write("VK testing trap hit");
            //}


            //ValueDataRaw = datablockRaw.Skip(internalDataOffset).Take((int)Math.Abs(dataLengthInternal)).ToArray();
            ValueDataRaw = new byte[dataLengthInternal];

            if (dataLengthInternal + internalDataOffset > datablockRaw.Length)
            {
                //we dont have enough data to copy, so take what we can get
                try
                    {
                        Array.Copy(datablockRaw, internalDataOffset, ValueDataRaw, 0, datablockRaw.Length - internalDataOffset);
                    }
                    catch (Exception)
                    {
              
                        Array.Copy(datablockRaw, 0, ValueDataRaw, 0, datablockRaw.Length);
                   
                    }                
            }
            else
            {
                Array.Copy(datablockRaw, internalDataOffset, ValueDataRaw, 0, dataLengthInternal);
            }

                
            
            //datablockRaw.Skip(internalDataOffset).Take((int)Math.Abs(dataLengthInternal)).ToArray();

            //we can determine max slack size since all data cells are a multiple of 8 bytes long
            //we know how long our data should be from the vk record (dataLengthInternal).
            //we add 4 to account for the length of where the value lives, then divide by 8 and round up.
            //this number * 8 is the maximum size the data record should be to hold the value.
            //take away what we used (dataLengthInternal) and then account for our offset to said data (internalDataOffset)
            var maxSlackSize = Math.Ceiling((double)(dataLengthInternal + 4) / 8) * 8 - dataLengthInternal - internalDataOffset;

            //ValueDataSlack =
            //    datablockRaw.Skip((int)(dataLengthInternal + internalDataOffset))
            //    .Take((int)(Math.Abs(maxSlackSize)))
            //    .ToArray();

            ValueDataSlack = new byte[0];

            if (datablockRaw.Length > dataLengthInternal + internalDataOffset)
            {
                ValueDataSlack = new Byte[Math.Abs((int)maxSlackSize)];

                Array.Copy(datablockRaw, (int)(dataLengthInternal + internalDataOffset), ValueDataSlack, 0, (int)(Math.Abs(maxSlackSize)));
            }
          
         

                //datablockRaw.Skip((int)(dataLengthInternal + internalDataOffset))
                //.Take((int)(Math.Abs(maxSlackSize)))
                //.ToArray();

                if (IsFree)
                {
                    // since its free but the data length is less than what we have, take what we do have and live with it
                    if (datablockRaw.Length < dataLengthInternal)
                    {
                        ValueData = datablockRaw;
                        return;
                    }
                }

            //this is a failsafe for when IsFree == true. a lot of time the data is there, but if not, stick what we do have in the value and call it a day
            try
            {
                switch (DataType)
                {
                    case DataTypeEnum.RegFileTime:
                        var ts = BitConverter.ToUInt64(datablockRaw, internalDataOffset);

                        ValueData = DateTimeOffset.FromFileTime((long)ts).ToUniversalTime();

                        break;

                    case DataTypeEnum.RegExpandSz:
                    case DataTypeEnum.RegMultiSz:
                    case DataTypeEnum.RegSz:
                        if ((int)dataLengthInternal > datablockRaw.Length || ValueDataRaw == null)
                        {
                            ValueData = "(!!!! UNABLE TO DETERMINE STRING VALUE !!!!)";
                        }
                        else
                        {
                            ValueData = Encoding.Unicode.GetString(datablockRaw, internalDataOffset,
                                (int)dataLengthInternal)
                                .Replace("\0", "");
                        }

                        //
                        //ValueData =
                        //Encoding.Unicode.GetString(datablockRaw, internalDataOffset, (int)dataLengthInternal).Replace("\0", " ").Trim();

                        break;

                    case DataTypeEnum.RegNone: // spec says RegNone means "No defined data type", and not "no data"
                    case DataTypeEnum.RegBinary:
                    case DataTypeEnum.RegResourceRequirementsList:
                    case DataTypeEnum.RegResourceList:
                    case DataTypeEnum.RegFullResourceDescription:

                        ValueData = new byte[Math.Abs(dataLengthInternal)];

                        Array.Copy(datablockRaw, internalDataOffset, (byte[])ValueData, 0, Math.Abs(dataLengthInternal));


                        //ValueData = datablockRaw.Skip(internalDataOffset).Take(Math.Abs(dataBlockSize)).ToArray();

                        break;

                    case DataTypeEnum.RegDword:
                        ValueData = dataLengthInternal == 4 ? BitConverter.ToUInt32(datablockRaw, 0) : 0;

                        break;

                    case DataTypeEnum.RegDwordBigEndian:
                        if (datablockRaw.Length > 0)
                        {
                            var reversedBlock = datablockRaw;

                            Array.Reverse(reversedBlock);

                            ValueData = BitConverter.ToUInt32(reversedBlock, 0);
                        }

                        break;

                    case DataTypeEnum.RegQword:
                        ValueData = BitConverter.ToUInt64(datablockRaw, internalDataOffset);

                        break;



                    case DataTypeEnum.RegUnknown:
                        ValueData = datablockRaw;

                        ValueDataSlack = new byte[0];

                        break;

                    case DataTypeEnum.RegLink:
                        ValueData =
                        Encoding.Unicode.GetString(datablockRaw, internalDataOffset, (int)dataLengthInternal).Replace("\0", " ").Trim();
                        break;

                    default:


                        DataType = DataTypeEnum.RegUnknown;
                        ValueData = datablockRaw;

                        ValueDataSlack = new byte[0];

                        break;
                }

                var paddingOffset = 0x18 + NameLength;

                var paddingBlock = (int)Math.Ceiling((double)paddingOffset / 8);

                var actualPaddingOffset = paddingBlock * 8;

                var paddingLength = actualPaddingOffset - paddingOffset;

                Padding = string.Empty;

                if (paddingLength > 0)
                {
                    Padding = BitConverter.ToString(rawBytes, paddingOffset, paddingLength);
                }

                //    RecordSlack = rawBytes.Skip(actualPaddingOffset).ToArray();

                if (!IsFree)
                {
                    //When records ARE free, different rules apply, so we process thsoe all at once later
                    Check.That(actualPaddingOffset).IsEqualTo(rawBytes.Length);
                }
                //else
                //{
                //    if (RecordSlack.Length > 0)
                //    {
                //           // var found = Helpers.ExtractRecordsFromSlack(RecordSlack, actualPaddingOffset + relativeOffset);
                //            //if (found > 0)
                //            //{
                //            //    if (RegistryHive.Verbosity == RegistryHive.VerbosityEnum.Full)
                //            //    {
                //            //        Console.WriteLine("Recovered {0:N0} records from free space in vk cell record at relative offset 0x{1:x}!", found, relativeOffset);
                //            //    }
                //                
                //            //}
                //    }
                //}                   
            }




            catch (Exception ex)
            {
                //if its a free record, errors are expected, but if not, throw so the issue can be addressed
                if (IsFree)
                {
                    ValueData = datablockRaw;
                }
                else
                {
                    throw;
                }
            }
        }

   

        public string Padding { get; private set; }

        // public enums...
        public enum DataTypeEnum
        {
            [Description("Binary data (any arbitrary data)")]
            RegBinary = 0x0003,
            [Description("A DWORD value, a 32-bit unsigned integer (little-endian)")]
            RegDword = 0x0004,
            [Description("A DWORD value, a 32-bit unsigned integer (big endian)")]
            RegDwordBigEndian = 0x0005,
            [Description("An 'expandable' string value that can contain environment variables, normally stored and exposed in UTF-16LE")]
            RegExpandSz = 0x0002,
            [Description("FILETIME data")]
            RegFileTime = 0x0010,
            [Description("A resource descriptor (used by the Plug-n-Play hardware enumeration and configuration)")]
            RegFullResourceDescription = 0x0009,
            [Description("A symbolic link (UNICODE) to another Registry key, specifying a root key and the path to the target key")]
            RegLink = 0x0006,
            [Description("A multi-string value, which is an ordered list of non-empty strings, normally stored and exposed in UTF-16LE, each one terminated by a NUL character")]
            RegMultiSz = 0x0007,
            [Description("No type (the stored value, if any)")]
            RegNone = 0x0000,
            [Description("A QWORD value, a 64-bit integer (either big- or little-endian, or unspecified)")]
            RegQword = 0x000B,
            [Description("A resource list (used by the Plug-n-Play hardware enumeration and configuration)")]
            RegResourceList = 0x0008,
            [Description("A resource requirements list (used by the Plug-n-Play hardware enumeration and configuration)")]
            RegResourceRequirementsList = 0x000A,
            [Description("A string value, normally stored and exposed in UTF-16LE")]
            RegSz = 0x0001,
            [Description("Unknown data type")]
            RegUnknown = 999
        }


        /// <summary>
        /// A list of offsets to data records.
        /// <remarks>This is used to mark each Data record's IsReferenced property to true</remarks>
        /// </summary>
        public List<ulong> DataOffets { get; private set; }

        // public properties...
        public long AbsoluteOffset
        {
            get
            {
                return RelativeOffset + 4096;
            }
        }

        // public properties...
        public uint DataLength { get; set; }
        public DataTypeEnum DataType { get; set; }
        public uint DataTypeRaw { get; set; }

        
        public bool IsFree { get; private set; }

        
        public bool IsReferenced { get; internal set; }

        public ushort NameLength { get; set; }

        /// <summary>
        /// Used to determine if the name is stored in ASCII (> 0) or Unicode (== 0)
        /// </summary>
        public ushort NamePresentFlag { get; set; }

        /// <summary>
        /// The relative offset to the data for this record. If the high bit is set, the data is resident in the offset itself.
        /// <remarks>When resident, this value will be similar to '0x80000002' or '0x80000004'. The actual length can be determined by subtracting 0x80000000</remarks>
        /// </summary>
        public uint OffsetToData { get; set; }

        
        public byte[] RawBytes { get; private set; }

        public long RelativeOffset { get; private set; }

        public string Signature { get; private set; }
        
        public int Size
        {
            get
            {
                return _size;
            }
        }

        public ushort Unknown { get; set; }

        /// <summary>
        /// The normalized Value of this value record. This is what is visible under the 'Data' column in RegEdit
        /// </summary>
        public object ValueData { get; set; }
        /// <summary>
        /// The raw contents of this value record's Value 
        /// </summary>
        public byte[] ValueDataRaw { get; set; }
        //The raw contents of this value record's slack space
        public byte[] ValueDataSlack { get; set; }

        /// <summary>
        /// The name of the value. This is what is visible under the 'Name' column in RegEdit.
        /// </summary>
        public string ValueName { get; set; }

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Size: 0x{0:X}", Math.Abs(_size)));
            sb.AppendLine(string.Format("Relative Offset: 0x{0:X}", RelativeOffset));
            sb.AppendLine(string.Format("Absolute Offset: 0x{0:X}", AbsoluteOffset));
            sb.AppendLine(string.Format("Signature: {0}", Signature));
            sb.AppendLine(string.Format("Data Type: {0}", DataType));
            sb.AppendLine();
            sb.AppendLine(string.Format("Is Free: {0}", IsFree));

            sb.AppendLine();

            sb.AppendLine(string.Format("Data Length: 0x{0:X}", DataLength));
            sb.AppendLine(string.Format("Offset To Data: 0x{0:X}", OffsetToData));

            sb.AppendLine();

            sb.AppendLine(string.Format("Name Length: 0x{0:X}", NameLength));
            sb.AppendLine(string.Format("Name Present Flag: 0x{0:X}", NamePresentFlag));

            sb.AppendLine();

            sb.AppendLine(string.Format("Value Name: {0}", ValueName));
            
            switch (DataType)
            {
                case DataTypeEnum.RegSz:
                case DataTypeEnum.RegExpandSz:
                case DataTypeEnum.RegMultiSz:
                case DataTypeEnum.RegLink:
                    sb.AppendLine(string.Format("Value Data: {0}", ValueData));

                    break;

                case DataTypeEnum.RegNone:
                case DataTypeEnum.RegBinary:
                case DataTypeEnum.RegResourceList:
                case DataTypeEnum.RegResourceRequirementsList:
                case DataTypeEnum.RegFullResourceDescription:
                    if (ValueData == null)
                    {
                        sb.AppendLine(string.Format("Value Data: {0}", ""));
                    }
                    else
                    {
                        sb.AppendLine(string.Format("Value Data: {0}", BitConverter.ToString((byte[])ValueData)));
                    }

                    break;

                case DataTypeEnum.RegFileTime:

                    if (ValueData != null)
                    {
                        DateTimeOffset dto = (DateTimeOffset) ValueData;

                        sb.AppendLine(string.Format("Value Data: {0}", dto));
                    }

                        break;

                case DataTypeEnum.RegDwordBigEndian:
                case DataTypeEnum.RegDword:
                case DataTypeEnum.RegQword:
                    sb.AppendLine(string.Format("Value Data: {0:N}", ValueData));
                    break;
                default:
                    if (ValueData == null)
                    {
                        sb.AppendLine(string.Format("Value Data: {0}", ""));
                    }
                    else
                    {
                        sb.AppendLine(string.Format("Value Data: {0}", BitConverter.ToString((byte[])ValueData)));
                    }
                    break;
            }
            
            if (ValueDataSlack != null)
            {
                sb.AppendLine(string.Format("Value Data Slack: {0}", BitConverter.ToString(ValueDataSlack, 0)));
            }
            
            sb.AppendLine();

        
            sb.AppendLine(string.Format("Padding: {0}", Padding));

            sb.AppendLine();
            //if (RecordSlack != null && RecordSlack.Length > 0)
            //{
            //    sb.AppendLine(string.Format("Record Slack: {0}", BitConverter.ToString(RecordSlack)));
            //}
            
            return sb.ToString();
        }
    }
}
