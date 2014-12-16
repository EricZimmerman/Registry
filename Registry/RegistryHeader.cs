using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFluent;

// namespaces...
namespace Registry
{
    // public classes...
    public class RegistryHeader
    {
        // protected internal constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="RegistryHeader"/> class.
        /// </summary>
        protected internal RegistryHeader(byte[] rawBytes)
        {
            Signature = Encoding.ASCII.GetString(rawBytes, 0, 4);

            Check.That(Signature).IsEqualTo("regf");

            Sequence1 = BitConverter.ToUInt32(rawBytes, 0x4);
            Sequence2 = BitConverter.ToUInt32(rawBytes, 0x8);

            var ts = BitConverter.ToInt64(rawBytes, 0xc);

            LastWriteTimestamp = DateTimeOffset.FromFileTime(ts);

            MajorVersion = BitConverter.ToUInt32(rawBytes, 0x14);
            MinorVersion = BitConverter.ToUInt32(rawBytes, 0x18);

            Type = BitConverter.ToUInt32(rawBytes, 0x1c);

            Format = BitConverter.ToUInt32(rawBytes, 0x20);

            RootCellOffset = BitConverter.ToUInt32(rawBytes, 0x24);

            Length = BitConverter.ToUInt32(rawBytes, 0x28);

            Cluster = BitConverter.ToUInt32(rawBytes, 0x2c);

            FileName = Encoding.Unicode.GetString(rawBytes, 0x30, 64).Replace("\0", string.Empty).Replace("\\??\\", string.Empty);

            CheckSum = BitConverter.ToUInt32(rawBytes, 0x1fc); //TODO 4.27 The “regf” Checksum 

            BootType = BitConverter.ToUInt32(rawBytes, 0xff8);
            BootRecover = BitConverter.ToUInt32(rawBytes, 0xffc);
        }

        // public properties...
        public uint BootRecover { get; private set; }
        public uint BootType { get; private set; }
        public uint CheckSum { get; private set; }
        public uint Cluster { get; private set; }
        /// <summary>
        /// Registry hive's embedded filename
        /// </summary>
        public string FileName { get; private set; }
        public uint Format { get; private set; }
        public uint Length { get; private set; }
        public uint MajorVersion { get; private set; }
        public uint MinorVersion { get; private set; }

        /// <summary>
        /// The offset in the first hbin record where root key is found
        /// </summary>
        public uint RootCellOffset { get; private set; }

        public uint Sequence1 { get; private set; }
        public uint Sequence2 { get; private set; }

        /// <summary>
        /// Signature of the registry hive. Should always be "regf"
        /// </summary>
        public string Signature { get; private set; }
        /// <summary>
        /// The last write timestamp of the registry hive
        /// </summary>
        public DateTimeOffset LastWriteTimestamp { get; private set; }
        public uint Type { get; private set; }
    }
}
