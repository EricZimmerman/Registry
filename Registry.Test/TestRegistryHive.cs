using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NFluent;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;

namespace Registry.Test
{
    [TestFixture]
    internal class TestRegistryHive
    {
        [SetUp]
        public void PreTestSetup()
        {
            LogManager.Configuration = null;
        }

        [Test]
        public void ShouldFindFiveValuesForSize4096()
        {
            var keys = TestSetup.UsrClass1.FindByValueSize(4096).ToList();

            Check.That(keys.Count).IsEqualTo(5);
        }

        [Test]
        public void ShouldFindTwoValuesForSize100000()
        {
            var keys = TestSetup.UsrClass1.FindByValueSize(100000).ToList();

            Check.That(keys.Count).IsEqualTo(2);
        }

        [Test]
        public void ShouldExportValuesToFile()
        {
            var keys = TestSetup.UsrClass1.FindByValueSize(100000).ToList();

            foreach (var valueBySizeInfo in keys)
            {
                File.WriteAllBytes($"{valueBySizeInfo.Value.ValueName}.bin", valueBySizeInfo.Value.ValueDataRaw);

                Check.That(File.Exists($"{valueBySizeInfo.Value.ValueName}.bin")).IsTrue();
            }
        }

        [Test]
        public void ShouldFind32HitsForFoodInKeyName()
        {
            var hits = TestSetup.UsrClass1.FindInKeyName("food").ToList();

            Check.That(hits.Count).IsEqualTo(32);
        }

        [Test]
        public void ShouldFindThreeHitsForMuiCacheInKeyName()
        {
            var hits = TestSetup.UsrClass1.FindInKeyName("MuiCache").ToList();

            Check.That(hits.Count).IsEqualTo(3);
        }

        [Test]
        public void ShouldFindNoHitsForZimmermanInKeyName()
        {
            var hits = TestSetup.UsrClass1.FindInKeyName("Zimmerman").ToList();

            Check.That(hits.Count).IsEqualTo(0);
        }

        [Test]
        public void ShouldFind100HitsForURLInKeyAndValueName()
        {
            var keyHits = TestSetup.UsrClass1.FindInKeyName("URL").ToList();

            Check.That(keyHits.Count).IsEqualTo(21);

            var valHits = TestSetup.UsrClass1.FindInValueName("URL").ToList();

            Check.That(valHits.Count).IsEqualTo(79);
        }

        [Test]
        public void ShouldFind4HitsForPostboxURLInValueData()
        {
            var hits = TestSetup.UsrClass1.FindInValueData("Postbox URL").ToList();

            Check.That(hits.Count).IsEqualTo(4);
        }

        [Test]
        public void ShouldFind4HitsForBingXInValueDataWithRegEx()
        {
            var hits = TestSetup.UsrClass1.FindInValueData("URL:bing[mhs]",true).ToList();

            Check.That(hits.Count).IsEqualTo(3);

             hits = TestSetup.UsrClass1.FindInValueData("URL:bing[mhts]", true).ToList();

            Check.That(hits.Count).IsEqualTo(4);
        }

        [Test]
        public void ShouldFind4HitsForBingXInKeyNamesWithRegEx()
        {
            var hits = TestSetup.UsrClass1.FindInKeyName("Microsoft.Bing[FHW]", true).ToList();

            Check.That(hits.Count).IsEqualTo(44);

            hits = TestSetup.UsrClass1.FindInKeyName("Microsoft.Bing[FHW]o", true).ToList();

            Check.That(hits.Count).IsEqualTo(11);
        }

        [Test]
        public void ShouldFindHitsValueNamesWithRegEx()
        {
            var hits = TestSetup.UsrClass1.FindInValueName("(App|Display)Name", true).ToList();

            Check.That(hits.Count).IsEqualTo(326);

            hits = TestSetup.UsrClass1.FindInValueName("Capability(Co|Si)", true).ToList();

            Check.That(hits.Count).IsEqualTo(66);
        }

        [Test]
        public void ShouldFind4HitsForBinaryDataInValueData()
        {
            var hits = TestSetup.UsrClass1.FindInValueData("43-74-53-83-24-55-30").ToList();

            Check.That(hits.Count).IsEqualTo(6);
        }

        [Test]
        public void ShouldFind4HitsForBinaryDataInValueDataWithRegEx()
        {
            var hits = TestSetup.UsrClass1.FindInValueData("04-00-EF-BE", true).ToList();

            Check.That(hits.Count).IsEqualTo(56);

             hits = TestSetup.UsrClass1.FindInValueData("47-4F-4F-4E", true).ToList();

            Check.That(hits.Count).IsEqualTo(4);

            hits = TestSetup.UsrClass1.FindInValueData("44-65-62", true).ToList(); //finds deb

            Check.That(hits.Count).IsEqualTo(2);

            hits = TestSetup.UsrClass1.FindInValueData("44-65-73", true).ToList(); //finds des

            Check.That(hits.Count).IsEqualTo(1);

            hits = TestSetup.UsrClass1.FindInValueData("44-65-(62|73)", true).ToList(); //finds deb or des

            Check.That(hits.Count).IsEqualTo(3);
        }

        [Test]
        public void ShouldThrowExceptionWhenCallingParseHiveTwice()
        {
            Check.ThatCode(() => { var r = new RegistryHive(@"..\..\Hives\SAMBadHBinHeader"); r.ParseHive(); r.ParseHive(); }).Throws<Exception>();
        }

        [Test]
        public void CheckHardAndSoftParsingErrors()
        {
            Check.That(TestSetup.Sam.SoftParsingErrors).IsEqualTo(0);
            Check.That(TestSetup.Sam.HardParsingErrors).IsEqualTo(0);
        }

        [Test]
        public void HBinSizeShouldMatchReadSize()
        {
            Check.That(TestSetup.Sam.Header.Length).IsEqualTo(TestSetup.Sam.HBinRecordTotalSize);
        }

        [Test]
        public void HBinSizeShouldNotMatchReadSize()
        {
            var r = new RegistryHive(@"..\..\Hives\SAM_DUPENAME");
            //if you don't call parse, it wont match

            Check.That(r.Header.Length).IsNotEqualTo(r.HBinRecordTotalSize);
        }

        [Test]
        public void NLogConfigInfoTest()
        {
            var config = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);
            var rule1 = new LoggingRule("*", LogLevel.Info, consoleTarget);
            config.LoggingRules.Add(rule1);

            TestSetup.Sam.NlogConfig = config;

            Check.That(config).Equals(TestSetup.Sam.NlogConfig);

            TestSetup.Sam.NlogConfig = null;
        }

        [Test]
        public void NLogConfigTraceTest()
        {
            var config = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);
            var rule1 = new LoggingRule("*", LogLevel.Trace, consoleTarget);
            config.LoggingRules.Add(rule1);

            TestSetup.SamDupeNameOnDemand.NlogConfig = config;

            Check.That(config).Equals(TestSetup.SamDupeNameOnDemand.NlogConfig);

            TestSetup.SamDupeNameOnDemand.NlogConfig = null;
        }

        [Test]
        public void RecoverDeletedShouldBeTrue()
        {
            TestSetup.Sam.RecoverDeleted = true;

            Check.That(TestSetup.Sam.RecoverDeleted).IsEqualTo(true);
            TestSetup.Sam.RecoverDeleted = false;
        }

        [Test]
        public void ShouldExportHiveWithRootValues()
        {
            TestSetup.SamRootValue.ExportDataToCommonFormat(@"SamRootValueNoDeletedStuff.txt", false);
        }


        [Test]
        public void ShouldExportFileAllRecords()
        {
            TestSetup.UsrclassDeleted.ExportDataToCommonFormat(@"UsrclassDeletedNoDeletedStuff.txt", false);

            Check.That(TestSetup.UsrclassDeleted.Header.Length).IsEqualTo(TestSetup.UsrclassDeleted.HBinRecordTotalSize);
        }

        [Test]
        public void ShouldExportFileDeletedRecords()
        {
            TestSetup.UsrclassDeleted.ExportDataToCommonFormat(@"UsrclassDeletedDeletedStuff.txt", true);

            Check.That(TestSetup.UsrclassDeleted.Header.Length).IsEqualTo(TestSetup.UsrclassDeleted.HBinRecordTotalSize);
        }

        [Test]
        public void ShouldFindAKeyWithClassName()
        {
            var key =
                TestSetup.SystemOnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\ControlSet001\Control\Lsa\Data");

            Check.That(key.ClassName).IsNotEmpty();
        }

        [Test]
        public void ShouldFindAKeyWithoutRootKeyName()
        {
            var key = TestSetup.Sam.GetKey(@"SAM\Domains");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void ShouldHaveHeaderLengthEqualToReadDataSize()
        {
            Check.That(TestSetup.UsrclassDeleted.Header.Length).IsEqualTo(TestSetup.UsrclassDeleted.HBinRecordTotalSize);
        }

        [Test]
        public void ShouldFindKeyWithMixedCaseName()
        {
            var key =
                TestSetup.UsrClassFtp.GetKey(
                    @"S-1-5-21-2417227394-2575385136-2411922467-1105_CLAsses\ActivAtableClasses\CLsID");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void ShouldFindKeyWithMixedCaseNameWithoutRootName()
        {
            var key = TestSetup.UsrClassFtp.GetKey(@"ActivAtableClasses\CLsID");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void ShouldHaveHardAndSoftParsingValuesOfZero()
        {
            Check.That(TestSetup.Sam.HardParsingErrors).IsEqualTo(0);
            Check.That(TestSetup.Sam.SoftParsingErrors).IsEqualTo(0);
        }

        [Test]
        public void ShouldReturnKeyBasedOnRelativePath()
        {
            var key =
                TestSetup.Sam.GetKey(0x418);

            Check.That(key).IsNotNull();
        }

        [Test]
        public void ShouldReturnNullWhenKeyPathNotFound()
        {
            var key =
                TestSetup.Sam.GetKey(@"SAM\Domains\DoesNotExist");

            Check.That(key).IsNull();
        }

        [Test]
        public void ShouldReturnNullWhenRelativeOffsetNotFound()
        {
            var key =
                TestSetup.Sam.GetKey(0x999418);

            Check.That(key).IsNull();
        }

        [Test]
        public void ShouldTakeByteArrayInConstructor()
        {
            var r = new RegistryHive(TestSetup.Sam.FileBytes);

            Check.That(r.Header).IsNotNull();
            Check.That(r.HivePath).IsEqualTo("None");
            Check.That(r.HiveType).IsEqualTo(HiveTypeEnum.Sam);
        }

        [Test]
        public void ShouldThrowExceptionNoRootKey()
        {
            Check.ThatCode(() =>{var r = new RegistryHive(@"..\..\Hives\SECURITYNoRoot");r.ParseHive();}).Throws<KeyNotFoundException>();
        }

        [Test]
        public void ShouldThrowExceptionWithBadHbinHeader()
        {
            Check.ThatCode(() =>{var r = new RegistryHive(@"..\..\Hives\SAMBadHBinHeader");r.ParseHive();}).Throws<Exception>();
        }

        [Test]
        public void TestsListRecordsContinued3()
        {
            var key =
                TestSetup.UsrClassFtp.GetKey(
                    @"S-1-5-21-2417227394-2575385136-2411922467-1105_Classes\ActivatableClasses\CLSID");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void VerifyHiveTestShouldPass()
        {
            var m = TestSetup.Sam.Verify();

            Check.That(m.HasValidHeader).IsTrue();
        }
    }
}