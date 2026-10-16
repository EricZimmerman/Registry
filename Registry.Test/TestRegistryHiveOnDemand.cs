using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace Registry.Test;

[TestFixture]
internal class TestRegistryHiveOnDemand
{
    [Test]
    public void GetKeyShouldBeNullWithNonExistentPath()
    {
        var samOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM"));
        var key = samOnDemand.GetKey(@"SAM\Domains\Account\This\Does\Not\Exist");

        key.Should().BeNull();
    }

    [Test]
    public void GetKeyShouldNotBeNullWithFullPath()
    {
        var samOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM"));
        var key =
            samOnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Account");

        key.Should().NotBeNull();
    }

    [Test]
    public void GetKeyShouldNotBeNullWithShortPath()
    {
        var samOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM"));
        var key = samOnDemand.GetKey(@"SAM\Domains\Account");

        key.Should().NotBeNull();
    }

    [Test]
    public void GetKeyShouldNotBeNullWithShortPathMixedSpelling()
    {
        var samOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM"));
        var key = samOnDemand.GetKey(@"SAM\DomAins\AccoUnt");

        key.Should().NotBeNull();
    }

    [Test]
    public void ShouldProcessLiListRecord()
    {
        var usrClass1OnDemand = new RegistryHiveOnDemand(TestHives.Path(@"UsrClass 1.dat"));

        var key =
            usrClass1OnDemand.GetKey(
                @"S-1-5-21-2417227394-2575385136-2411922467-1105_Classes\\ActivatableClasses\\Package\\Microsoft.BingSports_3.0.4.244_x64__8wekyb3d8bbwe\\ActivatableClassId");

        key.Should().NotBeNull();
        key.NkRecord.SubkeyCountsStable.Should().Be(0x224);
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

        var r = new RegistryHiveOnDemand(fileBytes, TestHives.Path(@"SAM"));

        r.Header.Should().NotBeNull();
        r.HivePath.Should().Be(TestHives.Path(@"SAM"));
        r.HiveType.Should().Be(HiveTypeEnum.Sam);
    }

    [Test]
    public void TestFileNameConstructor()
    {
        var r = new RegistryHiveOnDemand(TestHives.Path(@"SAM"));

        r.Header.Should().NotBeNull();
    }

    [Test]
    public void TestsListRecords()
    {
        var driversOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"DRIVERS"));

        var key =
            driversOnDemand.GetKey(@"{15a87b70-bc78-114a-95b7-b90ca5d0ec00}\DriverDatabase\DeviceIds");

        key.Should().NotBeNull();
        key.SubKeys.Count.Should().Be(3878);
    }

    [Test]
    public void TestsListRecordsContinued()
    {
        var driversOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"DRIVERS"));


        var key = driversOnDemand.GetKey(@"{15a87b70-bc78-114a-95b7-b90ca5d0ec00}");

        key.Should().NotBeNull();
        key.SubKeys.Count.Should().Be(1);
    }

    [Test]
    public void TestsListRecordsContinued3()
    {
        var usrClassFtp = new RegistryHiveOnDemand(TestHives.Path(@"UsrClass FTP.dat"));

        var key =
            usrClassFtp.GetKey(
                @"S-1-5-21-2417227394-2575385136-2411922467-1105_Classes\ActivatableClasses\CLSID");

        key.Should().NotBeNull();
        key.SubKeys.Count.Should().Be(2811);
    }
}