using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NFluent;
using Registry.Abstractions;
using Registry.Cells;
using Registry.Lists;
using Registry.Other;


// namespaces...
namespace Registry
{
    // public classes...
    public class RegistryHive : IDisposable
    {
        // private fields...
        private readonly string _filename;
        private static BinaryReader _binaryReader;
        private static FileStream _fileStream;

        // public constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="Registry"/> class.
        /// </summary>
        public RegistryHive(string fileName, bool autoParse = false, bool verboseOutput = false)
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


            CellRecords = new Dictionary<long, ICellTemplate>();
            ListRecords = new Dictionary<long, IListTemplate>();
            DataRecords = new Dictionary<long, DataNode>();

            _fileStream = new FileStream(_filename, FileMode.Open);
            _binaryReader = new BinaryReader(_fileStream);

            if (autoParse)
            {
                ParseHive(verboseOutput);
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

        public static RegistryHeader Header { get; private set; }

        public  RegistryKey Root { get; private set; }
        /// <summary>
        /// List of all NK, VK, and SK cell records, both in use and free, as found in the hive
        /// </summary>
        public static Dictionary<long,ICellTemplate> CellRecords { get; private set; }
        /// <summary>
        /// List of all data nodes, both in use and free, as found in the hive
        /// </summary>
        public static Dictionary<long, DataNode> DataRecords { get; private set; }

        /// <summary>
        /// List of all DB, LI, RI, LH, and LF list records, both in use and free, as found in the hive
        /// </summary>
        public static Dictionary<long, IListTemplate> ListRecords { get; private set; }

        public  int HardParsingErrors
        {
            get { return _hardParsingErrors; }
        
        }

        public  int SoftParsingErrors
        {
            get { return _softParsingErrors; }
    
        }

        // public methods...
        public void Dispose()
        {
            if (_binaryReader != null)
            {
                _binaryReader.Close();
            }
            if (_fileStream != null)
            {
                _fileStream.Close();
            }
        }

        public static bool VerboseOutput { get; private set; }

        // public methods...
        protected internal static long HiveLength()
        {
            return _binaryReader.BaseStream.Length;
        }

        // public methods...
        public bool ParseHive(bool verboseOutput)
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

            Header = new RegistryHeader(header);

            VerboseOutput = verboseOutput;

            _softParsingErrors = 0;
            _hardParsingErrors = 0;

            ////Look at first hbin, get its size, then read that many bytes to create hbin record
            //var hbBlockSize = BitConverter.ToUInt32(header, 0x8);

            //var rawhbin = ReadBytesFromHive(4096, (int)hbBlockSize);

            //var h = new HBinRecord(rawhbin);


            // for initial testing we just walk down the file looking at everything
            long offsetInHive = 4096;

            const uint hbinHeader = 0x6e696268;

            long hivelen = HiveLength();

            while (offsetInHive < HiveLength())
            {
                var hbinSize = BitConverter.ToUInt32(ReadBytesFromHive(offsetInHive + 8, 4), 0);

                if (hbinSize == 0)
                {
                    // Go to end if we find a 0 size block (padding?)
                    offsetInHive = HiveLength();
                    continue;
                }

                var hbinSig = BitConverter.ToUInt32(ReadBytesFromHive(offsetInHive, 4), 0);

                if (hbinSig != hbinHeader)
                {
                    Console.WriteLine("hbin header incorrect at offset 0x{0:X}!!!\t\t\t\tPercent done: {1:P}", offsetInHive, (double)offsetInHive / hivelen);
                    break;
                }

                Check.That(hbinSig).IsEqualTo(hbinHeader);

                if (VerboseOutput)
                {
                    Console.WriteLine("Pulling hbin at offset 0x{0:X}. Size 0x{1:X}\t\t\t\tPercent done: {2:P}", offsetInHive, hbinSize, (double)offsetInHive / hivelen);
                }



                var rawhbin = ReadBytesFromHive(offsetInHive, (int)hbinSize);

                try
                {
                    var h = new HBinRecord(rawhbin, offsetInHive - 4096);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("********Error processing hbin at offset 0x{0:X}. Error: {1}, Stack: {2}", offsetInHive, ex.Message, ex.StackTrace);
                }



                //File.AppendAllText(@"C:\temp\hbins.txt", h.ToString());


                offsetInHive += hbinSize;
            }


            Console.WriteLine("Initial processing complete. Building tree...");

            var rootNode = CellRecords.Values.OfType<NKCellRecord>().SingleOrDefault((f => f.Flags.ToString().Contains(NKCellRecord.FlagEnum.HiveEntryRootKey.ToString())));

            if (rootNode == null)
            {
                throw new Exception("Root nk record not found!");
            }

            Console.WriteLine("Found root node! Getting subkeys...");

            Root = new RegistryKey(rootNode, null);

            //TODO Build FullPath or Path property as this is being built

            var keys =   GetSubKeys(Root);

            Root.SubKeys.AddRange(keys);

            return true;
        }

        public void ExportDataToWilliFormat(string outfile)
        {
           KeyCount = 1; //root key
            ValueCount = 0;

            using (var sw = new StreamWriter(outfile,false))
            {
                sw.AutoFlush = true;

                sw.WriteLine("key|{0}|{1}", Root.KeyPath, Root.LastWriteTime.Value.UtcDateTime.ToString("o"));

                ValueCount += Root.Values.Count;
              
                foreach (var val in Root.Values)
                {


                    sw.WriteLine(@"value|{0}|{1}|{2}|{3}", Root.KeyPath, val.ValueName, (int)val.VKRecord.DataType, BitConverter.ToString(val.VKRecord.ValueDataRaw).Replace("-", " "));
                }


                DumpKeyWilli(Root, sw);

                sw.WriteLine("total_keys|{0}",KeyCount);
                sw.WriteLine("total_values|{0}",ValueCount);
                

            }


        }

        private static int KeyCount;
        private static int ValueCount;
        internal  static int _softParsingErrors;
        internal static int _hardParsingErrors;

        private void DumpKeyWilli(RegistryKey key, StreamWriter sw)
        {

            KeyCount += key.SubKeys.Count;

            foreach (var subkey in key.SubKeys)
            {
                // dump key format here "key"|path/path/path|<timestamp as iso8601, like 2014-12-16T00:00:00.00>

                sw.WriteLine("key|{0}|{1}", subkey.KeyPath, subkey.LastWriteTime.Value.UtcDateTime.ToString("o"));

                ValueCount += subkey.Values.Count;

                foreach (var val in subkey.Values)
                {
                    //dump value format here

                    sw.WriteLine(@"value|{0}|{1}|{2}|{3}", subkey.KeyPath, val.ValueName, (int)val.VKRecord.DataTypeRaw, BitConverter.ToString(val.VKRecord.ValueDataRaw).Replace("-", " "));
                }
                
               DumpKeyWilli(subkey, sw);
            }

            

        }

        //TODO this needs refactored to remove duplicated code
        private List<RegistryKey> GetSubKeys(RegistryKey key)
        {
            if (VerboseOutput)
            {
                Console.WriteLine("Getting subkeys for {0}", key.KeyPath);    
            }
            

            var keys = new List<RegistryKey>();
            
            if (key.NKRecord.ValueOffsets.Count != key.NKRecord.ValueListCount)
            {
                Console.WriteLine("Value count mismatch! ValueListCount is {0:N0} but NKRecord.ValueOffsets.Count is {1:N0}",key.NKRecord.ValueListCount, key.NKRecord.ValueOffsets.Count);
            }

            // look for values in this key HERE
            foreach (var valueOffset in key.NKRecord.ValueOffsets)
            {
                var vc = CellRecords[(long)valueOffset];

                var vk = vc as VKCellRecord;

                var valueDataString = string.Empty;

                switch (vk.DataType)
                {
                    case VKCellRecord.DataTypeEnum.RegFileTime:
                    case VKCellRecord.DataTypeEnum.RegExpandSz:
                    case VKCellRecord.DataTypeEnum.RegMultiSz:
                    case VKCellRecord.DataTypeEnum.RegDword:
                    case VKCellRecord.DataTypeEnum.RegDwordBigEndian:
                    case VKCellRecord.DataTypeEnum.RegQword:
                    case VKCellRecord.DataTypeEnum.RegSz:
                        valueDataString = vk.ValueData.ToString();

                        break;

                    case VKCellRecord.DataTypeEnum.RegNone:
                    case VKCellRecord.DataTypeEnum.RegBinary:
                    case VKCellRecord.DataTypeEnum.RegResourceRequirementsList:
                    case VKCellRecord.DataTypeEnum.RegResourceList:
                    case VKCellRecord.DataTypeEnum.RegFullResourceDescription:
                    case VKCellRecord.DataTypeEnum.RegUnknown:

                        valueDataString = BitConverter.ToString((byte[])vk.ValueData);

                        break;

                    default:

                        break;
                }


                var value = new KeyValue(vk.ValueName, vk.DataType.ToString(), valueDataString,
                    BitConverter.ToString(vk.ValueDataSlack), vk.ValueDataSlack, vk);

                key.Values.Add(value);
            }
          

            if (ListRecords.ContainsKey(key.NKRecord.SubkeyListsStableCellIndex) == false)
            {
               
                return keys;
            }

      
            var l = ListRecords[key.NKRecord.SubkeyListsStableCellIndex]; 

                switch (l.Signature)
                {
                    case "lf":
                    case "lh":
                        var sk1 = l as LxListRecord;

                        foreach (var offset in sk1.Offsets)
                        {
                            var cell = CellRecords[offset.Key]; 

                            var nk = cell as NKCellRecord;

                            var tempKey = new RegistryKey(nk, key.KeyPath);

                            

                            var sks = GetSubKeys(tempKey);
                            tempKey.SubKeys.AddRange(sks);

                            keys.Add(tempKey);
                        }


                        break;

                    case "ri":
                        var ri = l as RIListRecord;

                        foreach (var offset in ri.Offsets)
                        {
                            var tempList = ListRecords[offset];

                            //templist is now an li or lh list 

                            if (tempList.Signature == "li")
                            {
                                var sk3 = tempList as LIListRecord;

                                foreach (var offset1 in sk3.Offsets)
                                {
                                    var cell = CellRecords[offset1]; 

                                    var nk = cell as NKCellRecord;

                                    var tempKey = new RegistryKey(nk, key.KeyPath);

                                    var sks = GetSubKeys(tempKey); 
                                    tempKey.SubKeys.AddRange(sks);

                                    keys.Add(tempKey);
                                }
                            }
                            else
                            {
                                var sk3 = tempList as LxListRecord;

                                foreach (var offset3 in sk3.Offsets)
                                {
                                    var cell = CellRecords[offset3.Key]; 

                                    var nk = cell as NKCellRecord;

                                    var tempKey = new RegistryKey(nk, key.KeyPath);

                                    var sks = GetSubKeys(tempKey);
                                    tempKey.SubKeys.AddRange(sks);

                                    keys.Add(tempKey);
                                }
                            }
                            

                        }



                        break;

                    case "li":
                        var sk2 = l as LIListRecord;

                        foreach (var offset in sk2.Offsets)
                        {
                            var cell = CellRecords[offset]; 

                            var nk = cell as NKCellRecord;

                            var tempKey = new RegistryKey(nk, key.KeyPath);

                            var sks = GetSubKeys(tempKey);
                            tempKey.SubKeys.AddRange(sks);

                            keys.Add(tempKey);
                        }


                        break;
                    default:
                        throw new Exception(string.Format("Unknown subkey list type {0}!", l.Signature));
                }
          




            return keys;
        }

        /// <summary>
        /// Reads 'length' bytes from the registry starting at 'offset' and returns a byte array
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        protected internal static byte[] ReadBytesFromHive(long offset, int length)
        {
            _binaryReader.BaseStream.Seek(offset, SeekOrigin.Begin);

            return _binaryReader.ReadBytes(Math.Abs(length));
        }

        /// <summary>
        /// Given a file, confirm it is a registry hive and that hbin headers are found every 4096 * (size of hbin) bytes.
        /// </summary>
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
