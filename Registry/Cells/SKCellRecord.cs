using NFluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// namespaces...
namespace Registry.Cells
{
    // public classes...
    public class SKCellRecord : ICellTemplate
    {
        // private fields...
        private readonly int _size;

        // protected internal constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="NKCellRecord"/> class.
        /// </summary>
        protected internal SKCellRecord(byte[] rawBytes)
        {
            RawBytes = rawBytes;

            _size = BitConverter.ToInt32(rawBytes, 0);

            IsFree = _size > 0;

            Signature = Encoding.ASCII.GetString(rawBytes, 4, 2);

            Check.That(Signature).IsEqualTo("sk");

            Reserved = BitConverter.ToUInt16(rawBytes, 0x6);

            FLink = BitConverter.ToUInt32(rawBytes, 0x08);
            BLink = BitConverter.ToUInt32(rawBytes, 0x0c);

            ReferenceCount = BitConverter.ToUInt32(rawBytes, 0x10);

            DescriptorLength = BitConverter.ToUInt32(rawBytes, 0x14);

            var rawDescriptor = rawBytes.Skip(0x18).Take((int)DescriptorLength).ToArray();

            SecurityDescriptor = new SKSecurityDescriptor(rawDescriptor);
        }

        // public properties...
        public uint BLink { get; private set; }
        public uint DescriptorLength { get; private set; }
        public uint FLink { get; private set; }
        public bool IsFree { get; private set; }
        public byte[] RawBytes { get; private set; }
        public uint ReferenceCount { get; private set; }
        public ushort Reserved { get; private set; }
        public SKSecurityDescriptor SecurityDescriptor { get; private set; }
        public string Signature { get; private set; }
        // public properties...
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

            sb.AppendLine(string.Format("Size: 0x{0:X}", Size));
            sb.AppendLine(string.Format("Signature: {0}", Signature));

            sb.AppendLine(string.Format("IsFree: {0}", IsFree));

            sb.AppendLine();
            sb.AppendLine(string.Format("FLink: 0x{0:X}", FLink));
            sb.AppendLine(string.Format("BLink: 0x{0:X}", BLink));
            sb.AppendLine();

            sb.AppendLine(string.Format("ReferenceCount: {0:N0}", ReferenceCount));

            sb.AppendLine();
            sb.AppendLine(string.Format("Security descriptor length: 0x{0:X}", DescriptorLength));

            sb.AppendLine();
            sb.AppendLine(string.Format("Security descriptor: {0}", SecurityDescriptor));



            return sb.ToString();
        }
    }
}
