using System;
using System.Linq;
using System.Text;

// namespaces...
namespace Registry.Other
{
    // internal classes...
    // public classes...
    public class DataNode:IRecordBase
    {
        // private fields...
        private readonly int _size;

        // public constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="DataNode"/> class.
        /// </summary>
        public DataNode(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;

            RawBytes = rawBytes;

            _size = BitConverter.ToInt32(rawBytes, 0);

            IsFree = _size > 0;

            Data = new byte[rawBytes.Length - 4];

            Signature = "";
            

            Array.Copy(rawBytes,4,Data,0,rawBytes.Length-4);

           // Data = rawBytes.Skip(4).ToArray();
        }

        // public properties...
        /// <summary>
        /// The offset to this record from the beginning of the hive, in bytes
        /// </summary>
        public long AbsoluteOffset
        {
            get
            {
                return RelativeOffset + 4096;
            }
        }

        public string Signature { get; private set; }

        // public properties...
        public byte[] Data { get; private set; }
        public bool IsFree { get; private set; }
        public byte[] RawBytes { get; private set; }
        /// <summary>
        /// Set to true when a record is referenced by another referenced record.
        /// <remarks>This flag allows for determining records that are marked 'in use' by their size but never actually referenced by another record in a hive</remarks>
        /// </summary>
        public bool IsReferenced { get; internal set; }

        /// <summary>
        /// The offset as stored in other records to a given record
        /// <remarks>This value will be 4096 bytes (the size of the regf header) less than the AbsoluteOffset</remarks>
        /// </summary>
        public long RelativeOffset { get; private set; }
        public int Size
        {
            get
            {
                return Math.Abs(_size);
            }
        }

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Size: 0x{0:X}", Math.Abs(_size)));
            sb.AppendLine(string.Format("Relative Offset: 0x{0:X}", RelativeOffset));
            sb.AppendLine(string.Format("Absolute Offset: 0x{0:X}", AbsoluteOffset));

            sb.AppendLine();

            sb.AppendLine(string.Format("Is Free: {0}", IsFree));

            sb.AppendLine();

            sb.AppendLine(string.Format("Raw Bytes: {0}", BitConverter.ToString(RawBytes)));
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
