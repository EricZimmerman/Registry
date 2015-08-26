using NFluent;
using NUnit.Framework;
using Registry.Cells;

namespace Registry.Test
{
    [TestFixture]
    internal class TestNKCellRecord
    {
        [Test]
        public void ShouldHavePaddingLengthOfZeroWhenRecordIsFree()
        {
            var key = TestSetup.Bcd.GetKey(0x10e8);

            Check.That(key).IsNotNull();
            Check.That(key.NKRecord.Padding.Length).IsEqualTo(0);
        }

        [Test]
        public void ShouldHaveUnableToDetermineName()
        {
            var key = TestSetup.UsrClassBeef.CellRecords[0x783CD8] as NKCellRecord;

            Check.That(key).IsNotNull();

            Check.That(key.Padding.Length).IsEqualTo(0);
            Check.That(key.Name).IsEqualTo("(Unable to determine name)");
        }

        [Test]
        public void ShouldVerifyNkRecordProperties()
        {
            var key =
                TestSetup.Sam.GetKey(0x418);

            Check.That(key).IsNotNull();

            Check.That(key.NKRecord.Padding.Length).IsGreaterThan(0);
            Check.That(key.NKRecord.ToString()).IsNotEmpty();
            Check.That(key.NKRecord.SecurityCellIndex).IsGreaterThan(0);
            Check.That(key.NKRecord.SubkeyListsVolatileCellIndex).IsEqualTo((uint) 0);

            Check.That(key.KeyName).IsEqualTo("Domains");
            Check.That(key.KeyPath).IsEqualTo(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains");
            Check.That(key.LastWriteTime.ToString()).IsEqualTo("7/3/2014 6:05:37 PM +00:00");
            Check.That(key.NKRecord.Size).IsEqualTo(0x58);
            Check.That(key.NKRecord.RelativeOffset).IsEqualTo(0x418);
            Check.That(key.NKRecord.AbsoluteOffset).IsEqualTo(0x1418);
            Check.That(key.NKRecord.Signature).IsEqualTo("nk");
            Check.That(key.NKRecord.IsFree).IsFalse();
            Check.That(key.NKRecord.Debug).IsEqualTo((byte) 0);
            Check.That(key.NKRecord.MaximumClassLength).IsEqualTo((uint) 0);
            Check.That(key.NKRecord.ClassCellIndex).IsEqualTo((uint) 0);
            Check.That(key.NKRecord.ClassLength).IsEqualTo((ushort) 0);
            Check.That(key.NKRecord.MaximumValueDataLength).IsEqualTo((uint) 0);
            Check.That(key.NKRecord.MaximumValueNameLength).IsEqualTo((uint) 0);
            Check.That(key.NKRecord.NameLength).IsEqualTo((ushort) 7);
            Check.That(key.NKRecord.MaximumNameLength).IsEqualTo((ushort) 0xE);
            Check.That(key.NKRecord.ParentCellIndex).IsEqualTo((uint) 0xB0);
            Check.That(key.NKRecord.SecurityCellIndex).IsEqualTo((uint) 0x108);
            Check.That(key.NKRecord.SubkeyCountsStable).IsEqualTo((uint) 0x2);
            Check.That(key.NKRecord.SubkeyListsStableCellIndex).IsEqualTo((uint) 0x4580);
            Check.That(key.NKRecord.SubkeyCountsVolatile).IsEqualTo((uint) 0);
            Check.That(key.NKRecord.UserFlags).IsEqualTo(0);
            Check.That(key.NKRecord.VirtualControlFlags).IsEqualTo(0);
            Check.That(key.NKRecord.WorkVar).IsEqualTo((uint) 0);
            Check.That(key.NKRecord.ValueListCount).IsEqualTo((uint) 1);
            Check.That(key.NKRecord.ValueListCellIndex).IsEqualTo((uint) 0x1f0);
            Check.That(key.NKRecord.Padding.Length).IsEqualTo(1);

            //Key flags: HasActiveParent
            //
            //Flags: CompressedName
        }
    }
}