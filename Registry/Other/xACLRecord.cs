using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFluent;

// namespaces...

namespace Registry.Other
{
    // public classes...
    public class xACLRecord
    {
        // public enums...
        public enum ACLTypeEnum
        {
            Security,
            Discretionary
        }

        // public constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="xACLRecord" /> class.
        /// </summary>
        public xACLRecord(byte[] rawBytes, ACLTypeEnum aclTypetype)
        {
            RawBytes = rawBytes;

            ACLType = aclTypetype;
        }

        // public properties...
        public ushort AceCount
        {
            get { return BitConverter.ToUInt16(RawBytes, 0x4); }
        }

        public List<ACERecord> ACERecords
        {
            get
            {
                var index = 0x8; // the start of ACE structures

                var chunks = new List<byte[]>();

                for (var i = 0; i < AceCount; i++)
                {
                    if (index > RawBytes.Length)
                    {                   //ncrunch: no coverage
                        break;          //ncrunch: no coverage
                    }
                    var aceSize = RawBytes[index + 2];
                    var rawAce = RawBytes.Skip(index).Take(aceSize).ToArray();

                    chunks.Add(rawAce);

                    index += aceSize;
                }

                var records = new List<ACERecord>();

                foreach (var chunk in chunks)
                {
                    if (chunk.Length <= 0)
                    {
                        continue;
                    }

                    var ace = new ACERecord(chunk);

                    records.Add(ace);
                }

                return records;
            }
        }

        public byte AclRevision
        {
            get { return RawBytes[0]; }
        }

        public ushort AclSize
        {
            get { return BitConverter.ToUInt16(RawBytes, 0x2); }
        }

        public ACLTypeEnum ACLType { get;  private set;}
        public byte[] RawBytes { get;  private set;}

        public byte Sbz1
        {
            get { return RawBytes[1]; }
        }

        public ushort Sbz2
        {
            get { return BitConverter.ToUInt16(RawBytes, 0x6); }
        }

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("ACL Revision: 0x{0:X}", AclRevision));
            sb.AppendLine(string.Format("ACL Size: 0x{0:X}", AclSize));
            sb.AppendLine(string.Format("ACL Type: {0}", ACLType));
            sb.AppendLine(string.Format("Sbz1: 0x{0:X}", Sbz1));
            sb.AppendLine(string.Format("Sbz2: 0x{0:X}", Sbz2));

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