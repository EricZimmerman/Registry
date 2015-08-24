using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NFluent;
using NUnit.Framework;

namespace Registry.Test
{
    class TestRegistrySkeleton
    {

        [Test]
        public void ShouldCreateRegistrySkeleton()
        {
            var rs = new RegistrySkeleton(TestSetup.UsrclassDeleted);

            Check.That(rs).IsNotNull();
        }

        [Test]
        public void ShouldThrowNullRefExtensionOnNullHive()
        {
            Check.ThatCode(() => { var rs = new RegistrySkeleton(null); }).Throws<NullReferenceException>();
        }

        [Test]
        public void ShouldReturnTrueOnAddMuiCacheSubkeyToSkeletonList()
        {
            var rs = new RegistrySkeleton(TestSetup.UsrclassDeleted);

            var sk = new SkeletonKey(@"Local Settings\MuiCache\6\52C64B7E",true);
            
            var added= rs.AddEntry(sk);

            Check.That(added).IsTrue();
            Check.That(rs.Keys.Count).IsEqualTo(1);

            sk = new SkeletonKey(@"path\does\not\exist",false);

            added = rs.AddEntry(sk);

            Check.That(added).IsFalse();
            Check.That(rs.Keys.Count).IsEqualTo(1);
        }

        [Test]
        public void ShouldReturnFalseOnRemovingNonExistentKey()
        {
            var rs = new RegistrySkeleton(TestSetup.UsrclassDeleted);

            var sk = new SkeletonKey(@"path\does\not\exist", false);

            var added = rs.RemoveEntry(sk);

            Check.That(added).IsFalse();
            Check.That(rs.Keys.Count).IsEqualTo(0);
        }

        [Test]
        public void ShouldntAddDuplicateSkeletonKeys()
        {
            var rs = new RegistrySkeleton(TestSetup.UsrclassDeleted);

            var sk = new SkeletonKey(@"Local Settings\MuiCache\6\52C64B7E", true);

            var added = rs.AddEntry(sk);

            Check.That(added).IsTrue();
            Check.That(rs.Keys.Count).IsEqualTo(1);

            sk = new SkeletonKey(@"path\does\not\exist", false);

            added = rs.AddEntry(sk);

            Check.That(added).IsFalse();
            Check.That(rs.Keys.Count).IsEqualTo(1);

            var sk1 = new SkeletonKey(@"Local Settings\MuiCache\6\52C64B7E", true);

            added = rs.AddEntry(sk1);

            Check.That(added).IsTrue();
            Check.That(rs.Keys.Count).IsEqualTo(1);
        }

        [Test]
        public void KeysCountShouldBeZeroAfterAddRemove()
        {
            var rs = new RegistrySkeleton(TestSetup.UsrclassDeleted);

            var sk = new SkeletonKey(@"Local Settings\MuiCache\6\52C64B7E", true);

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
            var rs = new RegistrySkeleton(TestSetup.UsrclassDeleted);

            var sk = new SkeletonKey(@"path\does\not\exist", false);

            var added = rs.AddEntry(sk);

            Check.That(added).IsFalse();
            Check.That(rs.Keys.Count).IsEqualTo(0);
        }

        [Test]
        public void ShouldThrowExceptionIfWriteCalledWithNoKeysAdded()
        {
            Check.ThatCode(() => { var rs = new RegistrySkeleton(TestSetup.UsrclassDeleted);
                                     rs.Write(@"C:\temp\foo.bin");
            }).Throws<InvalidOperationException>();//ncrunch: no coverage
        }

        [Test]
        public void ShouldReturnTrueWhenWriteCalledWithKeyAdded()
        {
            var rs = new RegistrySkeleton(TestSetup.UsrclassDeleted);

            var sk = new SkeletonKey(@"Local Settings\MuiCache\6\52C64B7E", true);

            rs.AddEntry(sk);

            var write = rs.Write(@"C:\temp\foo.bin");

            Check.That(write).IsTrue();

        }

        [Test]
        public void WrittenHiveShouldContain163ValuesInMuiCacheSubkey()
        {
            var rs = new RegistrySkeleton(TestSetup.UsrclassDeleted);

            var sk = new SkeletonKey(@"Local Settings\MuiCache\6\52C64B7E", true);

            rs.AddEntry(sk);

            var outPath = @"C:\temp\foo.bin";
            
            var write = rs.Write(outPath);

            Check.That(write).IsTrue();

            var newReg = new RegistryHive(outPath);
            newReg.ParseHive();

            var key = newReg.GetKey(@"Local Settings\MuiCache\6\52C64B7E");

            Check.That(key).IsNotNull();
            
            Check.That(key.LastWriteTime.Value.Year).IsEqualTo(2015);
            Check.That(key.LastWriteTime.Value.Month).IsEqualTo(2);
            Check.That(key.LastWriteTime.Value.Day).IsEqualTo(1);
            Check.That(key.LastWriteTime.Value.Hour).IsEqualTo(7);
            Check.That(key.LastWriteTime.Value.Minute).IsEqualTo(15);
            Check.That(key.LastWriteTime.Value.Second).IsEqualTo(5);

            Check.That(key.Values.Count).IsEqualTo(163);

        }

        //        [Test]
        //        public void ShouldGenerateValidRegMultiSzValue()
        //        {
        //            //S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\MuiCache\6\52C64B7E
        //            var key = TestSetup.UsrclassDeleted.GetKey(@"Local Settings\MuiCache\6\52C64B7E");
        //
        //            var val = key.Values.Single(t => t.ValueName == "LanguageList");
        //
        //            Check.That(val).IsNotNull();
        //
        //            Check.That(val.ValueName).IsEqualTo("LanguageList");
        //            Check.That(val.ValueData).IsEqualTo("en-US en");
        //
        //            //This is what the record looks like from the hive itself
        //            var vkHash = GetSha256(val.VKRecord.RawBytes);
        //            Check.That(vkHash).IsEqualTo("6EF8A18565ABD7B6AB7950B0ECD8CA1BFFADF28D4E276DCE237727B08184A887");
        //
        //            //this is what the value data (and slack) looks like from the hive itself
        //            var valData = val.ValueDataRaw.Concat(val.ValueSlackRaw).ToArray();
        //            var dataHash = GetSha256(valData);
        //
        //
        //            Check.That(dataHash).IsEqualTo("654827CC02A024DF9FFFCFF57E51D132597749600FF1B93E3ECC7A39D1760B80");
        //
        //            var dataRecordSize = -1 * valData.Length - 4; //'add' 4 for the size prefix
        //
        //            var sizeB = new byte[] { 0xe8, 0xff, 0xff, 0xff };
        //            Check.That(dataRecordSize).IsEqualTo(BitConverter.ToInt32(sizeB, 0));
        //
        //            byte[] intBytes = BitConverter.GetBytes(dataRecordSize);
        //
        //            Check.That(intBytes[0]).Equals((byte)0xe8);
        //            Check.That(intBytes[1]).Equals((byte)0xFf);
        //            Check.That(intBytes[2]).Equals((byte)0xFf);
        //            Check.That(intBytes[3]).Equals((byte)0xFf);
        //
        //            //so when we get the bytes for the record and data, it should be the same
        //
        //
        //
        //        }


        //        private string GetSha256(byte[] contents)
        //        {
        //            var hash = new StringBuilder();
        //
        //            using (var shaM = SHA256.Create())
        //            {
        //                var hashRaw = shaM.ComputeHash(contents);
        //
        //                foreach (var t in hashRaw)
        //                {
        //                    hash.Append(t.ToString("X2"));
        //                }
        //            }
        //
        //            return hash.ToString();
        //        }
    }
}
