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
        private const string BasePath = @"C:\ProjectWorkingFolder\Registry2\Registry\Registry.Test\TestFiles";
        private  string _hive = Path.Combine(BasePath, "SECURITY");

        private RegistryBase SecurityHive;

        [TestFixtureSetUp]
        public void Init()
        {
            SecurityHive = new RegistryBase(_hive);
        }

        [Test]
        public void NLogConfigShouldBeSameAsWhatWasSet()
        {
            var config = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);
            var rule1 = new LoggingRule("*", LogLevel.Info, consoleTarget);
            config.LoggingRules.Add(rule1);

            SecurityHive.NlogConfig = config;

            Check.That(config).Equals(SecurityHive.NlogConfig);
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
            Check.That(SecurityHive.HivePath).IsEqualTo(_hive);
        }

        [Test]
        public void InvalidRegistryHiveShouldThrowException()
        {
            var hivePath = Path.Combine(BasePath, "NOTAHIVE");
            
            Check.ThatCode(() => { new RegistryBase(hivePath); }).Throws<Exception>();
        }

        [Test]
        public void ShouldTakeByteArrayInConstructor()
        {
            var hivePath = Path.Combine(BasePath, "SAM");

            var fileStream = new FileStream(hivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var binaryReader = new BinaryReader(fileStream);

            binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);

            var fileBytes = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);

            binaryReader.Close();
            fileStream.Close();

            var r = new RegistryBase(fileBytes);

            Check.That(r.Header).IsNotNull();
            Check.That(r.HivePath).IsEqualTo("None");
            Check.That(r.HiveType).IsEqualTo(HiveTypeEnum.Sam);
        }

        [Test]
        public void NullFileNameShouldThrowEArgumentNullException()
        {
           string nullFileName = null;
            Check.ThatCode(() => { new RegistryBase(nullFileName); }).Throws<ArgumentNullException>();
        }

        [Test]
        public void NullByteArrayShouldThrowEArgumentNullException()
        {
            byte[] nullBytes = null;
            Check.ThatCode(() => { new RegistryBase(nullBytes); }).Throws<ArgumentNullException>();
        }

        [Test]
        public void SecurityHiveShouldHaveSecurityHiveType()
        {
           Check.That(HiveTypeEnum.Security).IsEqualTo(SecurityHive.HiveType);
        }

        [Test]
        public void SamHiveShouldHaveSamHiveType()
        {
            var hivePath = Path.Combine(BasePath, "SAM");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.Sam).IsEqualTo(r.HiveType);
        }

        [Test]
        public void OtherHiveShouldHaveOtherHiveType()
        {
            var hivePath = Path.Combine(BasePath, "SAN(OTHER)");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.Other).IsEqualTo(r.HiveType);
        }

        [Test]
        public void SoftwareHiveShouldHaveSoftwareHiveType()
        {
            var hivePath = Path.Combine(BasePath, "SOFTWARE");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.Software).IsEqualTo(r.HiveType);
        }

        [Test]
        public void ComponentsHiveShouldHaveComponentsHiveType()
        {
            var hivePath = Path.Combine(BasePath, "Components");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.Components).IsEqualTo(r.HiveType);
        }

        [Test]
        public void BcdHiveShouldHaveBcdeHiveType()
        {
            var hivePath = Path.Combine(BasePath, "BCD");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.Bcd).IsEqualTo(r.HiveType);
        }

        [Test]
        public void SystemHiveShouldHaveSystemHiveType()
        {
            var hivePath = Path.Combine(BasePath, "SYSTEM");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.System).IsEqualTo(r.HiveType);
        }

        [Test]
        public void NtuserHiveShouldHaveNtuserHiveType()
        {
            var hivePath = Path.Combine(BasePath, "ntuser1.dat");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.NtUser).IsEqualTo(r.HiveType);
        }

        [Test]
        public void UsrclassHiveShouldHaveUsrclassHiveType()
        {
            var hivePath = Path.Combine(BasePath, "UsrClassDeletedBags.dat");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.UsrClass).IsEqualTo(r.HiveType);
        }

        [Test]
        public void DriversHiveShouldHaveDriversHiveType()
        {
            var hivePath = Path.Combine(BasePath, "DRIVERS");
            var r = new RegistryBase(hivePath);

            Check.That(HiveTypeEnum.Drivers).IsEqualTo(r.HiveType);
        }
    }
}