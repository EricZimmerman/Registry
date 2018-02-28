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
   public class TestTransactionLogs
    {

        [Test]
        public void Something()
        {
            var r = new TransactionLog(@"C:\Users\eric\Desktop\8.1-unreconciled\after\SYSTEM.LOG1");
            Check.That(HiveTypeEnum.System).IsEqualTo(r.HiveType);

            r.ParseLog();

            var hiveBytes = File.ReadAllBytes(@"C:\Users\eric\Desktop\8.1-unreconciled\after\SYSTEM");

            var hive = new RegistryHive(@"C:\Users\eric\Desktop\8.1-unreconciled\after\SYSTEM");


          var newHiveBytes =   r.UpdateHiveBytes(hiveBytes,(int) hive.Header.SecondarySequenceNumber);

            

            var r1 = new TransactionLog(@"C:\Users\eric\Desktop\8.1-unreconciled\after\SYSTEM.LOG2");
            Check.That(HiveTypeEnum.System).IsEqualTo(r.HiveType);

            r1.ParseLog();

            r1.UpdateHiveBytes(hiveBytes,(int) hive.Header.SecondarySequenceNumber);

            File.WriteAllBytes(@"C:\temp\newSYSTEM",newHiveBytes);

        }
    }
}
