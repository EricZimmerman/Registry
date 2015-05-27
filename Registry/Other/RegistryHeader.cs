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

            sb.AppendLine($"Signature: {Signature}");

            sb.AppendLine($"FileName: {FileName}");

            sb.AppendLine();

            sb.AppendLine($"Sequence1: 0x{Sequence1:X}");
            sb.AppendLine($"Sequence2: 0x{Sequence2:X}");

            sb.AppendLine();

            sb.AppendLine($"Last Write Timestamp: {LastWriteTimestamp}");

            sb.AppendLine();

            sb.AppendLine($"Major version: {MajorVersion}");
            sb.AppendLine($"Minor version: {MinorVersion}");

            sb.AppendLine();
            sb.AppendLine($"Type: 0x{Type:X}");
            sb.AppendLine($"Format: 0x{Format:X}");

            sb.AppendLine();
            sb.AppendLine($"RootCellOffset: 0x{RootCellOffset:X}");

            sb.AppendLine();
            sb.AppendLine($"Length: 0x{Length:X}");

            sb.AppendLine();
            sb.AppendLine($"Cluster: 0x{Cluster:X}");

            sb.AppendLine();
            sb.AppendLine($"CheckSum: 0x{CheckSum:X}");
            sb.AppendLine($"CheckSum: 0x{CalculatedChecksum:X}");
            sb.AppendLine($"CheckSums match: {CalculatedChecksum == CheckSum}");

            sb.AppendLine();
            sb.AppendLine($"BootType: 0x{BootType:X}");
            sb.AppendLine($"BootRecover: 0x{BootRecover:X}");

            return sb.ToString();
        }
    }
}