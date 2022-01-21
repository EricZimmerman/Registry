using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using Serilog;

namespace Registry.Test;

public class TestTransactionLogs
{
    [Test]
    [Ignore("Unknown test source file.")]
    public void HiveTestAmcache()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Verbose()
            .CreateLogger();


        var hive = @"D:\SynologyDrive\Registry\amcache\aa\Amcache.hve";
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
    public void OneOff()
    {
        var log1 = "C:\\Users\\eric\\Desktop\\RegistryExplorer - Failed to Load Hives\\Stack\\NTUSER.DAT.LOG1";
        var log2 = "C:\\Users\\eric\\Desktop\\RegistryExplorer - Failed to Load Hives\\Stack\\NTUSER.DAT.LOG2";
        var hive = "C:\\Users\\eric\\Desktop\\RegistryExplorer - Failed to Load Hives\\Stack\\NTUSER.DAT";

        var logs = new List<string>();
        logs.Add(log1);
        logs.Add(log2);

        var hive1 = new RegistryHive(hive);

        if (hive1.Header.PrimarySequenceNumber != hive1.Header.SecondarySequenceNumber)
        {
            Debug.WriteLine("");
            Debug.WriteLine(
                $"File: {hive} Valid checksum: {hive1.Header.ValidateCheckSum()} Primary: 0x{hive1.Header.PrimarySequenceNumber:X} Secondary: 0x{hive1.Header.SecondarySequenceNumber:X}");
            var newb = hive1.ProcessTransactionLogs(logs, true);

            var newName = hive + "_NONDIRTY";

            File.WriteAllBytes(newName, newb);
        }
    }


    [Test]
    [Ignore("Unknown test source file.")]
    public void HiveTests()
    {
        var dir = @"C:\Temp\hives";

        var files = Directory.GetFiles(dir);

        foreach (var file in files)
        {
            if (file.Contains("LOG") || file.EndsWith("_NONDIRTY")) continue;

            var log1 = $"{file}.LOG1";
            var log2 = $"{file}.LOG2";

            var hive1 = new RegistryHive(file);

            var logs = new List<string>();
            logs.Add(log1);
            logs.Add(log2);

            if (hive1.Header.PrimarySequenceNumber != hive1.Header.SecondarySequenceNumber)
            {
                Debug.WriteLine("");
                Debug.WriteLine(
                    $"File: {file} Valid checksum: {hive1.Header.ValidateCheckSum()} Primary: 0x{hive1.Header.PrimarySequenceNumber:X} Secondary: 0x{hive1.Header.SecondarySequenceNumber:X}");
                var newb = hive1.ProcessTransactionLogs(logs);

                var newName = file + "_NONDIRTY";

                File.WriteAllBytes(newName, newb);
            }
        }
    }
}