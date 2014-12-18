using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NFluent;
using Registry.Lists;

// namespaces...

namespace Registry.Cells
{
    // internal classes...
    /// <summary>
    ///     <remarks>Represents a Key Value Record</remarks>
    /// </summary>
    internal class VKCellRecord : ICellTemplate
    {
        // public enums...
        public enum DataTypeEnum
        {
            [Description("Binary data (any arbitrary data)")]
            RegBinary = 0x0003,
            [Description("A DWORD value, a 32-bit unsigned integer (little-endian)")]
            RegDword = 0x0004,
            [Description("A DWORD value, a 32-bit unsigned integer  (big endian)")]
            RegDwordBigEndian = 0x0005,
            [Description("An 'expandable' string value that can contain environment variables, normally stored and exposed in UTF-16LE")] RegExpandSz = 0x0002,
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
            [Description("FILETIME data")]
            RegFileTime = 0x0010,
            [Description("A string value, normally stored and exposed in UTF-16LE")]
            RegSz = 0x0001,
            [Description("Unknown data type")] RegUnknown = 999
        }

        // private fields...
        private readonly int _size;
        // public constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="VKCellRecord" /> class.
        /// </summary>
        public VKCellRecord(byte[] rawBytes)
        {
            RawBytes = rawBytes;

            _size = BitConverter.ToInt32(rawBytes, 0);

            IsFree = _size > 0;

            Signature = Encoding.ASCII.GetString(rawBytes, 4, 2);

            Check.That(Signature).IsEqualTo("vk");

            //there is still data in there, so get it if possible
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
                internalDataOffset = 0;
            }

            
            OffsetToData = BitConverter.ToUInt32(rawBytes, 0x0c);
            
            //we need to preserve the datatype as it exists (so we can see unsupported types easily)
            DataTypeRaw = BitConverter.ToUInt32(rawBytes, 0x10);

            //force to a known datatype 
            var dataTypeInternal = DataTypeRaw;

            if (dataTypeInternal > (ulong)DataTypeEnum.RegFileTime)
            {
                dataTypeInternal = 999;
            }

            DataType = (DataTypeEnum)dataTypeInternal;

            NamePresentFlag = BitConverter.ToUInt16(rawBytes, 0x14);
            Unknown = BitConverter.ToUInt16(rawBytes, 0x16);

            if (NamePresentFlag == 0)
            {
                ValueName = "(Default)";
            }
            else
            {
                ValueName = Encoding.ASCII.GetString(rawBytes, 0x18, NameLength);
            }
  
            byte[] datablockRaw;
            var dataBlockSize = 0;

            if (dataIsResident)
            {
                //Since its resident, the data lives in the OffsetToData.
                datablockRaw = new byte[4];

                //make a copy for processing below
                Array.Copy(rawBytes, 0xc, datablockRaw, 0, 4);
            }
            else
            {
                //We have to go look at the OffsetToData to see what we have so we can do the right thing

                //The first operations are always the same. Go get the length of the data cell, then see how big it is.
                var datablockSizeRaw = Registry.ReadBytesFromHive(4096 + OffsetToData, 4);

                // in some rare cases the bytes returned from the previous line are all zeros, so make sure we get something but all zeros
                if (datablockSizeRaw.Length == 4)
                {
                     dataBlockSize = BitConverter.ToInt32(datablockSizeRaw, 0);
                }
                   

                //The most common case is simply where the data we want lives at OffsetToData, so we just go get it

                //we know the offset to where the data lives, so grab bytes in order to get the size of the data *block* vs the size of the data in it
                datablockRaw = Registry.ReadBytesFromHive(4096 + OffsetToData, Math.Abs(dataBlockSize));
                
                //datablockRaw now has our value AND slack space!
                //value is dataLengthInternal long. rest is slack


                //Some values are huge, so look for them and, if found, get the data into dataBlockRaw
                if (dataLengthInternal > 16344 && (Registry.Header.MajorVersion == 1 && Registry.Header.MinorVersion > 3))
                {
                    // this is the BIG DATA case. here, we have to get the data pointed to by OffsetToData and process it to get to our (possibly fragmented) DataType

                    datablockRaw = Registry.ReadBytesFromHive(4096 + OffsetToData, Math.Abs(dataBlockSize));

                    var db = new DBListRecord(datablockRaw);

                    // db now contains a pointer to where we can get db.NumberOfEntries offsets to our data and reassemble it

                    datablockSizeRaw = Registry.ReadBytesFromHive(4096 + db.OffsetToOffsets, 4);
                    dataBlockSize = BitConverter.ToInt32(datablockSizeRaw, 0);

                    datablockRaw = Registry.ReadBytesFromHive(4096 + db.OffsetToOffsets, Math.Abs(dataBlockSize));

                    //datablockRaw now contains our list of pointers to fragmented Data

                    //make a place to reassemble things
                    var bigDataRaw = new ArrayList((int) dataLengthInternal);

                    for (int i = 1; i <= db.NumberOfEntries; i++)
                    {
                        // read the offset and go get that data. use i * 4 so we get 4, 8, 12, 16, etc
                        var os = BitConverter.ToUInt32(datablockRaw, i * 4);

                        var tempDataBlockSizeRaw = Registry.ReadBytesFromHive(4096 + os, 4);
                        var tempdataBlockSize = BitConverter.ToInt32(tempDataBlockSizeRaw, 0);

                      //get our data block
                        var tempDataRaw = Registry.ReadBytesFromHive(4096 + os, Math.Abs(tempdataBlockSize));
                        
                        // since the data is prefixed with its length (4 bytes), skip that so we do not include it in the final data 
                        //we read 16344 bytes as the rest is padding and jacks things up if you use the whole range of bytes
                        bigDataRaw.AddRange(tempDataRaw.Skip(4).Take(16344).ToArray());
                    }

                    datablockRaw = (byte[]) bigDataRaw.ToArray(typeof(byte)) ;

                    //reset this so slack calculation works
                    dataBlockSize = datablockRaw.Length;

                    //since dataBlockRaw doesnt have the size on it in this case, adjust internalDataOffset accordingly
                    internalDataOffset = 0;
                }

                //Now that we are here the data we need to convert to our Values resides in datablockRaw and is ready for more processing according to DataType
            }

            //TODO DO I NEED TO USE DataNode here?
            //http://amnesia.gtisc.gatech.edu/~moyix/suzibandit.ltd.uk/MSc/Registry%20Structure%20-%20Main%20V4.pdf
            // TODO page 64 data node? it would encapsulate ValueData, ValueSlack, and ValueDataRaw

            ValueDataRaw = datablockRaw;

            ValueDataSlack =
                    datablockRaw.Skip((int)(dataLengthInternal + internalDataOffset))
                        .Take((int)(Math.Abs(dataBlockSize) - internalDataOffset - dataLengthInternal))
                        .ToArray();

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

                        ValueData = DateTimeOffset.FromFileTime((long)ts);

                        break;

                    case DataTypeEnum.RegExpandSz:
                    case DataTypeEnum.RegMultiSz:
                        ValueData =
                        Encoding.Unicode.GetString(datablockRaw, internalDataOffset, (int)dataLengthInternal).Replace("\0", " ").Trim();

                        break;

                    case DataTypeEnum.RegNone:
                    case DataTypeEnum.RegBinary:
                    case DataTypeEnum.RegResourceRequirementsList:
                    case DataTypeEnum.RegResourceList:
                        ValueData = datablockRaw.Skip(internalDataOffset).Take(Math.Abs(dataBlockSize)).ToArray();

                     

                        break;

                    case DataTypeEnum.RegDword:
                        ValueData = BitConverter.ToUInt32(datablockRaw, 0);

                        break;

                    case DataTypeEnum.RegDwordBigEndian:
                        break;

                    case DataTypeEnum.RegFullResourceDescription:
                        break;

                    case DataTypeEnum.RegQword:
                        ValueData = BitConverter.ToUInt64(datablockRaw, internalDataOffset);

                        break;

                    case DataTypeEnum.RegSz:
                        ValueData = Encoding.Unicode.GetString(datablockRaw, internalDataOffset, (int)dataLengthInternal)
                        .Replace("\0", "");

                        break;

                    case DataTypeEnum.RegUnknown:
                        ValueData = datablockRaw;

                        ValueDataSlack = new byte[0];

                        break;

                    default:

                        break;
                }
            }
            catch (Exception)
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

        // public properties...
        public uint DataLength { get; set; }
        public DataTypeEnum DataType { get; set; }
        public uint DataTypeRaw { get; set; }
        public ushort NameLength { get; set; }
        public ushort NamePresentFlag { get; set; }
        public uint OffsetToData { get; set; }
        public ushort Unknown { get; set; }
        public byte[] ValueDataRaw { get; set; }
        public object ValueData { get; set; }
        public object ValueDataSlack { get; set; }
        public string ValueName { get; set; }
        public bool IsFree { get; private set; }
        public byte[] RawBytes { get; private set; }
        public string Signature { get; private set; }

        public int Size
        {
            get { return _size; }
        }

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Size: 0x{0:X}", Math.Abs(_size)));
            sb.AppendLine(string.Format("Signature: {0}", Signature));
            sb.AppendLine(string.Format("Data Type: {0}", DataType));
            sb.AppendLine();
            sb.AppendLine(string.Format("IsFree: {0}", IsFree));

            //if (IsFree)
            //{
            //    return sb.ToString();
            //}

            sb.AppendLine();

            sb.AppendLine(string.Format("DataLength: 0x{0:X}", DataLength));
            sb.AppendLine(string.Format("OffsetToData: 0x{0:X}", OffsetToData));

            sb.AppendLine();

            sb.AppendLine(string.Format("NameLength: 0x{0:X}", NameLength));
            sb.AppendLine(string.Format("NamePresentFlag: 0x{0:X}", NamePresentFlag));

            sb.AppendLine();

            sb.AppendLine(string.Format("ValueName: {0}", ValueName));


            switch (DataType)
            {
                case DataTypeEnum.RegSz:
                case DataTypeEnum.RegExpandSz:
                case DataTypeEnum.RegMultiSz:
                    sb.AppendLine(string.Format("ValueData: {0}", ValueData));

                    break;

                case DataTypeEnum.RegNone:
                case DataTypeEnum.RegBinary:
                case DataTypeEnum.RegResourceList:
                case DataTypeEnum.RegResourceRequirementsList:
                    if (ValueData == null)
                    {
                        sb.AppendLine(string.Format("ValueData: {0}", ""));
                    }
                    else
                    {
                        sb.AppendLine(string.Format("ValueData: {0}", BitConverter.ToString((byte[]) ValueData)));
                    }

                    break;

                case DataTypeEnum.RegDwordBigEndian:
                    break;
                case DataTypeEnum.RegLink:
                    break;

                case DataTypeEnum.RegFullResourceDescription:
                    break;

                case DataTypeEnum.RegDword:
                case DataTypeEnum.RegQword:
                    sb.AppendLine(string.Format("ValueData: {0:N}", ValueData));
                    break;
                default:
                    break;
            }


            if (ValueDataSlack != null)
            {
                sb.AppendLine(string.Format("ValueDataSlack: {0}", BitConverter.ToString((byte[]) ValueDataSlack, 0)));
            }


            sb.AppendLine();


            return sb.ToString();
        }
    }
}