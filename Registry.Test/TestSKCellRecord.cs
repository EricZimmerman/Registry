using NFluent;
using NUnit.Framework;
using Registry.Cells;

namespace Registry.Test
{
    [TestFixture]
    internal class TestSKCellRecord
    {

        [Test]
        public void SKRecordxACLNoDataForAceRecordsInSacl()
        {
            var sk = TestSetup.NtUserSlack.CellRecords[0x80] as SKCellRecord;

            Check.That(sk).IsNotNull();

            Check.That(sk.SecurityDescriptor.DACL).IsNotNull();
            Check.That(sk.SecurityDescriptor.SACL).IsNotNull();
            Check.That(sk.SecurityDescriptor.DACL.ACERecords).IsNotNull();
            Check.That(sk.SecurityDescriptor.DACL.ACERecords.Count).IsEqualTo(sk.SecurityDescriptor.DACL.AceCount);
            Check.That(sk.SecurityDescriptor.DACL.ACERecords.ToString()).IsNotEmpty();
            Check.That(sk.SecurityDescriptor.SACL.ACERecords).IsNotNull();
            Check.That(sk.SecurityDescriptor.SACL.ACERecords.Count).IsEqualTo(0);
                // this is a strange case where there is no data to build ace records
            Check.That(sk.SecurityDescriptor.SACL.ACERecords.ToString()).IsNotEmpty();

            Check.That(sk.ToString()).IsNotEmpty();
        }

        [Test]
        public void VerifySKInfo()
        {
            var key = TestSetup.Sam.GetKey(@"SAM\Domains\Account");

            Check.That(key).IsNotNull();

            var sk = TestSetup.Sam.CellRecords[key.NKRecord.SecurityCellIndex] as SKCellRecord;

            Check.That(sk).IsNotNull();
            Check.That(sk.ToString()).IsNotEmpty();
            Check.That(sk.Size).IsGreaterThan(0);
            Check.That(sk.Reserved).IsInstanceOf<ushort>();

            Check.That(sk.DescriptorLength).IsGreaterThan(0);
        }
    }
}