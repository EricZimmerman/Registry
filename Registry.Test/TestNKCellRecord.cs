using FluentAssertions;
using NUnit.Framework;
using Registry.Cells;

namespace Registry.Test;

[TestFixture]
internal class TestNkCellRecord
{
    [Test]
    public void ShouldHavePaddingLengthOfZeroWhenRecordIsFree()
    {
        var bcd = new RegistryHive(TestHives.Path(@"BCD"));
        bcd.FlushRecordListsAfterParse = false;
        bcd.RecoverDeleted = true;
        bcd.ParseHive();

        var key = bcd.GetKey(0x10e8);

        key.Should().NotBeNull();
        key.NkRecord.Padding.Length.Should().Be(0);
    }

    [Test]
    public void ShouldHaveUnableToDetermineName()
    {
        var usrClassBeef = new RegistryHive(TestHives.Path(@"UsrClass BEEF000E.dat"));
        usrClassBeef.RecoverDeleted = true;
        usrClassBeef.FlushRecordListsAfterParse = false;
        usrClassBeef.ParseHive();

        var key = usrClassBeef.CellRecords[0x783CD8] as NkCellRecord;

        key.Should().NotBeNull();

        key.Padding.Length.Should().Be(0);
        key.Name.Should().Be("(Unable to determine name)");
    }

    [Test]
    public void ShouldVerifyNkRecordProperties()
    {
        var sam = new RegistryHive(TestHives.Path(@"SAM"));
        sam.FlushRecordListsAfterParse = false;
        sam.ParseHive();

        var key =
            sam.GetKey(0x418);

        key.Should().NotBeNull();

        key.NkRecord.Padding.Length.Should().BeGreaterThan(0);
        key.NkRecord.ToString().Should().NotBeEmpty();
        key.NkRecord.SecurityCellIndex.Should().BeGreaterThan(0);
        key.NkRecord.SubkeyListsVolatileCellIndex.Should().Be(0);

        key.KeyName.Should().Be("Domains");
        key.KeyPath.Should().Be(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains");
        key.LastWriteTime.HasValue.Should().BeTrue();
        key.LastWriteTime.Value.ToString("MM-dd-yyyy").Should().Be("07-03-2014");
        key.NkRecord.Size.Should().Be(0x58);
        key.NkRecord.RelativeOffset.Should().Be(0x418);
        key.NkRecord.AbsoluteOffset.Should().Be(0x1418);
        key.NkRecord.Signature.Should().Be("nk");
        key.NkRecord.IsFree.Should().BeFalse();
        key.NkRecord.Debug.Should().Be(0);
        key.NkRecord.MaximumClassLength.Should().Be(0);
        key.NkRecord.ClassCellIndex.Should().Be(0);
        key.NkRecord.ClassLength.Should().Be(0);
        key.NkRecord.MaximumValueDataLength.Should().Be(0);
        key.NkRecord.MaximumValueNameLength.Should().Be(0);
        key.NkRecord.NameLength.Should().Be(7);
        key.NkRecord.MaximumNameLength.Should().Be(0xE);
        key.NkRecord.ParentCellIndex.Should().Be(0xB0);
        key.NkRecord.SecurityCellIndex.Should().Be(0x108);
        key.NkRecord.SubkeyCountsStable.Should().Be(0x2);
        key.NkRecord.SubkeyListsStableCellIndex.Should().Be(0x4580);
        key.NkRecord.SubkeyCountsVolatile.Should().Be(0);
        key.NkRecord.UserFlags.Should().Be(NkCellRecord.UserFlag.None);
        key.NkRecord.VirtualControlFlags.Should().Be(NkCellRecord.VirtualizationControlFlag.None);
        key.NkRecord.Access.Should().Be(NkCellRecord.AccessFlag.PreInitAccess);
        key.NkRecord.WorkVar.Should().Be(0);
        key.NkRecord.ValueListCount.Should().Be(1);
        key.NkRecord.ValueListCellIndex.Should().Be(0x1f0);
        key.NkRecord.Padding.Length.Should().Be(1);

        //Key flags: HasActiveParent
        //
        //Flags: CompressedName
    }
}