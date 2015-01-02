using NFluent;
using Registry.Abstractions;
using Registry.Cells;
using Registry.Lists;
using Registry.Other;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections;
using System.Diagnostics;
using System.Text;

// namespaces...
namespace Registry
{
    // public classes...
    public class RegistryHive : IDisposable
    {
        // private fields...
        private static BinaryReader _binaryReader;
        // private fields...
        private readonly string _filename;
        private static FileStream _fileStream;
        private static int KeyCount;
        private static int ValueCount;

        // internal fields...
        internal static int _hardParsingErrors;
        internal  static int _softParsingErrors;

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
            HBinRecords = new Dictionary<long, HBinRecord>();

            _fileStream = new FileStream(_filename, FileMode.Open);
            _binaryReader = new BinaryReader(_fileStream);

            if (autoParse)
            {
                ParseHive(verboseOutput);
            }
        }

        // public properties...
        /// <summary>
        /// List of all NK, VK, and SK cell records, both in use and free, as found in the hive
        /// </summary>
        public static Dictionary<long, ICellTemplate> CellRecords { get; private set; }
        /// <summary>
        /// List of all data nodes, both in use and free, as found in the hive
        /// </summary>
        public static Dictionary<long, DataNode> DataRecords { get; private set; }
        // public properties...
        public string Filename
        {
            get
            {
                return _filename;
            }
        }

        /// <summary>
        /// The total number of record parsing errors where the records were IsFree == false
        /// </summary>
        public  int HardParsingErrors
        {
            get
            {
                return _hardParsingErrors;
            }
        }

        public static RegistryHeader Header { get; private set; }
        /// <summary>
        /// List of all DB, LI, RI, LH, and LF list records, both in use and free, as found in the hive
        /// </summary>
        public static Dictionary<long, IListTemplate> ListRecords { get; private set; }
        public static Dictionary<long, HBinRecord> HBinRecords { get; private set; }


        public  RegistryKey Root { get; private set; }

        /// <summary>
        /// The total number of record parsing errors where the records were IsFree == true
        /// </summary>
        public  int SoftParsingErrors
        {
            get
            {
                return _softParsingErrors;
            }
        }

        public static bool VerboseOutput { get; private set; }

        // private methods...
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
        private List<RegistryKey> GetSubKeysAndValues(RegistryKey key)
        {
            if (VerboseOutput)
            {
                Console.WriteLine("Getting subkeys for {0}", key.KeyPath);
            }

            var keys = new List<RegistryKey>();

            if (key.NKRecord.ClassCellIndex > 0)
            {
                var d = DataRecords[key.NKRecord.ClassCellIndex];
                d.IsReferenceed = true;
                var clsName = Encoding.Unicode.GetString(d.Data, 0, key.NKRecord.ClassLength);
                key.ClassName = clsName;
            }

            //Build ValueOffsets for this NKRecord
            if (key.NKRecord.ValueListCellIndex > 0)
            {
                //there are values for this key, so get the offsets so we can pull them next

                var offsetList = DataRecords[key.NKRecord.ValueListCellIndex];

                offsetList.IsReferenceed = true;

                for (var i = 0; i < key.NKRecord.ValueListCount; i++)
                {
                    //use i * 4 so we get 4, 8, 12, 16, etc
                    var os = BitConverter.ToUInt32(offsetList.Data, i * 4);

                    key.NKRecord.ValueOffsets.Add(os);
                }

                //TODO need a trap here in case we run out of data ? test it

            }

            if (key.NKRecord.ValueOffsets.Count != key.NKRecord.ValueListCount)
            {
                //todo This needs to be a stronger warning since we will not have all data
                Console.WriteLine("Value count mismatch! ValueListCount is {0:N0} but NKRecord.ValueOffsets.Count is {1:N0}", key.NKRecord.ValueListCount, key.NKRecord.ValueOffsets.Count);
            }

            //TODO need to add check on each vk record below
            //for each d in dataoffsets, get data from datarecords and set isreferenced to true
            //in vkrecord, add dataoffsets list and then set them to referenced here
            //this keeps processing of datas in the vk class
            //

            // look for values in this key HERE
            foreach (var valueOffset in key.NKRecord.ValueOffsets)
            {
                var vc = CellRecords[(long)valueOffset];

                var vk = vc as VKCellRecord;
                vk.IsReferenceed = true;

                var dbListProcessed = false;

                foreach (var dataOffet in vk.DataOffets)
                {
                    //there is a special case when registry version > 1.4 and size > 16344
                    //if this is true, the first offset is a db record, found with the lists
                    if ((Header.MinorVersion > 4) && vk.DataLength > 16344 && dbListProcessed == false)
                    {
                        var db = ListRecords[(long)dataOffet];

                        var dbr = db as DBListRecord;
                        dbr.IsReferenceed = true;
                        dbListProcessed = true;
                    }
                    else
                    {
                        DataRecords[(long)dataOffet].IsReferenceed = true;
                    }

                        
                }

                var valueDataString = string.Empty;

                switch (vk.DataType)
                {
                    case VKCellRecord.DataTypeEnum.RegFileTime:
                    case VKCellRecord.DataTypeEnum.RegExpandSz:
                    case VKCellRecord.DataTypeEnum.RegMultiSz:
                    case VKCellRecord.DataTypeEnum.RegDword:
                    case VKCellRecord.DataTypeEnum.RegDwordBigEndian:
                    case VKCellRecord.DataTypeEnum.RegQword:
                    case VKCellRecord.DataTypeEnum.RegLink:
                    case VKCellRecord.DataTypeEnum.RegSz:
                        if (vk.ValueData == null)
                        {
                            valueDataString = "";
                        }
                        else
                        {
                            valueDataString = vk.ValueData.ToString(); 
                        }
                            

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

            var sk = CellRecords[key.NKRecord.SecurityCellIndex] as SKCellRecord;
            sk.IsReferenceed = true;

            //TODo THIS SHOULD ALSO CHECK THE # OF SUBKEYS == 0
            if (ListRecords.ContainsKey(key.NKRecord.SubkeyListsStableCellIndex) == false)
            {
                return keys;
            }


            var l = ListRecords[key.NKRecord.SubkeyListsStableCellIndex];

            switch (l.Signature)
            {
                case "lf":
                case "lh":
                    var lxRecord = l as LxListRecord;
                    lxRecord.IsReferenceed = true;

                    foreach (var offset in lxRecord.Offsets)
                    {
                        var cell = CellRecords[offset.Key];

                        var nk = cell as NKCellRecord;
                        nk.IsReferenceed = true;

                        var tempKey = new RegistryKey(nk, key.KeyPath);

                        var sks = GetSubKeysAndValues(tempKey);
                        tempKey.SubKeys.AddRange(sks);

                        keys.Add(tempKey);
                    }
                    break;

                case "ri":
                    var riRecord = l as RIListRecord;
                    riRecord.IsReferenceed = true;

                    foreach (var offset in riRecord.Offsets)
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
                                nk.IsReferenceed = true;

                                var tempKey = new RegistryKey(nk, key.KeyPath);

                                var sks = GetSubKeysAndValues(tempKey);
                                tempKey.SubKeys.AddRange(sks);

                                keys.Add(tempKey);
                            }
                        }
                        else
                        {
                            var lxRecord_ = tempList as LxListRecord;
                            lxRecord_.IsReferenceed = true;

                            foreach (var offset3 in lxRecord_.Offsets)
                            {
                                var cell = CellRecords[offset3.Key];

                                var nk = cell as NKCellRecord;
                                nk.IsReferenceed = true;

                                var tempKey = new RegistryKey(nk, key.KeyPath);

                                var sks = GetSubKeysAndValues(tempKey);
                                tempKey.SubKeys.AddRange(sks);

                                keys.Add(tempKey);
                            }
                        }
                    }



                    break;

                case "li":
                    var liRecord = l as LIListRecord;
                    liRecord.IsReferenceed = true;

                    foreach (var offset in liRecord.Offsets)
                    {
                        var cell = CellRecords[offset];

                        var nk = cell as NKCellRecord;
                        nk.IsReferenceed = true;
                        
                        var tempKey = new RegistryKey(nk, key.KeyPath);

                        var sks = GetSubKeysAndValues(tempKey);
                        tempKey.SubKeys.AddRange(sks);

                        keys.Add(tempKey);
                    }


                    break;
                default:
                    throw new Exception(string.Format("Unknown subkey list type {0}!", l.Signature));
            }

            return keys;
        }

        // protected internal methods...
        // public methods...
        protected internal static long HiveLength()
        {
            return _binaryReader.BaseStream.Length;
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

        public void ExportDataToWilliFormat(string outfile)
        {
            KeyCount = 1; //root key
            ValueCount = 0;

            using (var sw = new StreamWriter(outfile, false))
            {
                sw.AutoFlush = true;

                sw.WriteLine("key|{0}|{1}", Root.KeyPath, Root.LastWriteTime.Value.UtcDateTime.ToString("o"));

                ValueCount += Root.Values.Count;

                foreach (var val in Root.Values)
                {
                    sw.WriteLine(@"value|{0}|{1}|{2}|{3}", Root.KeyPath, val.ValueName, (int)val.VKRecord.DataType, BitConverter.ToString(val.VKRecord.ValueDataRaw).Replace("-", " "));
                }


                DumpKeyWilli(Root, sw);

                sw.WriteLine("total_keys|{0}", KeyCount);
                sw.WriteLine("total_values|{0}", ValueCount);
            }
        }

        public static long TotalBytesRead;

        public RegistryKey FindKey(string keypath)
        {
            //todo finish this
            return null;
        }

        public RegistryKey FindKey(long relativeOffset, RegistryKey parent)
        {
            RegistryKey found = null;
            if (Root == null)
            {
                return null;
            }

            if (parent.NKRecord.RelativeOffset == relativeOffset)
            {
                return parent;
            }

            foreach (var registryKey in parent.SubKeys)
            {
                if (found != null)
                {
                    break;
                }
                    found =   FindKey(relativeOffset, registryKey);
            }

            return found;

        }

        //private RegistryKey FindKeyByRelativeOffset(RegistryKey key,long relativeOffset)
        //{
        //    if (key.NKRecord.RelativeOffset )
        //}

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

            TotalBytesRead = 0;

            TotalBytesRead += 4096;

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

            var hivelen = HiveLength();

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

                    HBinRecords.Add(h.RelativeOffset,h);
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

            //validate what we found above
            Check.That((long)Header.RootCellOffset).IsEqualTo(rootNode.RelativeOffset);

            rootNode.IsReferenceed = true;

            Console.WriteLine("Found root node! Getting subkeys...");

            Root = new RegistryKey(rootNode, null);

            //TODO Build FullPath or Path property as this is being built

            var keys =   GetSubKeysAndValues(Root);

            Root.SubKeys.AddRange(keys);

            Console.WriteLine("Initial processing complete! Associating deleted keys and values...");
            //TODO MOVE THE stuff from program.cs inside the class so we have access to the kinds of things calculated there.
            //copy pasted for now

            var unreferencedNKCells = CellRecords.Where(t => t.Value.IsReferenceed == false && t.Value is NKCellRecord);
            var unreferencedVKCells = CellRecords.Where(t => t.Value.IsReferenceed == false && t.Value is VKCellRecord);
            var unreferencedLists = ListRecords.Where(t => t.Value.IsReferenceed == false);

            var restoredDeletedKeys = 0;

            foreach (var unreferencedNkCell in unreferencedNKCells)
            {
                var nk = unreferencedNkCell.Value  as NKCellRecord;
                if (CellRecords.ContainsKey(nk.ParentCellIndex))
                {
                    // unreferencedNKCell has a parent key that exists! Now to see if its referenced
                    if (CellRecords.ContainsKey(nk.ParentCellIndex))
                    {
                           var parentNK = CellRecords[nk.ParentCellIndex] as NKCellRecord;

                        if (parentNK == null)
                        {
                            //the data at that index is not an NKRecord
                            continue;
                        }

                            if (parentNK.IsReferenceed)
                            {
                                //parent exists in our tree, so add unreferencedNkCell as a child but mark it deleted

                            var pk = FindKey(nk.ParentCellIndex, Root);

                            var key = new RegistryKey(nk, pk.KeyPath);
                                //TODO code FIND method on root node. have an 'exact' flag and if set use contains vs ==
                                //be sure to force to lower
                            key.IsDeleted = true;
                             
                                pk.SubKeys.Add(key);
                            restoredDeletedKeys += 1;
                                

                                //TODO  does nk.ValueListCellIndex point to an unreferenced vk object?
                                //is this right? i think this is whats blanked to FFFFFFFF/0
                                //Does it have to reference an unreferenced list?
                             
                                if (ListRecords.ContainsKey(nk.ValueListCellIndex))
                                {
                                    Debug.WriteLine(nk.ValueListCellIndex);
                                }
                            }
                    }

                     
                }
            }

            //TODO reflect restoredDeletedKeys count somewhere, as well as whats left
           

            if (HiveLength() != TotalBytesRead)
            {
              
                var remainingHive = ReadBytesFromHive(TotalBytesRead,(int) (HiveLength() - TotalBytesRead));

                //Sometimes the remainder of the file is all zeros, which is useless, so check for that
                if (!Array.TrueForAll(remainingHive,a => a == 0))
                {
                    Debug.WriteLine("Hive length ({0:x}) does not equal bytes read ({1:x})!!", HiveLength(), TotalBytesRead);
                }

                //as a second check, compare Header length with what we read (taking the header into account as length is only for hbin records)
                Check.That((long)Header.Length).IsEqualTo(TotalBytesRead - 0x1000);
            }

            //TODO ADD THIS TO VK RECORD AND ALL LISTS (What about datanodes? sweep for signatures?)
            //can we defer the data checks until we look at unrefereenced stuff?
            //Check.That(paddingOffset + paddingLength).IsEqualTo(rawBytes.Length);


            return true;
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
