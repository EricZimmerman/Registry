using System;
using System.IO;
using NFluent;
using NLog.Config;
using NUnit.Framework;
using NLog;
using NLog.Targets;

namespace Registry.Test
{
    [TestFixture]
    public class TestRegistryBaseClass
    {
        private const string _basePath = @"C:\ProjectWorkingFolder\Registry2\Registry\Registry.Test\TestFiles";

        [Test]
        public void NLogConfigShouldBeSameAsWhatWasSet()
        {
            var r = new RegistryBase(Path.Combine(_basePath, "SECURITY"));

            var config = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);
            var rule1 = new LoggingRule("*", LogLevel.Info, consoleTarget);
            config.LoggingRules.Add(rule1);

            r.NlogConfig = config;

            Check.That(config).Equals(r.NlogConfig);
        }

        [Test]
        public void FileNameNotFoundShouldThrowFileNotFoundException()
        {
            Check.ThatCode(() => { new RegistryBase(@"c:\this\file\does\not\exist.reg"); }).Throws<FileNotFoundException>();
        }

        [Test]
        public void FileNameNotFoundShouldThrowNotSupportedException()
        {
            Check.ThatCode(() => { new RegistryBase(); }).Throws<NotSupportedException>();
        }

        [Test]
        public void HivePathShouldReflectWhatIsPassedIn()
        {
            var hivePath = Path.Combine(_basePath, "SECURITY");
            var r = new RegistryBase(hivePath);
            
            Check.That(r.HivePath).IsEqualTo(hivePath);
        }

        [Test]
        public void InValidHiveShouldReturnFalse()
        {
            var hivePath = Path.Combine(_basePath, "NOTAHIVE");

            Check.That(RegistryBase.HasValidHeader(hivePath)).IsFalse();
        }

        [Test]
        public void InvalidRegistryHiveShouldThrowException()
        {
            var hivePath = Path.Combine(_basePath, "NOTAHIVE");
            
            Check.ThatCode(() => { new RegistryBase(hivePath); }).Throws<Exception>();
        }

        [Test]
        public void NullFileNameShouldThrowEArgumentNullException()
        {
            Check.ThatCode(() => { new RegistryBase(null); }).Throws<ArgumentNullException>();
        }

        [Test]
        public void SecurityHiveShouldHaveSecurityHiveType()
        {
            var hivePath = Path.Combine(_basePath, "SECURITY");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.Security).IsEqualTo(r.HiveType);
        }

        [Test]
        public void SamHiveShouldHaveSamHiveType()
        {
            var hivePath = Path.Combine(_basePath, "SAM");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.Sam).IsEqualTo(r.HiveType);
        }

        [Test]
        public void OtherHiveShouldHaveOtherHiveType()
        {
            var hivePath = Path.Combine(_basePath, "SAN(OTHER)");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.Other).IsEqualTo(r.HiveType);
        }

        [Test]
        public void SoftwareHiveShouldHaveSoftwareHiveType()
        {
            var hivePath = Path.Combine(_basePath, "SOFTWARE");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.Software).IsEqualTo(r.HiveType);
        }

        [Test]
        public void ComponentsHiveShouldHaveComponentsHiveType()
        {
            var hivePath = Path.Combine(_basePath, "Components");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.Components).IsEqualTo(r.HiveType);
        }

        [Test]
        public void BcdHiveShouldHaveBcdeHiveType()
        {
            var hivePath = Path.Combine(_basePath, "BCD");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.Bcd).IsEqualTo(r.HiveType);
        }

        [Test]
        public void SystemHiveShouldHaveSystemHiveType()
        {
            var hivePath = Path.Combine(_basePath, "SYSTEM");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.System).IsEqualTo(r.HiveType);
        }

        [Test]
        public void NtuserHiveShouldHaveNtuserHiveType()
        {
            var hivePath = Path.Combine(_basePath, "ntuser1.dat");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.NtUser).IsEqualTo(r.HiveType);
        }

        [Test]
        public void UsrclassHiveShouldHaveUsrclassHiveType()
        {
            var hivePath = Path.Combine(_basePath, "UsrClassDeletedBags.dat");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.UsrClass).IsEqualTo(r.HiveType);
        }

        [Test]
        public void DriversHiveShouldHaveDriversHiveType()
        {
            var hivePath = Path.Combine(_basePath, "DRIVERS");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.Drivers).IsEqualTo(r.HiveType);
        }

        [Test]
        public void ValidHiveShouldHaveValidHeader()
        {
            var hivePath = Path.Combine(_basePath, "SECURITY");

            Check.That(RegistryBase.HasValidHeader(hivePath)).IsTrue();
        }
    }
}