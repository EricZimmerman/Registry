using System;
using System.Text;
using NFluent;

// namespaces...

namespace Registry.Other
{
    // public classes...
    public class RegistryHeader
    {
        public int CalculatedChecksum;
        // protected internal constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="RegistryHeader" /> class.
        /// </summary>
        protected internal RegistryHeader(byte[] rawBytes)
        {
            FileName = string.Empty;
            Signature = Encoding.ASCII.GetString(rawBytes, 0, 4);

            Check.That(Signature).IsEqualTo("regf");

            Sequence1 = BitConverter.ToUInt32(rawBytes, 0x4);
            Sequence2 = BitConverter.ToUInt32(rawBytes, 0x8);

            var ts = BitConverter.ToInt64(rawBytes, 0xc);

            LastWriteTimestamp = DateTimeOffset.FromFileTime(ts).ToUniversalTime();

            MajorVersion = BitConverter.ToInt32(rawBytes, 0x14);
            MinorVersion = BitConverter.ToInt32(rawBytes, 0x18);

            Type = BitConverter.ToUInt32(rawBytes, 0x1c);

            Format = BitConverter.ToUInt32(rawBytes, 0x20);

            RootCellOffset = BitConverter.ToUInt32(rawBytes, 0x24);

            Length = BitConverter.ToUInt32(rawBytes, 0x28);

            Cluster = BitConverter.ToUInt32(rawBytes, 0x2c);

            FileName = Encoding.Unicode.GetString(rawBytes, 0x30, 64)
                .Replace("\0", string.Empty)
                .Replace("\\??\\", string.Empty);

            CheckSum = BitConverter.ToInt32(rawBytes, 0x1fc); 

            var index = 0;
            var xsum = 0;
            while (index <= 0x1fb)
            {
                xsum ^= BitConverter.ToInt32(rawBytes, index);
                index += 0x04;
            }
            CalculatedChecksum = xsum;

            BootType = BitConverter.ToUInt32(rawBytes, 0xff8);
            BootRecover = BitConverter.ToUInt32(rawBytes, 0xffc);
        }

        // public properties...
        public uint BootRecover { get; private set; }
        public uint BootType { get; private set; }
        public int CheckSum { get; private set; }
        public uint Cluster { get; private set; }

        /// <summary>
        ///     Registry hive's embedded filename
        /// </summary>
        public string FileName { get; private set; }

        public uint Format { get; private set; }

        /// <summary>
        ///     The last write timestamp of the registry hive
        /// </summary>
        public DateTimeOffset LastWriteTimestamp { get; private set; }

        /// <summary>
        ///     The total number of bytes used by this hive
        /// </summary>
        public uint Length { get; private set; }

        public int MajorVersion { get; private set; }
        public int MinorVersion { get; private set; }

        /// <summary>
        ///     The offset in the first hbin record where root key is found
        /// </summary>
        public uint RootCellOffset { get; private set; }

        public uint Sequence1 { get; private set; }
        public uint Sequence2 { get; private set; }

        /// <summary>
        ///     Signature of the registry hive. Should always be "regf"
        /// </summary>
        public string Signature { get;  private set;}

        public uint Type { get;  private set;}

        public bool ValidateCheckSum()
        {
            return CheckSum == CalculatedChecksum;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Signature: {0}", Signature));

            sb.AppendLine(string.Format("FileName: {0}", FileName));

            sb.AppendLine();

            sb.AppendLine(string.Format("Sequence1: 0x{0:X}", Sequence1));
            sb.AppendLine(string.Format("Sequence2: 0x{0:X}", Sequence2));

            sb.AppendLine();

            sb.AppendLine(string.Format("Last Write Timestamp: {0}", LastWriteTimestamp));

            sb.AppendLine();

            sb.AppendLine(string.Format("Major version: {0}", MajorVersion));
            sb.AppendLine(string.Format("Minor version: {0}", MinorVersion));

            sb.AppendLine();
            sb.AppendLine(string.Format("Type: 0x{0:X}", Type));
            sb.AppendLine(string.Format("Format: 0x{0:X}", Format));

            sb.AppendLine();
            sb.AppendLine(string.Format("RootCellOffset: 0x{0:X}", RootCellOffset));

            sb.AppendLine();
            sb.AppendLine(string.Format("Length: 0x{0:X}", Length));

            sb.AppendLine();
            sb.AppendLine(string.Format("Cluster: 0x{0:X}", Cluster));

            sb.AppendLine();
            sb.AppendLine(string.Format("CheckSum: 0x{0:X}", CheckSum));
            sb.AppendLine(string.Format("CheckSum: 0x{0:X}", CalculatedChecksum));
            sb.AppendLine(string.Format("CheckSums match: {0}", CalculatedChecksum == CheckSum));

            sb.AppendLine();
            sb.AppendLine(string.Format("BootType: 0x{0:X}", BootType));
            sb.AppendLine(string.Format("BootRecover: 0x{0:X}", BootRecover));

            return sb.ToString();
        }
    }
}