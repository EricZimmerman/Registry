using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandLine;
using NLog;
using NLog.Config;
using NLog.Targets;
using Registry;
using Registry.Cells;
using Registry.Other;

// namespaces...

namespace ExampleApp
{
    // internal classes...
    internal class Program
    {
        // private methods...

        private static LoggingConfiguration GetNlogConfig(int level, string logFilePath)
        {
            var config = new LoggingConfiguration();

            var loglevel = LogLevel.Info;

            switch (level)
            {
                case 1:
                    loglevel = LogLevel.Debug;
                    break;

                case 2:
                    loglevel = LogLevel.Trace;
                    break;
                default:
                    break;
            }

            var callsite = "${callsite:className=false}";
            if (loglevel < LogLevel.Trace)
            {
                //if trace use expanded callstack
                callsite = "${callsite:className=false:fileName=true:includeSourcePath=true:methodName=true}";
            }

            // Step 2. Create targets and add them to the configuration 
            var consoleTarget = new ColoredConsoleTarget();

            //var consoleWrapper = new AsyncTargetWrapper();
            //consoleWrapper.WrappedTarget = consoleTarget;
            //consoleWrapper.QueueLimit = 5000;
            //consoleWrapper.OverflowAction = AsyncTargetWrapperOverflowAction.Grow;

            //     config.AddTarget("console", consoleWrapper);
            config.AddTarget("console", consoleTarget);


            if (logFilePath != null)
            {
                if (Directory.Exists(logFilePath))
                {
                    var fileTarget = new FileTarget();

                    //var fileWrapper = new AsyncTargetWrapper();
                    //fileWrapper.WrappedTarget = fileTarget;
                    //fileWrapper.QueueLimit = 5000;
                    //fileWrapper.OverflowAction = AsyncTargetWrapperOverflowAction.Grow;

                    //config.AddTarget("file", fileWrapper);
                    config.AddTarget("file", fileTarget);

                    fileTarget.FileName = string.Format("{0}/{1}_log.txt", logFilePath, Guid.NewGuid());
                        // "${basedir}/file.txt";

                    fileTarget.Layout = @"${longdate} ${logger} " + callsite +
                                        " ${level:uppercase=true} ${message} ${exception:format=ToString,StackTrace}";

                    //var rule2 = new LoggingRule("*", loglevel, fileWrapper);
                    var rule2 = new LoggingRule("*", loglevel, fileTarget);
                    config.LoggingRules.Add(rule2);
                }
            }

            consoleTarget.Layout = @"${longdate} ${logger} " + callsite +
                                   " ${level:uppercase=true} ${message} ${exception:format=ToString,StackTrace}";

            // Step 4. Define rules
            //   var rule1 = new LoggingRule("*", loglevel, consoleWrapper);
            var rule1 = new LoggingRule("*", loglevel, consoleTarget);
            config.LoggingRules.Add(rule1);


            return config;
        }

        private static void Main(string[] args)
        {
            var testFiles = new List<string>();


            var result = Parser.Default.ParseArguments<Options>(args);
            if (!result.Errors.Any())
            {
                if (result.Value.HiveName == null && result.Value.DirectoryName == null)
                {
                    Console.WriteLine(result.Value.GetUsage());
                    Environment.Exit(1);
                }

                if (!string.IsNullOrEmpty(result.Value.HiveName))
                {
                    if (!string.IsNullOrEmpty(result.Value.DirectoryName))
                    {
                        Console.WriteLine("Must specify either -d or -f, but not both");
                        Environment.Exit(1);
                    }
                }

                if (!string.IsNullOrEmpty(result.Value.DirectoryName))
                {
                    if (!string.IsNullOrEmpty(result.Value.HiveName))
                    {
                        Console.WriteLine("Must specify either -d or -f, but not both");
                        Environment.Exit(1);
                    }
                }

                if (!string.IsNullOrEmpty(result.Value.HiveName))
                {
                    testFiles.Add(result.Value.HiveName);
                }
                else
                {
                    if (Directory.Exists(result.Value.DirectoryName))
                    {
                        foreach (var file in Directory.GetFiles(result.Value.DirectoryName))
                        {
                            testFiles.Add(file);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Directory '{0}' does not exist!", result.Value.DirectoryName);
                        Environment.Exit(1);
                    }
                }
            }
            else
            {
                Console.WriteLine(result.Value.GetUsage());
                Environment.Exit(1);
            }

            var verboseLevel = result.Value.VerboseLevel;
            if (verboseLevel < 0)
            {
                verboseLevel = 0;
            }
            if (verboseLevel > 2)
            {
                verboseLevel = 2;
            }

            var config = GetNlogConfig(verboseLevel, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            LogManager.Configuration = config;

            var logger = LogManager.GetCurrentClassLogger();

			foreach (var testFile in testFiles)
            {
                if (File.Exists(testFile) == false)
                {
                    logger.Error("'{0}' does not exist!", testFile);
                    continue;
                }

                logger.Info("Processing '{0}'", testFile);
                Console.Title = string.Format("Processing '{0}'", testFile);

                var sw = new Stopwatch();
                try
                {
                    var fName1Test = new RegistryHive(testFile);

                    RegistryHive.NlogConfig = config;
              
                    sw.Start();

                    fName1Test.RecoverDeleted = result.Value.RecoverDeleted;

                    fName1Test.ParseHive();

                    logger.Info("Finished processing '{0}'", testFile);
                    Console.Title = string.Format("Finished processing '{0}'", testFile);

                    sw.Stop();

                    var freeCells = fName1Test.CellRecords.Where(t => t.Value.IsFree);
                    var referencedCells = fName1Test.CellRecords.Where(t => t.Value.IsReferenced);

                    var nkFree = freeCells.Count(t => t.Value is NKCellRecord);
                    var vkFree = freeCells.Count(t => t.Value is VKCellRecord);
                    var skFree = freeCells.Count(t => t.Value is SKCellRecord);
                    var lkFree = freeCells.Count(t => t.Value is LKCellRecord);

                    var freeLists = fName1Test.ListRecords.Where(t => t.Value.IsFree);
                    var referencedList = fName1Test.ListRecords.Where(t => t.Value.IsReferenced);

                    var goofyCellsShouldBeUsed =
                        fName1Test.CellRecords.Where(t => t.Value.IsFree == false && t.Value.IsReferenced == false);

                    var goofyListsShouldBeUsed =
                        fName1Test.ListRecords.Where(t => t.Value.IsFree == false && t.Value.IsReferenced == false);

                    var sb = new StringBuilder();

                    sb.AppendLine("Results:");
                    sb.AppendLine();

                    sb.AppendLine(
                        string.Format(
                            "Found {0:N0} hbin records. Total size of seen hbin records: 0x{1:X}, Header hive size: 0x{2:X}",
                            fName1Test.HBinRecordCount, fName1Test.HBinRecordTotalSize, fName1Test.Header.Length));
                    sb.AppendLine(
                        string.Format("Found {0:N0} Cell records (nk: {1:N0}, vk: {2:N0}, sk: {3:N0}, lk: {4:N0})",
                            fName1Test.CellRecords.Count, fName1Test.CellRecords.Count(w => w.Value is NKCellRecord),
                            fName1Test.CellRecords.Count(w => w.Value is VKCellRecord),
                            fName1Test.CellRecords.Count(w => w.Value is SKCellRecord),
                            fName1Test.CellRecords.Count(w => w.Value is LKCellRecord)));
                    sb.AppendLine(string.Format("Found {0:N0} List records", fName1Test.ListRecords.Count));

                    sb.AppendLine();

                    sb.AppendLine(string.Format("There are {0:N0} cell records marked as being referenced ({1:P})",
                        referencedCells.Count(), referencedCells.Count()/(double) fName1Test.CellRecords.Count));
                    sb.AppendLine(string.Format("There are {0:N0} list records marked as being referenced ({1:P})",
                        referencedList.Count(), referencedList.Count()/(double) fName1Test.ListRecords.Count));

                    if (result.Value.RecoverDeleted)
                    {
                        sb.AppendLine();
                        sb.AppendLine("Free record info");
                        sb.AppendLine(string.Format(
                            "{0:N0} free Cell records (nk: {1:N0}, vk: {2:N0}, sk: {3:N0}, lk: {4:N0})",
                            freeCells.Count(), nkFree, vkFree, skFree, lkFree));
                        sb.AppendLine(string.Format("{0:N0} free List records", freeLists.Count()));
                    }

                    sb.AppendLine();
                    sb.AppendLine(string.Format(
                        "There were {0:N0} hard parsing errors (a record marked 'in use' that didn't parse correctly.)",
                        fName1Test.HardParsingErrors));
                    sb.AppendLine(string.Format(
                        "There were {0:N0} soft parsing errors (a record marked 'free' that didn't parse correctly.)",
                        fName1Test.SoftParsingErrors));

                    sb.AppendLine();
                    sb.AppendLine(
                        string.Format("Cells: Free + referenced + marked as in use but not referenced == Total? {0}",
                            fName1Test.CellRecords.Count ==
                            freeCells.Count() + referencedCells.Count() + goofyCellsShouldBeUsed.Count()));
                    sb.AppendLine(
                        string.Format("Lists: Free + referenced + marked as in use but not referenced == Total? {0}",
                            fName1Test.ListRecords.Count ==
                            freeLists.Count() + referencedList.Count() + goofyListsShouldBeUsed.Count()));

                    logger.Info(sb.ToString());


                    if (result.Value.ExportHiveData)
                    {
                        Console.WriteLine();


                        var baseDir = Path.GetDirectoryName(testFile);
                        var baseFname = Path.GetFileName(testFile);

                        var myName = string.Empty;

                        var deletedOnly = result.Value.ExportDeletedOnly;

                        if (deletedOnly)
                        {
                            myName = "_EricZ_recovered.txt";
                        }
                        else
                        {
                            myName = "_EricZ_all.txt";
                        }

                        var outfile = Path.Combine(baseDir, string.Format("{0}{1}", baseFname, myName));

                        logger.Info("Exporting hive data to '{0}'", outfile);

                        fName1Test.ExportDataToCommonFormat(outfile, deletedOnly);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("There was an error: {0}", ex.Message);
                }


                logger.Info("Processing took {0:N4} seconds\r\n", sw.Elapsed.TotalSeconds);

                Console.WriteLine();
                Console.WriteLine();

                if (result.Value.PauseAfterEachFile)
                {
                    Console.WriteLine("Press any key to continue to next file");
                    Console.ReadKey();

                    Console.WriteLine();
                    Console.WriteLine();
                }
            }
        }
    }
}