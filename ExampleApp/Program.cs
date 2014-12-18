using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\others\.copy2 UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\1_UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\3_UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\a8.21_UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\Acronis_0x52_Usrclass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\admin2UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\AdminUsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\b18_UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\win7\copy0UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\others\copy0UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\others\copy1 UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\win7\copy1UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!ext block mismatch\DanBinghamUsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\DanP_1UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\DaveB_UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_2-samsung_galaxys3_android\default");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_3-samsung_galaxys4_android\default");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_1-sandisk_sansa_m240\default");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!Strange\Devon_UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\ERZ Bitlocker UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\ERZ_Win81_ntuser.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\ERZ_Win81_UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!Strange\erzUsrClass (2 unk blocks).dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\help2UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\helpdeskUsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!ext block mismatch\keggUsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MTP Galaxy 3 UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\nfuryUsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\nromanUsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_2-samsung_galaxys3_android\Users\Win7SP1\NTUSER.DAT");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_3-samsung_galaxys4_android\Users\Win7SP1\NTUSER.DAT");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!Strange\Donald\NTUSER.DAT");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_1-sandisk_sansa_m240\Users\Default\NTUSER.DAT");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_3-samsung_galaxys4_android\Users\Default\NTUSER.DAT");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_2-samsung_galaxys3_android\Users\Default\NTUSER.DAT");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\Susan Storm Richards\NTUSER.DAT");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_1-sandisk_sansa_m240\Users\Win7SP1\NTUSER.DAT");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\Reed Richards\NTUSER.DAT");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!ext block mismatch\punja UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!ext block mismatch\RichUsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\rsyd3UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\rsydowUsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_1-sandisk_sansa_m240\SAM");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_3-samsung_galaxys4_android\SAM");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_2-samsung_galaxys3_android\SAM");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_2-samsung_galaxys3_android\SECURITY");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_1-sandisk_sansa_m240\SECURITY");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_3-samsung_galaxys4_android\SECURITY");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_3-samsung_galaxys4_android\software");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_1-sandisk_sansa_m240\software");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_2-samsung_galaxys3_android\software");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!Strange\StewardUsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_3-samsung_galaxys4_android\system");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_1-sandisk_sansa_m240\system");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_2-samsung_galaxys3_android\system");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\tdun2UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\tdun3UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\tdunUsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\win7\UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\usrclass Meyer\usrclass Meyer\Windows.old\Users\shaun 3\AppData\Local\Microsoft\Windows\UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_1-sandisk_sansa_m240\Users\Win7SP1\UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\others\UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!Strange\Donald\UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\usrclass Meyer\usrclass Meyer\Users\shaun 2\UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\Reed Richards\UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\usrclass Meyer\usrclass Meyer\Windows.old\Users\Shauns 2\AppData\Local\Microsoft\Windows\UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\MPT\Win7_3-samsung_galaxys4_android\Users\Win7SP1\UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\usrclass Meyer\usrclass Meyer\Windows.old\Users\shaun\AppData\Local\Microsoft\Windows\UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\Susan Storm Richards\UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\usrclass Meyer\usrclass Meyer\Users\shaun\AppData\Local\Microsoft\Windows\UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\10\UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\UsrClass-Vista.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\UsrClass-win7.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\UsrClass-Win8.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\10\Working\UsrClass (2).dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\10\UsrClass (3).dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\10\UsrClass (4).dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\10\Working\UsrClass (6).dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\10\Working\UsrClass (7).dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\10\UsrClass (8).dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\10\UsrClass (9).dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\10\UsrClass (10) FTP example.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\10\UsrClass (11).dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\UsrClass BEEF000E.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\UsrClass_1.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\UsrClass_2 beef0010.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\UsrClassaaa.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\UsrClassbeef10.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\UsrClassJimW2.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\UsrClassNIDES.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!ext block mismatch\UsrClassRichWin8.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\UsrClassWillOSullivan.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\UsrClassWin10.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\vib2UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\vib3UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\weg 0x32 errors UsrClass 2.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!ext block mismatch\weg UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\ShellBagsExplorer\test data\!working\Weg2_UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\Registry2\Registry\ExampleApp\SOFTWARE");
            testFiles.Add(@"C:\ProjectWorkingFolder\Registry2\Registry\ExampleApp\COMPONENTS");
            testFiles.Add(@"C:\ProjectWorkingFolder\Registry2\Registry\ExampleApp\SYSTEM");
            testFiles.Add(@"C:\ProjectWorkingFolder\Registry2\Registry\ExampleApp\NTUSER.DAT");
            testFiles.Add(@"C:\ProjectWorkingFolder\Registry2\Registry\ExampleApp\UsrClass.dat");
            testFiles.Add(@"C:\ProjectWorkingFolder\Registry2\Registry\ExampleApp\DRIVERS");
            testFiles.Add(@"C:\ProjectWorkingFolder\Registry2\Registry\ExampleApp\DEFAULT");
            testFiles.Add(@"C:\ProjectWorkingFolder\Registry2\Registry\ExampleApp\SAM");
            testFiles.Add(@"C:\ProjectWorkingFolder\Registry2\Registry\ExampleApp\SECURITY");

            foreach (var testFile in testFiles)
            {
                Console.WriteLine("Processing '{0}'", testFile);
                using (var fName1Test = new Registry.Registry(testFile, false))
                {
                    fName1Test.ParseHive();

                    Console.WriteLine("Finished processing '{0}'", testFile);
                }
                
                

            }


            //This is a testing harness for now. once parser is complete it will do a lot more

            // a few tests to make sure the hive isnt damaged at a basic level
            //var fName1Test = new Registry.Registry(fName1, false);
            //var meta1 =   fName1Test.Verify();
            //fName1Test.ParseHive();
         //

        }
    }
}
