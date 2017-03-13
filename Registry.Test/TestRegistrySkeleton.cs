using System;
using System.Linq;
using NFluent;
using NUnit.Framework;

namespace Registry.Test
{
    internal class TestRegistrySkeleton
    {
        [Test]
        public void ShouldCreateRegistrySkeleton()
        {
            var UsrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
            UsrclassDeleted.RecoverDeleted = true;
            UsrclassDeleted.FlushRecordListsAfterParse = false;
            UsrclassDeleted.ParseHive();

            var rs = new RegistrySkeleton(UsrclassDeleted);

            Check.That(rs).IsNotNull();
        }

        [Test]
        public void ShouldThrowNullRefExtensionOnNullHive()
        {
            Check.ThatCode(() =>
                {
                    var rs = new RegistrySkeleton(null);
                })
                .Throws<NullReferenceException>();
        }

        [Test]
        public void ShouldReturnTrueOnAddMuiCacheSubkeyToSkeletonList()
        {
            var UsrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
            UsrclassDeleted.RecoverDeleted = true;
            UsrclassDeleted.FlushRecordListsAfterParse = false;
            UsrclassDeleted.ParseHive();

            var rs = new RegistrySkeleton(UsrclassDeleted);

            var sk = new SkeletonKeyRoot(@"Local Settings\MuiCache\6\52C64B7E", true, false);

            var added = rs.AddEntry(sk);

            Check.That(added).IsTrue();
            Check.That(rs.Keys.Count).IsEqualTo(1);

            sk = new SkeletonKeyRoot(@"path\does\not\exist", false, false);

            added = rs.AddEntry(sk);

            Check.That(added).IsFalse();
            Check.That(rs.Keys.Count).IsEqualTo(1);
        }

        [Test]
        public void ShouldReturnFalseOnRemovingNonExistentKey()
        {
            var UsrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
            UsrclassDeleted.RecoverDeleted = true;
            UsrclassDeleted.FlushRecordListsAfterParse = false;
            UsrclassDeleted.ParseHive();

            var rs = new RegistrySkeleton(UsrclassDeleted);

            var sk = new SkeletonKeyRoot(@"path\does\not\exist", false, false);

            var added = rs.RemoveEntry(sk);

            Check.That(added).IsFalse();
            Check.That(rs.Keys.Count).IsEqualTo(0);
        }

        [Test]
        public void ShouldntAddDuplicateSkeletonKeys()
        {
            var UsrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
            UsrclassDeleted.RecoverDeleted = true;
            UsrclassDeleted.FlushRecordListsAfterParse = false;
            UsrclassDeleted.ParseHive();

            var rs = new RegistrySkeleton(UsrclassDeleted);

            var sk = new SkeletonKeyRoot(@"Local Settings\MuiCache\6\52C64B7E", true, false);

            var added = rs.AddEntry(sk);

            Check.That(added).IsTrue();
            Check.That(rs.Keys.Count).IsEqualTo(1);

            sk = new SkeletonKeyRoot(@"path\does\not\exist", false, false);

            added = rs.AddEntry(sk);

            Check.That(added).IsFalse();
            Check.That(rs.Keys.Count).IsEqualTo(1);

            var sk1 = new SkeletonKeyRoot(@"Local Settings\MuiCache\6\52C64B7E", true, false);

            added = rs.AddEntry(sk1);

            Check.That(added).IsTrue();
            Check.That(rs.Keys.Count).IsEqualTo(1);
        }

        [Test]
        public void KeysCountShouldBeZeroAfterAddRemove()
        {
            var UsrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
            UsrclassDeleted.RecoverDeleted = true;
            UsrclassDeleted.FlushRecordListsAfterParse = false;
            UsrclassDeleted.ParseHive();

            var rs = new RegistrySkeleton(UsrclassDeleted);

            var sk = new SkeletonKeyRoot(@"Local Settings\MuiCache\6\52C64B7E", true, false);

            var added = rs.AddEntry(sk);

            Check.That(added).IsTrue();
            Check.That(rs.Keys.Count).IsEqualTo(1);

            var removed = rs.RemoveEntry(sk);

            Check.That(removed).IsTrue();
            Check.That(rs.Keys.Count).IsEqualTo(0);
        }


        [Test]
        public void ShouldReturnFalseOnAddNonExistentSubkeyToSkeletonList()
        {
            var UsrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
            UsrclassDeleted.RecoverDeleted = true;
            UsrclassDeleted.FlushRecordListsAfterParse = false;
            UsrclassDeleted.ParseHive();

            var rs = new RegistrySkeleton(UsrclassDeleted);

            var sk = new SkeletonKeyRoot(@"path\does\not\exist", false, false);

            var added = rs.AddEntry(sk);

            Check.That(added).IsFalse();
            Check.That(rs.Keys.Count).IsEqualTo(0);
        }

        [Test]
        public void ShouldThrowExceptionIfWriteCalledWithNoKeysAdded()
        {
            Check.ThatCode(() =>
                {
                    var UsrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
                    UsrclassDeleted.RecoverDeleted = true;
                    UsrclassDeleted.FlushRecordListsAfterParse = false;
                    UsrclassDeleted.ParseHive();

                    var rs = new RegistrySkeleton(UsrclassDeleted);
                    rs.Write(@"foo.reg");
                })
                .Throws<InvalidOperationException>(); //ncrunch: no coverage
        }

        [Test]
        public void ShouldReturnTrueWhenWriteCalledWithKeyAdded()
        {
            var UsrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
            UsrclassDeleted.RecoverDeleted = true;
            UsrclassDeleted.FlushRecordListsAfterParse = false;
            UsrclassDeleted.ParseHive();

            var rs = new RegistrySkeleton(UsrclassDeleted);

            var sk = new SkeletonKeyRoot(@"Local Settings\MuiCache\6\52C64B7E", true, false);

            rs.AddEntry(sk);

            var write = rs.Write(@"onekeytest.hve");

            Check.That(write).IsTrue();
        }

        [Test]
        [Timeout(360000)]
        public void BigRecursiveWithRegUnknown()
        {
            var System = new RegistryHive(@"..\..\Hives\System");
            System.FlushRecordListsAfterParse = false;
            System.ParseHive();

            var rs = new RegistrySkeleton(System);

            var sk = new SkeletonKeyRoot(@"Setup\AllowStart", true, true);

            rs.AddEntry(sk);

            sk = new SkeletonKeyRoot(@"Select", true, true);

            rs.AddEntry(sk);

            var write = rs.Write(@"bigrecursive.hve");

            Check.That(write).IsTrue();

            var newReg = new RegistryHive(@"bigrecursive.hve");

            newReg.ParseHive();

            var key = newReg.GetKey(@"ControlSet001\Control");

            Check.That(key).IsNotNull();

            key = newReg.GetKey(@"Select");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void BigDataCase()
        {
            var UsrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
            UsrclassDeleted.RecoverDeleted = true;
            UsrclassDeleted.FlushRecordListsAfterParse = false;
            UsrclassDeleted.ParseHive();

            var rs = new RegistrySkeleton(UsrclassDeleted);

            var sk = new SkeletonKeyRoot(@"Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify", true,
                false);

            rs.AddEntry(sk);

            var outPath = @"bigdatatest.hve";

            var write = rs.Write(outPath);

            Check.That(write).IsTrue();

            var newReg = new RegistryHive(outPath);
            newReg.ParseHive();

            var key = newReg.GetKey(@"Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "PastIconsStream");

            Check.That(val).IsNotNull();
            Check.That(val.ValueDataRaw.Length).IsEqualTo(52526);
            Check.That(val.ValueSlackRaw.Length).IsEqualTo(13014);
        }

        [Test]
        public void RecursiveCase()
        {
            var UsrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
            UsrclassDeleted.RecoverDeleted = true;
            UsrclassDeleted.FlushRecordListsAfterParse = false;
            UsrclassDeleted.ParseHive();

            var rs = new RegistrySkeleton(UsrclassDeleted);

            var sk = new SkeletonKeyRoot(@"Local Settings\Software\Microsoft\Windows\Shell\Bags", true, true);

            rs.AddEntry(sk);

            var outPath = @"recursivetest.hve";

            var write = rs.Write(outPath);

            Check.That(write).IsTrue();

            var newReg = new RegistryHive(outPath);

            newReg.ParseHive();

            var key =
                newReg.GetKey(
                    @"Local Settings\Software\Microsoft\Windows\Shell\Bags\3\Shell\{5C4F28B5-F869-4E84-8E60-F11DB97C5CC7}");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "FFlags");

            Check.That(val).IsNotNull();

            key = newReg.GetKey(@"Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders\Shell");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "ShowCmd");

            Check.That(val).IsNotNull();
            Check.That(val.ValueDataRaw.Length).IsEqualTo(4);
        }

        [Test]
        public void DeletedCase()
        {
            var UsrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
            UsrclassDeleted.RecoverDeleted = true;
            UsrclassDeleted.FlushRecordListsAfterParse = false;
            UsrclassDeleted.ParseHive();

            var rs = new RegistrySkeleton(UsrclassDeleted);

            var sk = new SkeletonKeyRoot(@"Local Settings\Software\Microsoft\Windows\Shell\BagMRU", true, true);

            rs.AddEntry(sk);

            var outPath = @"deletedTest.hve";

            var write = rs.Write(outPath);

            Check.That(write).IsTrue();

            var newReg = new RegistryHive(outPath)
            {
                RecoverDeleted = true
            };
            newReg.ParseHive();

            var key =
                newReg.GetKey(
                    @"Local Settings\Software\Microsoft\Windows\Shell\BagMRU\0\0");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "MRUListEx");

            Check.That(val).IsNotNull();

            key = newReg.GetKey(@"Local Settings\Software\Microsoft\Windows\Shell\BagMRU\1\0\0");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "0");

            Check.That(val).IsNotNull();
            Check.That(val.ValueDataRaw.Length).IsEqualTo(281);
        }

        [Test]
        public void WrittenHiveShouldContain163ValuesInMuiCacheSubkey()
        {
            var UsrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
            UsrclassDeleted.RecoverDeleted = true;
            UsrclassDeleted.FlushRecordListsAfterParse = false;
            UsrclassDeleted.ParseHive();

            var rs = new RegistrySkeleton(UsrclassDeleted);

            var sk = new SkeletonKeyRoot(@"Local Settings\MuiCache\6\52C64B7E", true, false);

            rs.AddEntry(sk);

            sk = new SkeletonKeyRoot(@"Local Settings\Software\Microsoft\Windows", true, false);

            rs.AddEntry(sk);

            sk = new SkeletonKeyRoot(@"VirtualStore\MACHINE", true, false);

            rs.AddEntry(sk);

            sk = new SkeletonKeyRoot(@"Local Settings\Software\Microsoft\Windows\Shell\BagMRU", true, false);

            rs.AddEntry(sk);

            var outPath = @"valuetest.hve";

            var write = rs.Write(outPath);

            Check.That(write).IsTrue();

            var newReg = new RegistryHive(outPath);
            newReg.ParseHive();

            var key = newReg.GetKey(@"Local Settings\MuiCache\6");

            Check.That(key).IsNotNull();

            Check.That(key.LastWriteTime.Value.Year).IsEqualTo(2011);
            Check.That(key.LastWriteTime.Value.Month).IsEqualTo(9);
            Check.That(key.LastWriteTime.Value.Day).IsEqualTo(19);
            Check.That(key.LastWriteTime.Value.Hour).IsEqualTo(19);
            Check.That(key.LastWriteTime.Value.Minute).IsEqualTo(2);
            Check.That(key.LastWriteTime.Value.Second).IsEqualTo(8);

            key = newReg.GetKey(@"Local Settings\MuiCache\6\52C64B7E");

            Check.That(key).IsNotNull();

            Check.That(key.Values.Count).IsEqualTo(163);
        }
    }
}