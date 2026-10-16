using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Registry.Test;

internal class TestRegistrySkeleton
{
    [Test]
    public void ShouldCreateRegistrySkeleton()
    {
        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();

        var rs = new RegistrySkeleton(usrclassDeleted);

        rs.Should().NotBeNull();
    }

    [Test]
    public void ShouldThrowNullRefExtensionOnNullHive()
    {
        FluentActions.Invoking(() =>
            {
                var rs = new RegistrySkeleton(null);
            }).Should().Throw<NullReferenceException>();
    }

    [Test]
    public void ShouldReturnTrueOnAddMuiCacheSubkeyToSkeletonList()
    {
        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();

        var rs = new RegistrySkeleton(usrclassDeleted);

        var sk = new SkeletonKeyRoot(@"Local Settings\MuiCache\6\52C64B7E", true, false);

        var added = rs.AddEntry(sk);

        added.Should().BeTrue();
        rs.Keys.Count.Should().Be(1);

        sk = new SkeletonKeyRoot(@"path\does\not\exist", false, false);

        added = rs.AddEntry(sk);

        added.Should().BeFalse();
        rs.Keys.Count.Should().Be(1);
    }

    [Test]
    public void ShouldReturnFalseOnRemovingNonExistentKey()
    {
        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();

        var rs = new RegistrySkeleton(usrclassDeleted);

        var sk = new SkeletonKeyRoot(@"path\does\not\exist", false, false);

        var added = rs.RemoveEntry(sk);

        added.Should().BeFalse();
        rs.Keys.Count.Should().Be(0);
    }

    [Test]
    public void ShouldntAddDuplicateSkeletonKeys()
    {
        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();

        var rs = new RegistrySkeleton(usrclassDeleted);

        var sk = new SkeletonKeyRoot(@"Local Settings\MuiCache\6\52C64B7E", true, false);

        var added = rs.AddEntry(sk);

        added.Should().BeTrue();
        rs.Keys.Count.Should().Be(1);

        sk = new SkeletonKeyRoot(@"path\does\not\exist", false, false);

        added = rs.AddEntry(sk);

        added.Should().BeFalse();
        rs.Keys.Count.Should().Be(1);

        var sk1 = new SkeletonKeyRoot(@"Local Settings\MuiCache\6\52C64B7E", true, false);

        added = rs.AddEntry(sk1);

        added.Should().BeTrue();
        rs.Keys.Count.Should().Be(1);
    }

    [Test]
    public void KeysCountShouldBeZeroAfterAddRemove()
    {
        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();

        var rs = new RegistrySkeleton(usrclassDeleted);

        var sk = new SkeletonKeyRoot(@"Local Settings\MuiCache\6\52C64B7E", true, false);

        var added = rs.AddEntry(sk);

        added.Should().BeTrue();
        rs.Keys.Count.Should().Be(1);

        var removed = rs.RemoveEntry(sk);

        removed.Should().BeTrue();
        rs.Keys.Count.Should().Be(0);
    }


    [Test]
    public void ShouldReturnFalseOnAddNonExistentSubkeyToSkeletonList()
    {
        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();

        var rs = new RegistrySkeleton(usrclassDeleted);

        var sk = new SkeletonKeyRoot(@"path\does\not\exist", false, false);

        var added = rs.AddEntry(sk);

        added.Should().BeFalse();
        rs.Keys.Count.Should().Be(0);
    }

    [Test]
    public void ShouldThrowExceptionIfWriteCalledWithNoKeysAdded()
    {
        FluentActions.Invoking(() =>
            {
                var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
                usrclassDeleted.RecoverDeleted = true;
                usrclassDeleted.FlushRecordListsAfterParse = false;
                usrclassDeleted.ParseHive();

                var rs = new RegistrySkeleton(usrclassDeleted);
                rs.Write(@"foo.reg");
            }).Should().Throw<InvalidOperationException>(); //ncrunch: no coverage
    }

    [Test]
    public void ShouldReturnTrueWhenWriteCalledWithKeyAdded()
    {
        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();

        var rs = new RegistrySkeleton(usrclassDeleted);

        var sk = new SkeletonKeyRoot(@"Local Settings\MuiCache\6\52C64B7E", true, false);

        rs.AddEntry(sk);

        var write = rs.Write(@"onekeytest.hve");

        write.Should().BeTrue();
    }

    [Test]
    public void BigRecursiveWithRegUnknown()
    {
        var system = new RegistryHive(TestHives.Path(@"System"));
        system.FlushRecordListsAfterParse = false;
        system.ParseHive();

        var rs = new RegistrySkeleton(system);

        var sk = new SkeletonKeyRoot(@"Setup\AllowStart", true, true);

        rs.AddEntry(sk);

        sk = new SkeletonKeyRoot(@"Select", true, true);

        rs.AddEntry(sk);

        var write = rs.Write(@"bigrecursive.hve");

        write.Should().BeTrue();

        var newReg = new RegistryHive(@"bigrecursive.hve");

        newReg.ParseHive();

        var key = newReg.GetKey(@"ControlSet001\Control");

        key.Should().NotBeNull();

        key = newReg.GetKey(@"Select");

        key.Should().NotBeNull();
    }

    [Test]
    public void BigDataCase()
    {
        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();

        var rs = new RegistrySkeleton(usrclassDeleted);

        var sk = new SkeletonKeyRoot(@"Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify", true,
            false);

        rs.AddEntry(sk);

        var outPath = @"bigdatatest.hve";

        var write = rs.Write(outPath);

        write.Should().BeTrue();

        var newReg = new RegistryHive(outPath);
        newReg.ParseHive();

        var key = newReg.GetKey(@"Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify");

        key.Should().NotBeNull();

        var val = key.Values.Single(t => t.ValueName == "PastIconsStream");

        val.Should().NotBeNull();
        val.ValueDataRaw.Length.Should().Be(52526);
        val.ValueSlackRaw.Length.Should().Be(13014);
    }

    [Test]
    public void RecursiveCase()
    {
        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();

        var rs = new RegistrySkeleton(usrclassDeleted);

        var sk = new SkeletonKeyRoot(@"Local Settings\Software\Microsoft\Windows\Shell\Bags", true, true);

        rs.AddEntry(sk);

        var outPath = @"recursivetest.hve";

        var write = rs.Write(outPath);

        write.Should().BeTrue();

        var newReg = new RegistryHive(outPath);

        newReg.ParseHive();

        var key =
            newReg.GetKey(
                @"Local Settings\Software\Microsoft\Windows\Shell\Bags\3\Shell\{5C4F28B5-F869-4E84-8E60-F11DB97C5CC7}");

        key.Should().NotBeNull();

        var val = key.Values.Single(t => t.ValueName == "FFlags");

        val.Should().NotBeNull();

        key = newReg.GetKey(@"Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders\Shell");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "ShowCmd");

        val.Should().NotBeNull();
        val.ValueDataRaw.Length.Should().Be(4);
    }

    [Test]
    public void DeletedCase()
    {
        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();

        var rs = new RegistrySkeleton(usrclassDeleted);

        var sk = new SkeletonKeyRoot(@"Local Settings\Software\Microsoft\Windows\Shell\BagMRU", true, true);

        rs.AddEntry(sk);

        var outPath = @"deletedTest.hve";

        var write = rs.Write(outPath);

        write.Should().BeTrue();

        var newReg = new RegistryHive(outPath)
        {
            RecoverDeleted = true
        };
        newReg.ParseHive();

        var key =
            newReg.GetKey(
                @"Local Settings\Software\Microsoft\Windows\Shell\BagMRU\0\0");

        key.Should().NotBeNull();

        var val = key.Values.Single(t => t.ValueName == "MRUListEx");

        val.Should().NotBeNull();

        key = newReg.GetKey(@"Local Settings\Software\Microsoft\Windows\Shell\BagMRU\1\0\0");

        key.Should().NotBeNull();

        val = key.Values.Single(t => t.ValueName == "0");

        val.Should().NotBeNull();
        val.ValueDataRaw.Length.Should().Be(281);
    }

    [Test]
    public void WrittenHiveShouldContain163ValuesInMuiCacheSubkey()
    {
        var usrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
        usrclassDeleted.RecoverDeleted = true;
        usrclassDeleted.FlushRecordListsAfterParse = false;
        usrclassDeleted.ParseHive();

        var rs = new RegistrySkeleton(usrclassDeleted);

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

        write.Should().BeTrue();

        var newReg = new RegistryHive(outPath);
        newReg.ParseHive();

        var key = newReg.GetKey(@"Local Settings\MuiCache\6");

        key.Should().NotBeNull();

        key.LastWriteTime.Value.Year.Should().Be(2011);
        key.LastWriteTime.Value.Month.Should().Be(9);
        key.LastWriteTime.Value.Day.Should().Be(19);
        key.LastWriteTime.Value.Hour.Should().Be(19);
        key.LastWriteTime.Value.Minute.Should().Be(2);
        key.LastWriteTime.Value.Second.Should().Be(8);

        key = newReg.GetKey(@"Local Settings\MuiCache\6\52C64B7E");

        key.Should().NotBeNull();

        key.Values.Count.Should().Be(163);
    }
}