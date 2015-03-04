using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NFluent;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using NLog;

namespace Registry.Test
{
    [TestFixture]
    class TestRegistryHiveClass
    {
        private const string _basePath = @"C:\ProjectWorkingFolder\Registry2\Registry\Registry.Test\TestFiles";

        [Test]
        public void RecoverDeletedShouldBeTrue()
        {
            var hivePath = Path.Combine(_basePath, "SAM");
            var r = new RegistryHive(hivePath);
            r.RecoverDeleted = true;

            Check.That(r.RecoverDeleted).IsEqualTo(true);
        }

        [Test]
        public void CheckHardAndSoftParsingErrors()
        {
            var hivePath = Path.Combine(_basePath, "SAM");
            var r = new RegistryHive(hivePath);
            

            Check.That(r.SoftParsingErrors).IsEqualTo(0);
            Check.That(r.HardParsingErrors).IsEqualTo(0);
        }

        [Test]
        public void HBinSizeShouldNotMatchReadSize()
        {
            var hivePath = Path.Combine(_basePath, "SAM");
            var r = new RegistryHive(hivePath);
           //if you don't call parse, it wont match

            Check.That(r.Header.Length).IsNotEqualTo(r.HBinRecordTotalSize);
            
        }

        [Test]
        public void HBinSizeShouldMatchReadSize()
        {
            var hivePath = Path.Combine(_basePath, "SAM");
            var r = new RegistryHive(hivePath);
            r.ParseHive();

            Check.That(r.Header.Length).IsEqualTo(r.HBinRecordTotalSize);
            
        }

        [Test]
        public void ShouldExportFileAllRecords()
        {
            var hivePath = Path.Combine(_basePath, "SAM");
            var r = new RegistryHive(hivePath);
            r.ParseHive();
            r.ExportDataToCommonFormat(@"C:\temp\samout.txt",false);

            Check.That(r.Header.Length).IsEqualTo(r.HBinRecordTotalSize);

        }

        [Test]
        public void ShouldFindAndExportDeletedRecords()
        {
            var hivePath = Path.Combine(_basePath, "UsrClassDeletedBags.dat");
            var r = new RegistryHive(hivePath);
            r.RecoverDeleted = true;
            r.FlushRecordListsAfterParse = false;
            r.ParseHive();
            r.ExportDataToCommonFormat(@"C:\temp\UsrClassDeletedBags.txt", false);

            Check.That(r.Header.Length).IsEqualTo(r.HBinRecordTotalSize);

        }

        [Test]
        public void ShouldExportRootValue()
        {
            var hivePath = Path.Combine(_basePath, "usrclassRootValue.dat");
            var r = new RegistryHive(hivePath);
            r.RecoverDeleted = true;
            r.FlushRecordListsAfterParse = false;
            r.ParseHive();
            r.ExportDataToCommonFormat(@"C:\temp\UsrClassDeletedBagsRootValue.txt", false);

            Check.That(r.Header.Length).IsEqualTo(r.HBinRecordTotalSize);

        }

        [Test]
        public void ExportLargeHive()
        {
            var hivePath = Path.Combine(_basePath, "SOFTWARE_BIG");
            var r = new RegistryHive(hivePath);
            r.RecoverDeleted = true;
            r.FlushRecordListsAfterParse = false;

            r.ParseHive();
            r.ExportDataToCommonFormat(@"C:\temp\SOFTWARE_BIGoutDeleted.txt", false);
        }

        [Test]
        public void ShouldExportFileDeletedRecords()
        {
            var hivePath = Path.Combine(_basePath, "SAM");
            var r = new RegistryHive(hivePath);
          
            r.ParseHive();
            r.ExportDataToCommonFormat(@"C:\temp\samoutDeleted.txt", true);

            Check.That(r.Header.Length).IsEqualTo(r.HBinRecordTotalSize);
        }

        [Test]
        public void NLogConfigTraceTest()
        {
            var r = new RegistryHive(Path.Combine(_basePath, "SECURITY"));

            var config = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);
            var rule1 = new LoggingRule("*", LogLevel.Trace, consoleTarget);
            config.LoggingRules.Add(rule1);

            r.NlogConfig = config;

            r.ParseHive();

            Check.That(config).Equals(r.NlogConfig);
        }

        [Test]
        public void NLogConfigDebugTest()
        {
            var r = new RegistryHive(Path.Combine(_basePath, "SECURITY"));

            var config = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);
            var rule1 = new LoggingRule("*", LogLevel.Debug, consoleTarget);
            config.LoggingRules.Add(rule1);

            r.NlogConfig = config;

            r.ParseHive();

            Check.That(config).Equals(r.NlogConfig);
        }

        [Test]
        public void ShouldFindAKeyWithClassName()
        {
            var hivePath = Path.Combine(_basePath, "system");
            var r = new RegistryHive(hivePath);
            r.ParseHive();

            var key =
                r.FindKey(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\ControlSet001\Control\Lsa\Data");

            Check.That(key.ClassName).IsNotEmpty();

        }

        [Test]
        public void ShouldFindAKeyWithoutRootKeyName()
        {
            var hivePath = Path.Combine(_basePath, "sam");
            var r = new RegistryHive(hivePath);
            r.ParseHive();

            var key =
                r.FindKey(@"SAM\Domains");

            Check.That(key).IsNotNull();

        }

        [Test]
        public void VerifiHiveTestShouldPass()
        {
            var hivePath = Path.Combine(_basePath, "sam");
            var r = new RegistryHive(hivePath);
           var m= r.Verify();
            
            Check.That(m.HasValidHeader).IsTrue();

        }

        [Test]
        public void ShouldDisplayWarningsOnDataRecovery()
        {
            var hivePath = Path.Combine(_basePath, "NTUSER.DAT_Warnings");
            var r = new RegistryHive(hivePath);
            r.RecoverDeleted = true;
            r.ParseHive();
        }

        [Test]
        public void TestsListRecordsContinued3()
        {
            var hivePath = Path.Combine(_basePath, "UsrClass FTP.dat");
            var r = new RegistryHive(hivePath);
            r.ParseHive();

            var key = r.FindKey(@"S-1-5-21-2417227394-2575385136-2411922467-1105_Classes\ActivatableClasses\CLSID");

            Check.That(key).IsNotNull();

        }

        [Test]
        public void ShouldReturnNullWhenKeyPathNotFound()
        {
            var hivePath = Path.Combine(_basePath, "sam");
            var r = new RegistryHive(hivePath);
            r.ParseHive();

            var key =
                r.FindKey(@"SAM\Domains\DoesNotExist");

            Check.That(key).IsNull();
        }

        [Test]
        public void ShouldReturnKeyBasedOnRelativePath()
        {
            var hivePath = Path.Combine(_basePath, "sam");
            var r = new RegistryHive(hivePath);
            r.ParseHive();

            var key =
                r.FindKey(0x418);

            Check.That(key).IsNotNull();

        }

        [Test]
        public void ShouldReturnNullWhenRelativeOffsetNotFound()
        {
            var hivePath = Path.Combine(_basePath, "sam");
            var r = new RegistryHive(hivePath);
            r.ParseHive();

            var key =
                r.FindKey(0x999418);

            Check.That(key).IsNull();

      
        }

        [Test]
        public void ShouldThrowExceptionWithBadHbinHeader()
        {

            Check.ThatCode(() =>
            {

                var hivePath = Path.Combine(_basePath, "SAMBadHBinHeader");
                var r = new RegistryHive(hivePath);
                r.ParseHive();

            }).Throws<Exception>();
        }

        [Test]
        public void ShouldThrowExceptionNoRootKey()
        {

            Check.ThatCode(() =>
            {

                var hivePath = Path.Combine(_basePath, "SECURITYNoRoot");
                var r = new RegistryHive(hivePath);
                r.ParseHive();

            }).Throws<KeyNotFoundException>();
        }

    }
}
