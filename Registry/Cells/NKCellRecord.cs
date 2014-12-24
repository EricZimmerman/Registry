using NFluent;
using System;
using System.Collections.Generic;
using System.Text;

// namespaces...
namespace Registry.Cells
{
    // public classes...
    public class NKCellRecord : ICellTemplate
    {
        // private fields...
        private readonly int _size;
        // protected internal constructors...

        // public fields...
        public List<ulong> ValueOffsets;

        // protected internal constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="NKCellRecord"/> class.
        /// <remarks>Represents a Key Node Record</remarks>
        /// </summary>
        protected internal NKCellRecord(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;
            RawBytes = rawBytes;

            ValueOffsets = new List<ulong>();

            _size = BitConverter.ToInt32(rawBytes, 0);

            IsFree = _size > 0;

            Signature = Encoding.ASCII.GetString(rawBytes, 4, 2);

            Check.That(Signature).IsEqualTo("nk");

            Flags = (FlagEnum) BitConverter.ToUInt16(rawBytes, 6);

            var ts = BitConverter.ToInt64(rawBytes, 0x8);

            LastWriteTimestamp = DateTimeOffset.FromFileTime(ts).ToUniversalTime();

            ParentCellIndex = BitConverter.ToUInt32(rawBytes, 0x14);

            SubkeyCountsStable = BitConverter.ToUInt32(rawBytes, 0x18);
            SubkeyCountsVolatile = BitConverter.ToUInt32(rawBytes, 0x1c);



            //SubkeyListsStableCellIndex
            var num = BitConverter.ToUInt32(rawBytes, 0x20);

            if (num == 0xFFFFFFFF)
            {
                SubkeyListsStableCellIndex = 0;
            }
            else
            {
                SubkeyListsStableCellIndex = num;
            }

            //SubkeyListsVolatileCellIndex
            num = BitConverter.ToUInt32(rawBytes, 0x24);

            if (num == 0xFFFFFFFF)
            {
                SubkeyListsVolatileCellIndex = 0;
            }
            else
            {
                SubkeyListsVolatileCellIndex = num;
            }

            ValueListCount = BitConverter.ToUInt32(rawBytes, 0x28);

            //ValueListCellIndex
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

            //ClassCellIndex
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

            var rawFlags = Convert.ToString(rawBytes[0x3a], 2).PadLeft(8, '0');

            //  rawFlags = string.Format("{0:X8}", rawBytes[0x3a]);


            var userInt = Convert.ToInt32(rawFlags.Substring(0, 4 )); //TODO is this a flag enum somewhere?


            var virtInt = Convert.ToInt32(rawFlags.Substring(4, 4));//TODO is this a flag enum somewhere?

            UserFlags = userInt;
            VirtualControlFlags = virtInt;

            Debug = rawBytes[0x3a];


            MaximumClassLength = BitConverter.ToUInt32(rawBytes, 0x3c);
            MaximumValueNameLength = BitConverter.ToUInt32(rawBytes, 0x40);
            MaximumValueDataLength = BitConverter.ToUInt32(rawBytes, 0x44);

            WorkVar = BitConverter.ToUInt32(rawBytes, 0x48);

            NameLength = BitConverter.ToUInt16(rawBytes, 0x4c);
            ClassLength = BitConverter.ToUInt16(rawBytes, 0x4e);

            if (Flags.ToString().Contains(FlagEnum.CompressedName.ToString()))
            {
                Name = Encoding.ASCII.GetString(rawBytes, 0x50, NameLength);
            }
            else
            {
                Name = Encoding.Unicode.GetString(rawBytes, 0x50, NameLength);
            }



            var paddingOffset = 0x50 + NameLength;
            var paddingLength = Math.Abs(Size) - paddingOffset;

            if (paddingLength > 0)
            {
                Padding = BitConverter.ToString(rawBytes, paddingOffset, paddingLength);
            }
            else
            {
                Padding = string.Empty;
            }

            Check.That(paddingOffset + paddingLength).IsEqualTo(rawBytes.Length);

        }

        // public enums...
        [Flags]
        public enum FlagEnum
        {
            CompressedName = 0x0020,
            HiveEntryRootKey = 0x0004,
            HiveExit = 0x0002,
            NoDelete = 0x0008,
            PredefinedHandle = 0x0040,
            SymbolicLink = 0x0010,
            Unused0400 = 0x0400,
            Unused0800 = 0x0800,
            Unused1000 = 0x1000,
            Unused2000 = 0x2000,
            Unused4000 = 0x4000,
            Unused8000 = 0x8000,
            UnusedVolatileKey = 0x0001,
            VirtMirrored = 0x0080,
            VirtTarget = 0x0100,
            VirtualStore = 0x0200
        }

        // public properties...
        public long AbsoluteOffset
        {
            get
            {
                return RelativeOffset + 4096;
            }
        }

        // public properties...
        public uint ClassCellIndex { get; private set; }
        public ushort ClassLength { get; private set; }
        public byte Debug { get; private set; }
        public FlagEnum Flags { get; private set; }
        public bool IsFree { get; private set; }
        public bool IsReferenceed { get; internal set; }
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
        public long RelativeOffset { get; private set; }
        public uint SecurityCellIndex { get; private set; }
        public string Signature { get; private set; }
        public int Size
        {
            get
            {
                return Math.Abs(_size);
            }
        }

        /// <summary>
        /// When true, this key has been deleted
        /// <remarks>The parent key is determined by checking whether ParentCellIndex 1) exists and 2) ParentCellIndex.IsReferenced == true. </remarks>
        /// </summary>
        public bool IsDeleted { get; internal set; }

        public uint SubkeyCountsStable { get; private set; }
        public uint SubkeyCountsVolatile { get; private set; }
        public uint SubkeyListsStableCellIndex { get; private set; }
        public uint SubkeyListsVolatileCellIndex { get; private set; }
        public int UserFlags { get; private set; }
        public uint ValueListCellIndex { get; private set; }
        public uint ValueListCount { get; private set; }
        public int VirtualControlFlags { get; private set; }
        public uint WorkVar { get; private set; }

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Size: 0x{0:X}", Math.Abs(_size)));
            sb.AppendLine(string.Format("RelativeOffset: 0x{0:X}", RelativeOffset));
            sb.AppendLine(string.Format("AbsoluteOffset: 0x{0:X}", AbsoluteOffset));
            sb.AppendLine(string.Format("Signature: {0}", Signature));
            sb.AppendLine(string.Format("Flags: {0}", Flags));
            sb.AppendLine();
            sb.AppendLine(string.Format("Name: {0}", Name));
            sb.AppendLine();
            sb.AppendLine(string.Format("Last Write Timestamp: {0}", LastWriteTimestamp));
            sb.AppendLine();


            sb.AppendLine(string.Format("IsFree: {0}", IsFree));

            sb.AppendLine();
            sb.AppendLine(string.Format("Debug: 0x{0:X}", Debug));

            sb.AppendLine();
            sb.AppendLine(string.Format("MaximumClassLength: 0x{0:X}", MaximumClassLength));
            sb.AppendLine(string.Format("ClassCellIndex: 0x{0:X}", ClassCellIndex));
            sb.AppendLine(string.Format("ClassLength: 0x{0:X}", ClassLength));

            sb.AppendLine();

            sb.AppendLine(string.Format("MaximumValueDataLength: 0x{0:X}", MaximumValueDataLength));
            sb.AppendLine(string.Format("MaximumValueDataLength: 0x{0:X}", MaximumValueDataLength));
            sb.AppendLine(string.Format("MaximumValueNameLength: 0x{0:X}", MaximumValueNameLength));

            sb.AppendLine();
            sb.AppendLine(string.Format("NameLength: 0x{0:X}", NameLength));
            sb.AppendLine(string.Format("MaximumNameLength: 0x{0:X}", MaximumNameLength));

            sb.AppendLine(string.Format("Padding: {0}", Padding));

            sb.AppendLine();
            sb.AppendLine(string.Format("ParentCellIndex: 0x{0:X}", ParentCellIndex));
            sb.AppendLine(string.Format("SecurityCellIndex: 0x{0:X}", SecurityCellIndex));

            sb.AppendLine();
            sb.AppendLine(string.Format("SubkeyCountsStable: 0x{0:X}", SubkeyCountsStable));
            sb.AppendLine(string.Format("SubkeyListsStableCellIndex: 0x{0:X}", SubkeyListsStableCellIndex));

            sb.AppendLine();
            sb.AppendLine(string.Format("SubkeyCountsVolatile: 0x{0:X}", SubkeyCountsVolatile));

            sb.AppendLine();
            sb.AppendLine(string.Format("UserFlags: 0x{0:X}", UserFlags));
            sb.AppendLine(string.Format("VirtualControlFlags: 0x{0:X}", VirtualControlFlags));
            sb.AppendLine(string.Format("WorkVar: 0x{0:X}", WorkVar));

            sb.AppendLine();
            sb.AppendLine(string.Format("ValueListCellIndex: 0x{0:X}", ValueListCellIndex));


            return sb.ToString();
        }
    }
}
