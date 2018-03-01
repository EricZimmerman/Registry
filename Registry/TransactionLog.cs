using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Registry.Other;

namespace Registry
{
   public class TransactionLog
    {
         const int RegfSignature = 0x66676572;
        internal readonly Logger Logger;

        public byte[] FileBytes { get; }

        public string LogPath { get; }

        public TransactionLog(byte[] rawBytes)
        {
            FileBytes = rawBytes;
            LogPath = "None";

            Logger = LogManager.GetLogger("rawBytes");

            if (!HasValidSignature())
            {
                Logger.Error("Data in byte array is not a Registry transaction log (bad signature)");

                throw new ArgumentException("Data in byte array is not a Registry transaction log (bad signature)");
            }

            Initialize();
        }

        private byte[] ReadBytesFromHive(long offset, int length)
        {
            var readLength = Math.Abs(length);
            
            var remaining = FileBytes.Length - offset;

            if (remaining <= 0)
            {
                return new byte[0];
            }

            if (readLength > remaining)
            {
                readLength = (int) remaining;
            }

            var r = new ArraySegment<byte>(FileBytes, (int) offset, readLength);

            return r.ToArray();
        }

        public RegistryHeader Header { get; set; }
        public HiveTypeEnum HiveType { get; private set; }

        private void Initialize()
        {
            var header = ReadBytesFromHive(0, 4096);

            Logger.Debug("Getting header");

            Header = new RegistryHeader(header);

            Logger.Debug("Got header. Embedded file name {0}", Header.FileName);

            var fNameBase = Path.GetFileName(Header.FileName).ToLower();

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
                default:
                    HiveType = HiveTypeEnum.Other;
                    break;
            }

            Logger.Debug("Hive is a {0} hive", HiveType);

            var version = $"{Header.MajorVersion}.{Header.MinorVersion}";

            Logger.Debug("Hive version is {0}", version);
        }

        public TransactionLog(string logFile)
        {
            if (logFile == null)
            {
                throw new ArgumentNullException(nameof(logFile));
            }

            if (!File.Exists(logFile))
            {
                throw new FileNotFoundException();
            }

            

            var fileStream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var binaryReader = new BinaryReader(fileStream);

            binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);

            FileBytes = binaryReader.ReadBytes((int) binaryReader.BaseStream.Length);

            binaryReader.Close();
            fileStream.Close();

            if (FileBytes.Length == 0)
            {
                throw new Exception("0 byte log file. Nothing to do");
            }


            Logger = LogManager.GetLogger(logFile);

            if (!HasValidSignature())
            {
                Logger.Error("'{0}' is not a Registry hive (bad signature)", logFile);

                throw new Exception($"'{logFile}' is not a Registry transaction log (bad signature)");
            }

            LogPath = logFile;

            TransactionLogEntries = new List<TransactionLogEntry>();

            Initialize();
        }

        private bool _parsed = false;
        public List<TransactionLogEntry> TransactionLogEntries { get; }

        public bool ParseLog()
        {
            if (_parsed)
            {
                throw new Exception("ParseLog already called");
            }

            var index = 0x200; //data starts at offset 500 decimal

            while (index<FileBytes.Length)
            {
                var size = BitConverter.ToInt32(FileBytes, index + 4);
                var buff = new byte[size];

                Buffer.BlockCopy(FileBytes,index,buff,0,size);

                var tle = new TransactionLogEntry(buff);
                TransactionLogEntries.Add(tle);

                index += size;
            }

            _parsed = true;
            
            return true;
        }

        public byte[] UpdateHiveBytes(byte[] hiveBytes, int startingSequenceNumber)
        {
            var baseOffset = 0x1000; //hbins start at 4096 bytes

            foreach (var transactionLogEntry in TransactionLogEntries)
            {
                if (transactionLogEntry.SequenceNumber < startingSequenceNumber)
                {
                    Logger.Warn($"Skipping transaction file '{LogPath}' since sequence number preceeds starting sequence number!");
                    continue;
                }

                Logger.Debug($"Processing log entry: {transactionLogEntry}");
                foreach (var dirtyPage in transactionLogEntry.DirtyPages)
                {
                    Logger.Debug($"Processing dirty page: {dirtyPage}");
                 
                    Buffer.BlockCopy(dirtyPage.PageBytes,0,hiveBytes,dirtyPage.Offset + baseOffset,dirtyPage.Size);

                }
            }


            return hiveBytes;
        }

        public bool HasValidSignature()
        {
            var sig = BitConverter.ToInt32(FileBytes, 0);

            return sig.Equals(RegfSignature);
        }
    }
}
