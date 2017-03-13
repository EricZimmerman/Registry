using System;
using System.IO;
using System.Linq;
using NLog;
using Registry.Other;
using static Registry.Other.Helpers;

namespace Registry
{
    public class RegistryBase : IRegistry
    {
        internal readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public RegistryBase()
        {
            throw new NotSupportedException("Call the other constructor and pass in the path to the Registry hive!");
        }

        public RegistryBase(byte[] rawBytes)
        {
            FileBytes = rawBytes;
            HivePath = "None";

            _logger = LogManager.GetLogger("rawBytes");

            if (!HasValidSignature())
            {
                _logger.Error("Data in byte array is not a Registry hive (bad signature)");

                throw new ArgumentException("Data in byte array is not a Registry hive (bad signature)");
            }

            Initialize();
        }

        public RegistryBase(string hivePath)
        {
            if (hivePath == null)
            {
                throw new ArgumentNullException("hivePath cannot be null");
            }

            if (!File.Exists(hivePath))
            {
                throw new FileNotFoundException();
            }

            var fileStream = new FileStream(hivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var binaryReader = new BinaryReader(fileStream);

            binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);

            FileBytes = binaryReader.ReadBytes((int) binaryReader.BaseStream.Length);

            binaryReader.Close();
            fileStream.Close();

            _logger = LogManager.GetLogger(hivePath);

            if (!HasValidSignature())
            {
                _logger.Error("'{0}' is not a Registry hive (bad signature)", hivePath);

                throw new Exception($"'{hivePath}' is not a Registry hive (bad signature)");
            }

            HivePath = hivePath;

            _logger.Debug("Set HivePath to {0}", hivePath);

            Initialize();
        }

        public long TotalBytesRead { get; internal set; }

        public byte[] FileBytes { get; }

        public HiveTypeEnum HiveType { get; private set; }

        public string HivePath { get; }

        public RegistryHeader Header { get; set; }

        public byte[] ReadBytesFromHive(long offset, int length)
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

        private void Initialize()
        {
            var header = ReadBytesFromHive(0, 4096);

            _logger.Debug("Getting header");

            Header = new RegistryHeader(header);

            _logger.Debug("Got header. Embedded file name {0}", Header.FileName);

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
            _logger.Debug("Hive is a {0} hive", HiveType);

            var version = $"{Header.MajorVersion}.{Header.MinorVersion}";

            _logger.Debug("Hive version is {0}", version);
        }

        public bool HasValidSignature()
        {
            var sig = BitConverter.ToInt32(FileBytes, 0);

            return sig.Equals(RegfSignature);
        }
    }
}