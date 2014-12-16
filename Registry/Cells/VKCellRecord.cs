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
    internal class VKCellRecord : ICellTemplate
    {
        // private fields...
        private readonly int _size;

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

            NameLength = BitConverter.ToUInt16(rawBytes, 0x06);
            DataLength = BitConverter.ToUInt16(rawBytes, 0x08);
            OffsetToData = BitConverter.ToUInt32(rawBytes, 0x0c);
            DataType = (DataTypeEnum) BitConverter.ToUInt32(rawBytes, 0x10);
            NamePresentFlag = BitConverter.ToUInt16(rawBytes, 0x14);
            Unknown = BitConverter.ToUInt16(rawBytes, 0x16);

            //            Value data is either stored internally in the value cell, if the value length does not exceed
            //four bytes, or in a separate data cell, in which case the value cell contains an offset to a
            //data cell. The data type field tells whether data is stored internally or externally: if the data
            //type equals 0x80, the field at offset 0x0c contains value data, otherwise the field contains
            //offset to a data cell where value data is stored. 

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
                //we know the offset, so grab some bytes in order to get the size of the data block vs the size of the data in it
                var datablockSizeRaw = Registry.ReadBytesFromHive(4096 + OffsetToData, 4);

                var dataBlockSize = BitConverter.ToInt32(datablockSizeRaw, 0);

                var datablockRaw = Registry.ReadBytesFromHive(4096 + OffsetToData, dataBlockSize);

                //datablockRaw now has our value AND slack space!

                //TODO DO I NEED TO USE DataNode here?
                switch (DataType)
                {
                    case DataTypeEnum.RegSz:

                        ValueData = Encoding.Unicode.GetString(datablockRaw, 4, DataLength).Replace("\0", "");
                        //TODO present this as BitConverter as well, in parens?
                        ValueDataSlack = Encoding.Unicode.GetString(datablockRaw, DataLength + 4, Math.Abs(dataBlockSize) - 4 - DataLength);


                        Debug.WriteLine("Value name: {0}, Value: {1}: Slack: {2}", ValueName, ValueData, ValueDataSlack);

                        break;

                    default:

                        break;
                }
            }



            //http://amnesia.gtisc.gatech.edu/~moyix/suzibandit.ltd.uk/MSc/Registry%20Structure%20-%20Main%20V4.pdf
            // TODO page 64 data node?

        }

        // public enums...
        public enum DataTypeEnum
        {
            [Description("Undefined type")]
            RegNone,
            [Description("Fixed length string")]
            RegSz,
            [Description("Variable length string")]
            RegExpandSz,
            [Description("Binary data")]
            RegBinary,
            [Description("32-bit number")]
            RegDword,
            [Description("32-bit number (big endian)")]
            RegDwordBigEndian,
            [Description("Unicode string")]
            RegLink,
            [Description("Multiple strings")]
            RegMultiSz,
            [Description("Binary data")]
            RegResourceList,
            [Description("Binary data")]
            RegFullResourceDescription,
            [Description("Binary data")]
            RegResourceRequirementsList,
            [Description("64-bit number")]
            RegQword
        }

        // public properties...
        public ushort DataLength { get; set; }
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
