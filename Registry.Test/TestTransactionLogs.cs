using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using Serilog;

namespace Registry.Test;




public class TestTransactionLogs
{
   
    
    
    
    [Test]
    public void HiveTestAmcache()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Verbose()
            .CreateLogger();


        var hive = TestHives.Corpus(@"amcache\aa\Amcache.hve");
        var hive1 = new RegistryHive(hive);

        var log1 = $"{hive}.LOG1";
        var log2 = $"{hive}.LOG2";

        var logs = new List<string>();
        logs.Add(log1);
        logs.Add(log2);

        var newb = hive1.ProcessTransactionLogs(logs);


        var newName = hive + "_NONDIRTY";

        File.WriteAllBytes(newName, newb);
    }

//        [Test]
//        public void Pooh()
//        {
//            var log1 = $"D:\\!downloads\\Pooh.hve.LOG1";
//            var hive = $"D:\\!downloads\\Pooh.hve";
//
//            var logs = new List<string>();
//            logs.Add(log1);
//
//            var hive1 = new RegistryHive(hive);
//
//            if (hive1.Header.PrimarySequenceNumber != hive1.Header.SecondarySequenceNumber)
//            {
//                Debug.WriteLine("");
//                Debug.WriteLine(
//                    $"File: {hive} Valid checksum: {hive1.Header.ValidateCheckSum()} Primary: 0x{hive1.Header.PrimarySequenceNumber:X} Secondary: 0x{hive1.Header.SecondarySequenceNumber:X}");
//                var newb = hive1.ProcessTransactionLogs(logs);
//
//                var newName = hive + "_NONDIRTY";
//
//                File.WriteAllBytes(newName, newb);
//            }
//
//        }

    [Test]
    public void OneOff2()
    {

    }



}