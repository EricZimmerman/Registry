using System;
using System.Linq;
using System.Text;

// namespaces...
namespace Registry.Other
{
    // internal classes...
    //TODO DO I NEED THIS?
    public class DataNode
    {
        // private fields...
        private readonly int _size;

        // public constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="DataNode"/> class.
        /// </summary>
        public DataNode(byte[] rawBytes, long absoluteOffset)
        {
            AbsoluteOffset = absoluteOffset;

            RawBytes = rawBytes;

            _size = BitConverter.ToInt32(rawBytes, 0);

            IsFree = _size > 0;


            Data = rawBytes.Skip(4).ToArray();
        }

        // public properties...
        public byte[] Data { get; private set; }
        public bool IsFree { get; private set; }
        public byte[] RawBytes { get; private set; }
        public int Size
        {
            get
            {
                return Math.Abs(_size);
            }
        }

        public long AbsoluteOffset { get; private set; }



        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Size: 0x{0:X}", Math.Abs(_size)));
            sb.AppendLine(string.Format("AbsoluteOffset: 0x{0:X}", AbsoluteOffset));
         
            sb.AppendLine();

            sb.AppendLine(string.Format("IsFree: {0}", IsFree));

            sb.AppendLine();

            sb.AppendLine(string.Format("RawBytes: {0}", BitConverter.ToString(RawBytes)));
            sb.AppendLine();




            return sb.ToString();
        }
    }

}
