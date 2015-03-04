using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NFluent;
using NUnit.Framework;
using Registry.Cells;
using Registry.Other;

namespace Registry.Test
{
    [TestFixture]
    class TestRegistryOtherClass
    {
        private readonly string _basePath = @"C:\ProjectWorkingFolder\Registry2\Registry\Registry.Test\TestFiles";

        [Test]
        public void VerifyHeaderInfo()
        {
            var hivePath = Path.Combine(_basePath, "SAM");
            var r = new RegistryHiveOnDemand(hivePath);
            
            Check.That(r.Header).IsNotNull();
            Check.That(r.Header.FileName).IsNotNull();
            Check.That(r.Header.FileName).IsNotEmpty();
            Check.That(r.Header.Length).IsGreaterThan(0);
            Check.That(r.Header.MajorVersion).IsGreaterThan(0);
            Check.That(r.Header.MinorVersion).IsGreaterThan(0);
            Check.That(r.Header.RootCellOffset).IsGreaterThan(0);
            Check.That(r.Header.CalculatedChecksum).Equals(r.Header.CheckSum);
            Check.That(r.Header.ValidateCheckSum()).Equals(true);
            Check.That(r.Header.ToString()).IsNotEmpty();
        }

        [Test]
        public void ExportToRegFormatSingleKey()
        {
            var hivePath = Path.Combine(_basePath, "SAM");
            var r = new RegistryHiveOnDemand(hivePath);

            var key = r.GetKey(@"SAM\Domains\Account");

        var exported =    Helpers.ExportToReg(@"C:\temp\exportSamTest.reg", key, HiveTypeEnum.Sam, false);

            Check.That(exported).IsTrue();

             hivePath = Path.Combine(_basePath, "NTUSER1.DAT");
             r = new RegistryHiveOnDemand(hivePath);

             key = r.GetKey(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Console");

             exported = Helpers.ExportToReg(@"C:\temp\exportntuser1Test.reg", key, HiveTypeEnum.NtUser, false);

            Check.That(exported).IsTrue();

            hivePath = Path.Combine(_basePath, "SECURITY");
            r = new RegistryHiveOnDemand(hivePath);

            key = r.GetKey(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Policy\Accounts\S-1-5-9");

            exported = Helpers.ExportToReg(@"C:\temp\exportsecTest.reg", key, HiveTypeEnum.Security, false);

            Check.That(exported).IsTrue();

            hivePath = Path.Combine(_basePath, "SOFTWARE");
            r = new RegistryHiveOnDemand(hivePath);

            key = r.GetKey(@"CMI-CreateHive{199DAFC2-6F16-4946-BF90-5A3FC3A60902}\Clients\Contacts\Address Book\Capabilities\FileAssociations");

            exported = Helpers.ExportToReg(@"C:\temp\exportsoftTest.reg", key, HiveTypeEnum.Software, false);

            Check.That(exported).IsTrue();

            hivePath = Path.Combine(_basePath, "SYSTEM");
            r = new RegistryHiveOnDemand(hivePath);

            key = r.GetKey(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\ControlSet001\Enum\ACPI\PNP0C02\1");

            exported = Helpers.ExportToReg(@"C:\temp\exportsysTest.reg", key, HiveTypeEnum.System, false);

            Check.That(exported).IsTrue();

            hivePath = Path.Combine(_basePath, "UsrClass FTP.dat");
            r = new RegistryHiveOnDemand(hivePath);

            key = r.GetKey(@"S-1-5-21-2417227394-2575385136-2411922467-1105_Classes\.3g2");

            exported = Helpers.ExportToReg(@"C:\temp\exportusrTest.reg", key, HiveTypeEnum.UsrClass, false);

            Check.That(exported).IsTrue();

            hivePath = Path.Combine(_basePath, "Components");
            r = new RegistryHiveOnDemand(hivePath);

            key = r.GetKey(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\CanonicalData\Catalogs\006884cbcced145efb8ebba5123eb394381e812474197d7a3d5410a6c8cf69ac");

            exported = Helpers.ExportToReg(@"C:\temp\exportcompTest.reg", key, HiveTypeEnum.Components, false);

            Check.That(exported).IsTrue();

            hivePath = Path.Combine(_basePath, "SAM_DUPENAME");
            r = new RegistryHiveOnDemand(hivePath);

            key = r.GetKey(@"SAM\SAM\Domains\Account\Aliases\000003E9");

            exported = Helpers.ExportToReg(@"C:\temp\exportotherTest.reg", key, HiveTypeEnum.Other, false);

            Check.That(exported).IsTrue();

        }

        [Test]
        public void ExportToRegFormatRecursive()
        {
            var hivePath = Path.Combine(_basePath, "SAM");
            var r = new RegistryHiveOnDemand(hivePath);

            var key = r.GetKey(@"SAM\Domains\Account");

            Helpers.ExportToReg(@"C:\temp\exportTest.reg", key, HiveTypeEnum.Sam, true);
        }

        [Test]
        public void ExerciseNKStuff()
        {
            var hivePath = Path.Combine(_basePath, "sam");
            var r = new RegistryHive(hivePath);
            r.ParseHive();

            var key =
                r.FindKey(0x418);

            Check.That(key).IsNotNull();

            Check.That(key.NKRecord.Padding.Length).IsGreaterThan(0);
            Check.That(key.NKRecord.ToString()).IsNotEmpty();
            Check.That(key.NKRecord.SecurityCellIndex).IsGreaterThan(0);
            Check.That(key.NKRecord.SubkeyListsVolatileCellIndex).IsEqualTo((uint)0);
        }

        [Test]
        public void ExerciseVKStuff()
        {
            var hivePath = Path.Combine(_basePath, "sam");
            var r = new RegistryHive(hivePath);
            r.ParseHive();

            var key =
                r.FindKey(0x418);

            Check.That(key).IsNotNull();

            Check.That(key.ToString()).IsNotEmpty();

            var val = key.Values[0];

            //TODO these need all split out by value type so all code is hit
            //TODO Need to export to reg each kind too

            Check.That(val).IsNotNull();

            Check.That(val.ValueName).IsNotEmpty();
            Check.That(val.ValueData).IsNotEmpty();
            Check.That(val.ValueSlack).IsEmpty();
            Check.That(val.ValueSlackRaw).IsEmpty();
            Check.That(val.ToString()).IsNotEmpty();

            //This key has slack
            key =
                r.FindKey(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Account\Users\000001F4");

            Check.That(key).IsNotNull();

            val = key.Values[0];

            Check.That(val).IsNotNull();

            Check.That(val.ValueName).IsNotEmpty();
            Check.That(val.ValueData).IsNotEmpty();
            Check.That(val.ValueSlack).IsNotEmpty();
            Check.That(val.ValueSlackRaw.Length).IsGreaterThan(0);
            Check.That(val.ToString()).IsNotEmpty();

            hivePath = Path.Combine(_basePath, "UsrClassDeletedBags.dat");
             r = new RegistryHive(hivePath);
            r.ParseHive();

             key =
                r.FindKey(@"S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify");

            Check.That(key).IsNotNull();

            foreach (var keyValue in key.Values)
            {
                Check.That(keyValue.ValueName).IsNotEmpty();
                Check.That(keyValue.ValueData).IsNotEmpty();
            }


            key =
               r.FindKey(@"S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags\1\Shell\{5C4F28B5-F869-4E84-8E60-F11DB97C5CC7}");

            Check.That(key).IsNotNull();

            foreach (var keyValue in key.Values)
            {
                Check.That(keyValue.ValueName).IsNotEmpty();
                Check.That(keyValue.ValueData).IsNotEmpty();
            }

            hivePath = Path.Combine(_basePath, "SAM_hasBigEndianDWord");
            r = new RegistryHive(hivePath);
            r.ParseHive();

            key =
   r.FindKey(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Account\Aliases");

            Check.That(key).IsNotNull();

            foreach (var keyValue in key.Values)
            {
                Check.That(keyValue.ValueName).IsNotEmpty();
                Check.That(keyValue.ValueData).IsNotEmpty();
            }

        }

        [Test]
        public void CheckUnableToDetermineNameOnRecoveredKey()
        {
            var hivePath = Path.Combine(_basePath, "SOFTWARE_BIG");
            var r = new RegistryHive(hivePath);
            r.RecoverDeleted = true;
            r.ParseHive();

          var  key = r.FindKey(116784912);

            Check.That(key).IsNotNull();

            Check.That(key.NKRecord.Name).Equals("(Unable to determine name)");

            Check.That(key.NKRecord.Padding.Length).IsEqualTo(0);

        }

        [Test]
        public void GetEnumFromDescriptionAndViceVersa()
        {
            var hivePath = Path.Combine(_basePath, "SAM");
            var r = new RegistryHiveOnDemand(hivePath);

            var desc = Helpers.GetDescriptionFromEnumValue(r.HiveType);

            var en = Helpers.GetEnumValueFromDescription<HiveTypeEnum>(desc);

            Check.ThatCode(() =>
            {
                var enBad = Helpers.GetEnumValueFromDescription<int>("NotAnEnum");

            }).Throws<ArgumentException>();

            

            Check.That(desc).IsNotEmpty();
            Check.That(desc).Equals("SAM");
            Check.That(en).IsInstanceOf<HiveTypeEnum>();
            Check.That(en).Equals(HiveTypeEnum.Sam);

        }

        [Test]
        public void TestGetSIDTypeFromSIDString()
        {
            var hivePath = Path.Combine(_basePath, "SAM");
            var r = new RegistryHiveOnDemand(hivePath);

            var sid = "S-1-5-5-111111";

            var desc = Helpers.GetSIDTypeFromSIDString(sid);

            Check.That(desc).IsInstanceOf<Helpers.SidTypeEnum>();

            sid = "S-1-2-0";

            desc = Helpers.GetSIDTypeFromSIDString(sid);

            Check.That(desc).IsEqualTo(Helpers.SidTypeEnum.Local);

        }

        [Test]
        public void ExportToRegFormatNullKey()
        {
            Check.ThatCode(() =>
            {

                var hivePath = Path.Combine(_basePath, "SAM");
                var r = new RegistryHiveOnDemand(hivePath);
                Helpers.ExportToReg(@"C:\temp\exportTest.reg", null, HiveTypeEnum.Sam, true);

            }).Throws<NullReferenceException>();
        }

        [Test]
        public void VerifySKInfo()
        {
            var hivePath = Path.Combine(_basePath, "SAM");
            var r = new RegistryHive(hivePath);
            r.FlushRecordListsAfterParse = false;
            r.ParseHive();

            var key = r.FindKey(@"SAM\Domains\Account");

            var sk = r.CellRecords[key.NKRecord.SecurityCellIndex] as SKCellRecord;

            Check.That(sk).IsNotNull();
            Check.That(sk.ToString()).IsNotEmpty();
            Check.That(sk.Size).IsGreaterThan(0);
            Check.That(sk.Reserved).IsInstanceOf<ushort>();

            sk.AbsoluteOffset = 1;
            Check.That(sk.AbsoluteOffset).IsNotEqualTo(1);
            sk.IsFree = true;
            Check.That(sk.IsFree).IsEqualTo(false);

            sk.Signature = "AA";
            Check.That(sk.Signature).IsNotEqualTo("AA");

            sk.Size = -12 ;
            Check.That(sk.Size).IsNotEqualTo(12);

            Check.That(sk.DescriptorLength).IsGreaterThan(0);
        }
    }
}
