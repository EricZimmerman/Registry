using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Registry.Other;
using Serilog;

namespace Registry;

public class TransactionLog
{
    private const int RegfSignature = 0x66676572;

    private bool _parsed;

    public TransactionLog(byte[] rawBytes, string logFile)
    {
        FileBytes = rawBytes;
        LogPath = "None";

        if (!HasValidSignature())
        {
            Log.Error("Data in byte array is not a Registry transaction log (bad signature)");

            throw new ArgumentException("Data in byte array is not a Registry transaction log (bad signature)");
        }

        LogPath = logFile;

        TransactionLogEntries = new List<TransactionLogEntry>();

        Initialize();
    }

    public TransactionLog(string logFile)
    {
        if (logFile == null) throw new ArgumentNullException(nameof(logFile));

        if (!File.Exists(logFile)) throw new FileNotFoundException();

        var fileStream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        var binaryReader = new BinaryReader(fileStream);

        binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);

        FileBytes = binaryReader.ReadBytes((int) binaryReader.BaseStream.Length);

        binaryReader.Close();
        fileStream.Close();

        if (FileBytes.Length == 0) throw new Exception("0 byte log file. Nothing to do");


        if (!HasValidSignature())
        {
            Log.Error("{LogFile} is not a Registry transaction log (bad signature)", logFile);

            throw new Exception($"{logFile} is not a Registry transaction log (bad signature)");
        }

        LogPath = logFile;

        TransactionLogEntries = new List<TransactionLogEntry>();

        Initialize();
    }

    public byte[] FileBytes { get; }

    public string LogPath { get; }
    public string Version { get; private set; }

    public RegistryHeader Header { get; set; }
    public HiveTypeEnum HiveType { get; private set; }
    public List<TransactionLogEntry> TransactionLogEntries { get; }

    public int NewSequenceNumber { get; private set; }

    private byte[] ReadBytesFromHive(long offset, int length)
    {
        var readLength = Helpers.AbsSize(length);

        var remaining = FileBytes.Length - offset;

        if (remaining <= 0) return new byte[0];

        if (readLength > remaining) readLength = (int) remaining;

        return Helpers.Slice(FileBytes, (int) offset, readLength);
    }

    private void Initialize()
    {
        var header = ReadBytesFromHive(0, 4096);

        Header = new RegistryHeader(header);

        var fileNameSegs = Header.FileName.Split('\\');

        var fNameBase = fileNameSegs.Last().ToLowerInvariant();;
        
        Log.Debug("Got transaction log header. Embedded file name {FileName}. Base Name {Base}", Header.FileName,fNameBase);


        switch (fNameBase)
        {
            case "ntuser.dat":
                HiveType = HiveTypeEnum.NtUser;
                break;
            case "sam":
                HiveType = HiveTypeEnum.Sam;
                break;
            case "security":
                HiveType = HiveTypeEnum.Security;
                break;
            case "software":
                HiveType = HiveTypeEnum.Software;
                break;
            case "system":
                HiveType = HiveTypeEnum.System;
                break;
            case "drivers":
                HiveType = HiveTypeEnum.Drivers;
                break;
            case "usrclass.dat":
                HiveType = HiveTypeEnum.UsrClass;
                break;
            case "components":
                HiveType = HiveTypeEnum.Components;
                break;
            case "bcd":
                HiveType = HiveTypeEnum.Bcd;
                break;
            case "amcache.hve":
            case "amcache.hve.tmp":
                HiveType = HiveTypeEnum.Amcache;
                break;
            case "syscache.hve":
                HiveType = HiveTypeEnum.Syscache;
                break;
            case "elam": 
                HiveType = HiveTypeEnum.Elam;
                break;
            case "default": 
                HiveType = HiveTypeEnum.Default;
                break;
            case "vsmidk":
                HiveType = HiveTypeEnum.Vsmidk;
                break;
            case "bcd-template":
                HiveType = HiveTypeEnum.BcdTemplate;
                break;
            case "bbi": 
                HiveType = HiveTypeEnum.Bbi;
                break;
            case "userdiff": 
                HiveType = HiveTypeEnum.Userdiff;
                break;
            case "user.dat": 
                HiveType = HiveTypeEnum.User;
                break;
            case "userclasses.dat": 
                HiveType = HiveTypeEnum.UserClasses;
                break;
            case "settings.dat": 
                HiveType = HiveTypeEnum.settings;
                break;
            case "registry.dat": 
                HiveType = HiveTypeEnum.Registry;
                break;
            default:
                HiveType = HiveTypeEnum.Other;
                break;
        }

        //     Logger.Trace($"Hive is a {HiveType} hive");

        Version = $"{Header.MajorVersion}.{Header.MinorVersion}";

        //    Logger.Trace($"Hive version is {version}");
    }

    /// <summary>Reads the log entries (HvLE) into <see cref="TransactionLogEntries" />, up to the first one that can't be read</summary>
    public bool ParseLog()
    {
        if (_parsed) throw new Exception("ParseLog already called");

        var index = 0x200; //data starts at offset 512 decimal

        while (index + 40 <= FileBytes.Length)
        {
            var sig = Encoding.ASCII.GetString(FileBytes, index, 4);

            if (sig != "HvLE")
                //things arent always HvLE as logs get reused, so check to see if we have another valid header at our current offset
                break;

            var size = BitConverter.ToInt32(FileBytes, index + 4);
            if (size < 512 || size % 512 != 0 || index + (long) size > FileBytes.Length)
            {
                Log.Debug("Log entry at offset 0x{Index:X} has an invalid size (0x{Size:X}). Stopping", index, size);
                break;
            }

            var buff = new byte[size];

            Buffer.BlockCopy(FileBytes, index, buff, 0, size);

            try
            {
                TransactionLogEntries.Add(new TransactionLogEntry(buff));
            }
            catch (Exception e)
            {
                Log.Debug("Log entry at offset 0x{Index:X} could not be read: {Message}. Stopping", index, e.Message);
                break;
            }

            index += size;
        }

        _parsed = true;

        return true;
    }

    /// <summary>
    ///     Applies this log's entries to <paramref name="hiveBytes" /> from the sequence number in its base block on, until
    ///     one is invalid (hashes, size) or out of sequence; the hive grows to the hive bins data size of the entries
    /// </summary>
    /// <remarks>
    ///     This does not check whether the log should be applied to the hive (see
    ///     <see cref="RegistryHive.ProcessTransactionLogs(System.Collections.Generic.List{TransactionLogFileInfo},bool)" />)
    /// </remarks>
    /// <returns>The updated hive: <paramref name="hiveBytes" />, or a bigger copy when the hive had to grow</returns>
    public byte[] UpdateHiveBytes(byte[] hiveBytes)
    {
        var state = LogReplay.StateFor(hiveBytes);
        LogReplay.ApplyEntries(state, FileBytes, Header.PrimarySequenceNumber, LogPath);

        if (state.LastSequence.HasValue)
        {
            NewSequenceNumber = unchecked((int) state.LastSequence.Value);
        }

        return state.Bytes;
    }

    public bool HasValidSignature()
    {
        var sig = BitConverter.ToInt32(FileBytes, 0);

        return sig.Equals(RegfSignature);
    }

    public override string ToString()
    {
        var x = 0;
        var sb = new StringBuilder();
        sb.AppendLine();
        foreach (var transactionLogEntry in TransactionLogEntries)
        {
            sb.AppendLine($"LogEntry #{x} {transactionLogEntry}");
            x += 1;
        }

        return
            $"Log path: {LogPath} Valid checksum: {Header.ValidateCheckSum()} primary: 0x{Header.PrimarySequenceNumber:X} secondary: 0x{Header.SecondarySequenceNumber:X} Entries count: {TransactionLogEntries.Count:N0} Entry info: {sb}";
    }
}
