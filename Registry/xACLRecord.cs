using NFluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// namespaces...
namespace Registry
{
    // public classes...
    public class xACLRecord
    {
        // public constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="xACLRecord"/> class.
        /// </summary>
        public xACLRecord(byte[] rawBytes, ACLTypeEnum aclTypetype)
        {
            RawBytes = rawBytes;

            ACLType = aclTypetype;

            AclRevision = rawBytes[0];

            var rev = (int)AclRevision;

            Check.That(rev.ToString()).IsOneOfThese("2", "4");

            Sbz1 = rawBytes[1];

            AclSize = BitConverter.ToUInt16(rawBytes, 0x2);

            AceCount = BitConverter.ToUInt16(rawBytes, 0x4);
            Sbz2 = BitConverter.ToUInt16(rawBytes, 0x6);

            var index = 0x8; // the start of ACE structures

            var chunks = new List<byte[]>();

            for (var i = 0; i < AceCount; i++)
            {
                var aceSize = rawBytes[index + 2];
                var rawAce = RawBytes.Skip(index).Take(aceSize).ToArray();

                chunks.Add(rawAce);

                index += aceSize;
            }

            ACERecords = new List<ACERecord>();

            foreach (var chunk in chunks)
            {
                var ace = new ACERecord(chunk);

                ACERecords.Add(ace);
            }
        }

        // public enums...
        public enum ACLTypeEnum
        {
            Security,
            Discretionary
        }

        // public properties...
        public ushort AceCount { get; private set; }
        public List<ACERecord> ACERecords { get; private set; }
        public byte AclRevision { get; private set; }
        public ushort AclSize { get; private set; }
        public ACLTypeEnum ACLType { get; private set; }
        public byte[] RawBytes { get; private set; }
        public byte Sbz1 { get; private set; }
        public ushort Sbz2 { get; private set; }

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("ACL Size: 0x{0:X}", AclRevision));
            sb.AppendLine(string.Format("ACL Type: {0}", ACLType));


            sb.AppendLine(string.Format("ACE Records Count: {0}", AceCount));


            sb.AppendLine();

            var i = 0;
            foreach (var aceRecord in ACERecords)
            {
                sb.AppendLine(string.Format("------------ Ace record #{0} ------------", i));
                sb.AppendLine(aceRecord.ToString());
                i += 1;
            }

            return sb.ToString();
        }
    }
}
