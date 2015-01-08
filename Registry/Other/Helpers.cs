using Registry.Cells;
using Registry.Lists;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// namespaces...
namespace Registry.Other
{
    // public classes...
    public static class Helpers
    {
        // public enums...
        //http://msdn.microsoft.com/en-us/library/cc980032.aspx
        public enum SidTypeEnum
        {
            [Description("SID does not map to a common SID or this is a user SID")]
            UnknownOrUserSID,
            [Description("S-1-0-0: No Security principal.")]
            Null,
            [Description("S-1-1-0: A group that includes all users.")]
            Everyone,
            [Description("S-1-2-0: A group that includes all users who have logged on locally.")]
            Local,
            [Description(
            "S-1-2-1: A group that includes users who are logged on to the physical console. This SID can be used to implement security policies that grant different rights based on whether a user has been granted physical access to the console."
            )]
            ConsoleLogon,
            [Description(
            "S-1-3-0: A placeholder in an inheritable access control entry (ACE). When the ACE is inherited, the system replaces this SID with the SID for the object's creator."
            )]
            CreatorOwner,
            [Description(
            "S-1-3-1: A placeholder in an inheritable ACE. When the ACE is inherited, the system replaces this SID with the SID for the primary group of the object's creator."
            )]
            CreatorGroup,
            [Description(
            "S-1-3-2: A placeholder in an inheritable ACE. When the ACE is inherited, the system replaces this SID with the SID for the object's owner server."
            )]
            OwnerServer,
            [Description(
            "S-1-3-3: A placeholder in an inheritable ACE. When the ACE is inherited, the system replaces this SID with the SID for the object's group server."
            )]
            GroupServer,
            [Description(
            "S-1-3-4: A group that represents the current owner of the object. When an ACE that carries this SID is applied to an object, the system ignores the implicit READ_CONTROL and WRITE_DAC permissions for the object owner."
            )]
            OwnerRights,
            [Description("S-1-5: A SID containing only the SECURITY_NT_AUTHORITY identifier authority.")]
            NtAuthority,
            [Description("S-1-5-1: A group that includes all users who have logged on through a dial-up connection.")]
            Dialup,
            [Description("S-1-5-2: A group that includes all users who have logged on through a network connection.")]
            Network,
            [Description("S-1-5-3: A group that includes all users who have logged on through a batch queue facility.")]
            Batch,
            [Description("S-1-5-4: A group that includes all users who have logged on interactively.")]
            Interactive,
            [Description(
            "S-1-5-5-x-y: A logon session. The X and Y values for these SIDs are different for each logon session and are recycled when the operating system is restarted."
            )]
            LogonId,
            [Description("S-1-5-6: A group that includes all security principals that have logged on as a service.")]
            Service,
            [Description("S-1-5-7: A group that represents an anonymous logon.")]
            Anonymous,
            [Description("S-1-5-8: Identifies a SECURITY_NT_AUTHORITY Proxy.")]
            Proxy,
            [Description(
            "S-1-5-9: A group that includes all domain controllers in a forest that uses an Active Directory directory service."
            )]
            EnterpriseDomainControllers,
            [Description(
            "S-1-5-10: A placeholder in an inheritable ACE on an account object or group object in Active Directory. When the ACE is inherited, the system replaces this SID with the SID for the security principal that holds the account."
            )]
            PrincipalSelf,
            [Description(
            "S-1-5-11: A group that includes all users whose identities were authenticated when they logged on.")]
            AuthenticatedUsers,
            [Description(
            "S-1-5-12: This SID is used to control access by untrusted code. ACL validation against tokens with RC consists of two checks, one against the token's normal list of SIDs and one against a second list (typically containing RC - the RESTRICTED_CODE token - and a subset of the original token SIDs). Access is granted only if a token passes both tests. Any ACL that specifies RC must also specify WD - the EVERYONE token. When RC is paired with WD in an ACL, a superset of EVERYONE, including untrusted code, is described."
            )]
            RestrictedCode,
            [Description("S-1-5-13: A group that includes all users who have logged on to a Terminal Services server.")]
            TerminalServerUser,
            [Description(
            "S-1-5-14: A group that includes all users who have logged on through a terminal services logon.")]
            RemoteInteractiveLogon,
            [Description("S-1-5-15: A group that includes all users from the same organization.")]
            ThisOrganization,
            [Description("S-1-5-1000: A group that includes all users and computers from another organization. ")]
            OtherOrganization,
            [Description("S-1-5-17: An account that is used by the default Internet Information Services (IIS) user.")]
            Iusr,
            [Description("S-1-5-18: An account that is used by the operating system.")]
            LocalSystem,
            [Description("S-1-5-19: A local service account.")]
            LocalService,
            [Description("S-1-5-20: A network service account.")]
            NetworkService,
            [Description(
            "S-1-5-21-<root domain>-498: A universal group containing all read-only domain controllers in a forest."
            )]
            EnterpriseReadonlyDomainControllers,
            [Description(
            "S-1-5-21-0-0-0-496: Device identity is included in the Kerberos service ticket. If a forest boundary was crossed, then claims transformation occurred."
            )]
            CompoundedAuthentication,
            [Description(
            "S-1-5-21-0-0-0-497: Claims were queried for in the account's domain, and if a forest boundary was crossed, then claims transformation occurred."
            )]
            ClaimsValid,
            [Description(
            "S-1-5-21-<machine>-500: A user account for the system administrator. By default, it is the only user account that is given full control over the system."
            )]
            Administrator,
            [Description(
            "S-1-5-21-<machine>-501: A user account for people who do not have individual accounts. This user account does not require a password. By default, the Guest account is disabled."
            )]
            Guest,
            [Description(
            "S-1-5-21-<domain>-512: A global group whose members are authorized to administer the domain. By default, the DOMAIN_ADMINS group is a member of the Administrators group on all computers that have joined a domain, including the domain controllers. DOMAIN_ADMINS is the default owner of any object that is created by any member of the group."
            )]
            DomainAdmins,
            [Description("S-1-5-21-<domain>-513: A global group that includes all user accounts in a domain.")]
            DomainUsers,
            [Description(
            "S-1-5-21-<domain>-514: A global group that has only one member, which is the built-in Guest account of the domain."
            )]
            DomainGuests,
            [Description(
            "S-1-5-21-<domain>-515: A global group that includes all clients and servers that have joined the domain."
            )]
            DomainComputers,
            [Description("S-1-5-21-<domain>-516: A global group that includes all domain controllers in the domain.")]
            DomainDomainControllers,
            [Description(
            "S-1-5-21-<domain>-517: A global group that includes all computers that are running an enterprise certification authority. Cert Publishers are authorized to publish certificates for User objects in Active Directory."
            )]
            CertPublishers,
            [Description(
            "S-1-5-21-<root-domain>-518: A universal group in a native-mode domain, or a global group in a mixed-mode domain. The group is authorized to make schema changes in Active Directory."
            )]
            SchemaAdministrators,
            [Description(
            "S-1-5-21-<root-domain>-519: A universal group in a native-mode domain, or a global group in a mixed-mode domain. The group is authorized to make forestwide changes in Active Directory, such as adding child domains."
            )]
            EnterpriseAdmins,
            [Description(
            "S-1-5-21-<domain>-520: A global group that is authorized to create new Group Policy Objects in Active Directory."
            )]
            GroupPolicyCreatorOwners,
            [Description("S-1-5-21-<domain>-521: A global group that includes all read-only domain controllers.")]
            ReadonlyDomainControllers,
            [Description(
            "S-1-5-21-<domain>-522: A global group that includes all domain controllers in the domain that may be cloned."
            )]
            CloneableControllers,
            [Description(
            "S-1-5-21-<domain>-525: A global group that are afforded additional protections against authentication security threats. For more information, see [MS-APDS] and [MS-KILE]."
            )]
            ProtectedUsers,
            [Description(
            "S-1-5-21-<domain>-553: A domain local group for Remote Access Services (RAS) servers. Servers in this group have Read Account Restrictions and Read Logon Information access to User objects in the Active Directory domain local group."
            )]
            RasServers,
            [Description(
            "S-1-5-32-544: A built-in group. After the initial installation of the operating system, the only member of the group is the Administrator account. When a computer joins a domain, the Domain Administrators group is added to the Administrators group. When a server becomes a domain controller, the Enterprise Administrators group also is added to the Administrators group."
            )]
            BuiltinAdministrators,
            [Description(
            "S-1-5-32-545: A built-in group. After the initial installation of the operating system, the only member is the Authenticated Users group. When a computer joins a domain, the Domain Users group is added to the Users group on the computer."
            )]
            BuiltinUsers,
            [Description(
            "S-1-5-32-546: A built-in group. The Guests group allows users to log on with limited privileges to a computer's built-in Guest account."
            )]
            BuiltinGuests,
            [Description(
            "S-1-5-32-547: A built-in group. Power users can perform the following actions: Create local users and groups, Modify and delete accounts that they have created, Remove users from the Power Users, Users, and Guests groups, Install programs, Create, manage, and delete local printers, Create and delete file shares."
            )]
            PowerUsers,
            [Description(
            "S-1-5-32-548: A built-in group that exists only on domain controllers. Account Operators have permission to create, modify, and delete accounts for users, groups, and computers in all containers and organizational units of Active Directory except the Built-in container and the Domain Controllers OU. Account Operators do not have permission to modify the Administrators and Domain Administrators groups, nor do they have permission to modify the accounts for members of those groups."
            )]
            AccountOperators,
            [Description(
            "S-1-5-32-549: A built-in group that exists only on domain controllers. Server Operators can perform the following actions: Log on to a server interactively, Create and delete network shares, Start and stop services, Back up and restore files, Format the hard disk of a computer, Shut down the computer"
            )]
            ServerOperators,
            [Description(
            "S-1-5-32-550: A built-in group that exists only on domain controllers. Print Operators can manage printers and document queues."
            )]
            PrinterOperators,
            [Description(
            "S-1-5-32-551: A built-in group. Backup Operators can back up and restore all files on a computer, regardless of the permissions that protect those files."
            )]
            BackupOperators,
            [Description(
            "S-1-5-32-552: A built-in group that is used by the File Replication Service (FRS) on domain controllers."
            )]
            Replicator,
            [Description(
            "S-1-5-32-554: A backward compatibility group that allows read access on all users and groups in the domain."
            )]
            AliasPrew2Kcompacc,
            [Description("S-1-5-32-555: An alias. Members of this group are granted the right to log on remotely.")]
            RemoteDesktop,
            [Description(
            "S-1-5-32-556: An alias. Members of this group can have some administrative privileges to manage configuration of networking features."
            )]
            NetworkConfigurationOps,
            [Description(
            "S-1-5-32-557: An alias. Members of this group can create incoming, one-way trusts to this forest.")]
            IncomingForestTrustBuilders,
            [Description("S-1-5-32-558: An alias. Members of this group have remote access to monitor this computer.")]
            PerfmonUsers,
            [Description(
            "S-1-5-32-559: An alias. Members of this group have remote access to schedule the logging of performance counters on this computer."
            )]
            PerflogUsers,
            [Description(
            "S-1-5-32-560: An alias. Members of this group have access to the computed tokenGroupsGlobalAndUniversal attribute on User objects."
            )]
            WindowsAuthorizationAccessGroup,
            [Description("S-1-5-32-561: An alias. A group for Terminal Server License Servers.")]
            TerminalServerLicenseServers,
            [Description(
            "S-1-5-32-562: An alias. A group for COM to provide computer-wide access controls that govern access to all call, activation, or launch requests on the computer."
            )]
            DistributedComUsers,
            [Description("S-1-5-32-568: A built-in group account for IIS users.")]
            IisIusrs,
            [Description("S-1-5-32-569: A built-in group account for cryptographic operators.")]
            CryptographicOperators,
            [Description(
            "S-1-5-32-573: A built-in local group. Members of this group can read event logs from the local machine."
            )]
            EventLogReaders,
            [Description(
            "S-1-5-32-574: A built-in local group. Members of this group are allowed to connect to Certification Authorities in the enterprise."
            )]
            CertificateServiceDcomAccess,
            [Description("S-1-5-32-575: A group that allows members use of Remote Application Services resources.")]
            RdsRemoteAccessServers,
            [Description("S-1-5-32-576: A group that enables member servers to run virtual machines and host sessions.")
            ]
            RdsEndpointServers,
            [Description(
            "S-1-5-32-577: A group that allows members to access WMI resources over management protocols (such as WS-Management via the Windows Remote Management service)."
            )]
            RdsManagementServers,
            [Description("S-1-5-32-578: A group that gives members access to all administrative features of Hyper-V.")]
            HyperVAdmins,
            [Description(
            "S-1-5-32-579: A local group that allows members to remotely query authorization attributes and permissions for resources on the local computer."
            )]
            AccessControlAssistanceOps,
            [Description(
            "S-1-5-32-580: Members of this group can access Windows Management Instrumentation (WMI) resources over management protocols (such as WS-Management [DMTF-DSP0226]). This applies only to WMI namespaces that grant access to the user."
            )]
            RemoteManagementUsers,
            [Description(
            "S-1-5-33: A SID that allows objects to have an ACL that lets any service process with a write-restricted token to write to the object."
            )]
            WriteRestrictedCode,
            [Description(
            "S-1-5-64-10: A SID that is used when the NTLM authentication package authenticated the client.")]
            NtlmAuthentication,
            [Description(
            "S-1-5-64-14: A SID that is used when the SChannel authentication package authenticated the client.")]
            SchannelAuthentication,
            [Description(
            "S-1-5-64-21: A SID that is used when the Digest authentication package authenticated the client.")]
            DigestAuthentication,
            [Description(
            "S-1-5-65-1: A SID that indicates that the client's Kerberos service ticket's PAC contained a NTLM_SUPPLEMENTAL_CREDENTIAL structure (as specified in [MS-PAC] section 2.6.4)."
            )]
            ThisOrganizationCertificate,
            [Description("S-1-5-80: An NT Service account prefix.")]
            NtService,
            [Description("S-1-5-84-0-0-0-0-0: Identifies a user-mode driver process.")]
            UserModeDrivers,
            [Description("S-1-5-113: A group that includes all users who are local accounts.")]
            LocalAccount,
            [Description(
            "S-1-5-114: A group that includes all users who are local accounts and members of the administrators group."
            )]
            LocalAccountAndMemberOfAdministratorsGroup,
            [Description("S-1-15-2-1: All applications running in an app package context.")]
            AllAppPackages,
            [Description("S-1-16-0: An untrusted integrity level.")]
            MlUntrusted,
            [Description("S-1-16-4096: A low integrity level.")]
            MlLow,
            [Description("S-1-16-8192: A medium integrity level.")]
            MlMedium,
            [Description("S-1-16-8448: A medium-plus integrity level.")]
            MlMediumPlus,
            [Description("S-1-16-12288: A high integrity level.")]
            MlHigh,
            [Description("S-1-16-16384: A system integrity level.")]
            MlSystem,
            [Description("S-1-16-20480: A protected-process integrity level.")]
            MlProtectedProcess,
            [Description(
            "S-1-18-1: A SID that means the client's identity is asserted by an authentication authority based on proof of possession of client credentials."
            )]
            AuthenticationAuthorityAssertedIdentity,
            [Description("S-1-18-2: A SID that means the client's identity is asserted by a service.")]
            ServiceAssertedIdentity
        }

        // public methods...
        /// <summary>
        /// Converts a SID as stored in the registry to a human readable version.
        /// <remarks>Use GetSIDTypeFromSIDString to get an Enum from this string with a description of what the SID is used for</remarks>
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static string ConvertHexStringToSidString(byte[] hex)
        {
            //If your SID is S-1-5-21-2127521184-1604012920-1887927527-72713, then your raw hex SID is 01 05 00 00 00 00 00 05 15000000 A065CF7E 784B9B5F E77C8770 091C0100

            //This breaks down as follows:
            //01 S-1
            //05 (seven dashes, seven minus two = 5)
            //000000000005 (5 = 0x000000000005, big-endian)
            //15000000 (21 = 0x00000015, little-endian)
            //A065CF7E (2127521184 = 0x7ECF65A0, little-endian)
            //784B9B5F (1604012920 = 0x5F9B4B78, little-endian)
            //E77C8770 (1887927527 = 0X70877CE7, little-endian)
            //091C0100 (72713 = 0x00011c09, little-endian)

            //page 191 http://amnesia.gtisc.gatech.edu/~moyix/suzibandit.ltd.uk/MSc/Registry%20Structure%20-%20Appendices%20V4.pdf

            //"01- 05- 00-00-00-00-00-05- 15-00-00-00- 82-F6-13-90- 30-42-81-99- 23-04-C3-8F- 51-04-00-00"
            //"01-01-00-00-00-00-00-05-12-00-00-00" == S-1-5-18  Local System 
            //"01-02-00-00-00-00-00-05-20-00-00-00-20-02-00-00" == S-1-5-32-544 Administrators
            //"01-01-00-00-00-00-00-05-0C-00-00-00" = S-1-5-12  Restricted Code 
            //"01-02-00-00-00-00-00-0F-02-00-00-00-01-00-00-00"

            const string header = "S";


            var sidVersion = hex[0].ToString();

            var authId = BitConverter.ToInt32(hex.Skip(4).Take(4).Reverse().ToArray(), 0);

            var index = 8;


            var sid = String.Format("{0}-{1}-{2}", header, sidVersion, authId);

            do
            {
                var tempAuthHex = hex.Skip(index).Take(4).ToArray();

                var tempAuth = BitConverter.ToUInt32(tempAuthHex, 0);

                index += 4;

                sid = String.Format("{0}-{1}", sid, tempAuth);
            }
            while (index < hex.Length);

            //some tests
            //var hexStr = BitConverter.ToString(hex);

            //switch (hexStr)
            //{
            //    case "01-01-00-00-00-00-00-05-12-00-00-00":

            //        Check.That(sid).IsEqualTo("S-1-5-18");

            //        break;

            //    case "01-02-00-00-00-00-00-05-20-00-00-00-20-02-00-00":

            //        Check.That(sid).IsEqualTo("S-1-5-32-544");

            //        break;

            //    case "01-01-00-00-00-00-00-05-0C-00-00-00":
            //        Check.That(sid).IsEqualTo("S-1-5-12");

            //        break;
            //    default:

            //        break;
            //}


            return sid;
        }

        private static List<string> GetGoodSigs()
        {
            var goodSigs = new List<string> { "lf", "lh", "li", "ri", "db", "lk", "nk", "sk", "vk" };
            return goodSigs;
        }

        private static List<string> _goodSigs = GetGoodSigs();

        private static void ExtractRecordsFromSlackExtracted(long offset, byte[] rawRecord)
        {
            try
            {
                //Find the index of the next vk or nk record in this chunk of data
                var regexObj = new Regex("00-(6E|73|76)-6B");
                var pos = regexObj.Match(BitConverter.ToString(rawRecord)).Index;

                if (pos > 0)
                {
                    //we found one, but since we converted it to a string, divide by 3 to get to the proper offset
                    //finaly go back 3 to get to the start of the record
                    var actualStart = (pos / 3) - 3;

                    //get the rest of the data after our starting position
                    var extradata = rawRecord.Skip(actualStart).ToArray();

                    //and now extract!
                    ExtractRecordsFromSlack(extradata, offset + actualStart);
                }
            }
            catch (ArgumentException)
            {
                // Syntax error in the regular expression
            }
        }

        public static int ExtractRecordsFromSlack(byte[] remainingData, long relativeoffset)
        {
            // a list of our known signatures, so we can only show when these are found vs data cells
            _goodSigs = GetGoodSigs();


            var foundRecords = 0;

            //get size from remaining data, then loop thru that chunk of data for records.
            //anything left should get processed by the constructor for the new record except in special cases as shown below

            var index = 0;

            while (index < remainingData.Length)
            {
                var len = BitConverter.ToUInt32(remainingData, index);

                var rawRecord = remainingData.Skip(index).Take((int)len).ToArray();

                var sig = Encoding.ASCII.GetString(rawRecord, 4, 2);

                if (_goodSigs.Contains(sig))
                {
                    Console.WriteLine("\tFound a {0} record at relative offset 0x{1:x}!", sig, relativeoffset + index);
                }


                    switch (sig)
                    {
                        case "nk":
                            var nk = new NKCellRecord(rawRecord, relativeoffset + index);
                            RegistryHive.CellRecords.Add(relativeoffset + index, nk);
                            foundRecords += 1;
                            break;
                        case "vk":
                            var vk = new VKCellRecord(rawRecord, relativeoffset + index);
                            RegistryHive.CellRecords.Add(relativeoffset + index, vk);
                            foundRecords += 1;
                            break;

                        case "lf":
                            var lf = new LxListRecord(rawRecord, relativeoffset + index);
                            RegistryHive.ListRecords.Add(relativeoffset + index, lf);
                            foundRecords += 1;

                            // there are often more records in these, so find the first occurrance of a known record type

                            ExtractRecordsFromSlackExtracted(relativeoffset+ index, rawRecord);

                            break;

                        default:
                            //we know about these signatures, so remove them. if we see others, tell someone so support can be added
                            var goodSigs2 = _goodSigs;
                            goodSigs2.Remove("nk");
                            goodSigs2.Remove("vk");
                            goodSigs2.Remove("li");

                            if (goodSigs2.Contains(sig))
                            {
                                throw new Exception("Found a good signature when expecting a data node! please send this hive to saericzimmerman@gmail.com so support can be added");
                            }

                            var dr = new DataNode(rawRecord, relativeoffset + index);
                            RegistryHive.DataRecords.Add(relativeoffset + index, dr);
                            foundRecords += 1;

                            // there are often more records in these, so find the first occurrance of a known record type
                            ExtractRecordsFromSlackExtracted(relativeoffset + index, rawRecord);

                            break;
                    }

                index += (int)len;
            }

            return foundRecords;
        }

        public static string GetDescriptionFromEnumValue(Enum value)
        {
            var attribute = value.GetType()
                .GetField(value.ToString())
                .GetCustomAttributes(typeof (DescriptionAttribute), false)
                .SingleOrDefault() as DescriptionAttribute;
            return attribute == null ? value.ToString() : attribute.Description;
        }

        public static T GetEnumValueFromDescription<T>(string description)
        {
            var type = typeof (T);
            if (!type.IsEnum)
            {
                throw new ArgumentException();
            }
            var fields = type.GetFields();
            var field = fields
                .SelectMany(f => f.GetCustomAttributes(
                    typeof (DescriptionAttribute), false), (
            f, a) => new
            { Field = f, Att = a }).SingleOrDefault(a => ((DescriptionAttribute) a.Att)
                            .Description == description);
            return field == null ? default(T) : (T) field.Field.GetRawConstantValue();
        }

        public static SidTypeEnum GetSIDTypeFromSIDString(string SID)
        {
            var SIDType = SidTypeEnum.UnknownOrUserSID;

            switch (SID)
            {
                case "S-1-0-0":
                    SIDType = SidTypeEnum.Null;
                    break;

                case "S-1-1-0":
                    SIDType = SidTypeEnum.Everyone;
                    break;

                case "S-1-2-0":
                    SIDType = SidTypeEnum.Local;
                    break;

                case "S-1-2-1":
                    SIDType = SidTypeEnum.ConsoleLogon;
                    break;

                case "S-1-3-0":
                    SIDType = SidTypeEnum.CreatorOwner;
                    break;

                case "S-1-3-1":
                    SIDType = SidTypeEnum.CreatorGroup;
                    break;

                case "S-1-3-2":
                    SIDType = SidTypeEnum.OwnerServer;
                    break;

                case "S-1-3-3":
                    SIDType = SidTypeEnum.GroupServer;
                    break;

                case "S-1-3-4":
                    SIDType = SidTypeEnum.OwnerServer;
                    break;

                case "S-1-5-1":
                    SIDType = SidTypeEnum.Dialup;
                    break;

                case "S-1-5-2":
                    SIDType = SidTypeEnum.Network;
                    break;

                case "S-1-5-3":
                    SIDType = SidTypeEnum.Batch;
                    break;

                case "S-1-5-4":
                    SIDType = SidTypeEnum.Interactive;
                    break;

                case "S-1-5-6":
                    SIDType = SidTypeEnum.Service;
                    break;

                case "S-1-5-7":
                    SIDType = SidTypeEnum.Anonymous;
                    break;

                case "S-1-5-8":
                    SIDType = SidTypeEnum.Proxy;
                    break;

                case "S-1-5-9":
                    SIDType = SidTypeEnum.EnterpriseDomainControllers;
                    break;

                case "S-1-5-10":
                    SIDType = SidTypeEnum.PrincipalSelf;
                    break;

                case "S-1-5-11":
                    SIDType = SidTypeEnum.AuthenticatedUsers;
                    break;

                case "S-1-5-12":
                    SIDType = SidTypeEnum.RestrictedCode;
                    break;

                case "S-1-5-13":
                    SIDType = SidTypeEnum.TerminalServerUser;
                    break;

                case "S-1-5-14":
                    SIDType = SidTypeEnum.RemoteInteractiveLogon;
                    break;

                case "S-1-5-15":
                    SIDType = SidTypeEnum.ThisOrganization;
                    break;

                case "S-1-5-17":
                    SIDType = SidTypeEnum.Iusr;
                    break;

                case "S-1-5-18":
                    SIDType = SidTypeEnum.LocalSystem;
                    break;

                case "S-1-5-19":
                    SIDType = SidTypeEnum.LocalService;
                    break;

                case "S-1-5-20":
                    SIDType = SidTypeEnum.NetworkService;
                    break;

                case "S-1-5-21-0-0-0-496":
                    SIDType = SidTypeEnum.CompoundedAuthentication;
                    break;

                case "S-1-5-21-0-0-0-497":
                    SIDType = SidTypeEnum.ClaimsValid;
                    break;

                case "S-1-5-32-544":
                    SIDType = SidTypeEnum.BuiltinAdministrators;
                    break;

                case "S-1-5-32-545":
                    SIDType = SidTypeEnum.BuiltinUsers;
                    break;

                case "S-1-5-32-546":
                    SIDType = SidTypeEnum.BuiltinGuests;
                    break;

                case "S-1-5-32-547":
                    SIDType = SidTypeEnum.PowerUsers;
                    break;

                case "S-1-5-32-548":
                    SIDType = SidTypeEnum.AccountOperators;
                    break;

                case "S-1-5-32-549":
                    SIDType = SidTypeEnum.ServerOperators;
                    break;

                case "S-1-5-32-550":
                    SIDType = SidTypeEnum.PrinterOperators;
                    break;

                case "S-1-5-32-551":
                    SIDType = SidTypeEnum.BackupOperators;
                    break;

                case "S-1-5-32-552":
                    SIDType = SidTypeEnum.Replicator;
                    break;

                case "S-1-5-32-554":
                    SIDType = SidTypeEnum.AliasPrew2Kcompacc;
                    break;

                case "S-1-5-32-555":
                    SIDType = SidTypeEnum.RemoteDesktop;
                    break;

                case "S-1-5-32-556":
                    SIDType = SidTypeEnum.NetworkConfigurationOps;
                    break;

                case "S-1-5-32-557":
                    SIDType = SidTypeEnum.IncomingForestTrustBuilders;
                    break;

                case "S-1-5-32-558":
                    SIDType = SidTypeEnum.PerfmonUsers;
                    break;

                case "S-1-5-32-559":
                    SIDType = SidTypeEnum.PerflogUsers;
                    break;

                case "S-1-5-32-560":
                    SIDType = SidTypeEnum.WindowsAuthorizationAccessGroup;
                    break;

                case "S-1-5-32-561":
                    SIDType = SidTypeEnum.TerminalServerLicenseServers;
                    break;

                case "S-1-5-32-562":
                    SIDType = SidTypeEnum.DistributedComUsers;
                    break;

                case "S-1-5-32-568":
                    SIDType = SidTypeEnum.IisIusrs;
                    break;

                case "S-1-5-32-569":
                    SIDType = SidTypeEnum.CryptographicOperators;
                    break;

                case "S-1-5-32-573":
                    SIDType = SidTypeEnum.EventLogReaders;
                    break;

                case "S-1-5-32-574":
                    SIDType = SidTypeEnum.CertificateServiceDcomAccess;
                    break;

                case "S-1-5-32-575":
                    SIDType = SidTypeEnum.RdsRemoteAccessServers;
                    break;

                case "S-1-5-32-576":
                    SIDType = SidTypeEnum.RdsEndpointServers;
                    break;

                case "S-1-5-32-577":
                    SIDType = SidTypeEnum.RdsManagementServers;
                    break;

                case "S-1-5-32-578":
                    SIDType = SidTypeEnum.HyperVAdmins;
                    break;

                case "S-1-5-32-579":
                    SIDType = SidTypeEnum.AccessControlAssistanceOps;
                    break;

                case "S-1-5-32-580":
                    SIDType = SidTypeEnum.RemoteManagementUsers;
                    break;

                case "S-1-5-33":
                    SIDType = SidTypeEnum.WriteRestrictedCode;
                    break;

                case "S-1-5-64-10":
                    SIDType = SidTypeEnum.NtlmAuthentication;
                    break;

                case "S-1-5-64-14":
                    SIDType = SidTypeEnum.SchannelAuthentication;
                    break;

                case "S-1-5-64-21":
                    SIDType = SidTypeEnum.DigestAuthentication;
                    break;

                case "S-1-5-65-1":
                    SIDType = SidTypeEnum.ThisOrganizationCertificate;
                    break;

                case "S-1-5-80":
                    SIDType = SidTypeEnum.NtService;
                    break;

                case "S-1-5-84-0-0-0-0-0":
                    SIDType = SidTypeEnum.UserModeDrivers;
                    break;

                case "S-1-5-113":
                    SIDType = SidTypeEnum.LocalAccount;
                    break;

                case "S-1-5-114":
                    SIDType = SidTypeEnum.LocalAccountAndMemberOfAdministratorsGroup;
                    break;

                case "S-1-5-1000":
                    SIDType = SidTypeEnum.OtherOrganization;
                    break;

                case "S-1-15-2-1":
                    SIDType = SidTypeEnum.AllAppPackages;
                    break;

                case "S-1-16-0":
                    SIDType = SidTypeEnum.MlUntrusted;
                    break;

                case "S-1-16-4096":
                    SIDType = SidTypeEnum.MlLow;
                    break;

                case "S-1-16-8192":
                    SIDType = SidTypeEnum.MlMedium;
                    break;

                case "S-1-16-8448":
                    SIDType = SidTypeEnum.MlMediumPlus;
                    break;

                case "S-1-16-12288":
                    SIDType = SidTypeEnum.MlHigh;
                    break;

                case "S-1-16-16384":
                    SIDType = SidTypeEnum.MlSystem;
                    break;

                case "S-1-16-20480":
                    SIDType = SidTypeEnum.MlProtectedProcess;
                    break;

                case "S-1-18-1":
                    SIDType = SidTypeEnum.AuthenticationAuthorityAssertedIdentity;
                    break;

                case "S-1-18-2":
                    SIDType = SidTypeEnum.ServiceAssertedIdentity;
                    break;

                default:
                    SIDType = SidTypeEnum.UnknownOrUserSID;
                    break;
            }

            if (SIDType == SidTypeEnum.UnknownOrUserSID)
            {
                if (SID.StartsWith("S-1-5-5-"))
                {
                    SIDType = SidTypeEnum.LogonId;
                }

                if (SID.StartsWith("S-1-5-21-") && SID.EndsWith("-498"))
                {
                    SIDType = SidTypeEnum.EnterpriseDomainControllers;
                }

                if (SID.StartsWith("S-1-5-21-") && SID.EndsWith("-500"))
                {
                    SIDType = SidTypeEnum.Administrator;
                }

                if (SID.StartsWith("S-1-5-21-") && SID.EndsWith("-501"))
                {
                    SIDType = SidTypeEnum.Guest;
                }

                if (SID.StartsWith("S-1-5-21-") && SID.EndsWith("-512"))
                {
                    SIDType = SidTypeEnum.DomainAdmins;
                }

                if (SID.StartsWith("S-1-5-21-") && SID.EndsWith("-513"))
                {
                    SIDType = SidTypeEnum.DomainUsers;
                }

                if (SID.StartsWith("S-1-5-21-") && SID.EndsWith("-514"))
                {
                    SIDType = SidTypeEnum.DomainGuests;
                }

                if (SID.StartsWith("S-1-5-21-") && SID.EndsWith("-515"))
                {
                    SIDType = SidTypeEnum.DomainComputers;
                }

                if (SID.StartsWith("S-1-5-21-") && SID.EndsWith("-516"))
                {
                    SIDType = SidTypeEnum.DomainDomainControllers;
                }

                if (SID.StartsWith("S-1-5-21-") && SID.EndsWith("-517"))
                {
                    SIDType = SidTypeEnum.CertPublishers;
                }

                if (SID.StartsWith("S-1-5-21-") && SID.EndsWith("-518"))
                {
                    SIDType = SidTypeEnum.SchemaAdministrators;
                }

                if (SID.StartsWith("S-1-5-21-") && SID.EndsWith("-519"))
                {
                    SIDType = SidTypeEnum.EnterpriseAdmins;
                }

                if (SID.StartsWith("S-1-5-21-") && SID.EndsWith("-520"))
                {
                    SIDType = SidTypeEnum.GroupPolicyCreatorOwners;
                }

                if (SID.StartsWith("S-1-5-21-") && SID.EndsWith("-521"))
                {
                    SIDType = SidTypeEnum.ReadonlyDomainControllers;
                }

                if (SID.StartsWith("S-1-5-21-") && SID.EndsWith("-522"))
                {
                    SIDType = SidTypeEnum.CloneableControllers;
                }

                if (SID.StartsWith("S-1-5-21-") && SID.EndsWith("-525"))
                {
                    SIDType = SidTypeEnum.ProtectedUsers;
                }

                if (SID.StartsWith("S-1-5-21-") && SID.EndsWith("-553"))
                {
                    SIDType = SidTypeEnum.RasServers;
                }
            }


            return SIDType;
        }
    }
}
