using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Registry.Abstractions;
using Registry.Cells;
using Registry.Lists;

namespace Registry.Test;

/// <summary>Damaged hives (changed copies of the test hives): they are parsed as far as they can be. Add regression tests here</summary>
[TestFixture]
public class RobustnessTests
{
    private static RegistryHive Parse(byte[] bytes, bool deleted = false)
    {
        var hive = new RegistryHive(bytes, "test") { RecoverDeleted = deleted, FlushRecordListsAfterParse = false };
        hive.ParseHive();
        return hive;
    }

    private static List<RegistryKey> All(RegistryKey root)
    {
        var all = new List<RegistryKey>();
        void Walk(RegistryKey k)
        {
            all.Add(k);
            foreach (var s in k.SubKeys) Walk(s);
        }

        Walk(root);
        return all;
    }

    private static byte[] Sam() => File.ReadAllBytes(TestHives.Path("SAM"));

    [Test]
    public void TruncatedHive()
    {
        var full = All(Parse(Sam()).Root).Count;
        var cut = Sam().Take(4096 + 3 * 4096 + 1234).ToArray();

        var hive = Parse(cut);

        All(hive.Root).Count.Should().BeInRange(1, full - 1);
    }

    [Test]
    public void ASubkeyListPointingBackAtTheRootDoesNotRecurseForever()
    {
        var bytes = Sam();
        var hive = Parse(bytes);
        var rootOffset = hive.Root.NkRecord.RelativeOffset;
        //the list of a key below the root: make its first entry the root
        var child = hive.Root.SubKeys.First();
        var listOffset = child.NkRecord.SubkeyListsStableCellIndex;
        var list = hive.ListRecords[listOffset];
        list.Signature.Should().BeOneOf("lf", "lh");
        BitConverter.GetBytes((uint)rootOffset).CopyTo(bytes, 4096 + listOffset + 8);

        var damaged = Parse(bytes);

        All(damaged.Root).Count(k => k.NkRecord.RelativeOffset == rootOffset).Should().Be(1);
        damaged.Root.SubKeys.First().SubKeys.Should().NotContain(k => k.NkRecord.RelativeOffset == rootOffset);
    }

    [Test]
    public void AKeyInTwoListsIsReadOnce()
    {
        var bytes = Sam();
        var hive = Parse(bytes);
        //the root's first subkey, listed again in a list further down
        var first = hive.Root.SubKeys.First().NkRecord.RelativeOffset;
        var other = All(hive.Root).Skip(1).First(k => k.NkRecord.SubkeyListsStableCellIndex != 0xFFFFFFFF && k.SubKeys.Count > 0 && k != hive.Root.SubKeys.First());
        var listOffset = other.NkRecord.SubkeyListsStableCellIndex;
        BitConverter.GetBytes((uint)first).CopyTo(bytes, 4096 + listOffset + 8);

        var damaged = Parse(bytes);

        All(damaged.Root).Count(k => k.NkRecord.RelativeOffset == first).Should().Be(1);
    }

    [Test]
    public void OneByteNamesAreLatin1()
    {
        //a compressed name has one byte per character, which Windows widens: 0x9F is U+009F, not Windows-1252's Ÿ
        var bytes = Sam();
        var hive = Parse(bytes);
        var key = hive.Root.SubKeys.First();
        ((key.NkRecord.Flags & NkCellRecord.FlagEnum.CompressedName) == NkCellRecord.FlagEnum.CompressedName).Should().BeTrue();
        bytes[key.NkRecord.AbsoluteOffset + 0x50] = 0x9F;

        Parse(bytes).Root.SubKeys.First().KeyName[0].Should().Be('\u009F');
    }

    [Test]
    public void InvalidCellAndBinSizes()
    {
        var bytes = Sam();
        var hive = Parse(bytes);
        var keys = All(hive.Root).Count;
        //a cell with size 0 in the second bin, and a bin size that is not possible further on
        var second = 4096 + 4096;
        BitConverter.GetBytes(0).CopyTo(bytes, second + 0x20);
        BitConverter.GetBytes(0x10).CopyTo(bytes, second + 4096 + 8);

        var damaged = Parse(bytes);

        All(damaged.Root).Count.Should().BeInRange(1, keys);
    }

    [TestCase("SAM")]
    [TestCase("NTUSER1.DAT")]
    [TestCase("UsrClassDeletedBags.dat")]
    [TestCase("BCD")]
    public void RandomDamageIsHandled(string name)
    {
        var original = File.ReadAllBytes(TestHives.Path(name));
        var random = new Random(int.TryParse(Environment.GetEnvironmentVariable("REGISTRY_FUZZ_SEED"), out var seed) ? seed : 20261016);
        var unexpected = new List<string>();
        var rounds = int.TryParse(Environment.GetEnvironmentVariable("REGISTRY_FUZZ_ROUNDS"), out var r) ? r : 100;
        for (var round = 0; round < rounds; round++)
        {
            var b = (byte[])original.Clone();
            for (var i = 0; i < 40; i++) b[random.Next(4096, b.Length)] = (byte)random.Next(256);
            if (round % 4 == 0) b = b.Take(random.Next(4096 + 64, b.Length)).ToArray();

            var task = Task.Run(() =>
            {
                try
                {
                    var hive = Parse(b, round % 2 == 0);
                    All(hive.Root).SelectMany(k => k.Values).ToList().ForEach(v => _ = v.ValueData);
                }
                catch (KeyNotFoundException e) when (e.Message == "Root nk record not found!")
                {
                    //what a hive without a root gives
                }
                catch (Exception e)
                {
                    lock (unexpected) unexpected.Add($"round {round}: {e.GetType().Name}: {e.Message} at {string.Join(" < ", (e.StackTrace ?? "").Split('\n').Take(4).Select(t => t.Trim()))}");
                }
            });
            task.Wait(TimeSpan.FromSeconds(30)).Should().BeTrue($"round {round} should finish");
        }

        unexpected.Should().BeEmpty();
    }

    [Test]
    public void DeletedKeyInTheSlackOfASubkeyList()
    {
        //issue #20: the Lone Wolf scenario's SYSTEM hive (without its logs): a deleted key whose remnant goes past the
        //end of the subkey list it is in the slack of
        var hive = new RegistryHive(TestHives.Corpus(@"ExpandoTestHives\SYSTEM_loneWolf")) { RecoverDeleted = true };
        hive.ParseHive();

        var deleted = hive.DeletedRegistryKeys.SelectMany(All).Select(k => k.KeyName).ToList();
        deleted.Should().Contain("____CL_ATA_FLASH_CARD_LEXAR__-HURRIC_-DC37");
    }
}
