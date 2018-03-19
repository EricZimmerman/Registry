using System;
using System.IO;
using System.Linq;
using NFluent;
using NLog;
using NUnit.Framework;

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
            var sam = new RegistryHive(@"..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            Check.That(sam.SoftParsingErrors).IsEqualTo(0);
            Check.That(sam.HardParsingErrors).IsEqualTo(0);
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
            ;
        }

        [Test]
        public void HBinSizeShouldMatchReadSize()
        {
            var sam = new RegistryHive(@"..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            Check.That(sam.Header.Length).IsEqualTo(sam.HBinRecordTotalSize);
        }

        [Test]
        public void HBinSizeShouldNotMatchReadSize()
        {
            var r = new RegistryHive(@"..\..\Hives\SAM_DUPENAME");
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
            var sam = new RegistryHive(@"..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            sam.RecoverDeleted = true;

            Check.That(sam.RecoverDeleted).IsEqualTo(true);
            sam.RecoverDeleted = false;
        }


        [Test]
        public void ShouldExportFileAllRecords()
        {
            var usrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
            usrclassDeleted.RecoverDeleted = true;
            usrclassDeleted.FlushRecordListsAfterParse = false;
            usrclassDeleted.ParseHive();

            usrclassDeleted.ExportDataToCommonFormat(@"UsrclassDeletedNoDeletedStuff.txt", false);

            Check.That(usrclassDeleted.Header.Length).IsEqualTo(usrclassDeleted.HBinRecordTotalSize);
        }


        [Test]
        public void ShouldExportFileDeletedRecords()
        {
            var usrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
            usrclassDeleted.RecoverDeleted = true;
            usrclassDeleted.FlushRecordListsAfterParse = false;
            usrclassDeleted.ParseHive();

            usrclassDeleted.ExportDataToCommonFormat(@"UsrclassDeletedDeletedStuff.txt", true);

            Check.That(usrclassDeleted.Header.Length).IsEqualTo(usrclassDeleted.HBinRecordTotalSize);
        }

        [Test]
        public void ShouldExportHiveWithRootValues()
        {
            var samRootValue = new RegistryHive(@"..\..\Hives\SAM_RootValue");
            samRootValue.FlushRecordListsAfterParse = false;
            samRootValue.ParseHive();


            samRootValue.ExportDataToCommonFormat(@"SamRootValueNoDeletedStuff.txt", false);
        }

        [Test]
        public void ShouldExportValuesToFile()
        {
            var usrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
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
            var usrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
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
            var usrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
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
            var usrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
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
            var usrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();


            var hits = usrClass1.FindInKeyName("food").ToList();

            Check.That(hits.Count).IsEqualTo(32);
        }

        [Test]
        public void ShouldFind4HitsFor320033003200InValueDataSlack()
        {
            var usrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var hits = usrClass1.FindInValueDataSlack("32-00-33-00-32-00", false, true).ToList();

            Check.That(hits.Count).IsEqualTo(6);
        }

        [Test]
        public void ShouldFind4HitsForBinaryDataInValueData()
        {
            var usrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
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
            var usrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
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
            var usrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
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
            var usrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
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
            var usrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var hits = usrClass1.FindInValueData("Postbox URL").ToList();

            Check.That(hits.Count).IsEqualTo(4);
        }

        [Test]
        public void ShouldFindAKeyWithClassName()
        {
            var systemOnDemand = new RegistryHiveOnDemand(@"..\..\Hives\SYSTEM");
            var key =
                systemOnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\ControlSet001\Control\Lsa\Data");

            Check.That(key.ClassName).IsNotEmpty();
        }

        [Test]
        public void ShouldFindAKeyWithoutRootKeyName()
        {
            var sam = new RegistryHive(@"..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            var key = sam.GetKey(@"SAM\Domains");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void ShouldFindBase64()
        {
            var usrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var hits = usrClass1.FindBase64(20).ToList();

            Check.That(hits.Count).IsEqualTo(137);
        }

        [Test]
        public void ShouldFindFiveValuesForSize4096()
        {
            var usrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var keys = usrClass1.FindByValueSize(4096).ToList();

            Check.That(keys.Count).IsEqualTo(5);
        }

        [Test]
        public void ShouldFindHitsValueNamesWithRegEx()
        {
            var usrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
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
            var usrClassFtp = new RegistryHiveOnDemand(@"..\..\Hives\UsrClass FTP.dat");

            var key =
                usrClassFtp.GetKey(
                    @"S-1-5-21-2417227394-2575385136-2411922467-1105_CLAsses\ActivAtableClasses\CLsID");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void ShouldFindKeyWithMixedCaseNameWithoutRootName()
        {
            var usrClassFtp = new RegistryHiveOnDemand(@"..\..\Hives\UsrClass FTP.dat");

            var key = usrClassFtp.GetKey(@"ActivAtableClasses\CLsID");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void ShouldFindNoHitsForZimmermanInKeyName()
        {
            var usrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();


            var hits = usrClass1.FindInKeyName("Zimmerman").ToList();

            Check.That(hits.Count).IsEqualTo(0);
        }

        [Test]
        public void ShouldFindThreeHitsForMuiCacheInKeyName()
        {
            var usrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var hits = usrClass1.FindInKeyName("MuiCache").ToList();

            Check.That(hits.Count).IsEqualTo(3);
        }

        [Test]
        public void ShouldFindTwoBetweenTimeStamp()
        {
            var usrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
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
            var usrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();

            var keys = usrClass1.FindByValueSize(100000).ToList();

            Check.That(keys.Count).IsEqualTo(2);
        }

        [Test]
        public void ShouldHaveGoodRegMultiSz()
        {
            var usrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
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
            var sam = new RegistryHive(@"..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            Check.That(sam.HardParsingErrors).IsEqualTo(0);
            Check.That(sam.SoftParsingErrors).IsEqualTo(0);
        }

        [Test]
        public void ShouldHaveHeaderLengthEqualToReadDataSize()
        {
            var usrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
            usrclassDeleted.RecoverDeleted = true;
            usrclassDeleted.FlushRecordListsAfterParse = false;
            usrclassDeleted.ParseHive();

            Check.That(usrclassDeleted.Header.Length).IsEqualTo(usrclassDeleted.HBinRecordTotalSize);
        }

        [Test]
        public void ShouldReturnKeyBasedOnRelativePath()
        {
            var sam = new RegistryHive(@"..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();
            var key =
                sam.GetKey(0x418);

            Check.That(key).IsNotNull();
        }

        [Test]
        public void ShouldReturnNullWhenKeyPathNotFound()
        {
            var sam = new RegistryHive(@"..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            var key =
                sam.GetKey(@"SAM\Domains\DoesNotExist");

            Check.That(key).IsNull();
        }

        [Test]
        public void ShouldReturnNullWhenRelativeOffsetNotFound()
        {
            var sam = new RegistryHive(@"..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            var key =
                sam.GetKey(0x999418);

            Check.That(key).IsNull();
        }

        [Test]
        public void ShouldTakeByteArrayInConstructor()
        {
            var sam = new RegistryHive(@"..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            var r = new RegistryHive(sam.FileBytes);

            Check.That(r.Header).IsNotNull();
            Check.That(r.HivePath).IsEqualTo("None");
            Check.That(r.HiveType).IsEqualTo(HiveTypeEnum.Sam);
        }

//        [Test]
//        public void ShouldThrowExceptionNoRootKey()
//        {
//            Check.ThatCode(() =>
//            {
//                var r = new RegistryHive(@"..\..\Hives\SECURITYNoRoot");
//                r.ParseHive();
//            }).Throws<KeyNotFoundException>();
//        }

        [Test]
        public void ShouldThrowExceptionWhenCallingParseHiveTwice()
        {
            Check.ThatCode(() =>
                {
                    var r = new RegistryHive(@"..\..\Hives\SAMBadHBinHeader");
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
                    var r = new RegistryHive(@"..\..\Hives\SAMBadHBinHeader");
                    r.ParseHive();
                })
                .Throws<Exception>();
        }

        [Test]
        public void TestsListRecordsContinued3()
        {
            var usrClassFtp = new RegistryHiveOnDemand(@"..\..\Hives\UsrClass FTP.dat");

            var key =
                usrClassFtp.GetKey(
                    @"S-1-5-21-2417227394-2575385136-2411922467-1105_Classes\ActivatableClasses\CLSID");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void VerifyHiveTestShouldPass()
        {
            var sam = new RegistryHive(@"..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            var m = sam.Verify();

            Check.That(m.HasValidHeader).IsTrue();
        }
    }
}