using System;
using NFluent;
using NUnit.Framework;
using Registry.Lists;
using Registry.Other;

namespace Registry.Test
{
    [TestFixture]
    internal class TestRegistryOther
    {
        [Test]
        public void ExportToRegFormatNullKey()
        {
            Check.ThatCode(() => { Helpers.ExportToReg(@"exportTest.reg", null, HiveTypeEnum.Sam, true); })
                .Throws<NullReferenceException>();
        }

        [Test]
        public void ExportToRegFormatRecursive()
        {
            var SamOnDemand = new RegistryHiveOnDemand(@"..\..\Hives\SAM");
            var key = SamOnDemand.GetKey(@"SAM\Domains\Account");

            var exported = Helpers.ExportToReg(@"exportTest.reg", key, HiveTypeEnum.Sam, true);

            Check.That(exported).IsTrue();
        }

        [Test]
        public void ExportToRegFormatSingleKey()
        {
            var SamOnDemand = new RegistryHiveOnDemand(@"..\..\Hives\SAM");
            var key = SamOnDemand.GetKey(@"SAM\Domains\Account");

            var exported = Helpers.ExportToReg(@"exportSamTest.reg", key, HiveTypeEnum.Sam, false);

            Check.That(exported).IsTrue();

            var NtUser1OnDemand = new RegistryHiveOnDemand(@"..\..\Hives\NTUSER1.DAT");
            key = NtUser1OnDemand.GetKey(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Console");

            exported = Helpers.ExportToReg(@"exportntuser1Test.reg", key, HiveTypeEnum.NtUser, false);

            Check.That(exported).IsTrue();

            var Security = new RegistryHiveOnDemand(@"..\..\Hives\SECURITY");
            key =
                Security.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Policy\Accounts\S-1-5-9");

            exported = Helpers.ExportToReg(@"exportsecTest.reg", key, HiveTypeEnum.Security, false);

            Check.That(exported).IsTrue();

            var SystemOnDemand = new RegistryHiveOnDemand(@"..\..\Hives\SYSTEM");
            key =
                SystemOnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\ControlSet001\Enum\ACPI\PNP0C02\1");

            exported = Helpers.ExportToReg(@"exportsysTest.reg", key, HiveTypeEnum.System, false);

            Check.That(exported).IsTrue();

            var UsrClassFtp = new RegistryHiveOnDemand(@"..\..\Hives\UsrClass FTP.dat");
            key = UsrClassFtp.GetKey(@"S-1-5-21-2417227394-2575385136-2411922467-1105_Classes\.3g2");

            exported = Helpers.ExportToReg(@"exportusrTest.reg", key, HiveTypeEnum.UsrClass, false);

            Check.That(exported).IsTrue();

            var SamDupeNameOnDemand = new RegistryHiveOnDemand(@"..\..\Hives\SAM_DUPENAME");
            key = SamDupeNameOnDemand.GetKey(@"SAM\SAM\Domains\Account\Aliases\000003E9");

            exported = Helpers.ExportToReg(@"exportotherTest.reg", key, HiveTypeEnum.Other, false);

            Check.That(exported).IsTrue();

            var UsrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
            UsrclassDeleted.RecoverDeleted = true;
            UsrclassDeleted.FlushRecordListsAfterParse = false;
            UsrclassDeleted.ParseHive();
            key =
                UsrclassDeleted.GetKey(
                    @"S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\Software\Microsoft\Windows\Shell\BagMRU\1");

            exported = Helpers.ExportToReg(@"exportDeletedTest.reg", key, HiveTypeEnum.UsrClass, false);

            Check.That(exported).IsTrue();
        }

        [Test]
        public void ExportUsrClassToCommonFormatWithDeleted()
        {
            var UsrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
            UsrclassDeleted.RecoverDeleted = true;
            UsrclassDeleted.FlushRecordListsAfterParse = false;
            UsrclassDeleted.ParseHive();

            UsrclassDeleted.ExportDataToCommonFormat("UsrClassDeletedExport.txt", true);

            UsrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
            UsrclassDeleted.FlushRecordListsAfterParse = true;
            UsrclassDeleted.ParseHive();

            UsrclassDeleted.ExportDataToCommonFormat("UsrClassDeletedWithFlushExport.txt", true);
        }

        [Test]
        public void GetEnumFromDescriptionAndViceVersa()
        {
            var SamOnDemand = new RegistryHiveOnDemand(@"..\..\Hives\SAM");
            var desc = Helpers.GetDescriptionFromEnumValue(SamOnDemand.HiveType);

            var en = Helpers.GetEnumValueFromDescription<HiveTypeEnum>(desc);

            Check.ThatCode(() =>
                {
                    var enBad = Helpers.GetEnumValueFromDescription<int>("NotAnEnum");
                })
                .Throws<ArgumentException>();

            Check.That(desc).IsNotEmpty();
            Check.That(desc).Equals("SAM");
            Check.That(en).IsInstanceOf<HiveTypeEnum>();
            Check.That(en).Equals(HiveTypeEnum.Sam);
        }

        [Test]
        public void ShouldCatchNKRecordThatsTooSmallFromSlackSpace()
        {
            var usrclass = new RegistryHive(@"..\..\Hives\ERZ_Win81_UsrClass.dat");
            usrclass.RecoverDeleted = true;
            usrclass.ParseHive();
        }

        [Test]
        public void ShouldCatchSlackRecordTooSmallToGetSignatureFrom()
        {
            var usrclass = new RegistryHive(@"..\..\Hives\UsrClassJVM.dat");
            usrclass.RecoverDeleted = true;
            usrclass.ParseHive();
        }

        [Test]
        public void ShouldCatchVKRecordThatsTooSmallFromSlackSpace()
        {
            var usrclass = new RegistryHive(@"..\..\Hives\NTUSER slack.DAT");
            usrclass.RecoverDeleted = true;
            usrclass.ParseHive();
        }

        [Test]
        public void ShouldFindADBRecordWhileParsing()
        {
            var usrclass = new RegistryHive(@"..\..\Hives\SYSTEM");
            usrclass.ParseHive();
        }

        [Test]
        public void ShouldFindDataNode()
        {
            var Bcd = new RegistryHive(@"..\..\Hives\BCD");
            Bcd.FlushRecordListsAfterParse = false;
            Bcd.RecoverDeleted = true;
            Bcd.ParseHive();

            var dnraw = Bcd.ReadBytesFromHive(0x0000000000001100, 8);
            var dn = new DataNode(dnraw, 0x0000000000000100);

            Check.That(dn).IsNotNull();
            Check.That(dn.ToString()).IsNotEmpty();
            Check.That(dn.Signature).IsEmpty();
        }

        [Test]
        public void ShouldFindDBRecord()
        {
            var System = new RegistryHive(@"..\..\Hives\System");
            System.FlushRecordListsAfterParse = false;
            System.ParseHive();

            var record = System.ListRecords[0x78f20] as DBListRecord;

            Check.That(record).IsNotNull();
            Check.That(record.ToString()).IsNotEmpty();
            record.IsReferenced = true;

            Check.That(record.IsReferenced).IsTrue();
            Check.That(record.NumberOfEntries).IsEqualTo(2);
            Check.That(record.OffsetToOffsets).IsEqualTo((uint) 0x78F30);
        }


        [Test]
        public void ShouldFindLFListRecord()
        {
            var Bcd = new RegistryHive(@"..\..\Hives\BCD");
            Bcd.FlushRecordListsAfterParse = false;
            Bcd.RecoverDeleted = true;
            Bcd.ParseHive();

            var record = Bcd.ListRecords[0xd0] as LxListRecord;

            Check.That(record).IsNotNull();
            Check.That(record.ToString()).IsNotEmpty();
        }

        [Test]
        public void ShouldFindLHListRecord()
        {
            var Drivers = new RegistryHive(@"..\..\Hives\DRIVERS");
            Drivers.FlushRecordListsAfterParse = false;
            Drivers.RecoverDeleted = true;
            Drivers.ParseHive();

            var record = Drivers.ListRecords[0x270] as LxListRecord;

            Check.That(record).IsNotNull();
            Check.That(record.ToString()).IsNotEmpty();
        }

        [Test]
        public void ShouldFindLIRecord()
        {
            var UsrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
            UsrClass1.RecoverDeleted = true;
            UsrClass1.FlushRecordListsAfterParse = false;
            UsrClass1.ParseHive();


            var record = UsrClass1.ListRecords[0x000000000015f020] as LIListRecord;

            Check.That(record).IsNotNull();
            record.IsReferenced = true;
            Check.That(record.IsReferenced).IsTrue();
            Check.That(record.NumberOfEntries).IsEqualTo(696);
            Check.That(record.Size).IsEqualTo(0x00001630);
            Check.That(record.Offsets[0]).IsEqualTo((uint) 0x103078);
            Check.That(record.Offsets[1]).IsEqualTo((uint) 0x103EE0);
            Check.That(record.ToString()).IsNotEmpty();
        }

        [Test]
        public void ShouldFindRIRecord()
        {
            var System = new RegistryHive(@"..\..\Hives\System");
            System.FlushRecordListsAfterParse = false;
            System.ParseHive();

            var record = System.ListRecords[0x7141D0] as RIListRecord;

            Check.That(record).IsNotNull();
            record.IsReferenced = true;
            Check.That(record.IsReferenced).IsTrue();
            Check.That(record.NumberOfEntries).IsEqualTo(2);
            Check.That(record.Offsets[0]).IsEqualTo((uint) 0x717020);
            Check.That(record.Offsets[1]).IsEqualTo((uint) 0x72F020);
            Check.That(record.ToString()).IsNotEmpty();
        }

        [Test]
        public void ShouldIncreaseSoftParsingError()
        {
            var usrclass = new RegistryHive(@"..\..\Hives\UsrClass-win7.dat");
            usrclass.RecoverDeleted = true;

            Check.That(usrclass.SoftParsingErrors).IsEqualTo(0);

            usrclass.ParseHive();

            Check.That(usrclass.SoftParsingErrors).IsGreaterThan(0);
        }

        [Test]
        public void TestGetSIDTypeFromSIDString()
        {
            var sid = "S-1-5-5-111111";

            var desc = Helpers.GetSIDTypeFromSIDString(sid);

            Check.That(desc).IsInstanceOf<Helpers.SidTypeEnum>();

            sid = "S-1-2-0";

            desc = Helpers.GetSIDTypeFromSIDString(sid);

            Check.That(desc).IsEqualTo(Helpers.SidTypeEnum.Local);
        }

        [Test]
        public void VerifyHeaderInfo()
        {
            var Sam = new RegistryHive(@"..\..\Hives\SAM");
            Sam.FlushRecordListsAfterParse = false;
            Sam.ParseHive();

            Check.That(Sam.Header).IsNotNull();
            Check.That(Sam.Header.FileName).IsNotNull();
            Check.That(Sam.Header.FileName).IsNotEmpty();
            Check.That(Sam.Header.Length).IsGreaterThan(0);
            Check.That(Sam.Header.MajorVersion).IsGreaterThan(0);
            Check.That(Sam.Header.MinorVersion).IsGreaterThan(0);
            Check.That(Sam.Header.RootCellOffset).IsGreaterThan(0);
            Check.That(Sam.Header.CalculatedChecksum).Equals(Sam.Header.CheckSum);
            Check.That(Sam.Header.ValidateCheckSum()).Equals(true);
            Check.That(Sam.Header.ToString()).IsNotEmpty();
        }
    }
}