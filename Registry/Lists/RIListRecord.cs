using System;
using System.Collections.Generic;
using System.Text;
using NFluent;
using Registry.Other;
using static Registry.Other.Helpers;

// namespaces...

namespace Registry.Lists
{
    // internal classes...
    public class RIListRecord : IListTemplate, IRecordBase
    {
        // private fields...
        private readonly int _size;
        // public constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="RIListRecord" />  class.
        /// </summary>
        /// <param name="rawBytes"></param>
        /// <param name="relativeOffset"></param>
        public RIListRecord(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;

            RawBytes = rawBytes;
            _size = BitConverter.ToInt32(rawBytes, 0);
        }

        /// <summary>
        ///     A list of relative offsets to other records
        /// </summary>
        public List<uint> Offsets
        {
            get
            {
                var offsets = new List<uint>();

                var index = 0x8;
                var counter = 0;

                while (counter < NumberOfEntries)
                {
                    if (index >= RawBytes.Length)
                    {                               //ncrunch: no coverage
                        // i have seen cases where there isnt enough data, so get what we can
                        break;                      //ncrunch: no coverage
                    }

                    var os = BitConverter.ToUInt32(RawBytes, index);
                    index += 4;

                    offsets.Add(os);

                    counter += 1;
                }
                return offsets;
            }
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

        public byte[] RawBytes { get;  private set;}
        public long RelativeOffset { get;  private set;}

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

            var i = 0;

            foreach (var offset in Offsets)
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