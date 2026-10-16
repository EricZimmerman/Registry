using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using Registry.Other;

namespace Registry.Test;

[TestFixture]
public class TestRegistryBase
{
    [SetUp]
    public void PreTestSetup()
    {
    }

    [Test]
    public void BcdHiveShouldHaveBcdHiveType()
    {
        var r = new RegistryBase(TestHives.Path(@"BCD"));
        HiveTypeEnum.Bcd.Should().Be(r.HiveType);
    }

    [Test]
    public void DriversHiveShouldHaveDriversHiveType()
    {
        var r = new RegistryBase(TestHives.Path(@"Drivers"));
        HiveTypeEnum.Drivers.Should().Be(r.HiveType);
    }

    [Test]
    public void FileNameNotFoundShouldThrowFileNotFoundException()
    {
        FluentActions.Invoking(() => { new RegistryBase(@"c:\this\file\does\not\exist.reg"); }).Should().Throw<FileNotFoundException>();
    }

    [Test]
    public void FileNameNotFoundShouldThrowNotSupportedException()
    {
        FluentActions.Invoking(() => { new RegistryBase(); }).Should().Throw<NotSupportedException>();
    }

    [Test]
    public void HivePathShouldReflectWhatIsPassedIn()
    {
        var security = new RegistryHiveOnDemand(TestHives.Path(@"SECURITY"));

        security.HivePath.Should().Be(TestHives.Path(@"SECURITY"));
    }

    [Test]
    public void InvalidRegistryHiveShouldThrowException()
    {
        FluentActions.Invoking(() => { new RegistryBase(TestHives.Path(@"NotAHive")); }).Should().Throw<Exception>();
    }


    [Test]
    public void NtuserHiveShouldHaveNtuserHiveType()
    {
        var r = new RegistryBase(TestHives.Path(@"NTUSER.DAT"));
        HiveTypeEnum.NtUser.Should().Be(r.HiveType);
    }

    [Test]
    public void NullByteArrayShouldThrowEArgumentNullException()
    {
        byte[] nullBytes = null;
        FluentActions.Invoking(() => { new RegistryBase(nullBytes, null); }).Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void NullFileNameShouldThrowEArgumentNullException()
    {
        string nullFileName = null;
        FluentActions.Invoking(() => { new RegistryBase(nullFileName); }).Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void OtherHiveShouldHaveOtherHiveType()
    {
        var r = new RegistryBase(TestHives.Path(@"SAN(OTHER)"));
        HiveTypeEnum.Other.Should().Be(r.HiveType);
    }

    [Test]
    public void SamHiveShouldHaveSamHiveType()
    {
        var r = new RegistryBase(TestHives.Path(@"SAM"));
        HiveTypeEnum.Sam.Should().Be(r.HiveType);
    }

    [Test]
    public void SecurityHiveShouldHaveSecurityHiveType()
    {
        var r = new RegistryBase(TestHives.Path(@"Security"));
        HiveTypeEnum.Security.Should().Be(r.HiveType);
    }

    [Test]
    public void ShouldTakeByteArrayInConstructor()
    {
        var fileStream = new FileStream(TestHives.Path(@"SAM"), FileMode.Open, FileAccess.Read, FileShare.Read);
        var binaryReader = new BinaryReader(fileStream);

        binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);

        var fileBytes = binaryReader.ReadBytes((int) binaryReader.BaseStream.Length);

        binaryReader.Close();
        fileStream.Close();

        var r = new RegistryBase(fileBytes, TestHives.Path(@"SAM"));

        r.Header.Should().NotBeNull();
        r.HivePath.Should().Be(TestHives.Path(@"SAM"));
        r.HiveType.Should().Be(HiveTypeEnum.Sam);
    }

    [Test]
    public void ShouldThrowExceptionWhenNotRegistryHiveAndByteArray()
    {
        var fileStream = new FileStream(TestHives.Path(@"NotAHive"), FileMode.Open, FileAccess.Read, FileShare.Read);
        var binaryReader = new BinaryReader(fileStream);

        binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);

        var fileBytes = binaryReader.ReadBytes((int) binaryReader.BaseStream.Length);

        binaryReader.Close();
        fileStream.Close();

        FluentActions.Invoking(() =>
            {
                var rb = new RegistryBase(fileBytes, TestHives.Path(@"NotAHive"));
            }).Should().Throw<ArgumentException>();
    }

    [Test]
    public void SoftwareHiveShouldHaveSoftwareHiveType()
    {
        var r = new RegistryBase(TestHives.Path(@"software"));
        HiveTypeEnum.Software.Should().Be(r.HiveType);
    }

    [Test]
    public void SystemHiveShouldHaveSystemHiveType()
    {
        var r = new RegistryBase(TestHives.Path(@"system"));
        HiveTypeEnum.System.Should().Be(r.HiveType);
    }

    [Test]
    public void UsrclassHiveShouldHaveUsrclassHiveType()
    {
        var r = new RegistryBase(TestHives.Path(@"UsrClass 1.dat"));
        HiveTypeEnum.UsrClass.Should().Be(r.HiveType);
    }

    [Test]
    public void Windows10ExtraData()
    {
        var r = new RegistryBase(TestHives.Corpus(@"SOFTWARE_win10"));
        r.Header.KtmFlags.Should().Be(KtmFlag.Unset);
        r.Header.LastReorganizedTimestamp.HasValue.Should().BeTrue();
    }
}