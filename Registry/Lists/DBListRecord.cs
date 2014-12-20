using NFluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// namespaces...
namespace Registry.Lists
{
    // internal classes...
    internal class DBListRecord : IListTemplate
    {
        // private fields...
        private readonly int _size;

        // public constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="DBListRecord"/>  class.
        /// </summary>
        /// <param name="rawBytes"></param>
        public DBListRecord(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;
            RawBytes = rawBytes;
            _size = BitConverter.ToInt32(rawBytes, 0);
            IsFree = _size > 0;

            if (IsFree)
            {
                return;
            }

            NumberOfEntries = BitConverter.ToUInt16(rawBytes, 0x06);

            Signature = Encoding.ASCII.GetString(rawBytes, 4, 2);

            Check.That(Signature).IsEqualTo("db");
            
            OffsetToOffsets = BitConverter.ToUInt32(rawBytes, 0x8);

            //TODO add slack support here (or is it always 00 00 00 00)

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
        public bool IsFree { get; private set; }
        public bool IsReferenceed { get; internal set; }
        public int NumberOfEntries { get; private set; }
        public uint OffsetToOffsets { get; private set; }
        public byte[] RawBytes { get; private set; }
        public long RelativeOffset { get; private set; }
        public string Signature { get; private set; }
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
            sb.AppendLine(string.Format("RelativeOffset: 0x{0:X}", RelativeOffset));
            sb.AppendLine(string.Format("AbsoluteOffset: 0x{0:X}", AbsoluteOffset));
            
            sb.AppendLine(string.Format("Signature: {0}", Signature));

            sb.AppendLine();

            sb.AppendLine(string.Format("IsFree: {0}", IsFree));

            //if (IsFree)
            //{
            //    return sb.ToString();
            //}
            sb.AppendLine();

            sb.AppendLine(string.Format("NumberOfEntries: {0:N0}", NumberOfEntries));
            sb.AppendLine();

            sb.AppendLine(string.Format("OffsetToOffsets: 0x{0:X}", OffsetToOffsets));


            sb.AppendLine();


            return sb.ToString();
        }
    }
}
