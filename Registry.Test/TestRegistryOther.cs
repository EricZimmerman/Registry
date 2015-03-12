using System;
using System.Data.SqlTypes;
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
        public void ShouldCatchNKRecordThatsTooSmallFromSlackSpace()
        {
         var   usrclass = new RegistryHive(@"..\..\Hives\ERZ_Win81_UsrClass.dat");
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
        public void ShouldCatchVKRecordThatsTooSmallFromSlackSpace()
        {
            var usrclass = new RegistryHive(@"..\..\Hives\NTUSER slack.DAT");
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
        public void ShouldIncreaseSoftParsingError()
        {
            var usrclass = new RegistryHive(@"..\..\Hives\UsrClass-win7.dat");
            usrclass.RecoverDeleted = true;

            Check.That(usrclass.SoftParsingErrors).IsEqualTo(0);

            usrclass.ParseHive();

            Check.That(usrclass.SoftParsingErrors).IsGreaterThan(0);
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
        public void ExportToRegFormatNullKey()
        {
            Check.ThatCode(() => { Helpers.ExportToReg(@"exportTest.reg", null, HiveTypeEnum.Sam, true); })
                .Throws<NullReferenceException>();
        }

        [Test]
        public void ExportToRegFormatRecursive()
        {
            var key = TestSetup.SamOnDemand.GetKey(@"SAM\Domains\Account");

            var exported = Helpers.ExportToReg(@"exportTest.reg", key, HiveTypeEnum.Sam, true);

            Check.That(exported).IsTrue();
        }

        [Test]
        public void ExportToRegFormatSingleKey()
        {
            var key = TestSetup.SamOnDemand.GetKey(@"SAM\Domains\Account");

            var exported = Helpers.ExportToReg(@"exportSamTest.reg", key, HiveTypeEnum.Sam, false);

            Check.That(exported).IsTrue();


            key = TestSetup.NtUser1OnDemand.GetKey(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Console");

            exported = Helpers.ExportToReg(@"exportntuser1Test.reg", key, HiveTypeEnum.NtUser, false);

            Check.That(exported).IsTrue();


            key =
                TestSetup.Security.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Policy\Accounts\S-1-5-9");

            exported = Helpers.ExportToReg(@"exportsecTest.reg", key, HiveTypeEnum.Security, false);

            Check.That(exported).IsTrue();

            key =
                TestSetup.SystemOnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\ControlSet001\Enum\ACPI\PNP0C02\1");

            exported = Helpers.ExportToReg(@"exportsysTest.reg", key, HiveTypeEnum.System, false);

            Check.That(exported).IsTrue();


            key = TestSetup.UsrClassFtp.GetKey(@"S-1-5-21-2417227394-2575385136-2411922467-1105_Classes\.3g2");

            exported = Helpers.ExportToReg(@"exportusrTest.reg", key, HiveTypeEnum.UsrClass, false);

            Check.That(exported).IsTrue();

            key = TestSetup.SamDupeNameOnDemand.GetKey(@"SAM\SAM\Domains\Account\Aliases\000003E9");

            exported = Helpers.ExportToReg(@"exportotherTest.reg", key, HiveTypeEnum.Other, false);

            Check.That(exported).IsTrue();


            key =
                TestSetup.UsrclassDeleted.FindKey(
                    @"S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\Software\Microsoft\Windows\Shell\BagMRU\1");

            exported = Helpers.ExportToReg(@"exportDeletedTest.reg", key, HiveTypeEnum.UsrClass, false);

            Check.That(exported).IsTrue();
        }

        [Test]
        public void GetEnumFromDescriptionAndViceVersa()
        {
            var desc = Helpers.GetDescriptionFromEnumValue(TestSetup.SamOnDemand.HiveType);

            var en = Helpers.GetEnumValueFromDescription<HiveTypeEnum>(desc);

            Check.ThatCode(() => { var enBad = Helpers.GetEnumValueFromDescription<int>("NotAnEnum"); })
                .Throws<ArgumentException>();

            Check.That(desc).IsNotEmpty();
            Check.That(desc).Equals("SAM");
            Check.That(en).IsInstanceOf<HiveTypeEnum>();
            Check.That(en).Equals(HiveTypeEnum.Sam);
        }

        [Test]
        public void ShouldFindDataNode()
        {
            var dnraw = TestSetup.Bcd.ReadBytesFromHive(0x0000000000001100, 8);
            var dn = new DataNode(dnraw, 0x0000000000000100);

            Check.That(dn).IsNotNull();
            Check.That(dn.ToString()).IsNotEmpty();
            Check.That(dn.Signature).IsEmpty();
        }



        [Test]
        public void ShouldFindLFListRecord()
        {
            var record = TestSetup.Bcd.ListRecords[0xd0] as LxListRecord;

            Check.That(record).IsNotNull();
            Check.That(record.ToString()).IsNotEmpty();
        }

        [Test]
        public void ShouldFindLHListRecord()
        {
            var record = TestSetup.Drivers.ListRecords[0x270] as LxListRecord;

            Check.That(record).IsNotNull();
            Check.That(record.ToString()).IsNotEmpty();
        }

        [Test]
        public void ShouldFindDBRecord()
        {
            var record = TestSetup.System.ListRecords[0x78f20] as DBListRecord;

            Check.That(record).IsNotNull();
            Check.That(record.ToString()).IsNotEmpty();
            record.IsReferenced = true;

            Check.That(record.IsReferenced).IsTrue();
            Check.That(record.NumberOfEntries).IsEqualTo(2);
            Check.That(record.OffsetToOffsets).IsEqualTo((uint)0x78F30);


        }

        [Test]
        public void ShouldFindLIRecord()
        {
            var record = TestSetup.UsrClass1.ListRecords[0x000000000015f020] as LIListRecord;
            
            Check.That(record).IsNotNull();
            record.IsReferenced = true;
            Check.That(record.IsReferenced).IsTrue();
            Check.That(record.NumberOfEntries).IsEqualTo(696);
            Check.That(record.Size).IsEqualTo(0x00001630);
            Check.That(record.Offsets[0]).IsEqualTo((uint)0x103078);
            Check.That(record.Offsets[1]).IsEqualTo((uint)0x103EE0);
            Check.That(record.ToString()).IsNotEmpty();
        }

        [Test]
        public void ShouldFindRIRecord()
        {
            var record = TestSetup.System.ListRecords[0x7141D0] as RIListRecord;

            Check.That(record).IsNotNull();
            record.IsReferenced = true;
            Check.That(record.IsReferenced).IsTrue();
            Check.That(record.NumberOfEntries).IsEqualTo(2);
            Check.That(record.Offsets[0]).IsEqualTo((uint)0x717020);
            Check.That(record.Offsets[1]).IsEqualTo((uint)0x72F020);
            Check.That(record.ToString()).IsNotEmpty();
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
            Check.That(TestSetup.Sam.Header).IsNotNull();
            Check.That(TestSetup.Sam.Header.FileName).IsNotNull();
            Check.That(TestSetup.Sam.Header.FileName).IsNotEmpty();
            Check.That(TestSetup.Sam.Header.Length).IsGreaterThan(0);
            Check.That(TestSetup.Sam.Header.MajorVersion).IsGreaterThan(0);
            Check.That(TestSetup.Sam.Header.MinorVersion).IsGreaterThan(0);
            Check.That(TestSetup.Sam.Header.RootCellOffset).IsGreaterThan(0);
            Check.That(TestSetup.Sam.Header.CalculatedChecksum).Equals(TestSetup.Sam.Header.CheckSum);
            Check.That(TestSetup.Sam.Header.ValidateCheckSum()).Equals(true);
            Check.That(TestSetup.Sam.Header.ToString()).IsNotEmpty();
        }
    }
}