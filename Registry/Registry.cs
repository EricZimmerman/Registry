using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// namespaces...
namespace Registry
{
    // public classes...
    public class Registry
    {
        // private fields...
        private string _filename = null;
        private static BinaryReader binaryReader;
        private static FileStream fileStream;

        // public constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="Registry"/> class.
        /// </summary>
        public Registry(string fileName, bool autoParse = false)
        {
            _filename = fileName;

            if (_filename == null)
            {
                throw new ArgumentNullException("Filename cannot be null");
            }

            if (!File.Exists(_filename))
            {
                throw new FileNotFoundException();
            }




            fileStream = new FileStream(_filename, FileMode.Open);
            binaryReader = new BinaryReader(fileStream);

            if (autoParse)
            {
                ParseHive();
            }
        }

        // public properties...
        public string Filename
        {
            get
            {
                return _filename;
            }
        }

        public RegistryHeader Header { get; private set; }

        // public methods...
        public static long HiveLength()
        {
            return binaryReader.BaseStream.Length;
        }

        // public methods...
        public bool ParseHive()
        {
            if (_filename == null)
            {
                throw new ArgumentNullException("Filename cannot be null");
            }

            if (!File.Exists(_filename))
            {
                throw new FileNotFoundException();
            }



            var header = ReadBytesFromHive(0, 4096);
            //br.ReadBytes(4096);

            Header = new RegistryHeader(header);

            //Look at first hbin, get its size, then read that many bytes to create hbin record

            var hbBlockSize = BitConverter.ToUInt32(header, 0x8);
            //br.ReadUInt32();


            //    br.BaseStream.Seek(4096, SeekOrigin.Begin); // get back to where we started for reading full hbin record

            ReadBytesFromHive(4096, (int)hbBlockSize);


            return true;
        }

        public static byte[] ReadBytesFromHive(long offset, int length)
        {
            binaryReader.BaseStream.Seek(offset, SeekOrigin.Begin);

            return binaryReader.ReadBytes(Math.Abs(length));
        }

        /// <summary>
        /// Given a file, confirm it is a registry hive and that hbin headers are found every 4096 * (size of hbin) bytes.
        /// </summary>
        /// <param name="hiveName"></param>
        /// <returns></returns>
        public HiveMetadata Verify()
        {
            const int regfHeader = 0x66676572;
            const int hbinHeader = 0x6e696268;

            if (_filename == null)
            {
                throw new ArgumentNullException("Filename cannot be null");
            }

            if (!File.Exists(_filename))
            {
                throw new FileNotFoundException();
            }

            var hiveMetadata = new HiveMetadata();


            var fileHeaderSig = BitConverter.ToUInt32(ReadBytesFromHive(0, 4), 0);

            if (fileHeaderSig != regfHeader)
            {
                return hiveMetadata;
            }

            hiveMetadata.HasValidHeader = true;

            long offset = 4096;

            while (offset < HiveLength())
            {
                var hbinSig =  BitConverter.ToUInt32(ReadBytesFromHive(offset, 4), 0);

                if (hbinSig == hbinHeader)
                {
                    hiveMetadata.NumberofHBins += 1;
                }

                var hbinSize = BitConverter.ToUInt32(ReadBytesFromHive(offset + 8, 4), 0);

                if (hbinSize == 0)
                {
                    // Go to end if we find a 0 size block (padding?)
                    offset = HiveLength();
                }

                offset += hbinSize;
            }


            return hiveMetadata;
        }
    }
}
