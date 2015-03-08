using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NFluent;
using NUnit.Framework;

namespace Registry.Test
{

    [TestFixture]
    class TestRegistryHiveOnDemandClass
    {
        private const string BasePath = @"C:\ProjectWorkingFolder\Registry2\Registry\Registry.Test\TestFiles";
        private string _driversHive = Path.Combine(BasePath, "DRIVERS");
        private string _samHive = Path.Combine(BasePath, "SAM");

        private RegistryHiveOnDemand DriversHive;
        private RegistryHiveOnDemand SamHive;

        [TestFixtureSetUp]
        public void Init()
        {
            DriversHive = new RegistryHiveOnDemand(_driversHive);
            SamHive = new RegistryHiveOnDemand(_samHive);
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

            var r = new RegistryHiveOnDemand(fileBytes);

            Check.That(r.Header).IsNotNull();
            Check.That(r.HivePath).IsEqualTo("None");
            Check.That(r.HiveType).IsEqualTo(HiveTypeEnum.Sam);
        }

        [Test]
        public void GetKeyShouldNotBeNullWithFullPath()
        {
            var key = SamHive.GetKey(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Account");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void GetKeyShouldNotBeNullWithShortPath()
        {
            var key = SamHive.GetKey(@"SAM\Domains\Account");

            Check.That(key).IsNotNull();
         }

        [Test]
        public void GetKeyShouldNotBeNullWithShortPathMixedSpelling()
        {
            var key = SamHive.GetKey(@"SAM\DomAins\AccoUnt");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void GetKeyShouldBeNullWithNonExistentPath()
        {
            var key = SamHive.GetKey(@"SAM\Domains\Account\This\Does\Not\Exist");

            Check.That(key).IsNull();
        }

        [Test]
        public void TestsListRecords()
        {
            var key = DriversHive.GetKey(@"{15a87b70-bc78-114a-95b7-b90ca5d0ec00}\DriverDatabase\DeviceIds");

            Check.That(key).IsNotNull();
            Check.That(key.SubKeys.Count).IsEqualTo(3878);
        }

        [Test]
        public void TestsListRecordsContinued2()
        {
            var hivePath = Path.Combine(BasePath, "NTUSER.DAT");
            var r = new RegistryHiveOnDemand(hivePath);

            var key = r.GetKey(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\PremiumSoft\NavicatSQLite\lvMainViewStyle");

            Check.That(key).IsNotNull();
            Check.That(key.SubKeys.Count).IsEqualTo(712);
            Check.That(key.Values.Count).IsEqualTo(84);

        }
        [Test]
        public void TestsListRecordsContinued3()
        {
            var hivePath = Path.Combine(BasePath, "UsrClass FTP.dat");
            var r = new RegistryHiveOnDemand(hivePath);

            var key = r.GetKey(@"S-1-5-21-2417227394-2575385136-2411922467-1105_Classes\ActivatableClasses\CLSID");

            Check.That(key).IsNotNull();
            Check.That(key.SubKeys.Count).IsEqualTo(2811);
        }

        [Test]
        public void TestsListRecordsContinued()
        {
            var key = DriversHive.GetKey(@"{15a87b70-bc78-114a-95b7-b90ca5d0ec00}");

            Check.That(key).IsNotNull();
            Check.That(key.SubKeys.Count).IsEqualTo(1);
        }

    }
}
