using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using Registry.Other;
using static Registry.Other.Helpers;

namespace Registry
{
    public class RegistryBase :IRegistry
    {
        private LoggingConfiguration _nlogConfig;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public RegistryBase()
        {
            throw new NotSupportedException("Call the other constructor and pass in the path to the Registry hive!");
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

            HivePath = hivePath;

            var fileStream = new FileStream(hivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var binaryReader = new BinaryReader(fileStream);

            binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);

            FileBytes = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);

            binaryReader.Close();
            fileStream.Close();

            if (!HasValidSignature(hivePath))
            {
                _logger.Error("'{0}' is not a Registry hive (bad signature)", hivePath);

                throw new Exception(String.Format("'{0}' is not a Registry hive (bad signature)", hivePath));
            }

            _logger.Debug("Set HivePath to {0}", hivePath);

            var header = ReadBytesFromHive(0, 4096);

            _logger.Debug("Getting header");

            Header = new RegistryHeader(header);

            _logger.Debug("Got header. Embedded file name {0}", Header.FileName);

            var fnameBase = Path.GetFileName(Header.FileName).ToLower();

            switch (fnameBase)
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

            var version = string.Format("{0}.{1}", Header.MajorVersion, Header.MinorVersion);

            _logger.Debug("Hive version is {0}", version);
        }

        public bool HasValidSignature(string filename)
        {
           var sig = BitConverter.ToInt32(FileBytes, 0);
            
            return sig.Equals(RegfSignature);
        }

        public byte[] FileBytes { get; private set; }
        public LoggingConfiguration NlogConfig
      {
            get { return _nlogConfig; }
            set
            {
                _nlogConfig = value;
                LogManager.Configuration = _nlogConfig;
            }
        }

        public HiveTypeEnum HiveType { get; private set; }

        public string HivePath { get;}

        public RegistryHeader Header {get;set;}
      public byte[] ReadBytesFromHive(long offset, int length)
      {
            var absLen = Math.Abs(length);

            var r = new ArraySegment<byte>(FileBytes, (int)offset, absLen);

            return r.ToArray();
        }
    }
}
