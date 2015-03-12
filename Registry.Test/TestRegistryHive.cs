using System;
using System.Collections.Generic;
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
            var key = TestSetup.Sam.FindKey(@"SAM\Domains");

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
                TestSetup.Sam.FindKey(0x418);

            Check.That(key).IsNotNull();
        }

        [Test]
        public void ShouldReturnNullWhenKeyPathNotFound()
        {
            var key =
                TestSetup.Sam.FindKey(@"SAM\Domains\DoesNotExist");

            Check.That(key).IsNull();
        }

        [Test]
        public void ShouldReturnNullWhenRelativeOffsetNotFound()
        {
            var key =
                TestSetup.Sam.FindKey(0x999418);

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