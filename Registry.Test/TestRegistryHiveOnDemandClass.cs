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
        private const string _basePath = @"C:\ProjectWorkingFolder\Registry2\Registry\Registry.Test\TestFiles";

        [Test]
        public void GetKeyShouldNotBeNullWithFullPath()
        {
            var hivePath = Path.Combine(_basePath, "SAM");
            var r = new RegistryHiveOnDemand(hivePath);

            var key = r.GetKey(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Account");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void GetKeyShouldNotBeNullWithShortPath()
        {
            var hivePath = Path.Combine(_basePath, "SAM");
            var r = new RegistryHiveOnDemand(hivePath);

            var key = r.GetKey(@"SAM\Domains\Account");

            Check.That(key).IsNotNull();
         }

        [Test]
        public void GetKeyShouldNotBeNullWithShortPathMixedSpelling()
        {
            var hivePath = Path.Combine(_basePath, "SAM");
            var r = new RegistryHiveOnDemand(hivePath);

            var key = r.GetKey(@"SAM\DomAins\AccoUnt");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void GetKeyShouldBeNullWithNonExistentPath()
        {
            var hivePath = Path.Combine(_basePath, "SAM");
            var r = new RegistryHiveOnDemand(hivePath);

            var key = r.GetKey(@"SAM\Domains\Account\This\Does\Not\Exist");

            Check.That(key).IsNull();
            
        }

        [Test]
        public void TestsListRecords()
        {
            var hivePath = Path.Combine(_basePath, "DRIVERS");
            var r = new RegistryHiveOnDemand(hivePath);

            var key = r.GetKey(@"{15a87b70-bc78-114a-95b7-b90ca5d0ec00}\DriverDatabase\DeviceIds");

            Check.That(key).IsNotNull();

        }

        [Test]
        public void TestsListRecordsContinued2()
        {
            var hivePath = Path.Combine(_basePath, "NTUSER.DAT");
            var r = new RegistryHiveOnDemand(hivePath);

            var key = r.GetKey(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\PremiumSoft\NavicatSQLite\lvMainViewStyle");

            Check.That(key).IsNotNull();

        }
        [Test]
        public void TestsListRecordsContinued3()
        {
            var hivePath = Path.Combine(_basePath, "UsrClass FTP.dat");
            var r = new RegistryHiveOnDemand(hivePath);

            var key = r.GetKey(@"S-1-5-21-2417227394-2575385136-2411922467-1105_Classes\ActivatableClasses\CLSID");

            Check.That(key).IsNotNull();

        }

       

        [Test]
        public void TestsListRecordsContinued()
        {
            var hivePath = Path.Combine(_basePath, "DRIVERS");
            var r = new RegistryHiveOnDemand(hivePath);

            var key = r.GetKey(@"{15a87b70-bc78-114a-95b7-b90ca5d0ec00}");

            Check.That(key).IsNotNull();


        }

    }
}
