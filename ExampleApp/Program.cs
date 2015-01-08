using Registry;
using Registry.Cells;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

// namespaces...
namespace ExampleApp
{
    // internal classes...
    internal class Program
    {
        // private methods...
        private static void Main(string[] args)
        {
            var testFiles = new List<string>();



            var result = CommandLine.Parser.Default.ParseArguments<Options>(args);
            if (!result.Errors.Any())
            {
                if (result.Value.HiveName == null && result.Value.DirectoryName == null)
                {
                    Console.WriteLine(result.Value.GetUsage());
                    Environment.Exit(1);
                }

                if (!string.IsNullOrEmpty(result.Value.HiveName) )
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



            foreach (var testFile in testFiles)
            {
                if (File.Exists(testFile) == false)
                {
                    Console.WriteLine("'{0}' does not exist!", testFile);
                    continue;
                }

                Console.WriteLine("Processing '{0}'", testFile);
                Console.Title = string.Format( "Processing '{0}'", testFile);

                using (var fName1Test = new RegistryHive(testFile, false))
                {
                    var sw = new Stopwatch();
                    sw.Start();

                    try
                    {
                        fName1Test.ParseHive(false);

                        Console.WriteLine("Finished processing '{0}'", testFile);
                        Console.Title = string.Format("Finished processing '{0}'", testFile);

                        sw.Stop();

                        var freeCells = RegistryHive.CellRecords.Where(t => t.Value.IsFree);
                        var referencedCells = RegistryHive.CellRecords.Where(t => t.Value.IsReferenced);

                        var nkFree = freeCells.Count(t => t.Value is NKCellRecord);
                        var vkFree = freeCells.Count(t => t.Value is VKCellRecord);
                        var skFree = freeCells.Count(t => t.Value is SKCellRecord);
                        var lkFree = freeCells.Count(t => t.Value is LKCellRecord);


                        var freeLists = RegistryHive.ListRecords.Where(t => t.Value.IsFree);
                        var referencedList = RegistryHive.ListRecords.Where(t => t.Value.IsReferenced);

                        var referencedData = RegistryHive.DataRecords.Where(t => t.Value.IsReferenced);
                        var freeData = RegistryHive.DataRecords.Where(t => t.Value.IsFree);

                        //need to change these to public classes first
                        //var dbFree = freeData.Count(t => t.Value is DBListRecord);
                        //var liFree = freeLists.Count(t => t.Value is lilistrecord);
                        //var riFree = freeLists.Count(t => t.Value is SKCellRecord);
                        //var lhFree = freeLists.Count(t => t.Value is SKCellRecord);
                        //var lfFree = freeLists.Count(t => t.Value is SKCellRecord);




                        //we can look thru records marked in use but not referenced to see if things are broken
                        //records marked as in use but not referenced by anything (should be 0?)
                        var goofyCellsShouldBeUsed = RegistryHive.CellRecords.Where(t => t.Value.IsFree == false && t.Value.IsReferenced == false);

                        //referenced by another record somewhere, but marked as free based on size
                        var goofyCellsShouldBeAllocated = RegistryHive.CellRecords.Where(t => t.Value.IsFree  && t.Value.IsReferenced);

                        var goofyListsShouldBeUsed = RegistryHive.ListRecords.Where(t => t.Value.IsFree == false && t.Value.IsReferenced == false);
                        var goofyListsShouldBeAllocated = RegistryHive.ListRecords.Where(t => t.Value.IsFree  && t.Value.IsReferenced );

                        var goofyDataShouldBeUsed = RegistryHive.DataRecords.Where(t => t.Value.IsFree == false && t.Value.IsReferenced == false);
                        var goofyDataShouldBeAllocated = RegistryHive.DataRecords.Where(t => t.Value.IsFree && t.Value.IsReferenced);

                        Console.WriteLine();
                        Console.WriteLine("Found {0:N0} hbin records", RegistryHive.HBinRecords.Count);
                        Console.WriteLine("Found {0:N0} Cell records (nk: {1:N0}, vk: {2:N0}, sk: {3:N0}, lk: {4:N0})", RegistryHive.CellRecords.Count, RegistryHive.CellRecords.Count(w => w.Value is NKCellRecord), RegistryHive.CellRecords.Count(w => w.Value is VKCellRecord), RegistryHive.CellRecords.Count(w => w.Value is SKCellRecord), RegistryHive.CellRecords.Count(w => w.Value is LKCellRecord));
                        Console.WriteLine("Found {0:N0} List records", RegistryHive.ListRecords.Count);
                        Console.WriteLine("Found {0:N0} Data records", RegistryHive.DataRecords.Count);

                        Console.WriteLine();
                        Console.WriteLine("Found {0:N0} free Cell records (nk: {1:N0}, vk: {2:N0}, sk: {3:N0}, lk: {4:N0})", freeCells.Count(), nkFree, vkFree, skFree, lkFree);
                        Console.WriteLine("Found {0:N0} free List records",  freeLists.Count());
                        Console.WriteLine("Found {0:N0} free Data records", freeData.Count());

                        Console.WriteLine();
                        Console.WriteLine("There are {0:N0} cell records marked as being referenced ({1:P})", referencedCells.Count(), (double)referencedCells.Count() / (double)RegistryHive.CellRecords.Count);
                        Console.WriteLine("There are {0:N0} list records marked as being referenced ({1:P})", referencedList.Count(), (double)referencedList.Count() / (double)RegistryHive.ListRecords.Count);
                        Console.WriteLine("There are {0:N0} data records marked as being referenced ({1:P})", referencedData.Count(), (double)referencedData.Count() / (double)RegistryHive.DataRecords.Count);

                        Console.WriteLine();
                        Console.WriteLine("There were {0:N0} cell records marked as in use but not referenced by anything in the registry tree", goofyCellsShouldBeUsed.Count());
                        Console.WriteLine("There were {0:N0} cell records referenced by another record somewhere, but marked as free based on size in the registry tree", goofyCellsShouldBeAllocated.Count());
                        Console.WriteLine("There were {0:N0} list records marked as in use but not referenced by anything in the registry tree", goofyListsShouldBeUsed.Count());
                        Console.WriteLine("There were {0:N0} list records referenced by another record somewhere, but marked as free based on size in the registry tree", goofyListsShouldBeAllocated.Count());
                        Console.WriteLine("There were {0:N0} data records marked as in use but not referenced by anything in the registry tree", goofyDataShouldBeUsed.Count());
                        Console.WriteLine("There were {0:N0} data records referenced by another record somewhere, but marked as free based on size in the registry tree", goofyDataShouldBeAllocated.Count());

                        Console.WriteLine();
                        Console.WriteLine("There were {0:N0} hard parsing errors (a record marked 'in use' that didn't parse correctly.)", fName1Test.HardParsingErrors);
                        Console.WriteLine("There were {0:N0} soft parsing errors (a record marked 'free' that didn't parse correctly.)", fName1Test.SoftParsingErrors);

                        Console.WriteLine();
                        Console.WriteLine("Cells: Free + referenced + marked as in use but not referenced == Total? {0}", RegistryHive.CellRecords.Count == freeCells.Count() + referencedCells.Count() + goofyCellsShouldBeUsed.Count());
                        Console.WriteLine("Lists: Free + referenced + marked as in use but not referenced == Total? {0}", RegistryHive.ListRecords.Count == freeLists.Count() + referencedList.Count() + goofyListsShouldBeUsed.Count());
                        Console.WriteLine("Data:  Free + referenced + marked as in use but not referenced == Total? {0}", RegistryHive.DataRecords.Count == freeData.Count() + referencedData.Count() + goofyDataShouldBeUsed.Count());



                        #region TestStuffForViewingUnreferenced
                        //      var baseDir1 = Path.GetDirectoryName(testFile);
                        //      var baseFname1 = Path.GetFileName(testFile);
                        //      var myName1 = "_unref-output.txt";

                        //      var outfile1 = Path.Combine(baseDir1, string.Format("{0}{1}", baseFname1, myName1));

                        //      var unrefcells = RegistryHive.CellRecords.Where(t => t.Value.IsReferenced == false);
                        //

                        //          if (unrefcells.Any())
                        //          {
                        //              File.WriteAllText(outfile1,string.Format("Found {0:N0} free Cell records (nk: {1:N0}, vk: {2:N0}, sk: {3:N0}, lk: {4:N0})\r\n", freeCells.Count(), nkFree, vkFree, skFree, lkFree));
                        //              File.AppendAllText(outfile1, string.Format("Found {0:N0} free List records\r\n", freeLists.Count()));
                        //              File.AppendAllText(outfile1, string.Format("Found {0:N0} free Data records\r\n\r\n", freeData.Count()));
                        //          }

                        //              foreach (var keyValuePair in unrefcells)
                        //              {
                        //              var content = string.Format("{0}\r\n---------------------------\r\n\r\n", keyValuePair.Value == null ? "(Null)" : keyValuePair.Value.ToString());

                        //                  //TODO add a check here into referenced cells to see if an active parent exists if its an nk record

                        //                  File.AppendAllText(outfile1, content);
                        //              }

                        //          // lists dont really do anything for us since free lists have their # of entries set to 0
                        //              //foreach (var keyValuePair in unrefLists)
                        //              //{
                        //              //    var content = string.Format("{0}\r\n---------------------------\r\n\r\n", keyValuePair.Value);
                        //              //    File.AppendAllText(outfile1, content);
                        //              //}
                        #endregion



                        if (result.Value.ExportHiveData)
                        {
                            var baseDir = Path.GetDirectoryName(testFile);
                            var baseFname = Path.GetFileName(testFile);
                            var myName = "eric-output.txt";

                            var outfile = Path.Combine(baseDir, string.Format("{0}{1}", baseFname, myName));
                            fName1Test.ExportDataToWilliFormat(outfile,true);
                        }
                    }
                    catch (Exception ex)
                    {
                 
                        Console.WriteLine("There was an error: {0}", ex.Message);
                    }
                    

                    Console.WriteLine();
                    Console.WriteLine();
                  

                    Console.WriteLine("Processing took {0:N4} seconds", sw.Elapsed.TotalSeconds);

                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue to next file");
                    Console.ReadKey();

                    Console.WriteLine();
                    Console.WriteLine();
                }
            }
        }
    }
}
