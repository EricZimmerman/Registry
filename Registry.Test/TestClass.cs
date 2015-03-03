using System;
using System.IO;
using NFluent;
using NUnit.Framework;

namespace Registry.Test
{
    [TestFixture]
    public class TestClass
    {
        private readonly string _basePath = @"C:\ProjectWorkingFolder\Registry2\Registry\Registry.Test\TestFiles";

        [Test]
        public void FileNameNotFoundShouldThrowFileNotFoundException()
        {
            Check.That(() => { new RegistryHive(@"c:\this\file\does\not\exist.reg"); }).Throws<FileNotFoundException>();
        }

        [Test]
        public void FileNameNotFoundShouldThrowNotSupportedException()
        {
            Check.That(() => { new RegistryBase(); }).Throws<NotSupportedException>();
        }

        [Test]
        public void HivePathShouldReflectWhatIsPassedIn()
        {
            var hivePath = Path.Combine(_basePath, "SECURITY");
            var r = new RegistryHive(hivePath);
            
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
            
            Check.That(() => { new RegistryHive(hivePath); }).Throws<Exception>();
        }

        [Test]
        public void NullFileNameShouldThrowEArgumentNullException()
        {
            Check.That(() => { new RegistryHive(null); }).Throws<ArgumentNullException>();
        }

        [Test]
        public void SecurityHiveShouldHaveSecurityHiveType()
        {
            var hivePath = Path.Combine(_basePath, "SECURITY");
            var r = new RegistryHive(hivePath);

            Check.That(HiveTypeEnum.Security).IsEqualTo(r.HiveType);
        }

        [Test]
        public void ValidHiveShouldHaveValidHeader()
        {
            var hivePath = Path.Combine(_basePath, "SECURITY");

            Check.That(RegistryBase.HasValidHeader(hivePath)).IsTrue();
        }
    }
}