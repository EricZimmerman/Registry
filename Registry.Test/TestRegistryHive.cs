using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NFluent;
using NLog;
using NUnit.Framework;
using Registry.Other;

namespace Registry.Test
{
    [TestFixture]
    internal class TestRegistryHive
    {
        [OneTimeSetUp]
        public void PreTestSetup()
        {
            LogManager.Configuration = null;
        }

        [Test]
        public void CheckHardAndSoftParsingErrors()
        {
            var sam = new RegistryHive(@"..\..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            Check.That(sam.SoftParsingErrors).IsEqualTo(0);
            Check.That(sam.HardParsingErrors).IsEqualTo(0);
        }

        
        [Test]
        public void LockedFileTest()
        {
            var f = @"C:\Windows\appcompat\Programs\Amcache.hve";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            r.ParseHive();

            var ts = "2014-12-08 13:39:33 +00:00";

            var td = DateTimeOffset.Parse(ts);

            var t = r.GetDeletedKey(@"Software\Microsoft\VisualStudio\12.0_Config\Debugger", td.ToString());

            Check.That(t).IsNotNull();
            Check.That(t.NkRecord.IsDeleted).IsTrue();
        }

        [Test]
        public void DeletedFindTest()
        {
            var f = @"D:\Sync\RegistryHives\NTUSER.DAT";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            r.ParseHive();

            var ts = "2014-12-08 13:39:33 +00:00";

            var td = DateTimeOffset.Parse(ts);

            var t = r.GetDeletedKey(@"Software\Microsoft\VisualStudio\12.0_Config\Debugger", td.ToString());

            Check.That(t).IsNotNull();
            Check.That(t.NkRecord.IsDeleted).IsTrue();
        }

        [Test]
        public void ExpandoTest00()
        {
            var f = @"D:\SynologyDrive\Registry\system_registry_hive";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();

            var oneKey = r.ExpandKeyPath("ControlSet001\\Services");

            Check.That(oneKey.Count).IsEqualTo(1);

          
        }

      //  private const string wildCardChar = "¿";
        private const string wildCardChar = "*";

        [Test]
        public void ExpandoTestOneOff()
        {

            var f = @"D:\SynologyDrive\Registry\ExpandoTestHives\AdminUsrClass.dat";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();

            var keys = r.ExpandKeyPath($"Local Settings\\Software\\Microsoft\\Windows\\Shell\\BagMRU\\{wildCardChar}\\0\\0");

            Check.That(keys.Count).IsEqualTo(3);
            Check.That(keys.First()).IsEqualTo("S-1-5-21-2036804247-3058324640-2116585241-500_Classes\\Local Settings\\Software\\Microsoft\\Windows\\Shell\\BagMRU\\0\\0\\0".ToLowerInvariant());
           Check.That(keys.Last()).IsEqualTo(@"S-1-5-21-2036804247-3058324640-2116585241-500_Classes\Local Settings\Software\Microsoft\Windows\Shell\BagMRU\3\0\0".ToLowerInvariant());

        }

        [Test]
        public void ExpandoTestOneOff2()
        {
            //Local Settings\Software\Microsoft\Windows\Shell\BagMRU\2\0\0
          var       f = @"D:\SynologyDrive\Registry\ExpandoTestHives\SOFTWARE_win10";
           var      r = new RegistryHive(f);
                 r.RecoverDeleted = true;
            
                 r.ParseHive();

         var      keys = r.ExpandKeyPath($"Classes\\{wildCardChar}\\OpenWithProgIds");

                   Check.That(keys.Count).IsEqualTo(95);
            Check.That(keys.First()).IsEqualTo("ROOT\\Classes\\.3g2\\OpenWithProgIds".ToLowerInvariant());
            Check.That(keys.Last()).IsEqualTo("ROOT\\Classes\\.zip\\OpenWithProgIds".ToLowerInvariant());
        }

        [Test]
        public void ExpandoTestOneOff3()
        {
            //Local Settings\Software\Microsoft\Windows\Shell\BagMRU\2\0\0
            var       f = @"D:\SynologyDrive\Registry\ExpandoTestHives\SOFTWARE_win10";
            var      r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();

            var      keys = r.ExpandKeyPath($"Classes\\{wildCardChar}\\ShellEx");

            Check.That(keys.Count).IsEqualTo(187);
            Check.That(keys.First()).IsEqualTo("root\\classes\\*\\shellex".ToLowerInvariant());
            Check.That(keys.Last()).IsEqualTo("root\\classes\\wshfile\\shellex".ToLowerInvariant());
        }

        [Test]
        public void ExpandoTestOneOff4()
        {

            var f = @"D:\SynologyDrive\Registry\ExpandoTestHives\AdminUsrClass.dat";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();

            var keys = r.ExpandKeyPath($"Local Settings\\Software\\Microsoft\\Windows\\Shell\\Bags\\{wildCardChar}\\Shell\\" + "{5C4F28B5-F869-4E84-8E60-F11DB97C5CC7}");

            Check.That(keys.Count).IsEqualTo(15);

        }

        [Test]
        public void ExpandoTestOneOff5()
        {

            var f = @"D:\SynologyDrive\Registry\ExpandoTestHives\NTUSER_Loveall.DAT";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();

            var keys = r.ExpandKeyPath($"AppEvents\\EventLabels\\System{wildCardChar}");

            Check.That(keys.Count).IsEqualTo(7);
        }

        [Test]
        public void ExpandoTestOneOff6()
        {

            var f = @"D:\SynologyDrive\Registry\ExpandoTestHives\NTUSER_Loveall.DAT";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();

            var keys = r.ExpandKeyPath($"AppEvents\\EventLabels\\{wildCardChar}up");

            Check.That(keys.Count).IsEqualTo(2);
        }

        [Test]
        public void ExpandoTestOneOff7()
        {

            var f = @"D:\SynologyDrive\Registry\ExpandoTestHives\NTUSER_Loveall.DAT";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();

            var keys = r.ExpandKeyPath($"AppEvents\\EventLabels\\System{wildCardChar}tion");

            Check.That(keys.Count).IsEqualTo(3);
        }

        [Test]
        public void ExpandoTestOneOff8()
        {

            var f = @"D:\SynologyDrive\Registry\ExpandoTestHives\UsrClassXWFPath.dat";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();

            var keys = r.ExpandKeyPath($"{wildCardChar}\\shell\\{wildCardChar}\\command");

            Check.That(keys.Count).IsEqualTo(224);

            Check.That(keys.First()).IsEqualTo("s-1-5-21-238543598-4054144643-4261915534-1114_classes\\*\\shell\\editpad\\command");
            Check.That(keys.Last()).IsEqualTo("s-1-5-21-238543598-4054144643-4261915534-1114_classes\\zoomrecording\\shell\\open\\command");
        }

        [Test]
        public void ExpandoTestOneOff9()
        {
            var f = @"D:\SynologyDrive\Registry\ExpandoTestHives\UsrClassXWFPath.dat";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();

            var keys = r.ExpandKeyPath($"Extensions\\ContractId\\Windows.Launch\\PackageId\\{wildCardChar}\\ActivatableClassId");

            Check.That(keys.Count).IsEqualTo(73);
        }

        [Test]
        public void ExpandoTestRedux01()
        {

            var f = @"D:\SynologyDrive\Registry\ExpandoTestHives\UsrClassXWFPath.dat";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();

            var keys = r.ExpandKeyPath($"{wildCardChar}\\shell\\{wildCardChar}\\command");

            Check.That(keys.Count).IsEqualTo(224);

            Check.That(keys.First()).IsEqualTo("s-1-5-21-238543598-4054144643-4261915534-1114_classes\\*\\shell\\editpad\\command".ToLowerInvariant());
            Check.That(keys.Last()).IsEqualTo("s-1-5-21-238543598-4054144643-4261915534-1114_classes\\zoomrecording\\shell\\open\\command".ToLowerInvariant());
        }


        [Test]
        public void ExpandoTest01()
        {
            var f = @"D:\SynologyDrive\Registry\ExpandoTestHives\system_registry_hive";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();

            var keys = r.ExpandKeyPath($"ControlSet00{wildCardChar}\\Services");

            Check.That(keys.Count).IsEqualTo(2);
            Check.That(keys.First()).IsEqualTo("$$$PROTO.HIV\\ControlSet001\\Services".ToLowerInvariant());
            Check.That(keys.Last()).IsEqualTo("$$$PROTO.HIV\\ControlSet002\\Services".ToLowerInvariant());
        }

         [Test]
        public void ExpandoTest02()
        {
            var f = @"D:\SynologyDrive\Registry\ExpandoTestHives\system_registry_hive";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();

          
            var otherKeys = r.ExpandKeyPath($"ControlSet002\\Services\\aic{wildCardChar}");
            Check.That(otherKeys.Count).IsEqualTo(2);
            Check.That(otherKeys.First()).IsEqualTo("$$$PROTO.HIV\\ControlSet002\\Services\\aic78u2".ToLowerInvariant());
            Check.That(otherKeys.Last()).IsEqualTo("$$$PROTO.HIV\\ControlSet002\\Services\\aic78xx".ToLowerInvariant());

  
        }

          [Test]
        public void ExpandoTest03()
        {
            var f = @"D:\SynologyDrive\Registry\ExpandoTestHives\system_registry_hive";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();

            var evenMoreKeys = r.ExpandKeyPath($"ControlSet001\\Control\\IDConfigDB\\{wildCardChar}\\0001");
            Check.That(evenMoreKeys.Count).IsEqualTo(2);
            Check.That(evenMoreKeys.First()).IsEqualTo("$$$PROTO.HIV\\ControlSet001\\Control\\IDConfigDB\\Alias\\0001".ToLowerInvariant());
            Check.That(evenMoreKeys.Last()).IsEqualTo("$$$PROTO.HIV\\ControlSet001\\Control\\IDConfigDB\\Hardware Profiles\\0001".ToLowerInvariant());

        }

        [Test]
        public void ExpandoTest03a()
        {
            var f = @"D:\SynologyDrive\Registry\ExpandoTestHives\system_registry_hive";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();

           
          var  evenMoreKeys = r.ExpandKeyPath($"ControlSet001\\Control\\IDConfigDB\\{wildCardChar}\\0{wildCardChar}");
            Check.That(evenMoreKeys.Count).IsEqualTo(3);
            Check.That(evenMoreKeys.First()).IsEqualTo("$$$PROTO.HIV\\ControlSet001\\Control\\IDConfigDB\\Alias\\0001".ToLowerInvariant());
            Check.That(evenMoreKeys.Last()).IsEqualTo("$$$PROTO.HIV\\ControlSet001\\Control\\IDConfigDB\\Hardware Profiles\\0001".ToLowerInvariant());
        }

        
        [Test]
        public void ExpandoTest04()
        {
            var f = @"D:\SynologyDrive\Registry\ExpandoTestHives\system_registry_hive";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();

            var otherKeys2 = r.ExpandKeyPath($"ControlSet002\\Services\\Avg{wildCardChar}x86");
            Check.That(otherKeys2.Count).IsEqualTo(3);
          //  Check.That(otherKeys2[0]).IsEqualTo(@"ControlSet002\Services\Avgldx86");
        //    Check.That(otherKeys2[1]).IsEqualTo(@"ControlSet002\Services\Avgmfx86");
        //    Check.That(otherKeys2[2]).IsEqualTo(@"ControlSet002\Services\Avgrkx86");

        }

        [Test]
        public void ExpandoTest05()
        {
            var f = @"D:\SynologyDrive\Registry\ExpandoTestHives\system_registry_hive";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();
         
            var shouldNotExist = r.ExpandKeyPath($"ControlSet002\\Services\\Avg{wildCardChar}x86\\DoesNotExist");
            Check.That(shouldNotExist.Count).IsEqualTo(0);
        }

        [Test]
        public void ExpandoTest06()
        {
            var f = @"D:\SynologyDrive\Registry\ExpandoTestHives\system_registry_hive";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();
         
            var endCheck = r.ExpandKeyPath($"Setup\\AllowStart\\{wildCardChar}ss");
            Check.That(endCheck.Count).IsEqualTo(2);
            Check.That(endCheck.First()).IsEqualTo("$$$PROTO.HIV\\Setup\\AllowStart\\Rpcss".ToLowerInvariant());
            Check.That(endCheck.Last()).IsEqualTo("$$$PROTO.HIV\\Setup\\AllowStart\\SamSs".ToLowerInvariant());
        }

        [Test]
        public void ExpandoTest07()
        {
            var f = @"D:\SynologyDrive\Registry\ExpandoTestHives\system_registry_hive";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();

            var endCheck2 = r.ExpandKeyPath($"Setup\\AllowStart\\{wildCardChar}mss");
            Check.That(endCheck2.Count).IsEqualTo(1);
            Check.That(endCheck2.First()).IsEqualTo("$$$PROTO.HIV\\Setup\\AllowStart\\SamSs".ToLowerInvariant());
        }

        [Test]
        public void ExpandoTest08()
        {
            var f = @"D:\SynologyDrive\Registry\ExpandoTestHives\system_registry_hive";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            
            r.ParseHive();

            var endCheck3 = r.ExpandKeyPath($"ControlSet002\\Services\\{wildCardChar}\\Parameters".ToLowerInvariant());
     
            Check.That(endCheck3.Count).IsEqualTo(127);
        }


        [Test]
        public void ExpandoTest09()
        {
            
            var f2 = @"D:\SynologyDrive\Registry\ExpandoTestHives\ntuser.dat";
            var r2 = new RegistryHive(f2);
            r2.RecoverDeleted = true;
            
            r2.ParseHive();

            var keys2 = r2.ExpandKeyPath($"Software\\Microsoft\\Office\\16.0\\Excel\\User MRU\\{wildCardChar}\\File MRU".ToLowerInvariant());

            Check.That(keys2.Count).IsEqualTo(1);
            Check.That(keys2.First()).IsEqualTo(	"ROOT\\Software\\Microsoft\\Office\\16.0\\Excel\\User MRU\\AD_B8387EDCD97012482021633037177683B71660DC7C410BE924536ACDF94CD5B4\\File MRU".ToLowerInvariant());

        }


        [Test]
        public void ExpandoTest10()
        {
            
            var f2 = @"D:\SynologyDrive\Registry\ExpandoTestHives\ntuser.dat";
            var r2 = new RegistryHive(f2);
            r2.RecoverDeleted = true;
            
            r2.ParseHive();

            var keys2 = r2.ExpandKeyPath($"SOFTWARE\\Microsoft\\Office\\{wildCardChar}\\{wildCardChar}\\User MRU");

            Check.That(keys2.Count).IsEqualTo(3);
            Check.That(keys2.First()).IsEqualTo(@"ROOT\Software\Microsoft\Office\16.0\Excel\User MRU".ToLowerInvariant());

        }

        [Test]
        public void ExpandoTest11()
        {
            
            var f2 = @"D:\SynologyDrive\Registry\ExpandoTestHives\SOFTWARE_win10";
            var r2 = new RegistryHive(f2);
            r2.RecoverDeleted = true;
            
            r2.ParseHive();

            var keys2 = r2.ExpandKeyPath($"WOW6432Node\\ODBC\\ODBCINST.INI\\Microsoft dBase Driver (*.dbf)");

            Check.That(keys2.Count).IsEqualTo(1);
            Check.That(keys2.First()).IsEqualTo(@"ROOT\WOW6432Node\ODBC\ODBCINST.INI\Microsoft dBase Driver (*.dbf)".ToLowerInvariant());

        }

        [Test]
        public void ExpandoTest12()
        {
            
            var f2 = @"D:\SynologyDrive\Registry\ExpandoTestHives\ntuser.dat";
            var r2 = new RegistryHive(f2);
            r2.RecoverDeleted = true;
            
            r2.ParseHive();

            var keys2 = r2.ExpandKeyPath($"SOFTWARE\\Microsoft\\Office\\{wildCardChar}\\{wildCardChar}\\User MRU\\{wildCardChar}");

            Check.That(keys2.Count).IsEqualTo(3);
            Check.That(keys2.First()).IsEqualTo(@"ROOT\Software\Microsoft\Office\16.0\Excel\User MRU\AD_B8387EDCD97012482021633037177683B71660DC7C410BE924536ACDF94CD5B4".ToLowerInvariant());
        }

        
       
        [Test]
        public void ExpandoTest13()
        {
            
            var f2 = @"D:\SynologyDrive\Registry\ExpandoTestHives\ntuser.dat";
            var r2 = new RegistryHive(f2);
            r2.RecoverDeleted = true;
            
            r2.ParseHive();

            var keys2 = r2.ExpandKeyPath($"Software\\Microsoft\\Office\\16.0\\Excel\\User MRU\\AD_B8387EDCD97012482021633037177683B71660DC7C410BE924536ACDF94CD5B4\\{wildCardChar}");

            Check.That(keys2.Count).IsEqualTo(2);
            Check.That(keys2.First()).IsEqualTo(@"ROOT\Software\Microsoft\Office\16.0\Excel\User MRU\AD_B8387EDCD97012482021633037177683B71660DC7C410BE924536ACDF94CD5B4\File MRU".ToLowerInvariant());
            Check.That(keys2.Last()).IsEqualTo(@"ROOT\Software\Microsoft\Office\16.0\Excel\User MRU\AD_B8387EDCD97012482021633037177683B71660DC7C410BE924536ACDF94CD5B4\Place MRU".ToLowerInvariant());
        }

        [Test]
        public void ExpandoTest14()
        {
            
            var f2 = @"D:\SynologyDrive\Registry\ExpandoTestHives\ntuser.dat";
            var r2 = new RegistryHive(f2);
            r2.RecoverDeleted = true;
            
            r2.ParseHive();

            //no wildcards should 
            var keys2 = r2.ExpandKeyPath($"Software\\Microsoft\\Office\\16.0\\Excel\\User MRU\\AD_B8387EDCD97012482021633037177683B71660DC7C410BE924536ACDF94CD5B4\\");

            Check.That(keys2.Count).IsEqualTo(1);
        }
        
        [Test]
        public void ExpandoTest15()
        {
            
            var f2 = @"D:\SynologyDrive\Registry\ExpandoTestHives\SYSTEM_loneWolf";
            var r2 = new RegistryHive(f2);
            r2.RecoverDeleted = true;
            
            r2.ParseHive();

            var keys2 = r2.ExpandKeyPath("ControlSet001\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}" + $"\\0001\\Ndi\\Params\\{wildCardChar}FlowControl");

            Check.That(keys2.Count).IsEqualTo(1);
            Check.That(keys2.First()).IsEqualTo(@"ROOT\ControlSet001\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}\0001\Ndi\Params\*FlowControl".ToLowerInvariant());
        }

        [Test]
        public void ExpandoTest16()
        {
            
            var f2 = @"D:\SynologyDrive\Registry\ExpandoTestHives\SYSTEM_loneWolf";
            var r2 = new RegistryHive(f2);
            r2.RecoverDeleted = true;
            
            r2.ParseHive();

            //TODO try to get this to work, if you do, remove WithoutWildCard
            var keys2 = r2.ExpandKeyPath("ControlSet001\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}"+ $"\\0001\\Ndi\\Params\\*FlowControl\\{wildCardChar}");

            Check.That(keys2.Count).IsEqualTo(1);
            Check.That(keys2.Last()).IsEqualTo(@"ROOT\ControlSet001\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}\0001\Ndi\Params\*FlowControl\Enum".ToLowerInvariant());
        }



        [Test]
        public void ReallocTest()
        {
            var f = @"D:\SynologyDrive\Registry\ReallocValueDataHive";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            r.ParseHive();

            var ts = "2017-09-10 21:47:31 +00:00";

            var td = DateTimeOffset.Parse(ts);

            var t = r.GetDeletedKey(@"2", td.ToString());

            Check.That(t).IsNotNull();
            Check.That(t.NkRecord.IsDeleted).IsTrue();
            

            Check.That(t.Values[0].VkRecord.DataRecordAllocated).IsEqualTo(true);
         //   Check.That(t.Values[0].ValueData).IsNotEqualTo("1111");

        }

        [Test]
        public void DeletedFindTestValue()
        {
            var f = @"D:\!downloads\yarp-master\hives_for_tests\DeletedDataHive";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            r.ParseHive();

            var k = r.GetKey("123");

            Check.That(k.Values[0].VkRecord.IsFree).IsFalse();

            Check.That(k.Values.Count).IsEqualTo(2);

            Check.That(k.Values[1].VkRecord.IsFree).IsTrue();

            foreach (var keyValue in k.Values)
            {
                Debug.WriteLine(keyValue);
            }
        }

        [Test]
        public void OneOffParse()
        {
            var f = @"D:\SynologyDrive\Registry\NTUSER_MarkElliot_RecentApps.DAT";
            var r = new RegistryHive(f);
            r.RecoverDeleted = true;
            r.ParseHive();

        }



        [Test]
        public void HBinSizeShouldMatchReadSize()
        {
            var sam = new RegistryHive(@"..\..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            Check.That(sam.Header.Length).IsEqualTo(sam.HBinRecordTotalSize);
        }

        [Test]
        public void HBinSizeShouldNotMatchReadSize()
        {
            var r = new RegistryHive(@"..\..\..\Hives\SAM_DUPENAME");
            //if you don't call parse, it wont match

            Check.That(r.Header.Length).IsNotEqualTo(r.HBinRecordTotalSize);
        }

        [Test]
        public void OneOff()
        {
            var r = new RegistryHive(@"C:\Users\eric\Desktop\SAM");
            r.RecoverDeleted = true;
            r.ParseHive();
        }


        [Test]
        public void RecoverDeletedShouldBeTrue()
        {
            var sam = new RegistryHive(@"..\..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            sam.RecoverDeleted = true;

            Check.That(sam.RecoverDeleted).IsEqualTo(true);
            sam.RecoverDeleted = false;
        }


        [Test]
        public void ShouldExportFileAllRecords()
        {
            var usrclassDeleted = new RegistryHive(@"..\..\..\Hives\UsrClassDeletedBags.dat");
            usrclassDeleted.RecoverDeleted = true;
            usrclassDeleted.FlushRecordListsAfterParse = false;
            usrclassDeleted.ParseHive();

            usrclassDeleted.ExportDataToCommonFormat(@"UsrclassDeletedNoDeletedStuff.txt", false);

            Check.That(usrclassDeleted.Header.Length).IsEqualTo(usrclassDeleted.HBinRecordTotalSize);
        }


        [Test]
        public void ShouldExportFileDeletedRecords()
        {
            var usrclassDeleted = new RegistryHive(@"..\..\..\Hives\UsrClassDeletedBags.dat");
            usrclassDeleted.RecoverDeleted = true;
            usrclassDeleted.FlushRecordListsAfterParse = false;
            usrclassDeleted.ParseHive();

            usrclassDeleted.ExportDataToCommonFormat(@"UsrclassDeletedDeletedStuff.txt", true);

            Check.That(usrclassDeleted.Header.Length).IsEqualTo(usrclassDeleted.HBinRecordTotalSize);
        }

        [Test]
        public void ShouldExportHiveWithRootValues()
        {
            var samRootValue = new RegistryHive(@"..\..\..\Hives\SAM_RootValue");
            samRootValue.FlushRecordListsAfterParse = false;
            samRootValue.ParseHive();


            samRootValue.ExportDataToCommonFormat(@"SamRootValueNoDeletedStuff.txt", false);
        }

        [Test]
        public void ShouldExportValuesToFile()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var keys = usrClass1.FindByValueSize(100000).ToList();

            foreach (var valueBySizeInfo in keys)
            {
                File.WriteAllBytes($"{valueBySizeInfo.Value.ValueName}.bin", valueBySizeInfo.Value.ValueDataRaw);

                Check.That(File.Exists($"{valueBySizeInfo.Value.ValueName}.bin")).IsTrue();
            }
        }

        [Test]
        public void ShouldFind100HitsForUrlInKeyAndValueName()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();
            var keyHits = usrClass1.FindInKeyName("URL").ToList();

            Check.That(keyHits.Count).IsEqualTo(21);

            var valHits = usrClass1.FindInValueName("URL").ToList();

            Check.That(valHits.Count).IsEqualTo(79);
        }

        [Test]
        public void ShouldFind1248AfterTimeStamp()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();


            var dt = new DateTimeOffset(2014, 11, 13, 15, 51, 17, TimeSpan.FromSeconds(0));
            var hits = usrClass1.FindByLastWriteTime(dt, null).ToList();

            Check.That(hits.Count).IsEqualTo(14);
        }

        [Test]
        public void ShouldFind1544EforeTimeStamp()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var dt = new DateTimeOffset(2014, 5, 20, 14, 19, 40, TimeSpan.FromSeconds(0));
            var hits = usrClass1.FindByLastWriteTime(null, dt).ToList();

            Check.That(hits.Count).IsEqualTo(21);
        }

        [Test]
        public void ShouldFind32HitsForFoodInKeyName()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();


            var hits = usrClass1.FindInKeyName("food").ToList();

            Check.That(hits.Count).IsEqualTo(32);
        }

        [Test]
        public void ShouldFind4HitsFor320033003200InValueDataSlack()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var hits = usrClass1.FindInValueDataSlack("32-00-33-00-32-00", false, true).ToList();

            Check.That(hits.Count).IsEqualTo(6);
        }

        [Test]
        public void ShouldFind4HitsForBinaryDataInValueData()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var hits = usrClass1.FindInValueData("43-74-53-83-24-55-30").ToList();

            Check.That(hits.Count).IsEqualTo(6);

            hits = usrClass1.FindInValueData("DeB").ToList();

            Check.That(hits.Count).IsEqualTo(28);
        }

        [Test]
        public void ShouldFind4HitsForBinaryDataInValueDataWithRegEx()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var hits = usrClass1.FindInValueData("04-00-EF-BE", true).ToList();

            Check.That(hits.Count).IsEqualTo(56);

            hits = usrClass1.FindInValueData("47-4F-4F-4E", true).ToList();

            Check.That(hits.Count).IsEqualTo(4);

            hits = usrClass1.FindInValueData("44-65-62", true).ToList(); //finds deb

            Check.That(hits.Count).IsEqualTo(2);

            hits = usrClass1.FindInValueData("44-65-73", true).ToList(); //finds des

            Check.That(hits.Count).IsEqualTo(1);

            hits = usrClass1.FindInValueData("44-65-(62|73)", true).ToList(); //finds deb or des

            Check.That(hits.Count).IsEqualTo(3);
        }

        [Test]
        public void ShouldFind4HitsForBingXInKeyNamesWithRegEx()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();


            var hits = usrClass1.FindInKeyName("Microsoft.Bing[FHW]", true).ToList();

            Check.That(hits.Count).IsEqualTo(44);

            hits = usrClass1.FindInKeyName("Microsoft.Bing[FHW]o", true).ToList();

            Check.That(hits.Count).IsEqualTo(11);
        }

        [Test]
        public void ShouldFind4HitsForBingXInValueDataWithRegEx()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var hits = usrClass1.FindInValueData("URL:bing[mhs]", true).ToList();

            Check.That(hits.Count).IsEqualTo(3);

            hits = usrClass1.FindInValueData("URL:bing[mhts]", true).ToList();

            Check.That(hits.Count).IsEqualTo(4);
        }

        [Test]
        public void ShouldFind4HitsForPostboxUrlInValueData()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var hits = usrClass1.FindInValueData("Postbox URL").ToList();

            Check.That(hits.Count).IsEqualTo(4);
        }

        [Test]
        public void ShouldFindAKeyWithClassName()
        {
            var systemOnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\SYSTEM");
            var key =
                systemOnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\ControlSet001\Control\Lsa\Data");

            Check.That(key.ClassName).IsNotEmpty();
        }

        [Test]
        public void ShouldFindAKeyWithoutRootKeyName()
        {
            var sam = new RegistryHive(@"..\..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            var key = sam.GetKey(@"SAM\Domains");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void ShouldFindBase64()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var hits = usrClass1.FindBase64(20).ToList();

            Check.That(hits.Count).IsEqualTo(137);
        }

        [Test]
        public void ShouldFindFiveValuesForSize4096()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var keys = usrClass1.FindByValueSize(4096).ToList();

            Check.That(keys.Count).IsEqualTo(5);
        }

        [Test]
        public void ShouldFindHitsValueNamesWithRegEx()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var hits = usrClass1.FindInValueName("(App|Display)Name", true).ToList();

            Check.That(hits.Count).IsEqualTo(326);

            hits = usrClass1.FindInValueName("Capability(Co|Si)", true).ToList();

            Check.That(hits.Count).IsEqualTo(66);
        }

        [Test]
        public void ShouldFindKeyWithMixedCaseName()
        {
            var usrClassFtp = new RegistryHiveOnDemand(@"..\..\..\Hives\UsrClass FTP.dat");

            var key =
                usrClassFtp.GetKey(
                    @"S-1-5-21-2417227394-2575385136-2411922467-1105_CLAsses\ActivAtableClasses\CLsID");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void ShouldFindKeyWithMixedCaseNameWithoutRootName()
        {
            var usrClassFtp = new RegistryHiveOnDemand(@"..\..\..\Hives\UsrClass FTP.dat");

            var key = usrClassFtp.GetKey(@"ActivAtableClasses\CLsID");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void ShouldFindNoHitsForZimmermanInKeyName()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();


            var hits = usrClass1.FindInKeyName("Zimmerman").ToList();

            Check.That(hits.Count).IsEqualTo(0);
        }

        [Test]
        public void ShouldFindThreeHitsForMuiCacheInKeyName()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var hits = usrClass1.FindInKeyName("MuiCache").ToList();

            Check.That(hits.Count).IsEqualTo(3);
        }

        [Test]
        public void ShouldFindTwoBetweenTimeStamp()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var start = new DateTimeOffset(2014, 5, 20, 19, 00, 00, TimeSpan.FromSeconds(0));
            var end = new DateTimeOffset(2014, 5, 20, 23, 59, 59, TimeSpan.FromSeconds(0));
            var hits = usrClass1.FindByLastWriteTime(start, end).ToList();

            Check.That(hits.Count).IsEqualTo(2);
        }

        [Test]
        public void ShouldFindTwoValuesForSize100000()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var keys = usrClass1.FindByValueSize(100000).ToList();

            Check.That(keys.Count).IsEqualTo(2);
        }

        [Test]
        public void ShouldHaveGoodRegMultiSz()
        {
            var usrclassDeleted = new RegistryHive(@"..\..\..\Hives\UsrClassDeletedBags.dat");
            usrclassDeleted.RecoverDeleted = true;
            usrclassDeleted.FlushRecordListsAfterParse = false;
            usrclassDeleted.ParseHive();

            //S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\MuiCache\6\52C64B7E
            var key =
                usrclassDeleted.GetKey(
                    @"S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\MuiCache\6\52C64B7E");

            var val = key.Values.Single(t => t.ValueName == "LanguageList");

            Check.That(val).IsNotNull();

            Check.That(val.ValueName).IsEqualTo("LanguageList");
            Check.That(val.ValueData).IsEqualTo("en-US en");
        }

        [Test]
        public void ShouldHaveHardAndSoftParsingValuesOfZero()
        {
            var sam = new RegistryHive(@"..\..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            Check.That(sam.HardParsingErrors).IsEqualTo(0);
            Check.That(sam.SoftParsingErrors).IsEqualTo(0);
        }

        [Test]
        public void ShouldHaveHeaderLengthEqualToReadDataSize()
        {
            var usrclassDeleted = new RegistryHive(@"..\..\..\Hives\UsrClassDeletedBags.dat");
            usrclassDeleted.RecoverDeleted = true;
            usrclassDeleted.FlushRecordListsAfterParse = false;
            usrclassDeleted.ParseHive();

            Check.That(usrclassDeleted.Header.Length).IsEqualTo(usrclassDeleted.HBinRecordTotalSize);
        }

        [Test]
        public void ShouldReturnKeyBasedOnRelativePath()
        {
            var sam = new RegistryHive(@"..\..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();
            var key =
                sam.GetKey(0x418);

            Check.That(key).IsNotNull();
        }

        [Test]
        public void ShouldReturnNullWhenKeyPathNotFound()
        {
            var sam = new RegistryHive(@"..\..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            var key =
                sam.GetKey(@"SAM\Domains\DoesNotExist");

            Check.That(key).IsNull();
        }

        [Test]
        public void ShouldReturnNullWhenRelativeOffsetNotFound()
        {
            var sam = new RegistryHive(@"..\..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            var key =
                sam.GetKey(0x999418);

            Check.That(key).IsNull();
        }

        [Test]
        public void ShouldTakeByteArrayInConstructor()
        {
            var sam = new RegistryHive(@"..\..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            var r = new RegistryHive(sam.FileBytes,@"..\..\..\Hives\SAM");

            Check.That(r.Header).IsNotNull();
            Check.That(r.HivePath).IsEqualTo("None");
            Check.That(r.HiveType).IsEqualTo(HiveTypeEnum.Sam);
        }

//        [Test]
//        public void ShouldThrowExceptionNoRootKey()
//        {
//            Check.ThatCode(() =>
//            {
//                var r = new RegistryHive(@"..\..\..\Hives\SECURITYNoRoot");
//                r.ParseHive();
//            }).Throws<KeyNotFoundException>();
//        }

        [Test]
        public void ShouldThrowExceptionWhenCallingParseHiveTwice()
        {
            Check.ThatCode(() =>
                {
                    var r = new RegistryHive(@"..\..\..\Hives\SAMBadHBinHeader");
                    r.ParseHive();
                    r.ParseHive();
                })
                .Throws<Exception>();
        }

        [Test]
        public void ShouldThrowExceptionWithBadHbinHeader()
        {
            Check.ThatCode(() =>
                {
                    var r = new RegistryHive(@"..\..\..\Hives\SAMBadHBinHeader");
                    r.ParseHive();
                })
                .Throws<Exception>();
        }

        [Test]
        public void TestsListRecordsContinued3()
        {
            var usrClassFtp = new RegistryHiveOnDemand(@"..\..\..\Hives\UsrClass FTP.dat");

            var key =
                usrClassFtp.GetKey(
                    @"S-1-5-21-2417227394-2575385136-2411922467-1105_Classes\ActivatableClasses\CLSID");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void VerifyHiveTestShouldPass()
        {
            var sam = new RegistryHive(@"..\..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            var m = sam.Verify();

            Check.That(m.HasValidHeader).IsTrue();
        }
    }
}