using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Registry.Cells;

namespace Registry.Test;

[TestFixture]
internal class TestVkCellRecord
{
    [Test]
    public void ShouldFindKeyValueAndCheckProperties()
    {
        var sam = new RegistryHive(TestHives.Path(@"SAM"));
        sam.FlushRecordListsAfterParse = false;
        sam.ParseHive();

        var key =
            sam.GetKey(0x418);

        key.Should().NotBeNull();

        key.ToString().Should().NotBeEmpty();

        var val = key.Values[0];

        //TODO Need to export to reg each kind too

        val.Should().NotBeNull();

        val.ValueName.Should().NotBeEmpty();
        val.ValueData.Should().BeEmpty();
        val.ValueSlack.Should().BeEmpty();
        val.ValueSlackRaw.Should().BeEmpty();
        val.ToString().Should().NotBeEmpty();
        val.ValueName.Should().Be("(default)");
        val.ValueType.Should().Be("RegNone");
        val.ValueData.Should().Be("");
        val.ValueSlack.Should().Be("");
        val.VkRecord.Size.Should().Be(-24);
        val.VkRecord.RelativeOffset.Should().Be(0x270);
        val.VkRecord.AbsoluteOffset.Should().Be(0x1270);
        val.VkRecord.Signature.Should().Be("vk");
        val.VkRecord.IsFree.Should().BeFalse();
        val.VkRecord.DataLength.Should().Be(0x80000000);
        val.VkRecord.OffsetToData.Should().Be(0);
        val.VkRecord.NameLength.Should().Be(0);
        val.VkRecord.NamePresentFlag.Should().Be(0);

        //This key has slack
        key =
            sam.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Account\Users\000001F4");

        key.Should().NotBeNull();

        val = key.Values[0];

        val.Should().NotBeNull();

        val.ValueName.Should().NotBeEmpty();
        val.ValueData.Should().NotBeEmpty();
        val.ValueSlack.Should().NotBeEmpty();
        val.ValueSlackRaw.Length.Should().BeGreaterThan(0);
        val.ToString().Should().NotBeEmpty();

        val.ValueName.Should().Be("F");
        val.ValueData.Should().NotBeEmpty();
        val.ValueData.Length.Should().Be(239);
        val.ValueSlack.Should().NotBeEmpty();
        val.ValueSlack.Length.Should().Be(11);
        val.ValueSlackRaw.Length.Should().Be(4);
        val.ToString().Should().NotBeEmpty();

        val.ValueType.Should().Be("RegBinary");
        val.ValueData.Should().Be(
                "02-00-01-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-FF-FF-FF-FF-FF-FF-FF-7F-00-00-00-00-00-00-00-00-F4-01-00-00-01-02-00-00-10-02-00-00-00-00-00-00-00-00-00-00-01-00-00-00-00-00-00-00-73-00-00-00");
        val.ValueSlack.Should().Be("1F-00-0F-00");
        val.VkRecord.Size.Should().Be(-32);
        val.VkRecord.RelativeOffset.Should().Be(0x39B8);
        val.VkRecord.AbsoluteOffset.Should().Be(0x49B8);
        val.VkRecord.Signature.Should().Be("vk");
        val.VkRecord.IsFree.Should().BeFalse();
        val.VkRecord.DataLength.Should().Be(0x50);
        val.VkRecord.OffsetToData.Should().Be(0x39D8);
        val.VkRecord.NameLength.Should().Be(0x1);
        val.VkRecord.NamePresentFlag.Should().Be(1);
        val.VkRecord.Padding.Length.Should().Be(7);
    }

    [Test]
    public void ShouldFindRegBigEndianDWordValues()
    {
        var samHasBigEndianOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM_hasBigEndianDWord"));
        var key =
            samHasBigEndianOnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Account\Aliases");

        key.Should().NotBeNull();

        var val = key.Values.Single(t => t.ValueName == "(default)");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegDwordBigEndian);
        val.VkRecord.ValueData.Should().Be((uint) 0);
        val.VkRecord.ValueDataSlack.Length.Should().Be(0);
    }

    [Test]
    public void ShouldFindRegBinaryValues()
    {
        var ntUser1OnDemand = new RegistryHiveOnDemand(TestHives.Path(@"NTUSER1.DAT"));
        var key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\Appearance\Schemes");

        key.Should().NotBeNull();

        var val = key.Values.Single(t => t.ValueName == "@themeui.dll,-850");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegBinary);
        val.VkRecord.ValueData.Should().BeOfType<byte[]>();
        val.ValueDataRaw.Length.Should().Be(712);
        val.VkRecord.ValueDataSlack.Length.Should().Be(4);


        key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\Desktop\WindowMetrics");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "IconFont");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegBinary);
        val.VkRecord.ValueData.Should().BeOfType<byte[]>();
        val.ValueDataRaw.Length.Should().Be(92);
        val.VkRecord.ValueDataSlack.Length.Should().Be(0);

        key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\Mouse");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "SmoothMouseXCurve");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegBinary);
        val.VkRecord.ValueData.Should().BeOfType<byte[]>();
        val.ValueDataRaw.Length.Should().Be(40);
        val.VkRecord.ValueDataSlack.Length.Should().Be(4);


        key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\PowerCfg\GlobalPowerPolicy");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "Policies");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegBinary);
        val.VkRecord.ValueData.Should().BeOfType<byte[]>();
        val.ValueDataRaw.Length.Should().Be(176);
        val.VkRecord.ValueDataSlack.Length.Should().Be(4);

        key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\Input Method\Hot Keys\00000010");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "Key Modifiers");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegBinary);
        val.VkRecord.ValueData.Should().BeOfType<byte[]>();
        val.ValueDataRaw.Length.Should().Be(4);
        val.VkRecord.ValueDataSlack.Length.Should().Be(0);
    }

    [Test]
    public void ShouldFindRegDWordValues()
    {
        var ntUser1OnDemand = new RegistryHiveOnDemand(TestHives.Path(@"NTUSER1.DAT"));
        var key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Wisp\Pen\SysEventParameters");

        key.Should().NotBeNull();

        var val = key.Values.Single(t => t.ValueName == "DblDist");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegDword);
        val.VkRecord.ValueData.Should().Be((uint) 20);
        val.VkRecord.ValueDataSlack.Length.Should().Be(0);

        key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows NT\CurrentVersion\Windows");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "UserSelectedDefault");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegDword);
        val.VkRecord.ValueData.Should().Be((uint) 0);
        val.VkRecord.ValueDataSlack.Length.Should().Be(0);

        key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows NT\CurrentVersion\MsiCorruptedFileRecovery\RepairedProducts");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "TimeWindowMinutes");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegDword);
        val.VkRecord.ValueData.Should().Be((uint) 1440);
        val.VkRecord.ValueDataSlack.Length.Should().Be(0);

        key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows\Windows Error Reporting");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "MaxArchiveCount");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegDword);
        val.VkRecord.ValueData.Should().Be((uint) 500);
        val.VkRecord.ValueDataSlack.Length.Should().Be(0);

        key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Console");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "ColorTable11");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegDword);
        val.VkRecord.ValueData.Should().Be((uint) 16776960);
        val.VkRecord.ValueDataSlack.Length.Should().Be(0);
    }

    [Test]
    public void ShouldFindRegExpandSzValues()
    {
        var ntUser1OnDemand = new RegistryHiveOnDemand(TestHives.Path(@"NTUSER1.DAT"));
        var key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Environment");

        key.Should().NotBeNull();

        var val = key.Values.Single(t => t.ValueName == "TEMP");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegExpandSz);
        val.VkRecord.ValueData.Should().Be(@"%USERPROFILE%\AppData\Local\Temp");
        val.VkRecord.ValueDataSlack.Length.Should().Be(2);

        key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\Cursors");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "Arrow");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegExpandSz);
        val.VkRecord.ValueData.Should().Be(@"%SystemRoot%\cursors\aero_arrow.cur");
        val.VkRecord.ValueDataSlack.Length.Should().Be(4);

        key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\AppEvents\Schemes\Apps\.Default\WindowsUAC\.Current");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "(default)");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegExpandSz);
        val.VkRecord.ValueData.Should().Be(@"%SystemRoot%\media\Windows User Account Control.wav");
        val.VkRecord.ValueDataSlack.Length.Should().Be(4);

        key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows\CurrentVersion\Themes");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "LastHighContrastTheme");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegExpandSz);
        val.VkRecord.ValueData.Should().Be(@"%SystemRoot%\resources\Ease of Access Themes\hcblack.theme");
        val.VkRecord.ValueDataSlack.Length.Should().Be(6);

        key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows\CurrentVersion\ThemeManager");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "DllName");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegExpandSz);
        val.VkRecord.ValueData.Should().Be(@"%SystemRoot%\resources\themes\Aero\Aero.msstyles");
        val.VkRecord.ValueDataSlack.Length.Should().Be(2);
    }

    [Test]
    public void ShouldFindRegMultiSzValues()
    {
        var ntUser1OnDemand = new RegistryHiveOnDemand(TestHives.Path(@"NTUSER1.DAT"));
        var key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\International\User Profile");

        key.Should().NotBeNull();

        var val = key.Values.Single(t => t.ValueName == "Languages");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegMultiSz);
        val.VkRecord.ValueData.Should().Be("en-US");
        val.VkRecord.ValueDataSlack.Length.Should().Be(0);


        var usrclassAcronis = new RegistryHive(TestHives.Path(@"Acronis_0x52_Usrclass.dat"));
        usrclassAcronis.RecoverDeleted = true;
        usrclassAcronis.FlushRecordListsAfterParse = false;
        usrclassAcronis.ParseHive();

        key =
            usrclassAcronis.GetKey(
                @"S-1-5-21-3851833874-1800822990-1357392098-1000_Classes\Local Settings\MuiCache\12\52C64B7E");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "LanguageList");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegMultiSz);
        val.VkRecord.ValueData.Should().Be("en-US en");
        val.VkRecord.ValueDataSlack.Length.Should().Be(0);

        var bcd = new RegistryHive(TestHives.Path(@"BCD"));
        bcd.FlushRecordListsAfterParse = false;
        bcd.RecoverDeleted = true;
        bcd.ParseHive();

        key =
            bcd.GetKey(
                @"System\Objects\{7ea2e1ac-2e61-4728-aaa3-896d9d0a9f0e}\Elements\14000006");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "Element");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegMultiSz);
        val.VkRecord.ValueData.Should().Be("{4636856e-540f-4170-a130-a84776f4c654} {0ce4991b-e6b3-4b16-b23c-5e0d9250e5d9}");
        val.VkRecord.ValueDataSlack.Length.Should().Be(6);

        key =
            bcd.GetKey(
                @"System\Objects\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\Elements\14000006");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "Element");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegMultiSz);
        val.VkRecord.ValueData.Should().Be("{7ea2e1ac-2e61-4728-aaa3-896d9d0a9f0e}");
        val.VkRecord.ValueDataSlack.Length.Should().Be(84);
    }

    [Test]
    public void ShouldFindRegQWordValues()
    {
        var ntUser1OnDemand = new RegistryHiveOnDemand(TestHives.Path(@"NTUSER1.DAT"));
        var key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows\Windows Error Reporting");

        key.Should().NotBeNull();

        var val = key.Values.Single(t => t.ValueName == "LastWatsonCabUploaded");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegQword);
        val.VkRecord.ValueData.Should().Be((ulong) 130557640214774914);
        val.VkRecord.ValueDataSlack.Length.Should().Be(4);

        key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows\CurrentVersion\Store\RefreshBannedAppList");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "BannedAppsLastModified");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegQword);
        val.VkRecord.ValueData.Should().Be((ulong) 0);
        val.VkRecord.ValueDataSlack.Length.Should().Be(4);

        var usrclassAcronis = new RegistryHive(TestHives.Path(@"Acronis_0x52_Usrclass.dat"));
        usrclassAcronis.RecoverDeleted = true;
        usrclassAcronis.FlushRecordListsAfterParse = false;
        usrclassAcronis.ParseHive();

        key =
            usrclassAcronis.GetKey(
                @"S-1-5-21-3851833874-1800822990-1357392098-1000_Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "LastAdvertisement");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegQword);
        val.VkRecord.ValueData.Should().Be((ulong) 130294002389413697);
        val.VkRecord.ValueDataSlack.Length.Should().Be(4);

        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();
        key =
            usrclassDeleted.GetKey(
                @"S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "LastAdvertisement");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegQword);
        val.VkRecord.ValueData.Should().Be((ulong) 130672934390152518);
        val.VkRecord.ValueDataSlack.Length.Should().Be(4);

        var ntUserSlack = new RegistryHive(TestHives.Path(@"NTUSER slack.DAT"));
        ntUserSlack.FlushRecordListsAfterParse = false;
        ntUserSlack.ParseHive();

        key =
            ntUserSlack.GetKey(
                @"$$$PROTO.HIV\Software\Microsoft\VisualStudio\7.0\External Tools");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "LastMerge");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegQword);
        val.VkRecord.ValueData.Should().Be((ulong) 127257359392030000);
        val.VkRecord.ValueDataSlack.Length.Should().Be(4);
    }

    [Test]
    public void ShouldFindRegSzValues()
    {
        var ntUser1OnDemand = new RegistryHiveOnDemand(TestHives.Path(@"NTUSER1.DAT"));
        var key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\CTF\Assemblies\0x00000409\{34745C63-B2F0-4784-8B67-5E12C8701A31}");

        key.Should().NotBeNull();

        var val = key.Values.Single(t => t.ValueName == "Default");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegSz);
        val.VkRecord.ValueData.Should().Be("{00000000-0000-0000-0000-000000000000}");
        val.VkRecord.ValueDataSlack.Length.Should().Be(6);

        key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\CTF\SortOrder\Language");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "00000000");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegSz);
        val.VkRecord.ValueData.Should().Be("00000409");
        val.VkRecord.ValueDataSlack.Length.Should().Be(2);

        key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Speech\Preferences\AppCompatDisableDictation");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "dwm.exe");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegSz);
        val.VkRecord.ValueData.Should().Be("");
        val.VkRecord.ValueDataSlack.Length.Should().Be(0);

        key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\EUDC\932");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "SystemDefaultEUDCFont");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegSz);
        val.VkRecord.ValueData.Should().Be("EUDC.TTE");
        val.VkRecord.ValueDataSlack.Length.Should().Be(2);

        key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\PowerCfg\PowerPolicies\4");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "Description");

        val.Should().NotBeNull();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegSz);
        val.VkRecord.ValueData.Should().Be("This scheme keeps the computer on and optimizes it for high performance.");
        val.VkRecord.ValueDataSlack.Length.Should().Be(2);
    }

    [Test]
    public void TestUnicodeNameWhereSupposedToBeAscii()
    {
        var ntUserSlack = new RegistryHive(TestHives.Path(@"NTUSER slack.DAT"));
        ntUserSlack.FlushRecordListsAfterParse = false;
        ntUserSlack.ParseHive();


        var key = ntUserSlack.CellRecords[0x293490] as VkCellRecord;

        key.Should().NotBeNull();
        key.ValueName.Should().NotBeEmpty();
        key.ValueData.Should().NotBeNull();
    }

    [Test]
    public void TestVkRecordBigData()
    {
        var softwareOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SOFTWARE"));
        var key =
            softwareOnDemand.GetKey(
                @"CMI-CreateHive{199DAFC2-6F16-4946-BF90-5A3FC3A60902}\\Microsoft\\SystemCertificates\\AuthRoot\\AutoUpdate");

        key.Should().NotBeNull();

        var val = key.Values.Single(t => t.ValueName == "EncodedCtl");

        val.Should().NotBeNull();

        val.ValueDataRaw.Length.Should().Be(123820);
    }

    [Test]
    public void TestVkRecordFileTimeRegType()
    {
        var systemOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SYSTEM"));
        var key =
            systemOnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\ControlSet001\Control\DeviceContainers\{00000000-0000-0000-FFFF-FFFFFFFFFFFF}\Properties\{3464f7a4-2444-40b1-980a-e0903cb6d912}\0008");

        key.Should().NotBeNull();

        var val = key.Values.Single(e => e.ValueName == "en-US");

        val.Should().NotBeNull();

        val.VkRecord.RelativeOffset.Should().Be(0x78170);
        val.VkRecord.AbsoluteOffset.Should().Be(0x79170);
        val.VkRecord.Size.Should().Be(-32);
        val.VkRecord.Signature.Should().Be("vk");
        val.VkRecord.IsFree.Should().BeFalse();
        val.VkRecord.NamePresentFlag.Should().Be(0x1);
        val.VkRecord.NameLength.Should().Be(0x5);
        val.VkRecord.ValueName.Should().Be("en-US");
        val.VkRecord.ValueData.Should().BeOfType<DateTimeOffset>();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegFileTime);
        val.VkRecord.DataTypeRaw.Should().Be(0x0010);
        val.VkRecord.DataLength.Should().Be((uint) 0x8);
        val.VkRecord.OffsetToData.Should().Be((uint) 0x77d78);
        val.VkRecord.Padding.Length.Should().Be(3);
        val.VkRecord.ValueDataRaw.Length.Should().Be(8);
        val.VkRecord.ValueDataSlack.Length.Should().Be(4);
        val.VkRecord.ToString().Should().NotBeEmpty();
    }

    [Test]
    public void TestVkRecordIsFreeDataBlockExceptions()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();


        var key = usrClass1.CellRecords[0x406180] as VkCellRecord;

        key.Should().NotBeNull();
        key.ValueDataRaw.Length.Should().Be(0);
        key.ValueData.Should().NotBeNull();
    }

    [Test]
    public void TestVkRecordIsFreeLessDataThanDataLength2()
    {
        var usrclassAcronis = new RegistryHive(TestHives.Path(@"Acronis_0x52_Usrclass.dat"));
        usrclassAcronis.RecoverDeleted = true;
        usrclassAcronis.FlushRecordListsAfterParse = false;
        usrclassAcronis.ParseHive();

        var val = usrclassAcronis.CellRecords[0x3f78] as VkCellRecord;

        val.Should().NotBeNull();

        val.RelativeOffset.Should().Be(0x3f78);
        val.AbsoluteOffset.Should().Be(0x4f78);
        val.ValueData.Should().BeOfType<byte[]>();
        val.Signature.Should().Be("vk");
        val.IsFree.Should().BeTrue();
        val.NamePresentFlag.Should().Be(0x1);
        val.NameLength.Should().Be(37);
        val.ValueName.Should().Be(@"@C:\Windows\System32\netcenter.dll,-2");
        val.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegSz);
        val.DataTypeRaw.Should().Be(1);
        val.DataLength.Should().Be((uint) 196);
        val.OffsetToData.Should().Be((uint) 61872);
        val.Padding.Length.Should().Be(3);
        val.ValueDataSlack.Length.Should().Be(0);
        val.ValueDataRaw.Length.Should().Be(76);
        val.ToString().Should().NotBeEmpty();
    }

    [Test]
    public void TestVkRecordQWordWithLengthOfZero()
    {
        var samDupeNameOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM_DUPENAME"));
        var key =
            samDupeNameOnDemand.GetKey(
                @"SAM\SAM\Domains\Builtin\Aliases\Members\S-1-5-21-4271176276-4210259494-4108073714");

        key.Should().NotBeNull();

        var val = key.Values.Single(e => e.ValueName == "(default)");

        val.Should().NotBeNull();

        val.VkRecord.RelativeOffset.Should().Be(0xA88);
        val.VkRecord.AbsoluteOffset.Should().Be(0x1A88);
        val.VkRecord.Size.Should().Be(-24);
        val.VkRecord.Signature.Should().Be("vk");
        val.VkRecord.IsFree.Should().BeFalse();
        val.VkRecord.NamePresentFlag.Should().Be(0x0);
        val.VkRecord.NameLength.Should().Be(0x0);
        val.VkRecord.ValueName.Should().Be("(default)");
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegQword);
        val.VkRecord.DataTypeRaw.Should().Be(11);
        val.VkRecord.DataLength.Should().Be(0x80000000);
        val.VkRecord.OffsetToData.Should().Be((uint) 0);
        val.VkRecord.Padding.Length.Should().Be(0);
        val.VkRecord.ValueData.Should().BeOfType<ulong>();
        val.VkRecord.ValueData.Should().Be((ulong) 0);
        val.VkRecord.ValueDataSlack.Length.Should().Be(0);
        val.VkRecord.ValueDataRaw.Length.Should().Be(0);
        val.VkRecord.ToString().Should().NotBeEmpty();
    }

    [Test]
    public void TestVkRecordRegBinary()
    {
        var samOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM"));
        var key =
            samOnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Account");

        key.Should().NotBeNull();

        var val = key.Values.Single(e => e.ValueName == "F");

        val.Should().NotBeNull();

        val.VkRecord.RelativeOffset.Should().Be(0x3078);
        val.VkRecord.AbsoluteOffset.Should().Be(0x4078);
        val.VkRecord.Size.Should().Be(-32);
        val.VkRecord.Signature.Should().Be("vk");
        val.VkRecord.IsFree.Should().BeFalse();
        val.VkRecord.NamePresentFlag.Should().Be(0x01);
        val.VkRecord.NameLength.Should().Be(1);
        val.VkRecord.ValueName.Should().Be("F");
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegBinary);
        val.VkRecord.DataLength.Should().Be((uint) 0xf0);
        val.VkRecord.OffsetToData.Should().Be((uint) 0x3098);
        val.VkRecord.Padding.Length.Should().Be(7);
        val.VkRecord.ValueData.Should().BeOfType<byte[]>();
        val.VkRecord.ValueDataSlack.Length.Should().Be(4);
        val.VkRecord.ValueDataRaw.Length.Should().Be(240);
        val.VkRecord.ToString().Should().NotBeEmpty();
    }

    [Test]
    public void TestVkRecordRegBinaryDeletedValue()
    {
        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();

        var key =
            usrclassDeleted.GetKey(
                @"S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\Software\Microsoft\Windows\Shell\BagMRU\1");

        key.Should().NotBeNull();

        var val = key.Values.Single(e => e.ValueName == "0");

        val.Should().NotBeNull();

        val.VkRecord.RelativeOffset.Should().Be(0x5328);
        val.VkRecord.AbsoluteOffset.Should().Be(0x6328);
        val.VkRecord.Size.Should().Be(0x100);
        val.VkRecord.Signature.Should().Be("vk");
        val.VkRecord.IsFree.Should().BeTrue();
        val.VkRecord.NamePresentFlag.Should().Be(0x01);
        val.VkRecord.NameLength.Should().Be(0x1);
        val.VkRecord.ValueName.Should().Be("0");
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegBinary);
        val.VkRecord.DataLength.Should().Be((uint) 0xE);
        val.VkRecord.OffsetToData.Should().Be((uint) 0x5348);
        val.VkRecord.Padding.Length.Should().Be(7);
        val.VkRecord.ValueData.Should().BeOfType<byte[]>();
        val.VkRecord.ValueDataSlack.Length.Should().Be(94);
        val.VkRecord.ValueDataRaw.Length.Should().Be(14);
        val.VkRecord.ToString().Should().NotBeEmpty();
    }

    [Test]
    public void TestVkRecordRegDWord()
    {
        var samOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM"));
        var key =
            samOnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\LastSkuUpgrade");

        key.Should().NotBeNull();

        var val = key.Values.Single(e => e.ValueName == "(default)");

        val.Should().NotBeNull();

        val.VkRecord.RelativeOffset.Should().Be(0x258);
        val.VkRecord.AbsoluteOffset.Should().Be(0x1258);
        val.VkRecord.Size.Should().Be(-24);
        val.VkRecord.Signature.Should().Be("vk");
        val.VkRecord.IsFree.Should().BeFalse();
        val.VkRecord.NamePresentFlag.Should().Be(0x00);
        val.VkRecord.NameLength.Should().Be(0);
        val.VkRecord.ValueName.Should().Be("(default)");
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegDword);
        val.VkRecord.DataLength.Should().Be(0x80000004);
        val.VkRecord.OffsetToData.Should().Be((uint) 0x07);
        val.ValueData.Should().Be("7");
        val.VkRecord.Padding.Length.Should().Be(0);
        val.VkRecord.ValueData.Should().BeOfType<uint>();
        val.VkRecord.ValueDataSlack.Length.Should().Be(0);
        val.VkRecord.ValueDataRaw.Length.Should().Be(4);
        val.VkRecord.ToString().Should().NotBeEmpty();
    }

    [Test]
    public void TestVkRecordRegMultiSz()
    {
        var usrClassDeletedBagsOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"UsrClassDeletedBags.dat"));
        var key =
            usrClassDeletedBagsOnDemand.GetKey(
                @"S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\MuiCache\6\52C64B7E");

        key.Should().NotBeNull();

        var val = key.Values.Single(e => e.ValueName == "LanguageList");

        val.Should().NotBeNull();

        val.VkRecord.RelativeOffset.Should().Be(0x5f0);
        val.VkRecord.AbsoluteOffset.Should().Be(0x15f0);
        val.VkRecord.Size.Should().Be(-40);
        val.VkRecord.Signature.Should().Be("vk");
        val.VkRecord.IsFree.Should().BeFalse();
        val.VkRecord.NamePresentFlag.Should().Be(0x01);
        val.VkRecord.NameLength.Should().Be(0xC);
        val.VkRecord.ValueName.Should().Be("LanguageList");
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegMultiSz);
        val.VkRecord.DataLength.Should().Be((uint) 0x14);
        val.VkRecord.OffsetToData.Should().Be((uint) 0xf70);
        val.ValueData.Should().Be("en-US en");
        val.VkRecord.Padding.Length.Should().Be(4);
        val.VkRecord.ValueData.Should().BeOfType<string>();
        val.VkRecord.ValueDataSlack.Length.Should().Be(0);
        val.VkRecord.ValueDataRaw.Length.Should().Be(20);
        val.VkRecord.ToString().Should().NotBeEmpty();
    }

    [Test]
    public void TestVkRecordRegNone()
    {
        var samOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM"));
        var key =
            samOnDemand.GetKey(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains");

        key.Should().NotBeNull();

        var val = key.Values.Single(e => e.ValueName == "(default)");

        val.Should().NotBeNull();

        val.VkRecord.RelativeOffset.Should().Be(0x270);
        val.VkRecord.AbsoluteOffset.Should().Be(0x1270);
        val.VkRecord.Size.Should().Be(-24);
        val.VkRecord.Signature.Should().Be("vk");
        val.VkRecord.IsFree.Should().BeFalse();
        val.VkRecord.NamePresentFlag.Should().Be(0x00);
        val.VkRecord.NameLength.Should().Be(0);
        val.VkRecord.ValueName.Should().Be("(default)");
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegNone);
        val.VkRecord.DataLength.Should().Be(0x80000000);
        val.VkRecord.OffsetToData.Should().Be((uint) 0x0);
        val.VkRecord.Padding.Length.Should().Be(0);
        val.VkRecord.ValueData.Should().BeOfType<byte[]>();
        val.VkRecord.ValueDataSlack.Length.Should().Be(0);
        val.VkRecord.ValueDataRaw.Length.Should().Be(0);
        val.VkRecord.ToString().Should().NotBeEmpty();
    }

    [Test]
    public void TestVkRecordRegqWord()
    {
        var ntUser1OnDemand = new RegistryHiveOnDemand(TestHives.Path(@"NTUSER1.DAT"));
        var key =
            ntUser1OnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows\CurrentVersion\Store\RefreshBannedAppList");

        key.Should().NotBeNull();

        var val = key.Values.Single(e => e.ValueName == "BannedAppsLastModified");

        val.Should().NotBeNull();

        val.VkRecord.RelativeOffset.Should().Be(0x5ce0);
        val.VkRecord.AbsoluteOffset.Should().Be(0x6ce0);
        val.VkRecord.Size.Should().Be(-48);
        val.VkRecord.Signature.Should().Be("vk");
        val.VkRecord.IsFree.Should().BeFalse();
        val.VkRecord.NamePresentFlag.Should().Be(0x01);
        val.VkRecord.NameLength.Should().Be(0x16);
        val.VkRecord.ValueName.Should().Be("BannedAppsLastModified");
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegQword);
        val.VkRecord.DataLength.Should().Be((uint) 0x8);
        val.VkRecord.OffsetToData.Should().Be((uint) 0x3b70);
        val.ValueData.Should().Be("0");
        val.VkRecord.Padding.Length.Should().Be(2);
        val.VkRecord.ValueData.Should().BeOfType<ulong>();
        val.VkRecord.ValueDataSlack.Length.Should().Be(4);
        val.VkRecord.ValueDataRaw.Length.Should().Be(8);
        val.VkRecord.ToString().Should().NotBeEmpty();
    }

    [Test]
    public void TestVkRecordRegSz()
    {
        var samOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM"));
        var key =
            samOnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Builtin\Aliases\Members\S-1-5-21-727398572-3617256236-2003601904\00000201");

        key.Should().NotBeNull();

        var val = key.Values.Single(e => e.ValueName == "(default)");

        val.Should().NotBeNull();

        val.VkRecord.RelativeOffset.Should().Be(0xFE0);
        val.VkRecord.AbsoluteOffset.Should().Be(0x1FE0);
        val.VkRecord.Size.Should().Be(-24);
        val.VkRecord.Signature.Should().Be("vk");
        val.VkRecord.IsFree.Should().BeFalse();
        val.VkRecord.NamePresentFlag.Should().Be(0x00);
        val.VkRecord.NameLength.Should().Be(0);
        val.VkRecord.ValueName.Should().Be("(default)");
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegSz);
        val.VkRecord.DataLength.Should().Be(0x80000004);
        val.VkRecord.OffsetToData.Should().Be((uint) 0x0221);
        val.VkRecord.Padding.Length.Should().Be(0);
        val.VkRecord.ValueData.Should().BeOfType<string>();
        val.VkRecord.ValueDataSlack.Length.Should().Be(0);
        val.VkRecord.ValueDataRaw.Length.Should().Be(4);
        val.VkRecord.ToString().Should().NotBeEmpty();
    }

    [Test]
    public void TestVkRecordRegUnknown()
    {
        var samHasBigEndianOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM_hasBigEndianDWord"));
        var key =
            samHasBigEndianOnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Account\Groups\Names\None");

        key.Should().NotBeNull();

        var val = key.Values.Single(e => e.ValueName == "(default)");

        val.Should().NotBeNull();

        val.VkRecord.RelativeOffset.Should().Be(0x1248);
        val.VkRecord.AbsoluteOffset.Should().Be(0x2248);
        val.VkRecord.Size.Should().Be(-24);
        val.VkRecord.Signature.Should().Be("vk");
        val.VkRecord.IsFree.Should().BeFalse();
        val.VkRecord.NamePresentFlag.Should().Be(0x0);
        val.VkRecord.NameLength.Should().Be(0);
        val.VkRecord.ValueName.Should().Be("(default)");
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegUnknown);
        val.VkRecord.DataTypeRaw.Should().Be(513);
        val.VkRecord.DataLength.Should().Be(0x80000000);
        val.VkRecord.OffsetToData.Should().Be((uint) 0);
        val.VkRecord.Padding.Length.Should().Be(0);
        val.VkRecord.ValueData.Should().BeOfType<byte[]>();
        val.VkRecord.ValueDataSlack.Length.Should().Be(0);
        //resident, with a data length of 0: no data (it used to be the 4 bytes of the data offset field)
        val.VkRecord.ValueDataRaw.Length.Should().Be(0);
        val.VkRecord.ToString().Should().NotBeEmpty();
    }

    [Test]
    public void TestVkRecordUnknownRegType()
    {
        var samDupeNameOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM_DUPENAME"));
        var key = samDupeNameOnDemand.GetKey(@"SAM\SAM\Domains\Account\Users");

        key.Should().NotBeNull();

        var val = key.Values.Single(e => e.ValueName == "(default)");

        val.Should().NotBeNull();

        val.VkRecord.RelativeOffset.Should().Be(0x1880);
        val.VkRecord.AbsoluteOffset.Should().Be(0x2880);
        val.VkRecord.Size.Should().Be(-24);
        val.VkRecord.Signature.Should().Be("vk");
        val.VkRecord.IsFree.Should().BeFalse();
        val.VkRecord.NamePresentFlag.Should().Be(0x0);
        val.VkRecord.NameLength.Should().Be(0x0);
        val.VkRecord.ValueName.Should().Be("(default)");
        val.VkRecord.ValueData.Should().BeOfType<byte[]>();
        val.VkRecord.DataType.Should().Be(VkCellRecord.DataTypeEnum.RegUnknown);
        val.VkRecord.DataTypeRaw.Should().Be(15);
        val.VkRecord.DataLength.Should().Be(0x80000000);
        val.VkRecord.OffsetToData.Should().Be((uint) 0);
        val.VkRecord.Padding.Length.Should().Be(0);
        //resident, with a data length of 0: no data (it used to be the 4 bytes of the data offset field)
        val.VkRecord.ValueDataRaw.Length.Should().Be(0);
        val.VkRecord.ValueDataSlack.Length.Should().Be(0);
        val.VkRecord.ToString().Should().NotBeEmpty();
    }

    [Test]
    public void ShouldReadUnicodeValueNamesWithoutLatinCharacters()
    {
        // Saved with reg.exe: each value name is stored in UTF-16LE (no ASCII flag). Names made only of characters
        // above U+00FF have no 0x00 byte in their UTF-16LE form.
        var hive = new RegistryHiveOnDemand(TestHives.Path(@"UnicodeValueNames"));
        var key = hive.GetKey(@"ApUnicodeValueNames\日本語キー");

        key.Should().NotBeNull();

        var names = key.Values.Select(v => v.ValueName).ToList();

        names.Should().Contain("値の名前", "中文名称", "Полиморфизм", "Abc値");
        names.Should().NotContain("(Unable to determine name)");

        var val = key.Values.Single(v => v.ValueName == "値の名前");

        val.VkRecord.IsFree.Should().BeFalse();
        val.VkRecord.NamePresentFlag.Should().Be(0x0);
        val.VkRecord.NameLength.Should().Be(8);
        val.ValueData.Should().Be("データ");
    }
}