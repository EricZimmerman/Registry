using System;
using System.Linq;
using System.Text;


// namespaces...

namespace Registry.Other
{
    // public classes...
    public class SKSecurityDescriptor
    {
        // public enums...
        [Flags]
        public enum ControlEnum
        {
            SeDaclAutoInherited = 0x0400,
            SeDaclAutoInheritReq = 0x0100,
            SeDaclDefaulted = 0x0008,
            SeDaclPresent = 0x0004,
            SeDaclProtected = 0x1000,
            SeGroupDefaulted = 0x0002,
            SeOwnerDefaulted = 0x0001,
            SeRmControlValid = 0x4000,
            SeSaclAutoInherited = 0x0800,
            SeSaclAutoInheritReq = 0x0200,
            SeSaclDefaulted = 0x0020,
            SeSaclPresent = 0x0010,
            SeSaclProtected = 0x2000,
            SeSelfRelative = 0x8000
        }

        private readonly uint sizeDacl;
        private readonly uint sizeGroupSid;
        private readonly uint sizeOwnerSid;
        private readonly uint sizeSacl;
        // public constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="SKSecurityDescriptor" /> class.
        /// </summary>
        public SKSecurityDescriptor(byte[] rawBytes)
        {
            RawBytes = rawBytes;

            sizeSacl = DaclOffset - SaclOffset;
            sizeDacl = OwnerOffset - DaclOffset;
            sizeOwnerSid = GroupOffset - OwnerOffset;
            sizeGroupSid = (uint) (rawBytes.Length - GroupOffset);


            Padding = String.Empty; //TODO VERIFY ITS ALWAYS ZEROs
        }

        // public properties...
        public ControlEnum Control
        {
            get { return (ControlEnum) BitConverter.ToUInt16(RawBytes, 0x02); }
        }

        public xACLRecord DACL
        {
            get
            {
                if ((Control & ControlEnum.SeDaclPresent) == ControlEnum.SeDaclPresent)
                {
                    var rawDacl = RawBytes.Skip((int) DaclOffset).Take((int) sizeDacl).ToArray();
                    return new xACLRecord(rawDacl, xACLRecord.ACLTypeEnum.Discretionary);
                }

                return null; //ncrunch: no coverage

            }
        }

        public uint DaclOffset
        {
            get { return BitConverter.ToUInt32(RawBytes, 0x10); }
        }

        public uint GroupOffset
        {
            get { return BitConverter.ToUInt32(RawBytes, 0x08); }
        }

        public string GroupSID
        {
            get
            {
                var rawGroup = RawBytes.Skip((int) GroupOffset).Take((int) sizeGroupSid).ToArray();
                return Helpers.ConvertHexStringToSidString(rawGroup);
            }
        }
        public Helpers.SidTypeEnum GroupSIDType
        {
            get { return Helpers.GetSIDTypeFromSIDString(GroupSID); }
        }

        public uint OwnerOffset
        {
            get { return BitConverter.ToUInt32(RawBytes, 0x04); }
        }

        public string OwnerSID
        {
            get
            {
                var rawOwner = RawBytes.Skip((int) OwnerOffset).Take((int) sizeOwnerSid).ToArray();
                return Helpers.ConvertHexStringToSidString(rawOwner);
            }
        }

        public Helpers.SidTypeEnum OwnerSIDType
        {
            get { return Helpers.GetSIDTypeFromSIDString(OwnerSID); }
        }

        public string Padding { get; private set; }
        public byte[] RawBytes { get;  private set;}

        public byte Revision
        {
            get { return RawBytes[0]; }
        }

        public xACLRecord SACL
        {
            get
            {
                if ((Control & ControlEnum.SeSaclPresent) == ControlEnum.SeSaclPresent)
                {
                    var rawSacl = RawBytes.Skip((int) SaclOffset).Take((int) sizeSacl).ToArray();
                    return new xACLRecord(rawSacl, xACLRecord.ACLTypeEnum.Security);
                }
                return null;
            }
        }

        public uint SaclOffset
        {
            get { return BitConverter.ToUInt32(RawBytes, 0x0c); }
        }

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Revision: 0x{Revision:X}");
            sb.AppendLine($"Control: {Control}");

            sb.AppendLine();
            sb.AppendLine($"Owner offset: 0x{OwnerOffset:X}");
            sb.AppendLine($"Owner SID: {OwnerSID}");
            sb.AppendLine($"Owner SID Type: {OwnerSIDType}");

            sb.AppendLine();
            sb.AppendLine($"Group offset: 0x{GroupOffset:X}");
            sb.AppendLine($"Group SID: {GroupSID}");
            sb.AppendLine($"Group SID Type: {GroupSIDType}");

            if (DACL != null)
            {
                sb.AppendLine();
                sb.AppendLine($"Dacl Offset: 0x{DaclOffset:X}");
                sb.AppendLine($"DACL: {DACL}");
            }

            if (SACL != null)
            {
                sb.AppendLine();
                sb.AppendLine($"Sacl Offset: 0x{SaclOffset:X}");
                sb.AppendLine($"SACL: {SACL}");
            }

            return sb.ToString();
        }
    }
}