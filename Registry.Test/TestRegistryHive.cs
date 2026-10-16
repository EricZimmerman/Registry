using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Serilog;

namespace Registry.Test;

[TestFixture]
internal class TestRegistryHive
{
    [OneTimeSetUp]
    public void PreTestSetup()
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console().MinimumLevel.Debug().CreateLogger();
    }

    [Test]
    public void CheckHardAndSoftParsingErrors()
    {
        var sam = new RegistryHive(TestHives.Path(@"SAM"));
        sam.FlushRecordListsAfterParse = false;
        sam.ParseHive();

        sam.SoftParsingErrors.Should().Be(0);
        sam.HardParsingErrors.Should().Be(0);
    }





    [Test]
    public void ExpandoTest00()
    {
        var f = TestHives.Corpus(@"system_registry_hive");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();

        var oneKey = r.ExpandKeyPath("ControlSet001\\Services");

        oneKey.Count.Should().Be(1);
    }

    //  private const string wildCardChar = "¿";
    private const string wildCardChar = "*";

    [Test]
    public void ExpandoTestOneOff()
    {
        var f = TestHives.Corpus(@"ExpandoTestHives\AdminUsrClass.dat");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();

        var keys = r.ExpandKeyPath(
            $"Local Settings\\Software\\Microsoft\\Windows\\Shell\\BagMRU\\{wildCardChar}\\0\\0");

        keys.Count.Should().Be(3);
        keys.First().Should().Be(
                "S-1-5-21-2036804247-3058324640-2116585241-500_Classes\\Local Settings\\Software\\Microsoft\\Windows\\Shell\\BagMRU\\0\\0\\0"
                    .ToLowerInvariant());
        keys.Last().Should().Be(
                @"S-1-5-21-2036804247-3058324640-2116585241-500_Classes\Local Settings\Software\Microsoft\Windows\Shell\BagMRU\3\0\0"
                    .ToLowerInvariant());
    }

    [Test]
    public void ExpandoTestOneOff2()
    {
        //Local Settings\Software\Microsoft\Windows\Shell\BagMRU\2\0\0
        var f = TestHives.Corpus(@"ExpandoTestHives\SOFTWARE_win10");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();

        var keys = r.ExpandKeyPath($"Classes\\{wildCardChar}\\OpenWithProgIds");

        keys.Count.Should().Be(95);
        keys.First().Should().Be("ROOT\\Classes\\.3g2\\OpenWithProgIds".ToLowerInvariant());
        keys.Last().Should().Be("ROOT\\Classes\\.zip\\OpenWithProgIds".ToLowerInvariant());
    }

    [Test]
    public void ExpandoTestOneOff3()
    {
        //Local Settings\Software\Microsoft\Windows\Shell\BagMRU\2\0\0
        var f = TestHives.Corpus(@"ExpandoTestHives\SOFTWARE_win10");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();

        var keys = r.ExpandKeyPath($"Classes\\{wildCardChar}\\ShellEx");

        keys.Count.Should().Be(187);
        keys.First().Should().Be("root\\classes\\*\\shellex".ToLowerInvariant());
        keys.Last().Should().Be("root\\classes\\wshfile\\shellex".ToLowerInvariant());
    }

    [Test]
    public void ExpandoTestOneOff4()
    {
        var f = TestHives.Corpus(@"ExpandoTestHives\AdminUsrClass.dat");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();

        var keys = r.ExpandKeyPath(
            $"Local Settings\\Software\\Microsoft\\Windows\\Shell\\Bags\\{wildCardChar}\\Shell\\" +
            "{5C4F28B5-F869-4E84-8E60-F11DB97C5CC7}");

        keys.Count.Should().Be(15);
    }

    [Test]
    public void ExpandoTestOneOff5()
    {
        var f = TestHives.Corpus(@"ExpandoTestHives\NTUSER_Loveall.DAT");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();

        var keys = r.ExpandKeyPath($"AppEvents\\EventLabels\\System{wildCardChar}");

        keys.Count.Should().Be(7);
    }

    [Test]
    public void ExpandoTestOneOff6()
    {
        var f = TestHives.Corpus(@"ExpandoTestHives\NTUSER_Loveall.DAT");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();

        var keys = r.ExpandKeyPath($"AppEvents\\EventLabels\\{wildCardChar}up");

        keys.Count.Should().Be(2);
    }

    [Test]
    public void ExpandoTestOneOff7()
    {
        var f = TestHives.Corpus(@"ExpandoTestHives\NTUSER_Loveall.DAT");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();

        var keys = r.ExpandKeyPath($"AppEvents\\EventLabels\\System{wildCardChar}tion");

        keys.Count.Should().Be(3);
    }

    [Test]
    public void ExpandoTestOneOff8()
    {
        var f = TestHives.Corpus(@"ExpandoTestHives\UsrClassXWFPath.dat");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();

        var keys = r.ExpandKeyPath($"{wildCardChar}\\shell\\{wildCardChar}\\command");

        keys.Count.Should().Be(224);

        keys.First().Should().Be("s-1-5-21-238543598-4054144643-4261915534-1114_classes\\*\\shell\\editpad\\command");
        keys.Last().Should().Be("s-1-5-21-238543598-4054144643-4261915534-1114_classes\\zoomrecording\\shell\\open\\command");
    }

    [Test]
    public void ExpandoTestOneOff9()
    {
        var f = TestHives.Corpus(@"ExpandoTestHives\UsrClassXWFPath.dat");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();

        var keys = r.ExpandKeyPath(
            $"Extensions\\ContractId\\Windows.Launch\\PackageId\\{wildCardChar}\\ActivatableClassId");

        keys.Count.Should().Be(73);
    }

    [Test]
    public void ExpandoTestRedux01()
    {
        var f = TestHives.Corpus(@"ExpandoTestHives\UsrClassXWFPath.dat");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();

        var keys = r.ExpandKeyPath($"{wildCardChar}\\shell\\{wildCardChar}\\command");

        keys.Count.Should().Be(224);

        keys.First().Should().Be("s-1-5-21-238543598-4054144643-4261915534-1114_classes\\*\\shell\\editpad\\command"
                .ToLowerInvariant());
        keys.Last().Should().Be("s-1-5-21-238543598-4054144643-4261915534-1114_classes\\zoomrecording\\shell\\open\\command"
                .ToLowerInvariant());
    }


    [Test]
    public void ExpandoTest01()
    {
        var f = TestHives.Corpus(@"ExpandoTestHives\system_registry_hive");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();

        var keys = r.ExpandKeyPath($"ControlSet00{wildCardChar}\\Services");

        keys.Count.Should().Be(2);
        keys.First().Should().Be("$$$PROTO.HIV\\ControlSet001\\Services".ToLowerInvariant());
        keys.Last().Should().Be("$$$PROTO.HIV\\ControlSet002\\Services".ToLowerInvariant());
    }

    [Test]
    public void ExpandoTest02()
    {
        var f = TestHives.Corpus(@"ExpandoTestHives\system_registry_hive");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();


        var otherKeys = r.ExpandKeyPath($"ControlSet002\\Services\\aic{wildCardChar}");
        otherKeys.Count.Should().Be(2);
        otherKeys.First().Should().Be("$$$PROTO.HIV\\ControlSet002\\Services\\aic78u2".ToLowerInvariant());
        otherKeys.Last().Should().Be("$$$PROTO.HIV\\ControlSet002\\Services\\aic78xx".ToLowerInvariant());
    }

    [Test]
    public void ExpandoTest03()
    {
        var f = TestHives.Corpus(@"ExpandoTestHives\system_registry_hive");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();

        var evenMoreKeys = r.ExpandKeyPath($"ControlSet001\\Control\\IDConfigDB\\{wildCardChar}\\0001");
        evenMoreKeys.Count.Should().Be(2);
        evenMoreKeys.First().Should().Be("$$$PROTO.HIV\\ControlSet001\\Control\\IDConfigDB\\Alias\\0001".ToLowerInvariant());
        evenMoreKeys.Last().Should().Be("$$$PROTO.HIV\\ControlSet001\\Control\\IDConfigDB\\Hardware Profiles\\0001".ToLowerInvariant());
    }

    [Test]
    public void ExpandoTest03a()
    {
        var f = TestHives.Corpus(@"ExpandoTestHives\system_registry_hive");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();


        var evenMoreKeys = r.ExpandKeyPath($"ControlSet001\\Control\\IDConfigDB\\{wildCardChar}\\0{wildCardChar}");
        evenMoreKeys.Count.Should().Be(3);
        evenMoreKeys.First().Should().Be("$$$PROTO.HIV\\ControlSet001\\Control\\IDConfigDB\\Alias\\0001".ToLowerInvariant());
        evenMoreKeys.Last().Should().Be("$$$PROTO.HIV\\ControlSet001\\Control\\IDConfigDB\\Hardware Profiles\\0001".ToLowerInvariant());
    }


    [Test]
    public void ExpandoTest04()
    {
        var f = TestHives.Corpus(@"ExpandoTestHives\system_registry_hive");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();

        var otherKeys2 = r.ExpandKeyPath($"ControlSet002\\Services\\Avg{wildCardChar}x86");
        otherKeys2.Count.Should().Be(3);
        //  otherKeys2[0].Should().Be(@"ControlSet002\Services\Avgldx86");
        //    otherKeys2[1].Should().Be(@"ControlSet002\Services\Avgmfx86");
        //    otherKeys2[2].Should().Be(@"ControlSet002\Services\Avgrkx86");
    }

    [Test]
    public void ExpandoTest05()
    {
        var f = TestHives.Corpus(@"ExpandoTestHives\system_registry_hive");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();

        var shouldNotExist = r.ExpandKeyPath($"ControlSet002\\Services\\Avg{wildCardChar}x86\\DoesNotExist");
        shouldNotExist.Count.Should().Be(0);
    }

    [Test]
    public void ExpandoTest06()
    {
        var f = TestHives.Corpus(@"ExpandoTestHives\system_registry_hive");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();

        var endCheck = r.ExpandKeyPath($"Setup\\AllowStart\\{wildCardChar}ss");
        endCheck.Count.Should().Be(2);
        endCheck.First().Should().Be("$$$PROTO.HIV\\Setup\\AllowStart\\Rpcss".ToLowerInvariant());
        endCheck.Last().Should().Be("$$$PROTO.HIV\\Setup\\AllowStart\\SamSs".ToLowerInvariant());
    }

    [Test]
    public void ExpandoTest07()
    {
        var f = TestHives.Corpus(@"ExpandoTestHives\system_registry_hive");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();

        var endCheck2 = r.ExpandKeyPath($"Setup\\AllowStart\\{wildCardChar}mss");
        endCheck2.Count.Should().Be(1);
        endCheck2.First().Should().Be("$$$PROTO.HIV\\Setup\\AllowStart\\SamSs".ToLowerInvariant());
    }

    [Test]
    public void ExpandoTest08()
    {
        var f = TestHives.Corpus(@"ExpandoTestHives\system_registry_hive");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;

        r.ParseHive();

        var endCheck3 = r.ExpandKeyPath($"ControlSet002\\Services\\{wildCardChar}\\Parameters".ToLowerInvariant());

        endCheck3.Count.Should().Be(127);
    }


    [Test]
    public void ExpandoTest09()
    {
        var f2 = TestHives.Corpus(@"ExpandoTestHives\ntuser.dat");
        var r2 = new RegistryHive(f2);
        r2.RecoverDeleted = true;

        r2.ParseHive();

        var keys2 = r2.ExpandKeyPath($"Software\\Microsoft\\Office\\16.0\\Excel\\User MRU\\{wildCardChar}\\File MRU"
            .ToLowerInvariant());

        keys2.Count.Should().Be(1);
        keys2.First().Should().Be(
                "ROOT\\Software\\Microsoft\\Office\\16.0\\Excel\\User MRU\\AD_B8387EDCD97012482021633037177683B71660DC7C410BE924536ACDF94CD5B4\\File MRU"
                    .ToLowerInvariant());
    }


    [Test]
    public void ExpandoTest10()
    {
        var f2 = TestHives.Corpus(@"ExpandoTestHives\ntuser.dat");
        var r2 = new RegistryHive(f2);
        r2.RecoverDeleted = true;

        r2.ParseHive();

        var keys2 = r2.ExpandKeyPath($"SOFTWARE\\Microsoft\\Office\\{wildCardChar}\\{wildCardChar}\\User MRU");

        keys2.Count.Should().Be(3);
        keys2.First().Should().Be(@"ROOT\Software\Microsoft\Office\16.0\Excel\User MRU".ToLowerInvariant());
    }

    [Test]
    public void ExpandoTest11()
    {
        var f2 = TestHives.Corpus(@"ExpandoTestHives\SOFTWARE_win10");
        var r2 = new RegistryHive(f2);
        r2.RecoverDeleted = true;

        r2.ParseHive();

        var keys2 = r2.ExpandKeyPath("WOW6432Node\\ODBC\\ODBCINST.INI\\Microsoft dBase Driver (*.dbf)");

        keys2.Count.Should().Be(1);
        keys2.First().Should().Be(@"ROOT\WOW6432Node\ODBC\ODBCINST.INI\Microsoft dBase Driver (*.dbf)".ToLowerInvariant());
    }

    [Test]
    public void ExpandoTest12()
    {
        var f2 = TestHives.Corpus(@"ExpandoTestHives\ntuser.dat");
        var r2 = new RegistryHive(f2);
        r2.RecoverDeleted = true;

        r2.ParseHive();

        var keys2 = r2.ExpandKeyPath(
            $"SOFTWARE\\Microsoft\\Office\\{wildCardChar}\\{wildCardChar}\\User MRU\\{wildCardChar}");

        keys2.Count.Should().Be(3);
        keys2.First().Should().Be(
                @"ROOT\Software\Microsoft\Office\16.0\Excel\User MRU\AD_B8387EDCD97012482021633037177683B71660DC7C410BE924536ACDF94CD5B4"
                    .ToLowerInvariant());
    }


    [Test]
    public void ExpandoTest13()
    {
        var f2 = TestHives.Corpus(@"ExpandoTestHives\ntuser.dat");
        var r2 = new RegistryHive(f2);
        r2.RecoverDeleted = true;

        r2.ParseHive();

        var keys2 = r2.ExpandKeyPath(
            $"Software\\Microsoft\\Office\\16.0\\Excel\\User MRU\\AD_B8387EDCD97012482021633037177683B71660DC7C410BE924536ACDF94CD5B4\\{wildCardChar}");

        keys2.Count.Should().Be(2);
        keys2.First().Should().Be(
                @"ROOT\Software\Microsoft\Office\16.0\Excel\User MRU\AD_B8387EDCD97012482021633037177683B71660DC7C410BE924536ACDF94CD5B4\File MRU"
                    .ToLowerInvariant());
        keys2.Last().Should().Be(
                @"ROOT\Software\Microsoft\Office\16.0\Excel\User MRU\AD_B8387EDCD97012482021633037177683B71660DC7C410BE924536ACDF94CD5B4\Place MRU"
                    .ToLowerInvariant());
    }

    [Test]
    public void ExpandoTest14()
    {
        var f2 = TestHives.Corpus(@"ExpandoTestHives\ntuser.dat");
        var r2 = new RegistryHive(f2);
        r2.RecoverDeleted = true;

        r2.ParseHive();

        //no wildcards should 
        var keys2 = r2.ExpandKeyPath(
            "Software\\Microsoft\\Office\\16.0\\Excel\\User MRU\\AD_B8387EDCD97012482021633037177683B71660DC7C410BE924536ACDF94CD5B4\\");

        keys2.Count.Should().Be(1);
    }

    [Test]
    public void ExpandoTest15()
    {
        var f2 = TestHives.Corpus(@"ExpandoTestHives\SYSTEM_loneWolf");
        var r2 = new RegistryHive(f2);
        r2.RecoverDeleted = true;

        r2.ParseHive();

        var keys2 = r2.ExpandKeyPath("ControlSet001\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}" +
                                     $"\\0001\\Ndi\\Params\\{wildCardChar}FlowControl");

        keys2.Count.Should().Be(1);
        keys2.First().Should().Be(
                @"ROOT\ControlSet001\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}\0001\Ndi\Params\*FlowControl"
                    .ToLowerInvariant());
    }

    [Test]
    public void ExpandoTest16()
    {
        var f2 = TestHives.Corpus(@"ExpandoTestHives\SYSTEM_loneWolf");
        var r2 = new RegistryHive(f2);
        r2.RecoverDeleted = true;

        r2.ParseHive();

        //TODO try to get this to work, if you do, remove WithoutWildCard
        var keys2 = r2.ExpandKeyPath("ControlSet001\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}" +
                                     $"\\0001\\Ndi\\Params\\*FlowControl\\{wildCardChar}");

        keys2.Count.Should().Be(1);
        keys2.Last().Should().Be(
                @"ROOT\ControlSet001\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}\0001\Ndi\Params\*FlowControl\Enum"
                    .ToLowerInvariant());
    }


    [Test]
    public void ReallocTest()
    {
        var f = TestHives.Corpus(@"ReallocValueDataHive");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;
        r.ParseHive();

        var ts = "2017-09-10 21:47:31 +00:00";

        var td = DateTimeOffset.Parse(ts);

        var t = r.GetDeletedKey(@"2", td.ToString());

        t.Should().NotBeNull();
        t.NkRecord.IsDeleted.Should().BeTrue();


        t.Values[0].VkRecord.DataRecordAllocated.Should().Be(true);
        //   t.Values[0].ValueData.Should().NotBe("1111");
    }


    [Test]
    public void OneOffParse()
    {
        var f = TestHives.Corpus(@"NTUSER_MarkElliot_RecentApps.DAT");
        var r = new RegistryHive(f);
        r.RecoverDeleted = true;
        r.ParseHive();
    }


    [Test]
    public void HBinSizeShouldMatchReadSize()
    {
        var sam = new RegistryHive(TestHives.Path(@"SAM"));
        sam.FlushRecordListsAfterParse = false;
        sam.ParseHive();

        sam.Header.Length.Should().Be(sam.HBinRecordTotalSize);
    }

    [Test]
    public void HBinSizeShouldNotMatchReadSize()
    {
        var r = new RegistryHive(TestHives.Path(@"SAM_DUPENAME"));
        //if you don't call parse, it wont match

        r.Header.Length.Should().NotBe(r.HBinRecordTotalSize);
    }




    [Test]
    public void RecoverDeletedShouldBeTrue()
    {
        var sam = new RegistryHive(TestHives.Path(@"SAM"));
        sam.FlushRecordListsAfterParse = false;
        sam.ParseHive();

        sam.RecoverDeleted = true;

        sam.RecoverDeleted.Should().Be(true);
        sam.RecoverDeleted = false;
    }


    [Test]
    public void ShouldExportFileAllRecords()
    {
        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();

        usrclassDeleted.ExportDataToCommonFormat(@"UsrclassDeletedNoDeletedStuff.txt", false);

        usrclassDeleted.Header.Length.Should().Be(usrclassDeleted.HBinRecordTotalSize);
    }


    [Test]
    public void ShouldExportFileDeletedRecords()
    {
        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();

        usrclassDeleted.ExportDataToCommonFormat(@"UsrclassDeletedDeletedStuff.txt", true);

        usrclassDeleted.Header.Length.Should().Be(usrclassDeleted.HBinRecordTotalSize);
    }

    [Test]
    public void ShouldExportHiveWithRootValues()
    {
        var samRootValue = new RegistryHive(TestHives.Path(@"SAM_RootValue"));
        samRootValue.FlushRecordListsAfterParse = false;
        samRootValue.ParseHive();


        samRootValue.ExportDataToCommonFormat(@"SamRootValueNoDeletedStuff.txt", false);
    }

    [Test]
    public void ShouldExportValuesToFile()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();

        var keys = usrClass1.FindByValueSize(100000).ToList();

        foreach (var valueBySizeInfo in keys)
        {
            File.WriteAllBytes($"{valueBySizeInfo.Value.ValueName}.bin", valueBySizeInfo.Value.ValueDataRaw);

            File.Exists($"{valueBySizeInfo.Value.ValueName}.bin").Should().BeTrue();
        }
    }

    [Test]
    public void ShouldFind100HitsForUrlInKeyAndValueName()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();
        var keyHits = usrClass1.FindInKeyName("URL").ToList();

        keyHits.Count.Should().Be(21);

        var valHits = usrClass1.FindInValueName("URL").ToList();

        valHits.Count.Should().Be(79);
    }

    [Test]
    public void ShouldFind1248AfterTimeStamp()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();


        var dt = new DateTimeOffset(2014, 11, 13, 15, 51, 17, TimeSpan.FromSeconds(0));
        var hits = usrClass1.FindByLastWriteTime(dt, null).ToList();

        hits.Count.Should().Be(14);
    }

    [Test]
    public void ShouldFind1544EforeTimeStamp()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();

        var dt = new DateTimeOffset(2014, 5, 20, 14, 19, 40, TimeSpan.FromSeconds(0));
        var hits = usrClass1.FindByLastWriteTime(null, dt).ToList();

        hits.Count.Should().Be(21);
    }

    [Test]
    public void ShouldFind32HitsForFoodInKeyName()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();


        var hits = usrClass1.FindInKeyName("food").ToList();

        hits.Count.Should().Be(32);
    }

    [Test]
    public void ShouldFind4HitsFor320033003200InValueDataSlack()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();

        var hits = usrClass1.FindInValueDataSlack("32-00-33-00-32-00", false, true).ToList();

        hits.Count.Should().Be(6);
    }

    [Test]
    public void ShouldFind4HitsForBinaryDataInValueData()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();

        var hits = usrClass1.FindInValueData("43-74-53-83-24-55-30").ToList();

        hits.Count.Should().Be(6);

        hits = usrClass1.FindInValueData("DeB").ToList();

        hits.Count.Should().Be(28);
    }

    [Test]
    public void ShouldFind4HitsForBinaryDataInValueDataWithRegEx()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();

        var hits = usrClass1.FindInValueData("04-00-EF-BE", true).ToList();

        hits.Count.Should().Be(56);

        hits = usrClass1.FindInValueData("47-4F-4F-4E", true).ToList();

        hits.Count.Should().Be(4);

        hits = usrClass1.FindInValueData("44-65-62", true).ToList(); //finds deb

        hits.Count.Should().Be(2);

        hits = usrClass1.FindInValueData("44-65-73", true).ToList(); //finds des

        hits.Count.Should().Be(1);

        hits = usrClass1.FindInValueData("44-65-(62|73)", true).ToList(); //finds deb or des

        hits.Count.Should().Be(3);
    }

    [Test]
    public void ShouldFind4HitsForBingXInKeyNamesWithRegEx()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();


        var hits = usrClass1.FindInKeyName("Microsoft.Bing[FHW]", true).ToList();

        hits.Count.Should().Be(44);

        hits = usrClass1.FindInKeyName("Microsoft.Bing[FHW]o", true).ToList();

        hits.Count.Should().Be(11);
    }

    [Test]
    public void ShouldFind4HitsForBingXInValueDataWithRegEx()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();

        var hits = usrClass1.FindInValueData("URL:bing[mhs]", true).ToList();

        hits.Count.Should().Be(3);

        hits = usrClass1.FindInValueData("URL:bing[mhts]", true).ToList();

        hits.Count.Should().Be(4);
    }

    [Test]
    public void ShouldFind4HitsForPostboxUrlInValueData()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();

        var hits = usrClass1.FindInValueData("Postbox URL").ToList();

        hits.Count.Should().Be(4);
    }

    [Test]
    public void ShouldFindAKeyWithClassName()
    {
        var systemOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SYSTEM"));
        var key =
            systemOnDemand.GetKey(
                @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\ControlSet001\Control\Lsa\Data");

        key.ClassName.Should().NotBeEmpty();
    }

    [Test]
    public void ShouldFindAKeyWithoutRootKeyName()
    {
        var sam = new RegistryHive(TestHives.Path(@"SAM"));
        sam.FlushRecordListsAfterParse = false;
        sam.ParseHive();

        var key = sam.GetKey(@"SAM\Domains");

        key.Should().NotBeNull();
    }

    [Test]
    public void ShouldFindBase64()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();

        var hits = usrClass1.FindBase64(20).ToList();

        hits.Count.Should().Be(137);
    }

    [Test]
    public void ShouldFindFiveValuesForSize4096()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();

        var keys = usrClass1.FindByValueSize(4096).ToList();

        keys.Count.Should().Be(5);
    }

    [Test]
    public void ShouldFindHitsValueNamesWithRegEx()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();

        var hits = usrClass1.FindInValueName("(App|Display)Name", true).ToList();

        hits.Count.Should().Be(326);

        hits = usrClass1.FindInValueName("Capability(Co|Si)", true).ToList();

        hits.Count.Should().Be(66);
    }

    [Test]
    public void ShouldFindKeyWithMixedCaseName()
    {
        var usrClassFtp = new RegistryHiveOnDemand(TestHives.Path(@"UsrClass FTP.dat"));

        var key =
            usrClassFtp.GetKey(
                @"S-1-5-21-2417227394-2575385136-2411922467-1105_CLAsses\ActivAtableClasses\CLsID");

        key.Should().NotBeNull();
    }

    [Test]
    public void ShouldFindKeyWithMixedCaseNameWithoutRootName()
    {
        var usrClassFtp = new RegistryHiveOnDemand(TestHives.Path(@"UsrClass FTP.dat"));

        var key = usrClassFtp.GetKey(@"ActivAtableClasses\CLsID");

        key.Should().NotBeNull();
    }

    [Test]
    public void ShouldFindNoHitsForZimmermanInKeyName()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();


        var hits = usrClass1.FindInKeyName("Zimmerman").ToList();

        hits.Count.Should().Be(0);
    }

    [Test]
    public void ShouldFindThreeHitsForMuiCacheInKeyName()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();

        var hits = usrClass1.FindInKeyName("MuiCache").ToList();

        hits.Count.Should().Be(3);
    }

    [Test]
    public void ShouldFindTwoBetweenTimeStamp()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();

        var start = new DateTimeOffset(2014, 5, 20, 19, 00, 00, TimeSpan.FromSeconds(0));
        var end = new DateTimeOffset(2014, 5, 20, 23, 59, 59, TimeSpan.FromSeconds(0));
        var hits = usrClass1.FindByLastWriteTime(start, end).ToList();

        hits.Count.Should().Be(2);
    }

    [Test]
    public void ShouldFindTwoValuesForSize100000()
    {
        var usrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
        usrClass1.RecoverDeleted = true;
        usrClass1.FlushRecordListsAfterParse = false;
        usrClass1.ParseHive();

        var keys = usrClass1.FindByValueSize(100000).ToList();

        keys.Count.Should().Be(2);
    }

    [Test]
    public void ShouldHaveGoodRegMultiSz()
    {
        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();

        //S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\MuiCache\6\52C64B7E
        var key =
            usrclassDeleted.GetKey(
                @"S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\MuiCache\6\52C64B7E");

        var val = key.Values.Single(t => t.ValueName == "LanguageList");

        val.Should().NotBeNull();

        val.ValueName.Should().Be("LanguageList");
        val.ValueData.Should().Be("en-US en");
    }

    [Test]
    public void ShouldHaveHardAndSoftParsingValuesOfZero()
    {
        var sam = new RegistryHive(TestHives.Path(@"SAM"));
        sam.FlushRecordListsAfterParse = false;
        sam.ParseHive();

        sam.HardParsingErrors.Should().Be(0);
        sam.SoftParsingErrors.Should().Be(0);
    }

    [Test]
    public void ShouldHaveHeaderLengthEqualToReadDataSize()
    {
        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();

        usrclassDeleted.Header.Length.Should().Be(usrclassDeleted.HBinRecordTotalSize);
    }

    [Test]
    public void ShouldReturnKeyBasedOnRelativePath()
    {
        var sam = new RegistryHive(TestHives.Path(@"SAM"));
        sam.FlushRecordListsAfterParse = false;
        sam.ParseHive();
        var key =
            sam.GetKey(0x418);

        key.Should().NotBeNull();
    }

    [Test]
    public void ShouldReturnNullWhenKeyPathNotFound()
    {
        var sam = new RegistryHive(TestHives.Path(@"SAM"));
        sam.FlushRecordListsAfterParse = false;
        sam.ParseHive();

        var key =
            sam.GetKey(@"SAM\Domains\DoesNotExist");

        key.Should().BeNull();
    }

    [Test]
    public void ShouldReturnNullWhenRelativeOffsetNotFound()
    {
        var sam = new RegistryHive(TestHives.Path(@"SAM"));
        sam.FlushRecordListsAfterParse = false;
        sam.ParseHive();

        var key =
            sam.GetKey(0x999418);

        key.Should().BeNull();
    }

    [Test]
    public void ShouldTakeByteArrayInConstructor()
    {
        var sam = new RegistryHive(TestHives.Path(@"SAM"));
        sam.FlushRecordListsAfterParse = false;
        sam.ParseHive();

        var r = new RegistryHive(sam.FileBytes, TestHives.Path(@"SAM"));

        r.Header.Should().NotBeNull();
        r.HivePath.Should().Be(TestHives.Path(@"SAM"));
        r.HiveType.Should().Be(HiveTypeEnum.Sam);
    }

//        [Test]
//        public void ShouldThrowExceptionNoRootKey()
//        {
//            Check.ThatCode(() =>
//            {
//                var r = new RegistryHive(TestHives.Path(@"SECURITYNoRoot"));
//                r.ParseHive();
//            }).Throws<KeyNotFoundException>();
//        }

    [Test]
    public void ShouldThrowExceptionWhenCallingParseHiveTwice()
    {
        FluentActions.Invoking(() =>
            {
                var r = new RegistryHive(TestHives.Path(@"SAMBadHBinHeader"));
                r.ParseHive();
                r.ParseHive();
            }).Should().Throw<Exception>();
    }

    [Test]
    [Ignore("Doesn't throw as expected")]
    public void ShouldThrowExceptionWithBadHbinHeader()
    {
        FluentActions.Invoking(() =>
            {
                var r = new RegistryHive(TestHives.Path(@"SAMBadHBinHeader"));
                r.ParseHive();
            }).Should().Throw<Exception>();
    }

    [Test]
    public void TestsListRecordsContinued3()
    {
        var usrClassFtp = new RegistryHiveOnDemand(TestHives.Path(@"UsrClass FTP.dat"));

        var key =
            usrClassFtp.GetKey(
                @"S-1-5-21-2417227394-2575385136-2411922467-1105_Classes\ActivatableClasses\CLSID");

        key.Should().NotBeNull();
    }

    [Test]
    public void VerifyHiveTestShouldPass()
    {
        var sam = new RegistryHive(TestHives.Path(@"SAM"));
        sam.FlushRecordListsAfterParse = false;
        sam.ParseHive();

        var m = sam.Verify();

        m.HasValidHeader.Should().BeTrue();
    }
}