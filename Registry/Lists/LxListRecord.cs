using NFluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// namespaces...
namespace Registry.Lists
{
    // internal classes...
    internal class LxListRecord : IListTemplate
    {
        // private fields...
        private Dictionary<uint, string> _offsets;
        private readonly int _size;

        // public constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="LFListRecord"/> or <see cref="LHListRecord"/>  class.
        /// <remarks>The signature determines how the hash is calculated/verified</remarks>
        /// </summary>
        /// <param name="rawBytes"></param>
        public LxListRecord(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;

            RawBytes = rawBytes;
            _size = BitConverter.ToInt32(rawBytes, 0);
            IsFree = _size > 0;

            NumberOfEntries = BitConverter.ToUInt16(rawBytes, 0x06);

            Signature = Encoding.ASCII.GetString(rawBytes, 4, 2);

            Check.That(Signature).IsOneOfThese("lh", "lf");

            _offsets = new Dictionary<uint, string>();

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

                var hash = string.Empty;

                if (Signature == "lf")
                {
                    //first 4 chars of string
                    hash = Encoding.ASCII.GetString(rawBytes, index, 4);
                }
                else
                {
                    //numerical hash
                    hash = BitConverter.ToUInt32(rawBytes, index).ToString();
                }


                index += 4;

                _offsets.Add(os, hash);

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
        public bool IsReferenceed { get; internal set; }
        public int NumberOfEntries { get; private set; }
        public Dictionary<uint, string> Offsets
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

            var i = 0;

            foreach (var offset in _offsets)
            {
                sb.AppendLine(string.Format("------------ Offset/hash record #{0} ------------", i));
                sb.AppendLine(string.Format("Offset: 0x{0:X}, Hash: {1}", offset.Key, offset.Value));
                i += 1;
            }
            sb.AppendLine();
            sb.AppendLine("------------ End of offsets ------------");
            sb.AppendLine();


            return sb.ToString();
        }
    }
}
