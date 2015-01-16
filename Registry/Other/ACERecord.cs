using System;
using System.Linq;
using System.Text;

// namespaces...

namespace Registry.Other
{
    // public classes...
    public class ACERecord
    {
        // public enums...
        [Flags]
        public enum AceFlagsEnum
        {
            ContainerInheritAce = 0x02,
            FailedAccessAceFlag = 0x80,
            InheritedAce = 0x10,
            InheritOnlyAce = 0x08,
            None = 0x0,
            NoPropagateInheritAce = 0x04,
            ObjectInheritAce = 0x01,
            SuccessfulAccessAceFlag = 0x40
        }

        public enum AceTypeEnum
        {
            AccessAllowedAceType = 0x0,
            AccessAllowedCompoundAceType = 0x4,
            AccessAllowedObjectAceType = 0x5,
            AccessDeniedAceType = 0x1,
            AccessDeniedObjectAceType = 0x6,
            SystemAlarmAceType = 0x3,
            SystemAlarmObjectAceType = 0x8,
            SystemAuditAceType = 0x2,
            SystemAuditObjectAceType = 0x7
        }

        [Flags]
        public enum MasksEnum
        {
            CreateLink = 0x00000020,
            CreateSubkey = 0x00000004,
            Delete = 0x00010000,
            EnumerateSubkeys = 0x00000008,
            FullControl = 0x000F003F,
            Notify = 0x00000010,
            QueryValue = 0x00000001,
            ReadControl = 0x00020000,
            SetValue = 0x00000002,
            WriteDAC = 0x00040000,
            WriteOwner = 0x00080000
        }

        // public constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="ACERecord" /> class.
        /// </summary>
        public ACERecord(byte[] rawBytes)
        {
            RawBytes = rawBytes;

            switch (rawBytes[0])
            {
                case 0x0:
                    ACEType = AceTypeEnum.AccessAllowedAceType;
                    break;
                case 0x1:
                    ACEType = AceTypeEnum.AccessDeniedAceType;
                    break;
                case 0x2:
                    ACEType = AceTypeEnum.SystemAuditAceType;
                    break;
                case 0x3:
                    ACEType = AceTypeEnum.SystemAlarmAceType;
                    break;
                case 0x4:
                    ACEType = AceTypeEnum.AccessAllowedCompoundAceType;
                    break;
                case 0x5:
                    ACEType = AceTypeEnum.AccessAllowedObjectAceType;
                    break;
                case 0x6:
                    ACEType = AceTypeEnum.AccessDeniedObjectAceType;
                    break;
                case 0x7:
                    ACEType = AceTypeEnum.SystemAuditObjectAceType;
                    break;
                case 0x8:
                    ACEType = AceTypeEnum.SystemAlarmObjectAceType;
                    break;
            }

            ACEFlags = (AceFlagsEnum) rawBytes[1];

            ACESize = BitConverter.ToUInt16(rawBytes, 2);

            Mask = (MasksEnum) BitConverter.ToUInt32(rawBytes, 4);

            var rawSid = rawBytes.Skip(0x8).Take(ACESize - 0x8).ToArray();

            SID = Helpers.ConvertHexStringToSidString(rawSid);

            SIDType = Helpers.GetSIDTypeFromSIDString(SID);
        }

        // public properties...
        public AceFlagsEnum ACEFlags { get; private set; }
        public ushort ACESize { get; private set; }
        public AceTypeEnum ACEType { get; private set; }
        public MasksEnum Mask { get; private set; }
        public byte[] RawBytes { get; private set; }
        public string SID { get; private set; }
        public Helpers.SidTypeEnum SIDType { get; private set; }
        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("ACE Size: 0x{0:X}", ACESize));

            sb.AppendLine(string.Format("ACE Type: {0}", ACEType));

            sb.AppendLine(string.Format("ACE Flags: {0}", ACEFlags));

            sb.AppendLine(string.Format("Mask: {0}", Mask));

            sb.AppendLine(string.Format("SID: {0}", SID));
            sb.AppendLine(string.Format("SID Type: {0}", SIDType));

            sb.AppendLine(string.Format("SID Type Description: {0}", Helpers.GetDescriptionFromEnumValue(SIDType)));

            return sb.ToString();
        }
    }
}