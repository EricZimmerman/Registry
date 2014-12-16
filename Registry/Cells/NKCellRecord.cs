using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NFluent;

// namespaces...
namespace Registry.Cells
{
    // public classes...
    public class NKCellRecord : ICellTemplate
    {
        // protected internal constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="NKCellRecord"/> class.
        /// </summary>
        protected internal NKCellRecord(byte[] rawBytes)
        {
            RawBytes = rawBytes;

            Size = BitConverter.ToInt32(rawBytes, 0);

            IsFree = Size > 0;

            Signature = Encoding.ASCII.GetString(rawBytes, 4, 2);

            Check.That(Signature).IsEqualTo("nk");

            Flags = BitConverter.ToUInt32(rawBytes, 6); //TODO 4.20 Key Node flag values do an enum or something

            var ts = BitConverter.ToInt64(rawBytes, 0x8);

            LastWriteTimestamp = DateTimeOffset.FromFileTime(ts);

            ParentCellIndex = BitConverter.ToUInt32(rawBytes, 0x14);

            SubkeyCountsStable = BitConverter.ToUInt32(rawBytes, 0x18);
            SubkeyCountsVolatile = BitConverter.ToUInt32(rawBytes, 0x1c);

            SubkeyListsStableCellIndex = BitConverter.ToUInt32(rawBytes, 0x20);

            var num = BitConverter.ToUInt32(rawBytes, 0x24);

            if (num == 0xFFFFFFFF)
            {
                SubkeyListsVolatileCellIndex = 0;
            }
            else
            {
                SubkeyListsVolatileCellIndex = num;
            }

            ValueListCount = BitConverter.ToUInt32(rawBytes, 0x28);

            num = BitConverter.ToUInt32(rawBytes, 0x2c);

            if (num == 0xFFFFFFFF)
            {
                ValueListCellIndex = 0;
            }
            else
            {
                ValueListCellIndex = num;
            }



            SecurityCellIndex = BitConverter.ToUInt32(rawBytes, 0x30);

            num = BitConverter.ToUInt32(rawBytes, 0x34);

            if (num == 0xFFFFFFFF)
            {
                ClassCellIndex = 0;
            }
            else
            {
                ClassCellIndex = num;
            }


            MaximumNameLength = BitConverter.ToUInt16(rawBytes, 0x38);

            var rawFlags = Convert.ToString(rawBytes[0x3a], 2) ;

            rawFlags = string.Format("{0:X8}", rawBytes[0x3a]);


            var userInt = Convert.ToInt32(rawFlags.Substring(0, 4 ));


            var virtInt = Convert.ToInt32(rawFlags.Substring(4, 4));

            UserFlags = userInt;
            VirtualControlFlags = virtInt;

            Debug = rawBytes[0x3a];


            MaximumClassLength = BitConverter.ToUInt32(rawBytes, 0x3c);
            MaximumValueNameLength = BitConverter.ToUInt32(rawBytes, 0x40);
            MaximumValueDataLength = BitConverter.ToUInt32(rawBytes, 0x44);

            WorkVar = BitConverter.ToUInt32(rawBytes, 0x48);

            NameLength = BitConverter.ToUInt16(rawBytes, 0x4c);
            ClassLength = BitConverter.ToUInt16(rawBytes, 0x4e);

            Name = Encoding.ASCII.GetString(rawBytes, 0x50, NameLength);

            var paddingOffset = 0x50 + NameLength;
            var paddingLength = Math.Abs(Size) - paddingOffset;

            Padding = BitConverter.ToString(rawBytes, paddingOffset, paddingLength);
        }

        // public properties...
        public uint ClassCellIndex { get; private set; }
        public ushort ClassLength { get; private set; }
        public byte Debug { get; private set; }
        public uint Flags { get; private set; }
        public bool IsFree { get; private set; }
        public DateTimeOffset LastWriteTimestamp { get; private set; }
        public uint MaximumClassLength { get; private set; }
        public ushort MaximumNameLength { get; private set; }
        public uint MaximumValueDataLength { get; private set; }
        public uint MaximumValueNameLength { get; private set; }
        public string Name { get; private set; }
        public ushort NameLength { get; private set; }
        public string Padding { get; private set; }
        public uint ParentCellIndex { get; private set; }
        public byte[] RawBytes { get; private set; }
        public uint SecurityCellIndex { get; private set; }
        public string Signature { get; private set; }
        public int Size { get; private set; }
        public uint SubkeyCountsStable { get; private set; }
        public uint SubkeyCountsVolatile { get; private set; }
        public uint SubkeyListsStableCellIndex { get; private set; }
        public uint SubkeyListsVolatileCellIndex { get; private set; }
        public int UserFlags { get; private set; }
        public uint ValueListCellIndex { get; private set; }
        public uint ValueListCount { get; private set; }
        public int VirtualControlFlags { get; private set; }
        public uint WorkVar { get; private set; }
    }
}
