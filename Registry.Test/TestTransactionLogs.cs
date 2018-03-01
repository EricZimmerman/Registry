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
            var log1 = @"C:\Users\eric\Desktop\8.1-unreconciled\after\SYSTEM.LOG1";
            var log2 = @"C:\Users\eric\Desktop\8.1-unreconciled\after\SYSTEM.LOG2";

            var hive1 = new RegistryHive(@"C:\Users\eric\Desktop\8.1-unreconciled\after\SYSTEM");
            hive1.ParseHive();

            var logs = new List<string>();
            logs.Add(log1);
            logs.Add(log2);

            var newb = hive1.ProcessTransactionLogs(logs, (int) hive1.Header.SecondarySequenceNumber);



            var r = new TransactionLog(@"C:\Users\eric\Desktop\8.1-unreconciled\after\SYSTEM.LOG1");
            Check.That(HiveTypeEnum.System).IsEqualTo(r.HiveType);

            Check.That(r.Header.ValidateCheckSum()).IsTrue();

            r.ParseLog();

            var hiveBytes = File.ReadAllBytes(@"C:\Users\eric\Desktop\8.1-unreconciled\after\SYSTEM");

            var hive = new RegistryHive(@"C:\Users\eric\Desktop\8.1-unreconciled\after\SYSTEM");


          var newHiveBytes =   r.UpdateHiveBytes(hiveBytes,(int) hive.Header.SecondarySequenceNumber);

            

            var r1 = new TransactionLog(@"C:\Users\eric\Desktop\8.1-unreconciled\after\SYSTEM.LOG2");
            Check.That(HiveTypeEnum.System).IsEqualTo(r.HiveType);

            Check.That(r1.Header.ValidateCheckSum()).IsTrue();

            r1.ParseLog();

            r1.UpdateHiveBytes(hiveBytes,(int) hive.Header.SecondarySequenceNumber);

            File.WriteAllBytes(@"C:\temp\newSYSTEM",newHiveBytes);

        }
    }
}
