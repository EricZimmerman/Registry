using FluentAssertions;
using NUnit.Framework;
using Registry.Cells;

namespace Registry.Test;

[TestFixture]
internal class TestSkCellRecord
{
    [Test]
    public void SkRecordxAclNoDataForAceRecordsInSacl()
    {
        var ntUserSlack = new RegistryHive(TestHives.Path(@"NTUSER slack.DAT"));
        ntUserSlack.FlushRecordListsAfterParse = false;
        ntUserSlack.ParseHive();

        var sk = ntUserSlack.CellRecords[0x80] as SkCellRecord;

        sk.Should().NotBeNull();

        sk.SecurityDescriptor.Dacl.Should().NotBeNull();
        sk.SecurityDescriptor.Sacl.Should().NotBeNull();
        sk.SecurityDescriptor.Dacl.AceRecords.Should().NotBeNull();
        sk.SecurityDescriptor.Dacl.AceRecords.Count.Should().Be(sk.SecurityDescriptor.Dacl.AceCount);
        sk.SecurityDescriptor.Dacl.AceRecords.ToString().Should().NotBeEmpty();
        sk.SecurityDescriptor.Sacl.AceRecords.Should().NotBeNull();
        sk.SecurityDescriptor.Sacl.AceRecords.Count.Should().Be(0);
        // this is a strange case where there is no data to build ace records
        sk.SecurityDescriptor.Sacl.AceRecords.ToString().Should().NotBeEmpty();

        sk.ToString().Should().NotBeEmpty();
    }

    [Test]
    public void VerifySkInfo()
    {
        var sam = new RegistryHive(TestHives.Path(@"SAM"));
        sam.FlushRecordListsAfterParse = false;
        sam.ParseHive();

        var key = sam.GetKey(@"SAM\Domains\Account");

        key.Should().NotBeNull();

        var sk = sam.CellRecords[key.NkRecord.SecurityCellIndex] as SkCellRecord;

        sk.Should().NotBeNull();
        sk.ToString().Should().NotBeEmpty();
        sk.Size.Should().BeGreaterThan(0);
        ((object) sk.Reserved).Should().BeOfType<ushort>();

        sk.DescriptorLength.Should().BeGreaterThan(0);
    }
}