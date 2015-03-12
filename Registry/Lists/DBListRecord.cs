using System;
using System.Text;
using NFluent;
using Registry.Other;
using static Registry.Other.Helpers;

// namespaces...

namespace Registry.Lists
{
    // internal classes...
    public class DBListRecord : IListTemplate, IRecordBase
    {
        // private fields...
        private readonly int _size;
        // public constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="DBListRecord" />  class.
        /// </summary>
        /// <param name="rawBytes"></param>
        /// <param name="relativeOffset"></param>
        public DBListRecord(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;
            RawBytes = rawBytes;
            _size = BitConverter.ToInt32(rawBytes, 0);

        }

        /// <summary>
        ///     The relative offset to another data node that contains a list of relative offsets to data for a VK record
        /// </summary>
        public uint OffsetToOffsets
        {
            get { return BitConverter.ToUInt32(RawBytes, 0x8); }
        }

        // public properties...

        public bool IsFree
        {
            get { return _size > 0; }
        }

        public bool IsReferenced { get;  set; }

        public int NumberOfEntries
        {
            get { return BitConverter.ToUInt16(RawBytes, 0x06); }
        }

        public byte[] RawBytes { get;  set; }
        public long RelativeOffset { get; set; }

        public string Signature
        {
            get { return Encoding.ASCII.GetString(RawBytes, 4, 2); }
        }

        public int Size
        {
            get { return Math.Abs(_size); }
        }

        // public properties...
        public long AbsoluteOffset
        {
            get { return RelativeOffset + 4096; }
        }

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Size: 0x{0:X}", Size));
            sb.AppendLine(string.Format("Relative Offset: 0x{0:X}", RelativeOffset));
            sb.AppendLine(string.Format("Absolute Offset: 0x{0:X}", AbsoluteOffset));

            sb.AppendLine(string.Format("Signature: {0}", Signature));

            sb.AppendLine();

            sb.AppendLine(string.Format("Is Free: {0}", IsFree));

            sb.AppendLine();

            sb.AppendLine(string.Format("Number Of Entries: {0:N0}", NumberOfEntries));
            sb.AppendLine();

            sb.AppendLine(string.Format("Offset To Offsets: 0x{0:X}", OffsetToOffsets));

            sb.AppendLine();

            return sb.ToString();
        }
    }
}