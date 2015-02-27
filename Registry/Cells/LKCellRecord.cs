using System;
using System.Collections.Generic;
using System.Text;
using NFluent;
using Registry.Other;

// namespaces...

namespace Registry.Cells
{
    // public classes...
    public class LKCellRecord : ICellTemplate, IRecordBase
    {
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

        // private fields...
        private readonly int _size;
        // protected internal constructors...

        // public fields...
        public List<ulong> ValueOffsets;
        // protected internal constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="NKCellRecord" /> class.
        ///     <remarks>Represents a Key Node Record</remarks>
        /// </summary>
        protected internal LKCellRecord(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;
            RawBytes = rawBytes;

            ValueOffsets = new List<ulong>();

            _size = BitConverter.ToInt32(rawBytes, 0);

            //TODO FINISH THIS LIKE NK

            //RootCellIndex
            var num = BitConverter.ToUInt32(rawBytes, 0x20);

            if (num == 0xFFFFFFFF)
            {
                RootCellIndex = 0;
            }
            else
            {
                RootCellIndex = num;
            }

            //HivePointer
            num = BitConverter.ToUInt32(rawBytes, 0x24);

            if (num == 0xFFFFFFFF)
            {
                HivePointer = 0;
            }
            else
            {
                HivePointer = num;
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

            //TODO is this a flag enum somewhere?
            var userInt = Convert.ToInt32(rawFlags.Substring(0, 4));

            var virtInt = Convert.ToInt32(rawFlags.Substring(4, 4));

            UserFlags = userInt;
            VirtualControlFlags = virtInt;

            Debug = rawBytes[0x3b];

            MaximumClassLength = BitConverter.ToUInt32(rawBytes, 0x3c);
            MaximumValueNameLength = BitConverter.ToUInt32(rawBytes, 0x40);
            MaximumValueDataLength = BitConverter.ToUInt32(rawBytes, 0x44);

            WorkVar = BitConverter.ToUInt32(rawBytes, 0x48);

            NameLength = BitConverter.ToUInt16(rawBytes, 0x4c);
            ClassLength = BitConverter.ToUInt16(rawBytes, 0x4e);

            //  if (Flags.ToString().Contains(FlagEnum.CompressedName.ToString()))
            if ((Flags & FlagEnum.CompressedName) == FlagEnum.CompressedName)
            {
                Name = Encoding.GetEncoding(1252).GetString(rawBytes, 0x50, NameLength);
            }
            else
            {
                Name = Encoding.Unicode.GetString(rawBytes, 0x50, NameLength);
            }

            var paddingOffset = 0x50 + NameLength;
            var paddingLength = Math.Abs(Size) - paddingOffset;

            if (paddingLength > 0)
            {
                Padding = new byte[paddingLength];
                Array.Copy(rawBytes, paddingOffset, Padding, 0, paddingLength);
                //Padding = BitConverter.ToString(rawBytes, paddingOffset, paddingLength);
            }
            
            //Check that we have accounted for all bytes in this record. this ensures nothing is hidden in this record or there arent additional data structures we havent processed in the record.
            Check.That(paddingOffset + paddingLength).IsEqualTo(rawBytes.Length);
        }

        // public properties...
        /// <summary>
        ///     The relative offset to a data node containing the classname
        ///     <remarks>
        ///         Use ClassLength to get the correct classname vs using the entire contents of the data node. There is often
        ///         slack slace in the data node when they hold classnames
        ///     </remarks>
        /// </summary>
        public uint ClassCellIndex { get;  private set;}

        /// <summary>
        ///     The length of the classname in the data node referenced by ClassCellIndex.
        /// </summary>
        public ushort ClassLength { get;  private set;}

        public byte Debug { get;  private set;}

        public FlagEnum Flags
        {
            get { return (FlagEnum) BitConverter.ToUInt16(RawBytes, 6); }
        }

        /// <summary>
        ///     The last write time of this key
        /// </summary>
        public DateTimeOffset LastWriteTimestamp
        {
            get
            {
                var ts = BitConverter.ToInt64(RawBytes, 0x8);

                return DateTimeOffset.FromFileTime(ts).ToUniversalTime();
            }
        }

        public uint MaximumClassLength { get;  private set;}
        public ushort MaximumNameLength { get;  private set;}
        public uint MaximumValueDataLength { get;  private set;}
        public uint MaximumValueNameLength { get;  private set;}

        /// <summary>
        ///     The name of this key. This is what is shown on the left side of RegEdit in the key and subkey tree.
        /// </summary>
        public string Name { get;  private set;}

        public ushort NameLength { get;  private set;}
        public byte[] Padding { get;  private set;}

        /// <summary>
        ///     The relative offset to the parent key for this record
        /// </summary>
        public uint ParentCellIndex
        {
            get { return BitConverter.ToUInt32(RawBytes, 0x14); }
        }

        /// <summary>
        ///     The relative offset to the security record for this record
        /// </summary>
        public uint SecurityCellIndex { get;  private set;}

        /// <summary>
        ///     When true, this key has been deleted
        ///     <remarks>
        ///         The parent key is determined by checking whether ParentCellIndex 1) exists and 2)
        ///         ParentCellIndex.IsReferenced == true.
        ///     </remarks>
        /// </summary>
        public bool IsDeleted { get; internal set; }

        /// <summary>
        ///     The number of subkeys this key contains
        /// </summary>
        public uint SubkeyCountsStable
        {
            get { return BitConverter.ToUInt32(RawBytes, 0x18); }
        }

        public uint SubkeyCountsVolatile
        {
            get { return BitConverter.ToUInt32(RawBytes, 0x1c); }
        }

        /// <summary>
        ///     The relative offset to the root cell this record is linked to.
        /// </summary>
        public uint RootCellIndex { get;  private set;}

        public uint HivePointer { get;  private set;}
        public int UserFlags { get;  private set;}
        public int VirtualControlFlags { get;  private set;}
        public uint WorkVar { get;  private set;}
        // public properties...
        public long AbsoluteOffset
        {
            get { return RelativeOffset + 4096; }
            set { }
        }

        public bool IsFree
        {
            get { return _size > 0; }
        }

        public bool IsReferenced { get; internal set; }
        public byte[] RawBytes { get;  private set;}
        public long RelativeOffset { get;  private set;}

        public string Signature
        {
            get { return Encoding.ASCII.GetString(RawBytes, 4, 2); }
            set { }
        }

        public int Size
        {
            get { return Math.Abs(_size); }
        }

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Size: 0x{0:X}", Math.Abs(_size)));
            sb.AppendLine(string.Format("Relative Offset: 0x{0:X}", RelativeOffset));
            sb.AppendLine(string.Format("Absolute Offset: 0x{0:X}", AbsoluteOffset));
            sb.AppendLine(string.Format("Signature: {0}", Signature));
            sb.AppendLine(string.Format("Flags: {0}", Flags));
            sb.AppendLine();
            sb.AppendLine(string.Format("Name: {0}", Name));
            sb.AppendLine();
            sb.AppendLine(string.Format("Last Write Timestamp: {0}", LastWriteTimestamp));
            sb.AppendLine();

            sb.AppendLine(string.Format("Is Free: {0}", IsFree));

            sb.AppendLine();
            sb.AppendLine(string.Format("Debug: 0x{0:X}", Debug));

            sb.AppendLine();
            sb.AppendLine(string.Format("Maximum Class Length: 0x{0:X}", MaximumClassLength));
            sb.AppendLine(string.Format("Class Cell Index: 0x{0:X}", ClassCellIndex));
            sb.AppendLine(string.Format("Class Length: 0x{0:X}", ClassLength));

            sb.AppendLine();

            sb.AppendLine(string.Format("Maximum Value Data Length: 0x{0:X}", MaximumValueDataLength));
            sb.AppendLine(string.Format("Maximum Value Name Length: 0x{0:X}", MaximumValueNameLength));

            sb.AppendLine();
            sb.AppendLine(string.Format("Name Length: 0x{0:X}", NameLength));
            sb.AppendLine(string.Format("Maximum Name Length: 0x{0:X}", MaximumNameLength));

            sb.AppendLine();
            sb.AppendLine(string.Format("Parent Cell Index: 0x{0:X}", ParentCellIndex));
            sb.AppendLine(string.Format("Security Cell Index: 0x{0:X}", SecurityCellIndex));

            sb.AppendLine();
            sb.AppendLine(string.Format("Subkey Counts Stable: 0x{0:X}", SubkeyCountsStable));

            sb.AppendLine();
            sb.AppendLine(string.Format("Hive pointer: 0x{0:X}", HivePointer));
            sb.AppendLine(string.Format("Root cell index: 0x{0:X}", RootCellIndex));

            sb.AppendLine();
            sb.AppendLine(string.Format("Subkey Counts Volatile: 0x{0:X}", SubkeyCountsVolatile));

            sb.AppendLine();
            sb.AppendLine(string.Format("User Flags: 0x{0:X}", UserFlags));
            sb.AppendLine(string.Format("Virtual Control Flags: 0x{0:X}", VirtualControlFlags));
            sb.AppendLine(string.Format("Work Var: 0x{0:X}", WorkVar));


            sb.AppendLine();
            sb.AppendLine(string.Format("Padding: {0}", BitConverter.ToString(Padding)));

            return sb.ToString();
        }
    }
}