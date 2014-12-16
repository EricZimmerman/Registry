using System;
using System.Linq;
using System.Text;

// namespaces...
namespace Registry
{
    // public classes...
    public class SKSecurityDescriptor
    {
        // public constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="SKSecurityDescriptor"/> class.
        /// </summary>
        public SKSecurityDescriptor(byte[] rawBytes)
        {
            RawBytes = rawBytes;

            Revision = rawBytes[0];

            Control = (ControlEnum)BitConverter.ToUInt16(rawBytes, 0x02);

            OwnerOffset = BitConverter.ToUInt32(rawBytes, 0x04);
            GroupOffset = BitConverter.ToUInt32(rawBytes, 0x08);

            SaclOffset = BitConverter.ToUInt32(rawBytes, 0x0c);
            DaclOffset = BitConverter.ToUInt32(rawBytes, 0x10);

            var sizeSacl = DaclOffset - SaclOffset;
            var sizeDacl = OwnerOffset - DaclOffset;
            var sizeOwnerSid = GroupOffset - OwnerOffset;
            var sizeGroupSid = rawBytes.Length - GroupOffset;

            var rawOwner = rawBytes.Skip((int)OwnerOffset).Take((int)sizeOwnerSid).ToArray();
            var rawGroup = rawBytes.Skip((int)GroupOffset).Take((int)sizeGroupSid).ToArray();

            OwnerSID = Helpers.ConvertHexStringToSidString(rawOwner);
            GroupSID = Helpers.ConvertHexStringToSidString(rawGroup);

            OwnerSIDType = Helpers.GetSIDTypeFromSIDString(OwnerSID);
            GroupSIDType = Helpers.GetSIDTypeFromSIDString(GroupSID);


            //((myProperties.AllowedColors & MyColor.Yellow) == MyColor.Yellow)
            if ((Control & ControlEnum.SeDaclPresent) == ControlEnum.SeDaclPresent)
            {
                var rawDacl = rawBytes.Skip((int)DaclOffset).Take((int)sizeDacl).ToArray();
                DACL = new xACLRecord(rawDacl, xACLRecord.ACLTypeEnum.Discretionary);
            }

            if ((Control & ControlEnum.SeSaclPresent) == ControlEnum.SeSaclPresent)
            {
                var rawSacl = rawBytes.Skip((int)SaclOffset).Take((int)sizeSacl).ToArray();
                SACL = new xACLRecord(rawSacl, xACLRecord.ACLTypeEnum.Security);
            }


            Padding = String.Empty; //TODO VERIFY ITS ALWAYS ZEROs
        }

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

        // public properties...
        public ControlEnum Control { get; private set; }
        public xACLRecord DACL { get; private set; }
        public uint DaclOffset { get; private set; }
        public uint GroupOffset { get; private set; }
        public string GroupSID { get; private set; }
        public Helpers.SidTypeEnum GroupSIDType { get; private set; }
        public uint OwnerOffset { get; private set; }
        public string OwnerSID { get; private set; }
        public Helpers.SidTypeEnum OwnerSIDType { get; private set; }
        public string Padding { get; private set; }
        public byte[] RawBytes { get; private set; }
        public byte Revision { get; private set; }
        public xACLRecord SACL { get; private set; }
        public uint SaclOffset { get; private set; }

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Revision: 0x{0:X}", Revision));
            sb.AppendLine(string.Format("Control: {0}", Control));

            sb.AppendLine();
            sb.AppendLine(string.Format("Owner offset: 0x{0:X}", OwnerOffset));
            sb.AppendLine(string.Format("Owner SID: {0}", OwnerSID));
            sb.AppendLine(string.Format("Owner SID Type: {0}", OwnerSIDType));

            sb.AppendLine();
            sb.AppendLine(string.Format("Group offset: 0x{0:X}", GroupOffset));
            sb.AppendLine(string.Format("Group SID: {0}", GroupSID));
            sb.AppendLine(string.Format("Group SID Type: {0}", GroupSIDType));

            if (DACL != null)
            {
                sb.AppendLine();
                sb.AppendLine(string.Format("DaclrOffset: 0x{0:X}", DaclOffset));
                sb.AppendLine(string.Format("DACL: {0}", DACL));
            }

            if (SACL != null)
            {
                sb.AppendLine();
                sb.AppendLine(string.Format("SaclOffset: 0x{0:X}", SaclOffset));
                sb.AppendLine(string.Format("SACL: {0}", SACL));
            }




            return sb.ToString();
        }
    }
}
