using System;
using System.Collections.Generic;
using System.Text;
using NFluent;
using Registry.Other;

// namespaces...

namespace Registry.Cells
{
    // public classes...
    public class NKCellRecord : ICellTemplate, IRecordBase
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
        protected internal NKCellRecord(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;
            RawBytes = rawBytes;

            ValueOffsets = new List<ulong>();

            _size = BitConverter.ToInt32(rawBytes, 0);

            Check.That(Signature).IsEqualTo("nk");

            var paddingOffset = 0x50 + NameLength;

            var paddingBlock = (int) Math.Ceiling((double) paddingOffset/8);

            var actualPaddingOffset = paddingBlock*8;

            var paddingLength = actualPaddingOffset - paddingOffset;

            Padding = new byte[paddingLength];

            if (paddingLength > 0 & !IsFree)
            {
                Array.Copy(rawBytes, paddingOffset, Padding, 0, paddingLength);
            }

            //Check that we have accounted for all bytes in this record. this ensures nothing is hidden in this record or there arent additional data structures we havent processed in the record.

//            if (!IsFree)
//            {
//                //When records ARE free, different rules apply, so we process thsoe all at once later
//                Check.That(actualPaddingOffset).IsEqualTo(rawBytes.Length);
//            }
        }


        /// <summary>
        ///     The relative offset to a data node containing the classname
        ///     <remarks>
        ///         Use ClassLength to get the correct classname vs using the entire contents of the data node. There is often
        ///         slack slace in the data node when they hold classnames
        ///     </remarks>
        /// </summary>
        public uint ClassCellIndex
        {
            get
            {
                var num = BitConverter.ToUInt32(RawBytes, 0x34);

                if (num == 0xFFFFFFFF)
                {
                    return 0;
                }
                return num;
            }
        }

        /// <summary>
        ///     The length of the classname in the data node referenced by ClassCellIndex.
        /// </summary>
        public ushort ClassLength
        {
            get { return BitConverter.ToUInt16(RawBytes, 0x4e); }
        }

        public byte Debug
        {
            get { return RawBytes[0x3b]; }
        }

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

        public uint MaximumClassLength
        {
            get { return BitConverter.ToUInt32(RawBytes, 0x3c); }
        }

        public ushort MaximumNameLength
        {
            get { return BitConverter.ToUInt16(RawBytes, 0x38); }
        }

        public uint MaximumValueDataLength
        {
            get { return BitConverter.ToUInt32(RawBytes, 0x44); }
        }

        public uint MaximumValueNameLength
        {
            get { return BitConverter.ToUInt32(RawBytes, 0x40); }
        }

        /// <summary>
        ///     The name of this key. This is what is shown on the left side of RegEdit in the key and subkey tree.
        /// </summary>
        public string Name
        {
            get
            {
                string _name;

                if ((Flags & FlagEnum.CompressedName) == FlagEnum.CompressedName)
                {
                    if (IsFree)
                    {
                        if (RawBytes.Length >= 0x50 + NameLength)
                        {
                            _name = Encoding.GetEncoding(1252).GetString(RawBytes, 0x50, NameLength);
                        }
                        else
                        {
                            _name = "(Unable to determine name)";
                        }
                    }
                    else
                    {
                        _name = Encoding.GetEncoding(1252).GetString(RawBytes, 0x50, NameLength);
                    }
                }
                else
                {
                    if (IsFree)
                    {
                        if (RawBytes.Length >= 0x50 + NameLength)
                        {
                            _name = Encoding.Unicode.GetString(RawBytes, 0x50, NameLength);
                        }
                        else
                        {
                            _name = "(Unable to determine name)";
                        }
                    }
                    else
                    {
                        _name = Encoding.Unicode.GetString(RawBytes, 0x50, NameLength);
                    }
                }

                return _name;
            }
        }

        public ushort NameLength
        {
            get { return BitConverter.ToUInt16(RawBytes, 0x4c); }
        }

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
        public uint SecurityCellIndex
        {
            get { return BitConverter.ToUInt32(RawBytes, 0x30); }
        }

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
        ///     The relative offset to a list (or list of lists) that points to other NKRecords. These records are subkeys of this
        ///     key.
        /// </summary>
        public uint SubkeyListsStableCellIndex
        {
            get
            {
                var num = BitConverter.ToUInt32(RawBytes, 0x20);

                if (num == 0xFFFFFFFF)
                {
                    return 0;
                }
                return num;
            }
        }

        public uint SubkeyListsVolatileCellIndex
        {
            get
            {
                var num = BitConverter.ToUInt32(RawBytes, 0x24);

                if (num == 0xFFFFFFFF)
                {
                    return 0;
                }
                return num;
            }
        }

        public int UserFlags
        {
            get
            {
                var rawFlags = Convert.ToString(RawBytes[0x3a], 2).PadLeft(8, '0');

                return Convert.ToInt32(rawFlags.Substring(0, 4));
            }
        }

        /// <summary>
        ///     The relative offset to a list of VKrecords for this key
        /// </summary>
        public uint ValueListCellIndex
        {
            get
            {
                var num = BitConverter.ToUInt32(RawBytes, 0x2c);

                if (num == 0xFFFFFFFF)
                {
                    return 0;
                }
                return num;
            }
        }

        /// <summary>
        ///     The number of values this key contains
        /// </summary>
        public uint ValueListCount
        {
            get { return BitConverter.ToUInt32(RawBytes, 0x28); }
        }

        public int VirtualControlFlags
        {
            get
            {
                var rawFlags = Convert.ToString(RawBytes[0x3a], 2).PadLeft(8, '0');

                return Convert.ToInt32(rawFlags.Substring(4, 4));
            }
        }

        public uint WorkVar
        {
            get { return BitConverter.ToUInt32(RawBytes, 0x48); }
        }

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
            get { return Encoding.GetEncoding(1252).GetString(RawBytes, 4, 2); }
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
            sb.AppendLine(string.Format("Subkey Lists Stable Cell Index: 0x{0:X}", SubkeyListsStableCellIndex));

            sb.AppendLine();
            sb.AppendLine(string.Format("Subkey Counts Volatile: 0x{0:X}", SubkeyCountsVolatile));

            sb.AppendLine();
            sb.AppendLine(string.Format("User Flags: 0x{0:X}", UserFlags));
            sb.AppendLine(string.Format("Virtual Control Flags: 0x{0:X}", VirtualControlFlags));
            sb.AppendLine(string.Format("Work Var: 0x{0:X}", WorkVar));

            sb.AppendLine();
            sb.AppendLine(string.Format("Value Count: 0x{0:X}", ValueListCount));
            sb.AppendLine(string.Format("Value List Cell Index: 0x{0:X}", ValueListCellIndex));

            if (Padding.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine(string.Format("Padding: {0}", BitConverter.ToString(Padding)));
            }

            return sb.ToString();
        }
    }
}