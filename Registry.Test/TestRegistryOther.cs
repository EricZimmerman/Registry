using System;
using FluentAssertions;
using NUnit.Framework;
using Registry.Lists;
using Registry.Other;

namespace Registry.Test;

[TestFixture]
internal class TestRegistryOther
{
    [Test]
    public void ExportToRegFormatNullKey()
    {
        FluentActions.Invoking(() => { Helpers.ExportToReg(@"exportTest.reg", null, HiveTypeEnum.Sam, true); }).Should().Throw<NullReferenceException>();
    }

    [Test]
    public void ExportToRegFormatRecursive()
    {
        var samOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM"));
        var key = samOnDemand.GetKey(@"SAM\Domains\Account");

        var exported = Helpers.ExportToReg(@"exportTest.reg", key, HiveTypeEnum.Sam, true);

        exported.Should().BeTrue();
    }

    [Test]
    public void ExportToRegFormatSingleKey()
    {
        var samOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM"));
        var key = samOnDemand.GetKey(@"SAM\Domains\Account");

        var exported = Helpers.ExportToReg(@"exportSamTest.reg", key, HiveTypeEnum.Sam, false);

        exported.Should().BeTrue();

        var ntUser1OnDemand = new RegistryHiveOnDemand(TestHives.Path(@"NTUSER1.DAT"));
        key = ntUser1OnDemand.GetKey(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Console");

        exported = Helpers.ExportToReg(@"exportntuser1Test.reg", key, HiveTypeEnum.NtUser, false);

        exported.Should().BeTrue();

        var security = new RegistryHiveOnDemand(TestHives.Path(@"SECURITY"));
        key =
            security.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Policy\Accounts\S-1-5-9");

        exported = Helpers.ExportToReg(@"exportsecTest.reg", key, HiveTypeEnum.Security, false);

        exported.Should().BeTrue();

        var systemOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SYSTEM"));
        key =
            systemOnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\ControlSet001\Enum\ACPI\PNP0C02\1");

        exported = Helpers.ExportToReg(@"exportsysTest.reg", key, HiveTypeEnum.System, false);

        exported.Should().BeTrue();

        var usrClassFtp = new RegistryHiveOnDemand(TestHives.Path(@"UsrClass FTP.dat"));
        key = usrClassFtp.GetKey(@"S-1-5-21-2417227394-2575385136-2411922467-1105_Classes\.3g2");

        exported = Helpers.ExportToReg(@"exportusrTest.reg", key, HiveTypeEnum.UsrClass, false);

        exported.Should().BeTrue();

        var samDupeNameOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM_DUPENAME"));
        key = samDupeNameOnDemand.GetKey(@"SAM\SAM\Domains\Account\Aliases\000003E9");

        exported = Helpers.ExportToReg(@"exportotherTest.reg", key, HiveTypeEnum.Other, false);

        exported.Should().BeTrue();

        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();
        key =
            usrclassDeleted.GetKey(
                @"S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\Software\Microsoft\Windows\Shell\BagMRU\1");

        exported = Helpers.ExportToReg(@"exportDeletedTest.reg", key, HiveTypeEnum.UsrClass, false);

        exported.Should().BeTrue();
    }

    [Test]
    public void ExportUsrClassToCommonFormatWithDeleted()
    {
        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();

        usrclassDeleted.ExportDataToCommonFormat("UsrClassDeletedExport.txt", true);

        usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.FlushRecordListsAfterParse = true;
        usrclassDeleted.ParseHive();

        usrclassDeleted.ExportDataToCommonFormat("UsrClassDeletedWithFlushExport.txt", true);
    }

    [Test]
    public void GetEnumFromDescriptionAndViceVersa()
    {
        var samOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM"));
        var desc = Helpers.GetDescriptionFromEnumValue(samOnDemand.HiveType);

        var en = Helpers.GetEnumValueFromDescription<HiveTypeEnum>(desc);

        FluentActions.Invoking(() =>
            {
                var enBad = Helpers.GetEnumValueFromDescription<int>("NotAnEnum");
            }).Should().Throw<ArgumentException>();

        desc.Should().NotBeEmpty();
        desc.Should().Be("SAM");
        ((object) en).Should().BeOfType<HiveTypeEnum>();
        en.Should().Be(HiveTypeEnum.Sam);
    }

    [Test]
    public void ShouldCatchNkRecordThatsTooSmallFromSlackSpace()
    {
        var usrclass = new RegistryHive(TestHives.Path(@"ERZ_Win81_UsrClass.dat"));
        usrclass.RecoverDeleted = true;
        usrclass.ParseHive();
    }

    [Test]
    public void ShouldCatchSlackRecordTooSmallToGetSignatureFrom()
    {
        var usrclass = new RegistryHive(TestHives.Path(@"UsrClassJVM.dat"));
        usrclass.RecoverDeleted = true;
        usrclass.ParseHive();
    }

    [Test]
    public void ShouldCatchVkRecordThatsTooSmallFromSlackSpace()
    {
        var usrclass = new RegistryHive(TestHives.Path(@"NTUSER slack.DAT"));
        usrclass.RecoverDeleted = true;
        usrclass.ParseHive();
    }

    [Test]
    public void ShouldFindAdbRecordWhileParsing()
    {
        var usrclass = new RegistryHive(TestHives.Path(@"SYSTEM"));
        usrclass.ParseHive();
    }

    [Test]
    public void ShouldFindDataNode()
    {
        var bcd = new RegistryHive(TestHives.Path(@"BCD"));
        bcd.FlushRecordListsAfterParse = false;
        bcd.RecoverDeleted = true;
        bcd.ParseHive();

        var dnraw = bcd.ReadBytesFromHive(0x0000000000001100, 8);
        var dn = new DataNode(dnraw, 0x0000000000000100);

        dn.Should().NotBeNull();
        dn.ToString().Should().NotBeEmpty();
        dn.Signature.Should().BeEmpty();
    }

    [Test]
    public void ShouldFindDbRecord()
    {
        var system = new RegistryHive(TestHives.Path(@"System"));
        system.FlushRecordListsAfterParse = false;
        system.ParseHive();

        var record = system.ListRecords[0x78f20] as DbListRecord;

        record.Should().NotBeNull();
        record.ToString().Should().NotBeEmpty();
        record.IsReferenced = true;

        record.IsReferenced.Should().BeTrue();
        record.NumberOfEntries.Should().Be(2);
        record.OffsetToOffsets.Should().Be(0x78F30);
    }


    [Test]
    public void ShouldFindLfListRecord()
    {
        var bcd = new RegistryHive(TestHives.Path(@"BCD"));
        bcd.FlushRecordListsAfterParse = false;
        bcd.RecoverDeleted = true;
        bcd.ParseHive();

        var record = bcd.ListRecords[0xd0] as LxListRecord;

        record.Should().NotBeNull();
        record.ToString().Should().NotBeEmpty();
    }

    [Test]
    public void ShouldFindLhListRecord()
    {
        var drivers = new RegistryHive(TestHives.Path(@"DRIVERS"));
        drivers.FlushRecordListsAfterParse = false;
        drivers.RecoverDeleted = true;
        drivers.ParseHive();

        var record = drivers.ListRecords[0x270] as LxListRecord;

        record.Should().NotBeNull();
        record.ToString().Should().NotBeEmpty();
    }

    [Test]
    public void ShouldFindLiRecord()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();


        var record = usrClass1.ListRecords[0x000000000015f020] as LiListRecord;

        record.Should().NotBeNull();
        record.IsReferenced = true;
        record.IsReferenced.Should().BeTrue();
        record.NumberOfEntries.Should().Be(696);
        record.Size.Should().Be(0x00001630);
        record.Offsets[0].Should().Be(0x103078);
        record.Offsets[1].Should().Be(0x103EE0);
        record.ToString().Should().NotBeEmpty();
    }

    [Test]
    public void ShouldFindRiRecord()
    {
        var system = new RegistryHive(TestHives.Path(@"System"));
        system.FlushRecordListsAfterParse = false;
        system.ParseHive();

        var record = system.ListRecords[0x7141D0] as RiListRecord;

        record.Should().NotBeNull();
        record.IsReferenced = true;
        record.IsReferenced.Should().BeTrue();
        record.NumberOfEntries.Should().Be(2);
        record.Offsets[0].Should().Be(0x717020);
        record.Offsets[1].Should().Be(0x72F020);
        record.ToString().Should().NotBeEmpty();
    }

    [Test]
    public void ShouldIncreaseSoftParsingError()
    {
        var usrclass = new RegistryHive(TestHives.Path(@"UsrClass-win7.dat"));
        usrclass.RecoverDeleted = true;

        usrclass.SoftParsingErrors.Should().Be(0);

        usrclass.ParseHive();

        usrclass.SoftParsingErrors.Should().BeGreaterThan(0);
    }

    [Test]
    public void TestGetSidTypeFromSidString()
    {
        var sid = "S-1-5-5-111111";

        var desc = Helpers.GetSidTypeFromSidString(sid);

        ((object) desc).Should().BeOfType<Helpers.SidTypeEnum>();

        sid = "S-1-2-0";

        desc = Helpers.GetSidTypeFromSidString(sid);

        desc.Should().Be(Helpers.SidTypeEnum.Local);
    }

    [Test]
    public void VerifyHeaderInfo()
    {
        var sam = new RegistryHive(TestHives.Path(@"SAM"));
        sam.FlushRecordListsAfterParse = false;
        sam.ParseHive();

        sam.Header.Should().NotBeNull();
        sam.Header.FileName.Should().NotBeNull();
        sam.Header.FileName.Should().NotBeEmpty();
        sam.Header.Length.Should().BeGreaterThan(0);
        sam.Header.MajorVersion.Should().BeGreaterThan(0);
        sam.Header.MinorVersion.Should().BeGreaterThan(0);
        sam.Header.RootCellOffset.Should().BeGreaterThan(0);
        sam.Header.CalculatedChecksum.Should().Be(sam.Header.CheckSum);
        sam.Header.ValidateCheckSum().Should().Be(true);
        sam.Header.ToString().Should().NotBeEmpty();
    }
}