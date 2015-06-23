using System;
using System.Linq;
using NFluent;
using NUnit.Framework;
using Registry.Cells;

namespace Registry.Test
{
    [TestFixture]
    internal class TestVKCellRecord
    {
        [Test]
        public void ShouldFindKeyValueAndCheckProperties()
        {
            var key =
                TestSetup.Sam.GetKey(0x418);

            Check.That(key).IsNotNull();

            Check.That(key.ToString()).IsNotEmpty();

            var val = key.Values[0];

            //TODO Need to export to reg each kind too

            Check.That(val).IsNotNull();

            Check.That(val.ValueName).IsNotEmpty();
            Check.That(val.ValueData).IsEmpty();
            Check.That(val.ValueSlack).IsEmpty();
            Check.That(val.ValueSlackRaw).IsEmpty();
            Check.That(val.ToString()).IsNotEmpty();
            Check.That(val.ValueName).IsEqualTo("(default)");
            Check.That(val.ValueType).IsEqualTo("RegNone");
            Check.That(val.ValueData).IsEqualTo("");
            Check.That(val.ValueSlack).IsEqualTo("");
            Check.That(val.VKRecord.Size).IsEqualTo(-24);
            Check.That(val.VKRecord.RelativeOffset).IsEqualTo(0x270);
            Check.That(val.VKRecord.AbsoluteOffset).IsEqualTo(0x1270);
            Check.That(val.VKRecord.Signature).IsEqualTo("vk");
            Check.That(val.VKRecord.IsFree).IsFalse();
            Check.That(val.VKRecord.DataLength).IsEqualTo(0x80000000);
            Check.That(val.VKRecord.OffsetToData).IsEqualTo((uint)0);
            Check.That(val.VKRecord.NameLength).IsEqualTo((ushort)0);
            Check.That(val.VKRecord.NamePresentFlag).IsEqualTo((ushort)0);

            //This key has slack
            key =
                TestSetup.Sam.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Account\Users\000001F4");

            Check.That(key).IsNotNull();

            val = key.Values[0];

            Check.That(val).IsNotNull();

            Check.That(val.ValueName).IsNotEmpty();
            Check.That(val.ValueData).IsNotEmpty();
            Check.That(val.ValueSlack).IsNotEmpty();
            Check.That(val.ValueSlackRaw.Length).IsGreaterThan(0);
            Check.That(val.ToString()).IsNotEmpty();

            Check.That(val.ValueName).IsEqualTo("F");
            Check.That(val.ValueData).IsNotEmpty();
            Check.That(val.ValueData.Length).IsEqualTo(239);
            Check.That(val.ValueSlack).IsNotEmpty();
            Check.That(val.ValueSlack.Length).IsEqualTo(11);
            Check.That(val.ValueSlackRaw.Length).IsEqualTo(4);
            Check.That(val.ToString()).IsNotEmpty();

            Check.That(val.ValueType).IsEqualTo("RegBinary");
            Check.That(val.ValueData).IsEqualTo("02-00-01-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-FF-FF-FF-FF-FF-FF-FF-7F-00-00-00-00-00-00-00-00-F4-01-00-00-01-02-00-00-10-02-00-00-00-00-00-00-00-00-00-00-01-00-00-00-00-00-00-00-73-00-00-00");
            Check.That(val.ValueSlack).IsEqualTo("1F-00-0F-00");
            Check.That(val.VKRecord.Size).IsEqualTo(-32);
            Check.That(val.VKRecord.RelativeOffset).IsEqualTo(0x39B8);
            Check.That(val.VKRecord.AbsoluteOffset).IsEqualTo(0x49B8);
            Check.That(val.VKRecord.Signature).IsEqualTo("vk");
            Check.That(val.VKRecord.IsFree).IsFalse();
            Check.That(val.VKRecord.DataLength).IsEqualTo((uint)0x50);
            Check.That(val.VKRecord.OffsetToData).IsEqualTo((uint)0x39D8);
            Check.That(val.VKRecord.NameLength).IsEqualTo((ushort)0x1);
            Check.That(val.VKRecord.NamePresentFlag).IsEqualTo((ushort)1);
            Check.That(val.VKRecord.Padding.Length).IsEqualTo(7);
        }

        [Test]
        public void ShouldFindRegBigEndianDWordValues()
        {
            var key =
                TestSetup.SamHasBigEndianOnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Account\Aliases");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "(default)");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegDwordBigEndian);
            Check.That(val.VKRecord.ValueData).IsEqualTo((uint) 0);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(0);
        }

        [Test]
        public void ShouldFindRegBinaryValues()
        {
            var key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\Appearance\Schemes");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "@themeui.dll,-850");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegBinary);
            Check.That(val.VKRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.ValueDataRaw.Length).IsEqualTo(712);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(4);

            key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\Desktop\WindowMetrics");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "IconFont");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegBinary);
            Check.That(val.VKRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.ValueDataRaw.Length).IsEqualTo(92);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(0);

            key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\Mouse");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "SmoothMouseXCurve");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegBinary);
            Check.That(val.VKRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.ValueDataRaw.Length).IsEqualTo(40);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(4);


            key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\PowerCfg\GlobalPowerPolicy");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "Policies");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegBinary);
            Check.That(val.VKRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.ValueDataRaw.Length).IsEqualTo(176);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(4);

            key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\Input Method\Hot Keys\00000010");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "Key Modifiers");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegBinary);
            Check.That(val.VKRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.ValueDataRaw.Length).IsEqualTo(4);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(0);
        }

        [Test]
        public void ShouldFindRegDWordValues()
        {
            var key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Wisp\Pen\SysEventParameters");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "DblDist");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegDword);
            Check.That(val.VKRecord.ValueData).IsEqualTo((uint) 20);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(0);

            key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows NT\CurrentVersion\Windows");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "UserSelectedDefault");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegDword);
            Check.That(val.VKRecord.ValueData).IsEqualTo((uint) 0);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(0);

            key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows NT\CurrentVersion\MsiCorruptedFileRecovery\RepairedProducts");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "TimeWindowMinutes");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegDword);
            Check.That(val.VKRecord.ValueData).IsEqualTo((uint) 1440);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(0);

            key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows\Windows Error Reporting");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "MaxArchiveCount");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegDword);
            Check.That(val.VKRecord.ValueData).IsEqualTo((uint) 500);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(0);

            key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Console");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "ColorTable11");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegDword);
            Check.That(val.VKRecord.ValueData).IsEqualTo((uint) 16776960);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(0);
        }

        [Test]
        public void ShouldFindRegExpandSzValues()
        {
            var key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Environment");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "TEMP");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegExpandSz);
            Check.That(val.VKRecord.ValueData).IsEqualTo(@"%USERPROFILE%\AppData\Local\Temp");
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(2);

            key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\Cursors");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "Arrow");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegExpandSz);
            Check.That(val.VKRecord.ValueData).IsEqualTo(@"%SystemRoot%\cursors\aero_arrow.cur");
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(4);

            key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\AppEvents\Schemes\Apps\.Default\WindowsUAC\.Current");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "(default)");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegExpandSz);
            Check.That(val.VKRecord.ValueData).IsEqualTo(@"%SystemRoot%\media\Windows User Account Control.wav");
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(4);

            key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows\CurrentVersion\Themes");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "LastHighContrastTheme");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegExpandSz);
            Check.That(val.VKRecord.ValueData).IsEqualTo(@"%SystemRoot%\resources\Ease of Access Themes\hcblack.theme");
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(6);

            key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows\CurrentVersion\ThemeManager");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "DllName");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegExpandSz);
            Check.That(val.VKRecord.ValueData).IsEqualTo(@"%SystemRoot%\resources\themes\Aero\Aero.msstyles");
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(2);
        }

        [Test]
        public void ShouldFindRegMultiSzValues()
        {
            var key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\International\User Profile");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "Languages");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegMultiSz);
            Check.That(val.VKRecord.ValueData).IsEqualTo("en-US");
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(0);

            key =
                TestSetup.UsrclassAcronis.GetKey(
                    @"S-1-5-21-3851833874-1800822990-1357392098-1000_Classes\Local Settings\MuiCache\12\52C64B7E");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "LanguageList");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegMultiSz);
            Check.That(val.VKRecord.ValueData).IsEqualTo("en-US en");
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(0);

            key =
                TestSetup.Bcd.GetKey(
                    @"System\Objects\{7ea2e1ac-2e61-4728-aaa3-896d9d0a9f0e}\Elements\14000006");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "Element");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegMultiSz);
            Check.That(val.VKRecord.ValueData)
                .IsEqualTo("{4636856e-540f-4170-a130-a84776f4c654} {0ce4991b-e6b3-4b16-b23c-5e0d9250e5d9}");
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(6);

            key =
                TestSetup.Bcd.GetKey(
                    @"System\Objects\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\Elements\14000006");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "Element");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegMultiSz);
            Check.That(val.VKRecord.ValueData).IsEqualTo("{7ea2e1ac-2e61-4728-aaa3-896d9d0a9f0e}");
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(84);
        }

        [Test]
        public void ShouldFindRegQWordValues()
        {
            var key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows\Windows Error Reporting");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "LastWatsonCabUploaded");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegQword);
            Check.That(val.VKRecord.ValueData).IsEqualTo((ulong) 130557640214774914);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(4);

            key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows\CurrentVersion\Store\RefreshBannedAppList");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "BannedAppsLastModified");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegQword);
            Check.That(val.VKRecord.ValueData).IsEqualTo((ulong) 0);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(4);

            key =
                TestSetup.UsrclassAcronis.GetKey(
                    @"S-1-5-21-3851833874-1800822990-1357392098-1000_Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "LastAdvertisement");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegQword);
            Check.That(val.VKRecord.ValueData).IsEqualTo((ulong) 130294002389413697);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(4);


            key =
                TestSetup.UsrclassDeleted.GetKey(
                    @"S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "LastAdvertisement");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegQword);
            Check.That(val.VKRecord.ValueData).IsEqualTo((ulong) 130672934390152518);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(4);

            key =
                TestSetup.NtUserSlack.GetKey(
                    @"$$$PROTO.HIV\Software\Microsoft\VisualStudio\7.0\External Tools");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "LastMerge");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegQword);
            Check.That(val.VKRecord.ValueData).IsEqualTo((ulong) 127257359392030000);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(4);
        }

        [Test]
        public void ShouldFindRegSzValues()
        {
            var key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\CTF\Assemblies\0x00000409\{34745C63-B2F0-4784-8B67-5E12C8701A31}");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "Default");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegSz);
            Check.That(val.VKRecord.ValueData).IsEqualTo("{00000000-0000-0000-0000-000000000000}");
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(6);

            key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\CTF\SortOrder\Language");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "00000000");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegSz);
            Check.That(val.VKRecord.ValueData).IsEqualTo("00000409");
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(2);

            key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Speech\Preferences\AppCompatDisableDictation");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "dwm.exe");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegSz);
            Check.That(val.VKRecord.ValueData).IsEqualTo("");
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(0);

            key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\EUDC\932");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "SystemDefaultEUDCFont");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegSz);
            Check.That(val.VKRecord.ValueData).IsEqualTo("EUDC.TTE");
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(2);

            key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\PowerCfg\PowerPolicies\4");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "Description");

            Check.That(val).IsNotNull();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegSz);
            Check.That(val.VKRecord.ValueData)
                .IsEqualTo("This scheme keeps the computer on and optimizes it for high performance.");
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(2);
        }

        [Test]
        public void TestUnicodeNameWhereSupposedToBeASCII()
        {
            var key = TestSetup.NtUserSlack.CellRecords[0x293490] as VKCellRecord;

            Check.That(key).IsNotNull();
            Check.That(key.ValueName).IsNotEmpty();
            Check.That(key.ValueData).IsNotNull();
        }

        [Test]
        public void TestVKRecordBigData()
        {
            var key = TestSetup.SoftwareOnDemand.GetKey(@"CMI-CreateHive{199DAFC2-6F16-4946-BF90-5A3FC3A60902}\\Microsoft\\SystemCertificates\\AuthRoot\\AutoUpdate");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "EncodedCtl");

            Check.That(val).IsNotNull();

            Check.That(val.ValueDataRaw.Length).Equals(123820);
        }

        [Test]
        public void TestVKRecordFileTimeRegType()
        {
            var key =
                TestSetup.SystemOnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\ControlSet001\Control\DeviceContainers\{00000000-0000-0000-FFFF-FFFFFFFFFFFF}\Properties\{3464f7a4-2444-40b1-980a-e0903cb6d912}\0008");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "en-US");

            Check.That(val).IsNotNull();

            Check.That(val.VKRecord.RelativeOffset).IsEqualTo(0x78170);
            Check.That(val.VKRecord.AbsoluteOffset).IsEqualTo(0x79170);
            Check.That(val.VKRecord.Size).IsEqualTo(-32);
            Check.That(val.VKRecord.Signature).IsEqualTo("vk");
            Check.That(val.VKRecord.IsFree).IsFalse();
            Check.That(val.VKRecord.NamePresentFlag).IsEqualTo((ushort) 0x1);
            Check.That(val.VKRecord.NameLength).IsEqualTo((ushort) 0x5);
            Check.That(val.VKRecord.ValueName).IsEqualTo("en-US");
            Check.That(val.VKRecord.ValueData).IsInstanceOf<DateTimeOffset>();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegFileTime);
            Check.That(val.VKRecord.DataTypeRaw).IsEqualTo((uint) 0x0010);
            Check.That(val.VKRecord.DataLength).Equals((uint) 0x8);
            Check.That(val.VKRecord.OffsetToData).Equals((uint) 0x77d78);
            Check.That(val.VKRecord.Padding.Length).Equals(3);
            Check.That(val.VKRecord.ValueDataRaw.Length).IsEqualTo(8);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(4);
            Check.That(val.VKRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVKRecordIsFreeDataBlockExceptions()
        {
            var key = TestSetup.UsrClass1.CellRecords[0x406180] as VKCellRecord;

            Check.That(key).IsNotNull();
            Check.That(key.ValueDataRaw.Length).IsEqualTo(0);
            Check.That(key.ValueData).IsNotNull();
        }

        [Test]
        public void TestVKRecordIsFreeLessDataThanDataLength2()
        {
            var val = TestSetup.UsrclassAcronis.CellRecords[0x3f78] as VKCellRecord;

            Check.That(val).IsNotNull();

            Check.That(val.RelativeOffset).IsEqualTo(0x3f78);
            Check.That(val.AbsoluteOffset).IsEqualTo(0x4f78);
            Check.That(val.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.Signature).IsEqualTo("vk");
            Check.That(val.IsFree).IsTrue();
            Check.That(val.NamePresentFlag).IsEqualTo((ushort) 0x1);
            Check.That(val.NameLength).IsEqualTo((ushort) 37);
            Check.That(val.ValueName).IsEqualTo(@"@C:\Windows\System32\netcenter.dll,-2");
            Check.That(val.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegSz);
            Check.That(val.DataTypeRaw).IsEqualTo((uint) 1);
            Check.That(val.DataLength).Equals((uint) 196);
            Check.That(val.OffsetToData).Equals((uint) 61872);
            Check.That(val.Padding.Length).Equals(3);
            Check.That(val.ValueDataSlack.Length).IsEqualTo(0);
            Check.That(val.ValueDataRaw.Length).IsEqualTo(76);
            Check.That(val.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVKRecordQWordWithLengthOfZero()
        {
            var key =
                TestSetup.SamDupeNameOnDemand.GetKey(
                    @"SAM\SAM\Domains\Builtin\Aliases\Members\S-1-5-21-4271176276-4210259494-4108073714");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "(default)");

            Check.That(val).IsNotNull();

            Check.That(val.VKRecord.RelativeOffset).IsEqualTo(0xA88);
            Check.That(val.VKRecord.AbsoluteOffset).IsEqualTo(0x1A88);
            Check.That(val.VKRecord.Size).IsEqualTo(-24);
            Check.That(val.VKRecord.Signature).IsEqualTo("vk");
            Check.That(val.VKRecord.IsFree).IsFalse();
            Check.That(val.VKRecord.NamePresentFlag).IsEqualTo((ushort) 0x0);
            Check.That(val.VKRecord.NameLength).IsEqualTo((ushort) 0x0);
            Check.That(val.VKRecord.ValueName).IsEqualTo("(default)");
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegQword);
            Check.That(val.VKRecord.DataTypeRaw).IsEqualTo((uint) 11);
            Check.That(val.VKRecord.DataLength).Equals(0x80000000);
            Check.That(val.VKRecord.OffsetToData).Equals((uint) 0);
            Check.That(val.VKRecord.Padding.Length).Equals(0);
            Check.That(val.VKRecord.ValueData).IsInstanceOf<ulong>();
            Check.That(val.VKRecord.ValueData).IsEqualTo((ulong) 0);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(0);
            Check.That(val.VKRecord.ValueDataRaw.Length).IsEqualTo(0);
            Check.That(val.VKRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVKRecordRegBinary()
        {
            var key =
                TestSetup.SamOnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Account");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "F");

            Check.That(val).IsNotNull();

            Check.That(val.VKRecord.RelativeOffset).IsEqualTo(0x3078);
            Check.That(val.VKRecord.AbsoluteOffset).IsEqualTo(0x4078);
            Check.That(val.VKRecord.Size).IsEqualTo(-32);
            Check.That(val.VKRecord.Signature).IsEqualTo("vk");
            Check.That(val.VKRecord.IsFree).IsFalse();
            Check.That(val.VKRecord.NamePresentFlag).IsEqualTo((ushort) 0x01);
            Check.That(val.VKRecord.NameLength).IsEqualTo((ushort) 1);
            Check.That(val.VKRecord.ValueName).IsEqualTo("F");
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegBinary);
            Check.That(val.VKRecord.DataLength).Equals((uint) 0xf0);
            Check.That(val.VKRecord.OffsetToData).Equals((uint) 0x3098);
            Check.That(val.VKRecord.Padding.Length).Equals(7);
            Check.That(val.VKRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(4);
            Check.That(val.VKRecord.ValueDataRaw.Length).IsEqualTo(240);
            Check.That(val.VKRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVKRecordRegBinaryDeletedValue()
        {
            var key =
                TestSetup.UsrclassDeleted.GetKey(
                    @"S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\Software\Microsoft\Windows\Shell\BagMRU\1");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "0");

            Check.That(val).IsNotNull();

            Check.That(val.VKRecord.RelativeOffset).IsEqualTo(0x5328);
            Check.That(val.VKRecord.AbsoluteOffset).IsEqualTo(0x6328);
            Check.That(val.VKRecord.Size).IsEqualTo(0x100);
            Check.That(val.VKRecord.Signature).IsEqualTo("vk");
            Check.That(val.VKRecord.IsFree).IsTrue();
            Check.That(val.VKRecord.NamePresentFlag).IsEqualTo((ushort) 0x01);
            Check.That(val.VKRecord.NameLength).IsEqualTo((ushort) 0x1);
            Check.That(val.VKRecord.ValueName).IsEqualTo("0");
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegBinary);
            Check.That(val.VKRecord.DataLength).Equals((uint) 0xE);
            Check.That(val.VKRecord.OffsetToData).Equals((uint) 0x5348);
            Check.That(val.VKRecord.Padding.Length).Equals(7);
            Check.That(val.VKRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(94);
            Check.That(val.VKRecord.ValueDataRaw.Length).IsEqualTo(14);
            Check.That(val.VKRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVKRecordRegDWord()
        {
            var key =
                TestSetup.SamOnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\LastSkuUpgrade");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "(default)");

            Check.That(val).IsNotNull();

            Check.That(val.VKRecord.RelativeOffset).IsEqualTo(0x258);
            Check.That(val.VKRecord.AbsoluteOffset).IsEqualTo(0x1258);
            Check.That(val.VKRecord.Size).IsEqualTo(-24);
            Check.That(val.VKRecord.Signature).IsEqualTo("vk");
            Check.That(val.VKRecord.IsFree).IsFalse();
            Check.That(val.VKRecord.NamePresentFlag).IsEqualTo((ushort) 0x00);
            Check.That(val.VKRecord.NameLength).IsEqualTo((ushort) 0);
            Check.That(val.VKRecord.ValueName).IsEqualTo("(default)");
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegDword);
            Check.That(val.VKRecord.DataLength).Equals(0x80000004);
            Check.That(val.VKRecord.OffsetToData).Equals((uint) 0x07);
            Check.That(val.ValueData).Equals("7");
            Check.That(val.VKRecord.Padding.Length).Equals(0);
            Check.That(val.VKRecord.ValueData).IsInstanceOf<uint>();
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(0);
            Check.That(val.VKRecord.ValueDataRaw.Length).IsEqualTo(4);
            Check.That(val.VKRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVKRecordRegMultiSz()
        {
            var key =
                TestSetup.UsrClassDeletedBagsOnDemand.GetKey(
                    @"S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\MuiCache\6\52C64B7E");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "LanguageList");

            Check.That(val).IsNotNull();

            Check.That(val.VKRecord.RelativeOffset).IsEqualTo(0x5f0);
            Check.That(val.VKRecord.AbsoluteOffset).IsEqualTo(0x15f0);
            Check.That(val.VKRecord.Size).IsEqualTo(-40);
            Check.That(val.VKRecord.Signature).IsEqualTo("vk");
            Check.That(val.VKRecord.IsFree).IsFalse();
            Check.That(val.VKRecord.NamePresentFlag).IsEqualTo((ushort) 0x01);
            Check.That(val.VKRecord.NameLength).IsEqualTo((ushort) 0xC);
            Check.That(val.VKRecord.ValueName).IsEqualTo("LanguageList");
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegMultiSz);
            Check.That(val.VKRecord.DataLength).Equals((uint) 0x14);
            Check.That(val.VKRecord.OffsetToData).Equals((uint) 0xf70);
            Check.That(val.ValueData).Equals("en-US en");
            Check.That(val.VKRecord.Padding.Length).Equals(4);
            Check.That(val.VKRecord.ValueData).IsInstanceOf<string>();
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(0);
            Check.That(val.VKRecord.ValueDataRaw.Length).IsEqualTo(20);
            Check.That(val.VKRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVKRecordRegNone()
        {
            var key =
                TestSetup.SamOnDemand.GetKey(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "(default)");

            Check.That(val).IsNotNull();

            Check.That(val.VKRecord.RelativeOffset).IsEqualTo(0x270);
            Check.That(val.VKRecord.AbsoluteOffset).IsEqualTo(0x1270);
            Check.That(val.VKRecord.Size).IsEqualTo(-24);
            Check.That(val.VKRecord.Signature).IsEqualTo("vk");
            Check.That(val.VKRecord.IsFree).IsFalse();
            Check.That(val.VKRecord.NamePresentFlag).IsEqualTo((ushort) 0x00);
            Check.That(val.VKRecord.NameLength).IsEqualTo((ushort) 0);
            Check.That(val.VKRecord.ValueName).IsEqualTo("(default)");
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegNone);
            Check.That(val.VKRecord.DataLength).Equals(0x80000000);
            Check.That(val.VKRecord.OffsetToData).Equals((uint) 0x0);
            Check.That(val.VKRecord.Padding.Length).Equals(0);
            Check.That(val.VKRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(0);
            Check.That(val.VKRecord.ValueDataRaw.Length).IsEqualTo(0);
            Check.That(val.VKRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVKRecordRegqWord()
        {
            var key =
                TestSetup.NtUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows\CurrentVersion\Store\RefreshBannedAppList");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "BannedAppsLastModified");

            Check.That(val).IsNotNull();

            Check.That(val.VKRecord.RelativeOffset).IsEqualTo(0x5ce0);
            Check.That(val.VKRecord.AbsoluteOffset).IsEqualTo(0x6ce0);
            Check.That(val.VKRecord.Size).IsEqualTo(-48);
            Check.That(val.VKRecord.Signature).IsEqualTo("vk");
            Check.That(val.VKRecord.IsFree).IsFalse();
            Check.That(val.VKRecord.NamePresentFlag).IsEqualTo((ushort) 0x01);
            Check.That(val.VKRecord.NameLength).IsEqualTo((ushort) 0x16);
            Check.That(val.VKRecord.ValueName).IsEqualTo("BannedAppsLastModified");
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegQword);
            Check.That(val.VKRecord.DataLength).Equals((uint) 0x8);
            Check.That(val.VKRecord.OffsetToData).Equals((uint) 0x3b70);
            Check.That(val.ValueData).Equals("0");
            Check.That(val.VKRecord.Padding.Length).Equals(2);
            Check.That(val.VKRecord.ValueData).IsInstanceOf<ulong>();
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(4);
            Check.That(val.VKRecord.ValueDataRaw.Length).IsEqualTo(8);
            Check.That(val.VKRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVKRecordRegSz()
        {
            var key =
                TestSetup.SamOnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Builtin\Aliases\Members\S-1-5-21-727398572-3617256236-2003601904\00000201");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "(default)");

            Check.That(val).IsNotNull();

            Check.That(val.VKRecord.RelativeOffset).IsEqualTo(0xFE0);
            Check.That(val.VKRecord.AbsoluteOffset).IsEqualTo(0x1FE0);
            Check.That(val.VKRecord.Size).IsEqualTo(-24);
            Check.That(val.VKRecord.Signature).IsEqualTo("vk");
            Check.That(val.VKRecord.IsFree).IsFalse();
            Check.That(val.VKRecord.NamePresentFlag).IsEqualTo((ushort) 0x00);
            Check.That(val.VKRecord.NameLength).IsEqualTo((ushort) 0);
            Check.That(val.VKRecord.ValueName).IsEqualTo("(default)");
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegSz);
            Check.That(val.VKRecord.DataLength).Equals(0x80000004);
            Check.That(val.VKRecord.OffsetToData).Equals((uint) 0x0221);
            Check.That(val.VKRecord.Padding.Length).Equals(0);
            Check.That(val.VKRecord.ValueData).IsInstanceOf<string>();
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(0);
            Check.That(val.VKRecord.ValueDataRaw.Length).IsEqualTo(4);
            Check.That(val.VKRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVKRecordRegUnknown()
        {
            var key =
                TestSetup.SamHasBigEndianOnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Account\Groups\Names\None");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "(default)");

            Check.That(val).IsNotNull();

            Check.That(val.VKRecord.RelativeOffset).IsEqualTo(0x1248);
            Check.That(val.VKRecord.AbsoluteOffset).IsEqualTo(0x2248);
            Check.That(val.VKRecord.Size).IsEqualTo(-24);
            Check.That(val.VKRecord.Signature).IsEqualTo("vk");
            Check.That(val.VKRecord.IsFree).IsFalse();
            Check.That(val.VKRecord.NamePresentFlag).IsEqualTo((ushort) 0x0);
            Check.That(val.VKRecord.NameLength).IsEqualTo((ushort) 0);
            Check.That(val.VKRecord.ValueName).IsEqualTo("(default)");
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegUnknown);
            Check.That(val.VKRecord.DataTypeRaw).IsEqualTo((uint) 513);
            Check.That(val.VKRecord.DataLength).Equals(0x80000000);
            Check.That(val.VKRecord.OffsetToData).Equals((uint) 0);
            Check.That(val.VKRecord.Padding.Length).Equals(0);
            Check.That(val.VKRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(0);
            Check.That(val.VKRecord.ValueDataRaw.Length).IsEqualTo(4);
            Check.That(val.VKRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVKRecordUnknownRegType()
        {
            var key = TestSetup.SamDupeNameOnDemand.GetKey(@"SAM\SAM\Domains\Account\Users");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "(default)");

            Check.That(val).IsNotNull();

            Check.That(val.VKRecord.RelativeOffset).IsEqualTo(0x1880);
            Check.That(val.VKRecord.AbsoluteOffset).IsEqualTo(0x2880);
            Check.That(val.VKRecord.Size).IsEqualTo(-24);
            Check.That(val.VKRecord.Signature).IsEqualTo("vk");
            Check.That(val.VKRecord.IsFree).IsFalse();
            Check.That(val.VKRecord.NamePresentFlag).IsEqualTo((ushort) 0x0);
            Check.That(val.VKRecord.NameLength).IsEqualTo((ushort) 0x0);
            Check.That(val.VKRecord.ValueName).IsEqualTo("(default)");
            Check.That(val.VKRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.VKRecord.DataType).IsEqualTo(VKCellRecord.DataTypeEnum.RegUnknown);
            Check.That(val.VKRecord.DataTypeRaw).IsEqualTo((uint) 15);
            Check.That(val.VKRecord.DataLength).Equals(0x80000000);
            Check.That(val.VKRecord.OffsetToData).Equals((uint) 0);
            Check.That(val.VKRecord.Padding.Length).Equals(0);
            Check.That(val.VKRecord.ValueDataRaw.Length).IsEqualTo(4);
            Check.That(val.VKRecord.ValueDataSlack.Length).IsEqualTo(0);
            Check.That(val.VKRecord.ToString()).IsNotEmpty();
        }
    }
}