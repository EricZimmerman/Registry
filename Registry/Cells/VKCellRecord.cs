using NFluent;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

// namespaces...
namespace Registry.Cells
{
    // internal classes...
    /// <summary>
    /// <remarks>Represents a Key Value Record</remarks>
    /// </summary>
    internal class VKCellRecord : ICellTemplate
    {
        // private fields...
        private readonly int _size;


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
                default:
                    break;
                case DataTypeEnum.RegNone:
                    break;
                case DataTypeEnum.RegSz:
                case DataTypeEnum.RegDword:
                case DataTypeEnum.RegExpandSz:
                case DataTypeEnum.RegMultiSz:
                    sb.AppendLine(string.Format("ValueData: {0}", ValueData));

                    break;
                case DataTypeEnum.RegBinary:
                    if (ValueData == null)
                    {
                        sb.AppendLine(string.Format("ValueData: {0}", ""));
                    }
                    else
                    {
                        sb.AppendLine(string.Format("ValueData: {0}", BitConverter.ToString((byte[])ValueData)));
                    }
                        
                    break;
      
                case DataTypeEnum.RegDwordBigEndian:
                    break;
                case DataTypeEnum.RegLink:
                    break;

                case DataTypeEnum.RegResourceList:
                    break;
                case DataTypeEnum.RegFullResourceDescription:
                    break;
                case DataTypeEnum.RegResourceRequirementsList:
                    break;
                case DataTypeEnum.RegQword:
                    sb.AppendLine(string.Format("ValueData: {0:N}", ValueData));
                    break;
            }



            if (ValueDataSlack != null)
            {
                sb.AppendLine(string.Format("ValueDataSlack: {0}", BitConverter.ToString((byte[])ValueDataSlack, 0)));
            }

                

            sb.AppendLine();


            return sb.ToString();
        }

        // public constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="VKCellRecord"/> class.
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
            OffsetToData = BitConverter.ToUInt32(rawBytes, 0x0c);
            DataType = (DataTypeEnum) BitConverter.ToUInt32(rawBytes, 0x10);
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

            if (OffsetToData > 0)
            {
                byte[] datablockSizeRaw = null;
                byte[] datablockRaw = null;
                var dataBlockSize = 0;

                if (DataLength <= 4 | DataLength > 0x80000000)
                {
                    //When less than or equal to 4, the data lives in the OffsetToData
                    datablockRaw=new byte[4];

                    Array.Copy(rawBytes,0xc,datablockRaw,0,4);
                }
                else
                {
                    //we know the offset, so grab some bytes in order to get the size of the data block vs the size of the data in it
                    datablockSizeRaw = Registry.ReadBytesFromHive(4096 + OffsetToData, 4);

                 dataBlockSize = BitConverter.ToInt32(datablockSizeRaw, 0);

                 datablockRaw = Registry.ReadBytesFromHive(4096 + OffsetToData, Math.Abs(dataBlockSize));

                    //datablockRaw now has our value AND slack space!
                }

                //TODO DO I NEED TO USE DataNode here?
                //http://amnesia.gtisc.gatech.edu/~moyix/suzibandit.ltd.uk/MSc/Registry%20Structure%20-%20Main%20V4.pdf
                // TODO page 64 data node?

                switch (DataType)
                {
           
                    case DataTypeEnum.RegExpandSz:
                    case DataTypeEnum.RegMultiSz:
                    case DataTypeEnum.RegBinary:

                      

                            ValueData = datablockRaw.Skip(4).Take(Math.Abs(dataBlockSize)).ToArray();
        
                        ValueDataSlack =
                            datablockRaw.Skip((int) (DataLength + 4)).Take((int) (Math.Abs(dataBlockSize) - 4 - DataLength)).ToArray(); // Encoding.Unicode.GetString(datablockRaw, DataLength + 4, Math.Abs(dataBlockSize) - 4 - DataLength);


                        break;
                    case DataTypeEnum.RegDword:
                        ValueData = BitConverter.ToUInt32(datablockRaw, 0);

                        break;
                    case DataTypeEnum.RegDwordBigEndian:
                        break;
                  
                    case DataTypeEnum.RegResourceList:
                        break;
                    case DataTypeEnum.RegFullResourceDescription:
                        break;
                    case DataTypeEnum.RegResourceRequirementsList:
                        break;
                    case DataTypeEnum.RegQword:

                        ValueData = BitConverter.ToUInt64(datablockRaw, 4);

                         ValueDataSlack =
                            datablockRaw.Skip((int) (DataLength + 4)).Take((int) (Math.Abs(dataBlockSize) - 4 - DataLength)).ToArray(); // Encoding.Unicode.GetString(datablockRaw, DataLength + 4, Math.Abs(dataBlockSize) - 4 - DataLength);


                        break;

                    case DataTypeEnum.RegSz:

                        if (DataLength <= 4 | DataLength > 0x80000000)
                        {
                            var dl = DataLength - 0x80000000;
                            ValueData = Encoding.Unicode.GetString(datablockRaw, 0, (int)dl).Replace("\0", "");

                        }
                        else
                        {
                            ValueData = Encoding.Unicode.GetString(datablockRaw, 4, (int) DataLength).Replace("\0", "");
                            ValueDataSlack =
                            datablockRaw.Skip((int) (DataLength + 4)).Take((int) (Math.Abs(dataBlockSize) - 4 - DataLength)).ToArray(); // Encoding.Unicode.GetString(datablockRaw, DataLength + 4, Math.Abs(dataBlockSize) - 4 - DataLength);
                        }

                            

                        

                       // Debug.WriteLine("Value name: {0}, Value: {1}: Slack: {2}", ValueName, ValueData, ValueDataSlack);

                        break;

                    default:

                        break;
                }
            }



    

        }

        // public enums...
        public enum DataTypeEnum
        {
            [Description("Undefined type")]
            RegNone=0,
            [Description("Fixed length string")]
            RegSz=1,
            [Description("Variable length string")]
            RegExpandSz=2,
            [Description("Binary data")]
            RegBinary=3,
            [Description("32-bit number")]
            RegDword=4,
            [Description("32-bit number (big endian)")]
            RegDwordBigEndian=5,
            [Description("Unicode string")]
            RegLink=6,
            [Description("Multiple strings")]
            RegMultiSz=7,
            [Description("Binary data")]
            RegResourceList=8,
            [Description("Binary data")]
            RegFullResourceDescription=9,
            [Description("Binary data")]
            RegResourceRequirementsList=10,
            [Description("64-bit number")]
            RegQword=11
        }

        // public properties...
        public uint DataLength { get; set; }
        public DataTypeEnum DataType { get; set; }
        public bool IsFree { get; private set; }
        public ushort NameLength { get; set; }
        public ushort NamePresentFlag { get; set; }
        public uint OffsetToData { get; set; }
        public byte[] RawBytes { get; private set; }
        public string Signature { get; private set; }
        public int Size
        {
            get
            {
                return _size;
            }
        }

        public ushort Unknown { get; set; }
        public object ValueData { get; set; }
        public object ValueDataSlack { get; set; }
        public string ValueName { get; set; }
    }
}
