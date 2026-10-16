using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using static Registry.Test.LogBuilder;

namespace Registry.Test;

/// <summary>
///     Transaction log replay (LogReplay), on primaries and logs built in memory. The rules are those of the Windows
///     registry file format specification (msuhanov/regf); the corpus results were checked against yarp's recovered hives
/// </summary>
[TestFixture]
public class LogReplayTests
{
    private const int Bin = 4096;

    private static byte Page(byte[] hive, int bin) => hive[Bin + bin * Bin + 0x100];

    [Test]
    public void EntriesFromTheLogsSequenceNumberUntilAGap()
    {
        //primary: 2 bins, sequence numbers 10/9 (dirty)
        var primary = Primary(10, 9, 2);
        var log = NewLog(10, 2 * Bin,
            Entry(10, 2 * Bin, (0, HiveBin(0, Bin, 0xA1))),
            Entry(11, 2 * Bin, (Bin, HiveBin(Bin, Bin, 0xB2))),
            //a gap: 13 after 11, so neither this nor what follows is applied
            Entry(13, 2 * Bin, (0, HiveBin(0, Bin, 0xEE))));

        var r = LogReplay.Replay(primary, Logs(("TEST.LOG1", log)));

        r.Recovered.Should().BeTrue();
        r.LastSequenceNumber.Should().Be(11);
        Page(r.Bytes, 0).Should().Be(0xA1);
        Page(r.Bytes, 1).Should().Be(0xB2);
        BitConverter.ToUInt32(r.Bytes, 4).Should().Be(11);
        BitConverter.ToUInt32(r.Bytes, 8).Should().Be(11);
        BitConverter.ToUInt32(r.Bytes, 508).Should().Be(LogReplay.Checksum(r.Bytes));
        //the primary is not changed
        Page(primary, 0).Should().Be(1);
    }

    [Test]
    public void OlderEntriesAfterTheCurrentOnesAreNotApplied()
    {
        //a reused log: entries 20 and 21, then leftovers of 5 and 6 (the LogExtend Amcache had this, and replaying the
        //leftovers too wrote old pages over new ones)
        var primary = Primary(20, 19, 1);
        var log = NewLog(20, Bin,
            Entry(20, Bin, (0, HiveBin(0, Bin, 0x20))),
            Entry(21, Bin, (0, HiveBin(0, Bin, 0x21))),
            Entry(5, Bin, (0, HiveBin(0, Bin, 0x05))),
            Entry(6, Bin, (0, HiveBin(0, Bin, 0x06))));

        var r = LogReplay.Replay(primary, Logs(("TEST.LOG1", log)));

        Page(r.Bytes, 0).Should().Be(0x21);
        r.LastSequenceNumber.Should().Be(21);
    }

    [Test]
    public void TheHiveGrowsToTheEntriesHiveBinsDataSize()
    {
        //issue #24: an entry with a bigger hive bins data size, writing a bin past the end of the primary
        var primary = Primary(3, 2, 1);
        var log = NewLog(3, Bin, Entry(3, 3 * Bin, (2 * Bin, HiveBin(2 * Bin, Bin, 0xC3))));

        var r = LogReplay.Replay(primary, Logs(("TEST.LOG1", log)));

        r.Bytes.Length.Should().Be(Bin + 3 * Bin);
        Page(r.Bytes, 2).Should().Be(0xC3);
        //the bin in between (never written) is zeros
        Page(r.Bytes, 1).Should().Be(0);
        BitConverter.ToUInt32(r.Bytes, 40).Should().Be(3 * Bin);
    }

    [Test]
    public void BothLogsTheOneWithEarlierEntriesFirst()
    {
        var primary = Primary(30, 29, 1);
        //LOG2 has the earlier entries; LOG1 continues them
        var log1 = NewLog(32, Bin, Entry(32, Bin, (0, HiveBin(0, Bin, 0x32))));
        var log2 = NewLog(30, Bin, Entry(30, Bin, (0, HiveBin(0, Bin, 0x30))), Entry(31, Bin, (0, HiveBin(0, Bin, 0x31))));

        var r = LogReplay.Replay(primary, Logs(("TEST.LOG1", log1), ("TEST.LOG2", log2)));

        r.LogsApplied.Select(Path.GetFileName).Should().Equal("TEST.LOG2", "TEST.LOG1");
        r.LastSequenceNumber.Should().Be(32);
        Page(r.Bytes, 0).Should().Be(0x32);
    }

    [Test]
    public void TheSecondLogOnlyWhenItContinuesTheFirst()
    {
        var primary = Primary(30, 29, 1);
        var log1 = NewLog(30, Bin, Entry(30, Bin, (0, HiveBin(0, Bin, 0x30))));
        //starts at 40, not 31
        var log2 = NewLog(40, Bin, Entry(40, Bin, (0, HiveBin(0, Bin, 0x40))));

        var r = LogReplay.Replay(primary, Logs(("TEST.LOG1", log1), ("TEST.LOG2", log2)));

        r.LogsApplied.Select(Path.GetFileName).Should().Equal("TEST.LOG1");
        Page(r.Bytes, 0).Should().Be(0x30);
    }

    [Test]
    public void ALogOlderThanTheHiveIsNotApplied()
    {
        var primary = Primary(50, 49, 1);
        var log = NewLog(40, Bin, Entry(40, Bin, (0, HiveBin(0, Bin, 0x40))));

        var r = LogReplay.Replay(primary, Logs(("TEST.LOG1", log)));

        r.Recovered.Should().BeFalse();
        r.Bytes.Should().BeSameAs(primary);
    }

    [Test]
    public void AnEntryWithBadHashesEndsTheLog()
    {
        var primary = Primary(7, 6, 1);
        var bad = Entry(8, Bin, (0, HiveBin(0, Bin, 0x08)));
        bad[bad.Length - 1] ^= 0xFF;
        var log = NewLog(7, Bin, Entry(7, Bin, (0, HiveBin(0, Bin, 0x07))), bad, Entry(9, Bin, (0, HiveBin(0, Bin, 0x09))));

        var r = LogReplay.Replay(primary, Logs(("TEST.LOG1", log)));

        r.LastSequenceNumber.Should().Be(7);
        Page(r.Bytes, 0).Should().Be(0x07);
    }

    [Test]
    public void AnInvalidBaseBlockIsRecoveredFromTheLatestLog()
    {
        var primary = Primary(60, 60, 1, validChecksum: false);
        var log1 = NewLog(61, Bin, Entry(61, Bin, (0, HiveBin(0, Bin, 0x61))));
        var log2 = NewLog(60, Bin, Entry(60, Bin, (0, HiveBin(0, Bin, 0x60))));

        var r = LogReplay.Replay(primary, Logs(("TEST.LOG1", log1), ("TEST.LOG2", log2)));

        //only the log with the latest entries, as Windows does
        r.LogsApplied.Select(Path.GetFileName).Should().Equal("TEST.LOG1");
        Page(r.Bytes, 0).Should().Be(0x61);
        BitConverter.ToUInt32(r.Bytes, 508).Should().Be(LogReplay.Checksum(r.Bytes));
        BitConverter.ToUInt32(r.Bytes, 28).Should().Be(0, "the base block from the log is a primary file's again");
    }

    [Test]
    public void OldFormatLog()
    {
        var primary = Primary(2, 1, 2);
        var page = Fill(512, 0x77);
        var log = OldLog(1, 2 * Bin, 0x01D0000000000000, (1, page), (8, Fill(512, 0x88)));

        var r = LogReplay.Replay(primary, Logs(("TEST.LOG", log)));

        r.Recovered.Should().BeTrue();
        r.Bytes.Skip(Bin + 512).Take(512).Should().Equal(page);
        r.Bytes[Bin + 8 * 512].Should().Be(0x88);
        r.Bytes[Bin].Should().Be((byte)'h', "pages that aren't in the log are not changed");
    }

    [Test]
    public void ACleanHiveIsNotChanged()
    {
        var primary = Primary(4, 4, 1);
        var r = LogReplay.Replay(primary, Logs(("TEST.LOG1", NewLog(4, Bin, Entry(4, Bin, (0, HiveBin(0, Bin, 0x44)))))));

        r.Recovered.Should().BeFalse();
        r.Bytes.Should().BeSameAs(primary);
    }

    [Test]
    public void UpdateHiveBytesUsesTheSameRules()
    {
        var primary = Primary(10, 9, 1);
        var log = new TransactionLog(NewLog(10, 2 * Bin,
            Entry(10, 2 * Bin, (Bin, HiveBin(Bin, Bin, 0xA0))),
            Entry(3, 2 * Bin, (0, HiveBin(0, Bin, 0x03)))), "TEST.LOG1");
        log.ParseLog();

        var updated = log.UpdateHiveBytes(primary);

        updated.Length.Should().Be(Bin + 2 * Bin);
        Page(updated, 1).Should().Be(0xA0);
        Page(updated, 0).Should().Be(1, "the leftover entry 3 is not applied");
        log.NewSequenceNumber.Should().Be(10);
        log.TransactionLogEntries.Should().HaveCount(2);
    }

    [Test]
    public void ParseLogStopsAtAnEntryThatCantBeRead()
    {
        //an entry with size 0 used to loop forever
        var log = NewLog(1, Bin, Entry(1, Bin, (0, HiveBin(0, Bin, 1))));
        var withZero = log.Concat(new byte[] { (byte)'H', (byte)'v', (byte)'L', (byte)'E' }).Concat(new byte[508]).ToArray();

        var t = new TransactionLog(withZero, "TEST.LOG1");
        t.ParseLog().Should().BeTrue();
        t.TransactionLogEntries.Should().HaveCount(1);
    }

    [Test]
    public void LogExtendAmcache()
    {
        //issue #24's error, on a real Amcache: replayed as before, its LOG2 ends with entries older than the hive's,
        //which were applied over newer data, and the hive could not be parsed
        var path = TestHives.Corpus(@"!LogExtend\Amcache.hve");
        var hive = new RegistryHive(path);
        hive.ProcessTransactionLogs(new[] { path + ".LOG1", path + ".LOG2" }.ToList(), true);
        hive.ParseHive();

        hive.Header.PrimarySequenceNumber.Should().Be(hive.Header.SecondarySequenceNumber);
        hive.Root.Should().NotBeNull();
        var keys = 0;
        var values = 0;
        void Walk(Abstractions.RegistryKey k)
        {
            keys++;
            values += k.Values.Count;
            foreach (var s in k.SubKeys) Walk(s);
        }

        Walk(hive.Root);
        //as yarp gives it (its recovered hive is byte for byte the same)
        keys.Should().Be(2291);
        values.Should().Be(35636);
    }
}
