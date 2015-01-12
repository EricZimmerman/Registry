using System;
using System.Collections.Generic;
using System.Text;
using NFluent;

// namespaces...
namespace Registry.Lists
{
    // internal classes...
    internal class RIListRecord : IListTemplate
    {
        // private fields...
        private List<uint> _offsets;
        private readonly int _size;

        // public constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="RIListRecord"/>  class.
        /// </summary>
        /// <param name="rawBytes"></param>
        /// <param name="relativeOffset"></param>
        public RIListRecord(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;

            RawBytes = rawBytes;
            _size = BitConverter.ToInt32(rawBytes, 0);
            IsFree = _size > 0;

            if (IsFree)
            {
                return;
            }
            
            Signature = Encoding.ASCII.GetString(rawBytes, 4, 2);

            Check.That(Signature).IsEqualTo("ri");

            NumberOfEntries = BitConverter.ToUInt16(rawBytes, 0x06);

            _offsets = new List<uint>();

            var index = 0x8;
            var counter = 0;

            while (counter < NumberOfEntries)
            {
                if (index >= rawBytes.Length)
                {
                    // i have seen cases where there isnt enough data, so get what we can
                    break;
                }

                var os = BitConverter.ToUInt32(rawBytes, index);
                index += 4;
                
                _offsets.Add(os);

                counter += 1;
            }
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
        public bool IsReferenced { get; internal set; }
        public int NumberOfEntries { get; private set; }

        /// <summary>
        /// A list of relative offsets to other records
        /// </summary>
        public List<uint> Offsets
        {
            get
            {
                return _offsets;
            }
        }

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
            sb.AppendLine(string.Format("Relative Offset: 0x{0:X}", RelativeOffset));
            sb.AppendLine(string.Format("Absolute Offset: 0x{0:X}", AbsoluteOffset));
            sb.AppendLine(string.Format("Signature: {0}", Signature));

            sb.AppendLine();

            sb.AppendLine(string.Format("Is Free: {0}", IsFree));

            sb.AppendLine();

            sb.AppendLine(string.Format("Number Of Entries: {0:N0}", NumberOfEntries));
            sb.AppendLine();

            var i = 0;

            foreach (var offset in _offsets)
            {
                sb.AppendLine(string.Format("------------ Offset/hash record #{0} ------------", i));
                sb.AppendLine(string.Format("Offset: 0x{0:X}", offset));
                i += 1;
            }
            sb.AppendLine();
            sb.AppendLine("------------ End of offsets ------------");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
