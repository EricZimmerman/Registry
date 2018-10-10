using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace Registry.Test
{
    public class TestTransactionLogs
    {
        [Test]
        public void HiveTests()
        {
            var dir = @"C:\Temp\hives";

            var files = Directory.GetFiles(dir);

            foreach (var file in files)
            {
                if (file.Contains("LOG") || file.EndsWith("_NONDIRTY"))
                {
                    continue;
                }

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
}