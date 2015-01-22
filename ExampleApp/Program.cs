using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using Registry;
using Registry.Cells;

// namespaces...

namespace ExampleApp
{
    // internal classes...
    internal class Program
    {
        private static void DumpConsoleMessage(string msg)
        {
            if (msg.Length > 0)
            {
                Console.WriteLine("{0}: {1}", DateTimeOffset.Now, msg);
            }
            else
            {
                Console.WriteLine();
            }
                
        }

        // private methods...
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


            foreach (var testFile in testFiles)
            {
                if (File.Exists(testFile) == false)
                {
                    DumpConsoleMessage(string.Format("'{0}' does not exist!", testFile));
                    continue;
                }

               DumpConsoleMessage(string.Format("Processing '{0}'", testFile));
                Console.Title = string.Format("Processing '{0}'", testFile);

                var fName1Test = new RegistryHive(testFile);

                var sw = new Stopwatch();
                sw.Start();

                try
                {
                    fName1Test.Message += (ss, ee) =>
                    {
                        DumpConsoleMessage(ee.Detail);
                    };

                    fName1Test.ParseHive();

                    DumpConsoleMessage(string.Format("Finished processing '{0}'", testFile));
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

                    var referencedData = fName1Test.DataRecords.Where(t => t.Value.IsReferenced);
                    var freeData = fName1Test.DataRecords.Where(t => t.Value.IsFree);

                    //we can look thru records marked in use but not referenced to see if things are broken
                    //records marked as in use but not referenced by anything (should be 0?)
                    var goofyCellsShouldBeUsed =
                        fName1Test.CellRecords.Where(t => t.Value.IsFree == false && t.Value.IsReferenced == false);

                    //referenced by another record somewhere, but marked as free based on size
                    //var goofyCellsShouldBeAllocated =
                    //    fName1Test.CellRecords.Where(t => t.Value.IsFree && t.Value.IsReferenced);

                    var goofyListsShouldBeUsed =
                        fName1Test.ListRecords.Where(t => t.Value.IsFree == false && t.Value.IsReferenced == false);
                    //var goofyListsShouldBeAllocated =
                    //    fName1Test.ListRecords.Where(t => t.Value.IsFree && t.Value.IsReferenced);

                    var goofyDataShouldBeUsed =
                        fName1Test.DataRecords.Where(t => t.Value.IsFree == false && t.Value.IsReferenced == false);
                    //var goofyDataShouldBeAllocated =
                    //    fName1Test.DataRecords.Where(t => t.Value.IsFree && t.Value.IsReferenced);

                    var sb = new StringBuilder();

                    sb.AppendLine("Results:");
                    sb.AppendLine();
                 
                    sb.AppendLine(string.Format("Found {0:N0} hbin records", fName1Test.HBinRecords.Count));
                   sb.AppendLine(string.Format("Found {0:N0} Cell records (nk: {1:N0}, vk: {2:N0}, sk: {3:N0}, lk: {4:N0})",
                        fName1Test.CellRecords.Count, fName1Test.CellRecords.Count(w => w.Value is NKCellRecord),
                        fName1Test.CellRecords.Count(w => w.Value is VKCellRecord),
                        fName1Test.CellRecords.Count(w => w.Value is SKCellRecord),
                        fName1Test.CellRecords.Count(w => w.Value is LKCellRecord)));
                    sb.AppendLine(string.Format("Found {0:N0} List records", fName1Test.ListRecords.Count));
                   sb.AppendLine(string.Format("Found {0:N0} Data records", fName1Test.DataRecords.Count));

                    sb.AppendLine();

                        sb.AppendLine(string.Format("There are {0:N0} cell records marked as being referenced ({1:P})",
                        referencedCells.Count(), referencedCells.Count() / (double)fName1Test.CellRecords.Count));
                        sb.AppendLine(string.Format("There are {0:N0} list records marked as being referenced ({1:P})",
                        referencedList.Count(), referencedList.Count() / (double)fName1Test.ListRecords.Count));
                        sb.AppendLine(string.Format("There are {0:N0} data records marked as being referenced ({1:P})",
                        referencedData.Count(), referencedData.Count() / (double)fName1Test.DataRecords.Count));

                    sb.AppendLine();
                    sb.AppendLine("Free record info");
                          sb.AppendLine(string.Format(
                        "{0:N0} free Cell records (nk: {1:N0}, vk: {2:N0}, sk: {3:N0}, lk: {4:N0})",
                        freeCells.Count(), nkFree, vkFree, skFree, lkFree));
                          sb.AppendLine(string.Format("{0:N0} free List records", freeLists.Count()));
                          sb.AppendLine(string.Format("{0:N0} free Data records", freeData.Count()));

                      

                     

                    //Console.WriteLine();
                    //Console.WriteLine(
                    //    "There were {0:N0} cell records marked as in use but not referenced by anything in the registry tree",
                    //    goofyCellsShouldBeUsed.Count());
                    //Console.WriteLine(
                    //    "There were {0:N0} cell records referenced by another record somewhere, but marked as free based on size in the registry tree",
                    //    goofyCellsShouldBeAllocated.Count());
                    //Console.WriteLine(
                    //    "There were {0:N0} list records marked as in use but not referenced by anything in the registry tree",
                    //    goofyListsShouldBeUsed.Count());
                    //Console.WriteLine(
                    //    "There were {0:N0} list records referenced by another record somewhere, but marked as free based on size in the registry tree",
                    //    goofyListsShouldBeAllocated.Count());
                    //Console.WriteLine(
                    //    "There were {0:N0} data records marked as in use but not referenced by anything in the registry tree",
                    //    goofyDataShouldBeUsed.Count());
                    //Console.WriteLine(
                    //    "There were {0:N0} data records referenced by another record somewhere, but marked as free based on size in the registry tree",
                    //    goofyDataShouldBeAllocated.Count());

                    sb.AppendLine();
                          sb.AppendLine(string.Format(
                        "There were {0:N0} hard parsing errors (a record marked 'in use' that didn't parse correctly.)",
                        fName1Test.HardParsingErrors));
                          sb.AppendLine(string.Format(
                        "There were {0:N0} soft parsing errors (a record marked 'free' that didn't parse correctly.)",
                        fName1Test.SoftParsingErrors));

                          sb.AppendLine();
                          sb.AppendLine(string.Format("Cells: Free + referenced + marked as in use but not referenced == Total? {0}",
                        fName1Test.CellRecords.Count ==
                        freeCells.Count() + referencedCells.Count() + goofyCellsShouldBeUsed.Count()));
                          sb.AppendLine(string.Format("Lists: Free + referenced + marked as in use but not referenced == Total? {0}",
                        fName1Test.ListRecords.Count ==
                        freeLists.Count() + referencedList.Count() + goofyListsShouldBeUsed.Count()));
                          sb.AppendLine(string.Format("Data:  Free + referenced + marked as in use but not referenced == Total? {0}",
                        fName1Test.DataRecords.Count ==
                        freeData.Count() + referencedData.Count() + goofyDataShouldBeUsed.Count()));

                          DumpConsoleMessage(sb.ToString());

                    #region TestStuffForViewingUnreferenced

                    //var baseDir1 = Path.GetDirectoryName(testFile);
                    //var baseFname1 = Path.GetFileName(testFile);
                    //var myName1 = "_unref-output.txt";

                    //var outfile1 = Path.Combine(baseDir1, string.Format("{0}{1}", baseFname1, myName1));

                    //File.WriteAllText(outfile1, "NK absoluteoffsets");
                    //foreach (var source in RegistryHive.CellRecords.Where(q=>q.Value is NKCellRecord))
                    //{
                    //    File.AppendAllText(outfile1, string.Format("0x{0:X}\r\n", source.Value.AbsoluteOffset));
                    //}


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

                        //A file ending with "_fullpath.txt" can contain both allocated and unallocated named and value keys.
                        //A file ending with "_recovered_fullpath.txt" is only recovered unallocated keys with the fullpath.
                        //A file ending with "_recovered_nameonly.txt" is only recovered with just the name of the object.

                        var myName = string.Empty;

                        bool deletedOnly = false;
                        bool fullpath = false;

                        if (deletedOnly)
                        {
                            if (fullpath)
                            {
                                myName = "_EricZ_recovered_fullpath.txt";
                            }
                            else
                            {
                                myName = "_EricZ_recovered_nameonly.txt";
                            }
                        }
                        else
                        {
                            myName = "_EricZ_fullpath.txt";
                        }


                        var outfile = Path.Combine(baseDir, string.Format("{0}{1}", baseFname, myName));

                        fName1Test.ExportDataToCommonFormat(outfile, deletedOnly, fullpath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("There was an error: {0}", ex.Message);
                }



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