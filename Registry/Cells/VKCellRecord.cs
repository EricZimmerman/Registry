using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NFluent;

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
            [Description("Binary data")]
            RegBinary = 0x0003,
            [Description("32-bit number")]
            RegDword = 0x0004,
            [Description("32-bit number (big endian)")]
            RegDwordBigEndian = 0x0005,
            [Description("Variable length string")] RegExpandSz = 0x0002,
            [Description("Binary data")]
            RegFullResourceDescription = 0x0009,
            [Description("Unicode string")]
            RegLink = 0x0006,
            [Description("Multiple strings")]
            RegMultiSz = 0x0007,
            [Description("Undefined type")]
            RegNone = 0x0000,
            [Description("64-bit number")]
            RegQword = 0x000B,
            [Description("Binary data")]
            RegResourceList = 0x0008,
            [Description("Binary data")]
            RegResourceRequirementsList = 0x000A,
            [Description("FILETIME data")]
            RegFileTime = 0x0010,
            [Description("Fixed length string")] RegSz = 0x0001,
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

            if (IsFree)
            {
                return;
            }

            NameLength = BitConverter.ToUInt16(rawBytes, 0x06);
            DataLength = BitConverter.ToUInt32(rawBytes, 0x08);

            var dataLengthInternal = DataLength;

            var dataIsResident = Convert.ToString(dataLengthInternal, 2).PadLeft(32, '0').StartsWith("1");

            if (dataIsResident)
            {
                dataLengthInternal = dataLengthInternal - 0x80000000;
            }

            if (dataLengthInternal == 0x7A78)
            {
                Debug.WriteLine("dataLengthInternal: 0x{0:X}", dataLengthInternal);
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
                //When less than or equal to 4, the data lives in the OffsetToData
                datablockRaw = new byte[4];

                Array.Copy(rawBytes, 0xc, datablockRaw, 0, 4);
            }
            else
            {
                //we know the offset, so grab some bytes in order to get the size of the data block vs the size of the data in it
                var datablockSizeRaw = Registry.ReadBytesFromHive(4096 + OffsetToData, 4);

                dataBlockSize = BitConverter.ToInt32(datablockSizeRaw, 0);

                datablockRaw = Registry.ReadBytesFromHive(4096 + OffsetToData, Math.Abs(dataBlockSize));

                //datablockRaw now has our value AND slack space!
                //value is dataLengthInternal long. rest is slack
            }

            //TODO DO I NEED TO USE DataNode here?
            //http://amnesia.gtisc.gatech.edu/~moyix/suzibandit.ltd.uk/MSc/Registry%20Structure%20-%20Main%20V4.pdf
            // TODO page 64 data node?

            ValueDataRaw = datablockRaw;

            switch (DataType)
            {
                case DataTypeEnum.RegExpandSz:
                case DataTypeEnum.RegMultiSz:

                    ValueData =
                        Encoding.Unicode.GetString(datablockRaw, 4, (int) dataLengthInternal).Replace("\0", " ").Trim();
                    ValueDataSlack =
                        datablockRaw.Skip((int) (dataLengthInternal + 4))
                            .Take((int) (Math.Abs(dataBlockSize) - 4 - dataLengthInternal))
                            .ToArray();
                    break;

                case DataTypeEnum.RegNone:
                case DataTypeEnum.RegBinary:
                case DataTypeEnum.RegResourceRequirementsList:
                case DataTypeEnum.RegResourceList:

                    ValueData = datablockRaw.Skip(4).Take(Math.Abs(dataBlockSize)).ToArray();

                    ValueDataSlack =
                        datablockRaw.Skip((int) (dataLengthInternal + 4))
                            .Take((int) (Math.Abs(dataBlockSize) - 4 - dataLengthInternal))
                            .ToArray();

                    break;
                case DataTypeEnum.RegDword:
                    ValueData = BitConverter.ToUInt32(datablockRaw, 0);

                    break;
                case DataTypeEnum.RegDwordBigEndian:

                    
                    
                    break;


                case DataTypeEnum.RegFullResourceDescription:
                    break;

                case DataTypeEnum.RegQword:

                    ValueData = BitConverter.ToUInt64(datablockRaw, 4);

                    ValueDataSlack =
                        datablockRaw.Skip((int) (dataLengthInternal + 4))
                            .Take((int) (Math.Abs(dataBlockSize) - 4 - dataLengthInternal))
                            .ToArray();

                    break;

                case DataTypeEnum.RegSz:

                    if (dataIsResident)
                    {
                        ValueData = Encoding.Unicode.GetString(datablockRaw, 0, (int) dataLengthInternal)
                            .Replace("\0", "");
                    }
                    else
                    {
                        ValueData = Encoding.Unicode.GetString(datablockRaw, 4, (int) dataLengthInternal)
                            .Replace("\0", "");
                        ValueDataSlack =
                            datablockRaw.Skip((int) (dataLengthInternal + 4))
                                .Take((int) (Math.Abs(dataBlockSize) - 4 - dataLengthInternal))
                                .ToArray();
                    }

                    //test 
                    // Debug.WriteLine("Value name: {0}, Value: {1}: Slack: {2}", ValueName, ValueData, ValueDataSlack);

                    break;

                case DataTypeEnum.RegUnknown:

                    ValueData = datablockRaw;
                    ValueDataSlack = new byte[0];
                    
                    

                    break;

                default:

                    break;
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

            if (IsFree)
            {
                return sb.ToString();
            }

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