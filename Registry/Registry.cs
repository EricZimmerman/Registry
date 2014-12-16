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

        // public constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="Registry"/> class.
        /// </summary>
        public Registry(string fileName, bool autoParse = false)
        {
            _filename = fileName;

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

            using (var fs = new FileStream(_filename, FileMode.Open))
            {
                using (var br = new BinaryReader(fs))
                {
                    var header = br.ReadBytes(4096);

                    Header = new RegistryHeader(header);

                    //Look at first hbin, get its size, then read that many bytes to create hbin record
                    var hbHeader = br.ReadBytes(4);
                    var hbOffset = br.ReadBytes(4);
                    var hbBlockSize = br.ReadUInt32();


                    br.BaseStream.Seek(4096, SeekOrigin.Begin); // get back to where we started for reading full hbin record

                    var hbin = br.ReadBytes((int)hbBlockSize);

                    var h = new HBinRecord(hbin);




                }
            }

            return true;
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

            using (var fs = new FileStream(_filename, FileMode.Open))
            {
                using (var br = new BinaryReader(fs))
                {
                    var fileHeaderSig = br.ReadUInt32();

                    if (fileHeaderSig != regfHeader)
                    {
                        return hiveMetadata;
                    }

                    hiveMetadata.HasValidHeader = true;

                    //look for hbin headers every 4096 bytes
                    br.BaseStream.Seek(4096, SeekOrigin.Begin);

                    while (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        var hbinSig = br.ReadUInt32();

                        if (hbinSig == hbinHeader)
                        {
                            hiveMetadata.NumberofHBins += 1;
                        }

                        br.ReadUInt32(); //skip offset to first hbin
                        var hbinSize = br.ReadUInt32();

                        if (hbinSize == 0)
                        {
                            // Go to end if we find a 0 size block (padding?)
                            br.BaseStream.Seek(0 + 12, SeekOrigin.End);
                        }

                        // Account  for 12 bytes from the previous reads, then jump hbinSize to get to where next header should be
                        br.BaseStream.Seek((long)hbinSize - 12, SeekOrigin.Current);
                    }
                }
            }

            return hiveMetadata;
        }
    }
}
