using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NFluent;
using Registry.Abstractions;
using Registry.Cells;
using Registry.Lists;
using Registry.Other;
using RegistryValueKind = Microsoft.Win32.RegistryValueKind;

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
        private static int KeyCountDeleted;
        private static int ValueCountDeleted;

        // internal fields...
        internal static int _hardParsingErrors;
        internal  static int _softParsingErrors;

        // public fields...
        public static long TotalBytesRead;

        /// <summary>
        /// The number of deleted keys that were reassociated with a key in the active registry
        /// </summary>
        public int RestoredDeletedKeyCount { get; private set; }

        /// <summary>
        /// Contains all recovered 
        /// </summary>
        public List<RegistryKey> DeletedRegistryKeys { get; private set; }

        public event EventHandler<MessageEventArgs> Message;

        private MessageEventArgs _msgArgs;

        protected virtual void OnMessage(MessageEventArgs e)
        {
            EventHandler<MessageEventArgs> handler = Message;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        // public constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="Registry"/> class.
        /// </summary>
        public RegistryHive(string fileName, bool autoParse = false)
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

            DeletedRegistryKeys= new List<RegistryKey>();

            Verbosity = VerbosityEnum.Normal;

            if (autoParse)
            {
                ParseHive();
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

        public static Dictionary<long, HBinRecord> HBinRecords { get; private set; }
        public static RegistryHeader Header { get; private set; }
        /// <summary>
        /// List of all DB, LI, RI, LH, and LF list records, both in use and free, as found in the hive
        /// </summary>
        public static Dictionary<long, IListTemplate> ListRecords { get; private set; }
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

        public enum VerbosityEnum
        {
            Normal,
            Full
        }

        public static VerbosityEnum Verbosity { get; private set; }

        // private methods...
        private void DumpKeyCommonFormat(RegistryKey key, StreamWriter sw)
        {
            if (key.IsDeleted)
            {
                KeyCountDeleted += key.SubKeys.Count;
            }
            else
            {
                KeyCount += key.SubKeys.Count;
            }
               

            foreach (var subkey in key.SubKeys)
            {
                // dump key format here "key"|path/path/path|<timestamp as iso8601, like 2014-12-16T00:00:00.00>

                if (key.IsDeleted)
                {
                    sw.WriteLine("key|{0}|{1}|{2}|{3}", subkey.NKRecord.IsFree ? "U" : "A", subkey.NKRecord.AbsoluteOffset, subkey.KeyName, subkey.LastWriteTime.Value.UtcDateTime.ToString("o"));
                }
                else
                {
                    sw.WriteLine("key|{0}|{1}|{2}|{3}", subkey.NKRecord.IsFree ? "U" : "A", subkey.NKRecord.AbsoluteOffset, subkey.KeyPath, subkey.LastWriteTime.Value.UtcDateTime.ToString("o")); 
                }

                   
               // sw.WriteLine("key|{0}|{1}{2}{3}{4}", subkey.KeyPath, subkey.LastWriteTime.Value.UtcDateTime.ToString("o"), delKey, isRef,osKey);

                if (subkey.IsDeleted)
                {
                    ValueCountDeleted += subkey.Values.Count;
                    
                }
                else
                {
                    ValueCount += subkey.Values.Count;

                }

                    
                foreach (var val in subkey.Values)
                {
                    //dump value format here

                    if (subkey.IsDeleted)
                    {
                        sw.WriteLine(@"value|{0}|{1}|{2}|{3}|{4}|{5}", val.VKRecord.IsFree ? "U" : "A", val.VKRecord.AbsoluteOffset, subkey.KeyName, val.ValueName, (int)val.VKRecord.DataType, BitConverter.ToString(val.VKRecord.ValueDataRaw).Replace("-", " "));
                        
                    }
                    else
                    {
                        sw.WriteLine(@"value|{0}|{1}|{2}|{3}|{4}|{5}", val.VKRecord.IsFree ? "U" : "A", val.VKRecord.AbsoluteOffset, subkey.KeyPath, val.ValueName, (int)val.VKRecord.DataType, BitConverter.ToString(val.VKRecord.ValueDataRaw).Replace("-", " "));
                 
                    }

                         //  sw.WriteLine(@"value|{0}|{1}|{2}|{3}{4}", subkey.KeyPath, val.ValueName, (int)val.VKRecord.DataTypeRaw, BitConverter.ToString(val.VKRecord.ValueDataRaw).Replace("-", " ") + "", osVal);
                }

                DumpKeyCommonFormat(subkey, sw);
            }
        }

        //TODO this needs refactored to remove duplicated code
        private List<RegistryKey> GetSubKeysAndValues(RegistryKey key)
        {
            if (Verbosity == VerbosityEnum.Full)
            {
                var args = new MessageEventArgs
                {
                    Detail = string.Format("Getting subkeys for {0}", key.KeyPath),
                    Exception = null,
                    Message = string.Format("Getting subkeys for {0}", key.KeyPath),
                    MsgType = MessageEventArgs.MsgTypeEnum.Info

                };

                OnMessage(args);

            }

            var keys = new List<RegistryKey>();

            if (key.NKRecord.ClassCellIndex > 0)
            {
                var d = DataRecords[key.NKRecord.ClassCellIndex];
                d.IsReferenced = true;
                var clsName = Encoding.Unicode.GetString(d.Data, 0, key.NKRecord.ClassLength);
                key.ClassName = clsName;
            }

            //Build ValueOffsets for this NKRecord
            if (key.NKRecord.ValueListCellIndex > 0)
            {
                //there are values for this key, so get the offsets so we can pull them next

                var offsetList = DataRecords[key.NKRecord.ValueListCellIndex];

                offsetList.IsReferenced = true;

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

                var args = new MessageEventArgs
                {
                    Detail = string.Format("Value count mismatch! ValueListCount is {0:N0} but NKRecord.ValueOffsets.Count is {1:N0}", key.NKRecord.ValueListCount, key.NKRecord.ValueOffsets.Count),
                    Exception = null,
                    Message = string.Format("Value count mismatch!"),
                    MsgType = MessageEventArgs.MsgTypeEnum.Warning

                };

                OnMessage(args);


                
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


                vk.IsReferenced = true;


                var dbListProcessed = false;

                foreach (var dataOffet in vk.DataOffets)
                {

                    //there is a special case when registry version > 1.4 and size > 16344
                    //if this is true, the first offset is a db record, found with the lists
                    if ((Header.MinorVersion > 4) && vk.DataLength > 16344 && dbListProcessed == false)
                    {
                        var db = ListRecords[(long)dataOffet];

                        var dbr = db as DBListRecord;
                        dbr.IsReferenced = true;
                        dbListProcessed = true;
                    }
                    else
                    {
                        DataRecords[(long)dataOffet].IsReferenced = true;
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
            sk.IsReferenced = true;

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
                    lxRecord.IsReferenced = true;

                    foreach (var offset in lxRecord.Offsets)
                    {
                        var cell = CellRecords[offset.Key];

                        var nk = cell as NKCellRecord;
                        nk.IsReferenced = true;

                        var tempKey = new RegistryKey(nk, key.KeyPath);

                        var sks = GetSubKeysAndValues(tempKey);
                        tempKey.SubKeys.AddRange(sks);

                        keys.Add(tempKey);
                    }
                    break;

                case "ri":
                    var riRecord = l as RIListRecord;
                    riRecord.IsReferenced = true;

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
                                nk.IsReferenced = true;

                                var tempKey = new RegistryKey(nk, key.KeyPath);

                                var sks = GetSubKeysAndValues(tempKey);
                                tempKey.SubKeys.AddRange(sks);

                                keys.Add(tempKey);
                            }
                        }
                        else
                        {
                            var lxRecord_ = tempList as LxListRecord;
                            lxRecord_.IsReferenced = true;

                            foreach (var offset3 in lxRecord_.Offsets)
                            {
                                var cell = CellRecords[offset3.Key];

                                var nk = cell as NKCellRecord;
                                nk.IsReferenced = true;

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
                    liRecord.IsReferenced = true;

                    foreach (var offset in liRecord.Offsets)
                    {
                        var cell = CellRecords[offset];

                        var nk = cell as NKCellRecord;
                        nk.IsReferenced = true;

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
        /// <summary>
        /// Returns the length, in bytes, of the file being processed
        /// <remarks>This is the length returned by the underlying stream used to open the file</remarks>
        /// </summary>
        /// <returns></returns>
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
            //_binaryReader.BaseStream.Seek(offset, SeekOrigin.Begin);

            var retArray = new byte[length];

            Array.Copy(FileBytes, offset, retArray, 0, length);

            return retArray; //_binaryReader.ReadBytes(Math.Abs(length));
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

        public void ExportDataToCommonFormat(string outfile)
        {
            KeyCount = 1; //root key
            ValueCount = 0;
            KeyCountDeleted = 0;
            ValueCountDeleted = 0;

            using (var sw = new StreamWriter(outfile, false))
            {
                sw.AutoFlush = true;


                if (Root.LastWriteTime != null)
                {
                    sw.WriteLine("key|{0}|{1}|{2}|{3}", Root.NKRecord.IsFree ? "U" : "A",Root.NKRecord.AbsoluteOffset, Root.KeyPath, Root.LastWriteTime.Value.UtcDateTime.ToString("o"));
                }

                ValueCount += Root.Values.Count;

                foreach (var val in Root.Values)
                {
                    sw.WriteLine(@"value|{0}|{1}|{2}|{3}|{4}|{5}",val.VKRecord.IsFree ? "U": "A", val.VKRecord.AbsoluteOffset, Root.KeyPath, val.ValueName, (int)val.VKRecord.DataType, BitConverter.ToString(val.VKRecord.ValueDataRaw).Replace("-", " "));
                }


                DumpKeyCommonFormat(Root, sw);

                //dump recovered keys and values not associated with anything else

                foreach (var source in DeletedRegistryKeys) //.Where(t=>t.NKRecord.IsReferenced == false))
                {
                    KeyCountDeleted += 1;
                    sw.WriteLine("key|{0}|{1}|{2}|{3}", source.NKRecord.IsFree ? "U" : "A", source.NKRecord.AbsoluteOffset, source.KeyName, source.LastWriteTime.Value.UtcDateTime.ToString("o"));


                    foreach (var val in source.Values)
                    {
                        //dump value format here

                        sw.WriteLine(@"value|{0}|{1}|{2}|{3}|{4}|{5}", val.VKRecord.IsFree ? "U" : "A", val.VKRecord.AbsoluteOffset, source.KeyName, val.ValueName, (int)val.VKRecord.DataType, BitConverter.ToString(val.VKRecord.ValueDataRaw).Replace("-", " "));
                        //  sw.WriteLine(@"value|{0}|{1}|{2}|{3}{4}", subkey.KeyPath, val.ValueName, (int)val.VKRecord.DataTypeRaw, BitConverter.ToString(val.VKRecord.ValueDataRaw).Replace("-", " ") + "", osVal);
                    }

                    DumpKeyCommonFormat(source, sw);
                }

                //TODO here we need to look at whats left in CellRecords that arent referenced?
                var theRest = CellRecords.Where(a => a.Value.IsReferenced == false);
                //may not need to if we do not care about orphaned values

                foreach (var keyValuePair in theRest)
                {
                    if (keyValuePair.Value.Signature == "vk")
                    {
                        ValueCountDeleted += 1;
                        var val = keyValuePair.Value as VKCellRecord;
                        sw.WriteLine(@"value|{0}|{1}|{2}|{3}|{4}|{5}", val.IsFree ? "U" : "A", val.AbsoluteOffset, "", val.ValueName, (int)val.DataType, BitConverter.ToString(val.ValueDataRaw).Replace("-", " "));
               
                    }

                    if (keyValuePair.Value.Signature == "nk")
                    {
                        var nk = keyValuePair.Value as NKCellRecord;
                        var key = new RegistryKey(nk, null);


                        DumpKeyCommonFormat(key,sw);
                    }
                }


                sw.WriteLine("total_keys|{0}", KeyCount);
                sw.WriteLine("total_values|{0}", ValueCount);
                sw.WriteLine("total_deleted_keys|{0}", KeyCountDeleted);
                sw.WriteLine("total_deleted_values|{0}", ValueCountDeleted);
            }
        }

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

        internal static byte[] FileBytes;

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

            _binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);

            FileBytes = _binaryReader.ReadBytes((int)HiveLength());

         

            var header = ReadBytesFromHive(0, 4096);

            TotalBytesRead = 0;

            TotalBytesRead += 4096;
            
            Header = new RegistryHeader(header);

        

            _softParsingErrors = 0;
            _hardParsingErrors = 0;

            ////Look at first hbin, get its size, then read that many bytes to create hbin record
            long offsetInHive = 4096;

            const uint hbinHeader = 0x6e696268;

            var hivelen = HiveLength();

            //keep reading the file until we reach the end
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
                    var args = new MessageEventArgs
                    {
                        Detail = string.Format("Percent done: {0:P}", (double)offsetInHive / hivelen),
                        Exception = null,
                        Message = string.Format("hbin header incorrect at offset 0x{0:X}!!!",offsetInHive),
                        MsgType = MessageEventArgs.MsgTypeEnum.Error

                    };

                    OnMessage(args);

                    
                    break;
                }

                Check.That(hbinSig).IsEqualTo(hbinHeader);

                if (Verbosity == VerbosityEnum.Full)
                {
                    var args = new MessageEventArgs
                    {
                        Detail = string.Format("Size 0x{0:X}\t\t\t\tPercent done: {1:P}", hbinSize, (double)offsetInHive / hivelen),
                        Exception = null,
                        Message = string.Format("Pulling hbin at offset 0x{0:X}.", offsetInHive),
                        MsgType = MessageEventArgs.MsgTypeEnum.Info

                    };

                    OnMessage(args);

                 
                }

                var rawhbin = ReadBytesFromHive(offsetInHive, (int)hbinSize);

                try
                {
                    

                    var h = new HBinRecord(rawhbin, offsetInHive - 4096);



                    HBinRecords.Add(h.RelativeOffset, h);
                }
                catch (Exception ex)
                {
                     _msgArgs = new MessageEventArgs
                    {
                        Detail = string.Format("Error: {0}, Stack: {1}", ex.Message, ex.StackTrace),
                        Exception = ex,
                        Message = string.Format("Error processing hbin at offset 0x{0:X}.", offsetInHive),
                        MsgType = MessageEventArgs.MsgTypeEnum.Error

                    };

                    OnMessage(_msgArgs);

                 
                }

                offsetInHive += hbinSize;
            }


            _msgArgs = new MessageEventArgs
            {
                Detail = string.Format("Initial processing complete. Building tree..."),
                Exception = null,
                Message = string.Format("Initial processing complete. Building tree..."),
                MsgType = MessageEventArgs.MsgTypeEnum.Info

            };

            OnMessage(_msgArgs);

           

            //The root node can be found by either looking at Header.RootCellOffset or looking for an nk record with HiveEntryRootKey flag set.
            //here we are looking for the flag
            var rootNode = CellRecords.Values.OfType<NKCellRecord>().SingleOrDefault((f => f.Flags.ToString().Contains(NKCellRecord.FlagEnum.HiveEntryRootKey.ToString())));

            if (rootNode == null)
            {
                throw new Exception("Root nk record not found!");
            }

            //validate what we found above via the flag method
            Check.That((long)Header.RootCellOffset).IsEqualTo(rootNode.RelativeOffset);

            rootNode.IsReferenced = true;

            _msgArgs = new MessageEventArgs
            {
                Detail = string.Format("Found root node! Getting subkeys..."),
                Exception = null,
                Message = string.Format("Found root node! Getting subkeys..."),
                MsgType = MessageEventArgs.MsgTypeEnum.Info

            };

            OnMessage(_msgArgs);


           

            Root = new RegistryKey(rootNode, null);

            var keys = GetSubKeysAndValues(Root);

            Root.SubKeys.AddRange(keys);

             _msgArgs = new MessageEventArgs
            {
                Detail = "Initial processing complete! Associating deleted keys and values...",
                Exception = null,
                Message = "Initial processing complete! Associating deleted keys and values...",
                MsgType = MessageEventArgs.MsgTypeEnum.Info

            };

            OnMessage(_msgArgs);


            //All processing is complete, so we do some tests to see if we really saw everything
            if (HiveLength() != TotalBytesRead)
            {
                var remainingHive = ReadBytesFromHive(TotalBytesRead, (int)(HiveLength() - TotalBytesRead));

                //Sometimes the remainder of the file is all zeros, which is useless, so check for that
                if (!Array.TrueForAll(remainingHive, a => a == 0))
                {
                    _msgArgs = new MessageEventArgs
                    {
                        Detail = string.Format("Extra, non-zero data found beyond hive length! Check for erroneous data starting at 0x{0:x}!", HiveLength()),
                        Exception = null,
                        Message = string.Format("Extra, non-zero data found beyond hive length! Check for erroneous data starting at 0x{0:x}!", HiveLength()),
                        MsgType = MessageEventArgs.MsgTypeEnum.Warning

                    };

                    OnMessage(_msgArgs);

                }

                //as a second check, compare Header length with what we read (taking the header into account as Header.Length is only for hbin records)
                try
                {
                    Check.That((long)Header.Length).IsEqualTo(TotalBytesRead - 0x1000);
                }
                catch (Exception)
                {
                    _msgArgs = new MessageEventArgs
                    {
                        Detail = string.Format("Hive length (0x{0:x}) does not equal bytes read (0x{1:x})!! Check the end of the hive for erroneous data", HiveLength(), TotalBytesRead),
                        Exception = null,
                        Message = string.Format("Hive length (0x{0:x}) does not equal bytes read (0x{1:x})!! Check the end of the hive for erroneous data", HiveLength(), TotalBytesRead),
                        MsgType = MessageEventArgs.MsgTypeEnum.Warning

                    };

                    OnMessage(_msgArgs);

                }
            }


      

            //TODO split this out into separate functions

            //TODO MOVE THE stuff from program.cs inside the class so we have access to the kinds of things calculated there.
            //copy pasted for now

            var unreferencedNKCells = CellRecords.Where(t => t.Value.IsReferenced == false && t.Value is NKCellRecord);
            //CellRecords.Where(t => t.Value.IsReferenced == false && t.Value is VKCellRecord);

            //for each unref NK, look for VKs associated with it and create RegistryKey objects
            //save to DeletedRegistryKeys collection

            var _deletedRegistryKeys = new Dictionary<long, RegistryKey>();

            //Phase one is to associate any value records with key records
            foreach (var unreferencedNkCell in unreferencedNKCells)
            {
                try
                {
                    var nk = unreferencedNkCell.Value as NKCellRecord;

                    //System.Diagnostics.Debug.WriteLine("reloffset: 0x{0}", nk.RelativeOffset);

                    //if (nk.RelativeOffset == 0x2620B0)
                    //    System.Diagnostics.Debug.WriteLine("reloffset: 0x{0}", nk.RelativeOffset);
                    

                    var regKey = new RegistryKey(nk, string.Empty)
                    {
                        IsDeleted = true
                    };
                                 
                        //Build ValueOffsets for this NKRecord
                        if (regKey.NKRecord.ValueListCellIndex > 0)
                        {
                            //there are values for this key, so get the offsets so we can pull them next

                            DataNode offsetList = null;

                            if (DataRecords.ContainsKey(regKey.NKRecord.ValueListCellIndex))
                            {
                                offsetList = DataRecords[regKey.NKRecord.ValueListCellIndex];
                                offsetList.IsReferenced = true;
                            }
                            else
                            {
                                if (Verbosity == VerbosityEnum.Full)
                                {
                                    _msgArgs = new MessageEventArgs { Detail = string.Format("When getting values for nk record at relative offset 0x{0:X}, no data record found at offset 0x{1:X} containing value offsets. Getting data from hive...",
                                    nk.RelativeOffset, regKey.NKRecord.ValueListCellIndex),
                                    Exception = null,
                                    Message = string.Format("When getting values for nk record at relative offset 0x{0:X}, no data record found at offset 0x{1:X} containing value offsets. Getting data from hive...",
                                    nk.RelativeOffset, regKey.NKRecord.ValueListCellIndex),
                                    MsgType = MessageEventArgs.MsgTypeEnum.Warning };

                                    OnMessage(_msgArgs);
                                }


                                //if we are here that means there was data cells next to each other and as such, they could not be found via normal means.
                                //so we read it from the hive directly

                                var size = ReadBytesFromHive(regKey.NKRecord.ValueListCellIndex + 4096, 4);

                                try
                                {
                                    var rawData = ReadBytesFromHive(regKey.NKRecord.ValueListCellIndex + 4096, (int)BitConverter.ToUInt32(size, 0));

                                    var dr = new DataNode(rawData, regKey.NKRecord.ValueListCellIndex)
                                    {
                                        IsReferenced = true
                                    };
                                    DataRecords.Add(regKey.NKRecord.ValueListCellIndex, dr);
                                    
                                    offsetList = dr;
                                }
                                catch (Exception)
                                {
                                    //sometimes the data node doesnt have enough data to even do this, or its wrong data
                                if (Verbosity == VerbosityEnum.Full)
                                {
_msgArgs = new MessageEventArgs { Detail = string.Format("\t**** When getting values for nk record at relative offset 0x{0:X}, not enough/invalid data was found at offset 0x{1:X} to look for value offsets. Value recovery is not possible", nk.RelativeOffset, regKey.NKRecord.ValueListCellIndex),
                                    Exception = null,
                                                                      Message = string.Format("\t**** When getting values for nk record at relative offset 0x{0:X}, not enoughinvalid/invalid data was found at offset 0x{1:X} to look for value offsets. Value recovery is not possible", nk.RelativeOffset, regKey.NKRecord.ValueListCellIndex),
                                    MsgType = MessageEventArgs.MsgTypeEnum.Warning };

                                    OnMessage(_msgArgs);

                                }
                                    
                                }
                            }

                            if (offsetList != null)
                            {
                                offsetList.IsReferenced = true;


                                try
                                {
                                    for (var i = 0; i < regKey.NKRecord.ValueListCount; i++)
                                    {

                                        //use i * 4 so we get 4, 8, 12, 16, etc
                                        var os = BitConverter.ToUInt32(offsetList.Data, i * 4);

                                        regKey.NKRecord.ValueOffsets.Add(os);
                                    }
                                }
                                catch (Exception ex)
                                {
                                if (Verbosity == VerbosityEnum.Full)
                                {
//we are out of Data
                                    _msgArgs = new MessageEventArgs { Detail = string.Format("\t**** When getting value offsets for nk record at relative offset 0x{0:X}, not enough data was found at offset 0x{1:X} to look for all value offsets. Only partial value recovery possible", nk.RelativeOffset, regKey.NKRecord.ValueListCellIndex),
                                    Exception = null,
                                    Message = string.Format("\t**** When getting value offsets for nk record at relative offset 0x{0:X}, not enough data was found at offset 0x{1:X} to look for all value offsets. Only partial value recovery possible", nk.RelativeOffset, regKey.NKRecord.ValueListCellIndex),
                                    MsgType = MessageEventArgs.MsgTypeEnum.Warning };

                                    OnMessage(_msgArgs);
                                }
                                    
                                }
                            }
                        }

                    //For each value offset, get the vk record if it exists, create a KeyValue, and assign it to the current RegistryKey
                    foreach (var valueOffset in nk.ValueOffsets)
                    {
                        if (CellRecords.ContainsKey((long)valueOffset))
                        {
                            var val = CellRecords[(long)valueOffset] as VKCellRecord;
                            
                            //we have a value for this key

                            if (val != null)
                            {
                                val.IsReferenced = true;

                                foreach (var dataOffset in val.DataOffets)
                                {
                                    if (DataRecords.ContainsKey((long) dataOffset))
                                    {
                                        DataRecords[(long) dataOffset].IsReferenced = true;
                                    }
                                }

                                //TODO should this check to see if the vk record IsFree? probably not a bad idea
                                var kv = new KeyValue(val.ValueName, val.DataType.ToString(), val.ValueData.ToString(),
                                BitConverter.ToString(val.ValueDataSlack), val.ValueDataSlack, val);

                                regKey.Values.Add(kv);
                            }
                        }
                        else
                        {
                            if (Verbosity == VerbosityEnum.Full)
                            {
                                 _msgArgs = new MessageEventArgs
                                {
                                    Detail = string.Format("\t**** When getting values for nk record at relative offset 0x{0:X}, VK record at relative offset 0x{1:X} was not found",
                                    nk.RelativeOffset, valueOffset),
                                    Exception = null,
                                    Message = string.Format("\t**** When getting values for nk record at relative offset 0x{0:X}, VK record at relative offset 0x{1:X} was not found",
                                    nk.RelativeOffset, valueOffset),
                                    MsgType = MessageEventArgs.MsgTypeEnum.Warning

                                };

                                OnMessage(_msgArgs);

                            }
                        }
                    }
                    
                    if (Verbosity == VerbosityEnum.Full)
                    {
                         _msgArgs = new MessageEventArgs
                        {
                            Detail = string.Format("\tAssociated {0:N0} value(s) out of {1:N0} possible values for nk record at relative offset 0x{2:X}",
                            regKey.Values.Count, nk.ValueListCount, nk.RelativeOffset),
                            Exception = null,
                            Message = string.Format("\tAssociated {0:N0} value(s) out of {1:N0} possible values for nk record at relative offset 0x{2:X}",
                            regKey.Values.Count, nk.ValueListCount, nk.RelativeOffset),
                            MsgType = MessageEventArgs.MsgTypeEnum.Warning

                        };

                        OnMessage(_msgArgs);

                        
                        }


                    _deletedRegistryKeys.Add(nk.RelativeOffset, regKey);
                }
                catch (Exception ex)
                {
                       _msgArgs = new MessageEventArgs
                        {
                            Detail = string.Format("\tError {0} for nk record at relative offset 0x{1:X}",
                            ex.Message, unreferencedNkCell.Value.RelativeOffset),
                            Exception = ex,
                            Message = string.Format("\tError {0} for nk record at relative offset 0x{1:X}",
                            ex.Message, unreferencedNkCell.Value.RelativeOffset),
                      
                            MsgType = MessageEventArgs.MsgTypeEnum.Warning

                        };

                        OnMessage(_msgArgs);

                        
                
                }

            }

            //DeletedRegistryKeys now contains all deleted nk records and their associated values.
            //Phase 2 is to build a tree of key/subkeys

            #region OutputTesting

            //var baseDir1 = @"C:\temp";
            //var baseFname1 = "DeletedRegistryKeys.txt";

            //var outfile1 = Path.Combine(baseDir1, baseFname1);

            //File.WriteAllText(outfile1,
            //    "KeyName-ValueCount-RelativeOffset-ParentCellIndex\r\n\tValues (Name-->Value)\r\n\r\n");

            //foreach (var keyValuePair in DeletedRegistryKeys)
            //{
            //    var sb = new StringBuilder();

            //    foreach (var value in keyValuePair.Value.Values)
            //    {
            //        sb.AppendLine(string.Format("\t{0}--->{1}", value.ValueName, value.ValueData));
            //    }

            //    var content =
            //        string.Format(
            //            "{0}-{1:N0}, Rel offset: 0x{2:X}, Parent Cell Offset: 0x{3:X}\r\n{4}---------------------------\r\n",
            //            keyValuePair.Value.KeyPath, keyValuePair.Value.Values.Count,
            //            keyValuePair.Value.NKRecord.RelativeOffset, keyValuePair.Value.NKRecord.ParentCellIndex, sb);



            //    //  var content = string.Format("{0}\r\n---------------------------\r\n\r\n", keyValuePair.Value == null ? "(Null)" : keyValuePair.Value.ToString());


            //    File.AppendAllText(outfile1, content);
            //}

            #endregion


            //TODO Initial testing indicates this is working. do more testing/validation to make sure we do not need to make this recursive to the lowest level of subkeys
            var matchFound = true;
            while (matchFound)
            {
                var keysToRemove = new List<long>();
                matchFound = false;

                foreach (var deletedRegistryKey in _deletedRegistryKeys)
                {
                    if (_deletedRegistryKeys.ContainsKey(deletedRegistryKey.Value.NKRecord.ParentCellIndex))
                    {
                        //deletedRegistryKey is a child of RegistryKey with relative offset ParentCellIndex

                        //add the key as as subkey of its parent
                        var parent = _deletedRegistryKeys[deletedRegistryKey.Value.NKRecord.ParentCellIndex];
                        
                        deletedRegistryKey.Value.KeyPath = string.Format(@"{0}\{1}", parent.KeyPath,
                            deletedRegistryKey.Value.KeyName);
                        
                        parent.SubKeys.Add(deletedRegistryKey.Value);
                        
                        //mark the subkey for deletion so we do not blow up the collection while iterating it
                        keysToRemove.Add(deletedRegistryKey.Value.NKRecord.RelativeOffset);

                        //reset this so the loop continutes
                        matchFound = true;
                    }

                }

                foreach (var l in keysToRemove)
                {
                    //take out the key from main collection since we copied it above to its parent's subkey list
                    _deletedRegistryKeys.Remove(l);
                }
            }


            RestoredDeletedKeyCount = 0;

            //TODO  make this optional or not at all? perhaps just buuld the correct keypath and show them under the deleted nodes?
            //Phase 3 is looking at top level keys from Phase 2 and seeing if any of those can be assigned to non-deleted keys in the main tree
            //foreach (var deletedRegistryKey in _deletedRegistryKeys)
            //{
            //    if (CellRecords.ContainsKey(deletedRegistryKey.Value.NKRecord.ParentCellIndex))
            //    {
            //        //an parent key has been located, so get it
            //        var parentNK = CellRecords[deletedRegistryKey.Value.NKRecord.ParentCellIndex] as NKCellRecord;

            //        if (parentNK == null)
            //                {
            //                    //the data at that index is not an nkrecord
            //                    continue;
            //                }

            //        if (parentNK.IsReferenced)
            //        {
            //            //parent exists in our primary tree, so get that key
            //            var pk = FindKey(deletedRegistryKey.Value.NKRecord.ParentCellIndex, Root);

            //            deletedRegistryKey.Value.KeyPath = string.Format(@"{0}\{1}", pk.KeyPath,
            //               deletedRegistryKey.Value.KeyName);

            //            foreach (var sk in deletedRegistryKey.Value.SubKeys)
            //            {
            //                sk.KeyPath = string.Format(@"{0}\{1}", deletedRegistryKey.Value.KeyPath,
            //               sk.KeyName);
            //            }
                        
            //            //add a copy of deletedRegistryKey under its original parent
            //            pk.SubKeys.Add(deletedRegistryKey.Value);


            //            if (Verbosity == VerbosityEnum.Full)
            //            {
            //                 _msgArgs = new MessageEventArgs
            //                {
            //                    Detail = string.Format("\tAssociated deleted key at relative offset 0x{0:X} to active parent key at relative offset 0x{1:X}",
            //                    deletedRegistryKey.Value.NKRecord.RelativeOffset, pk.NKRecord.RelativeOffset),
            //                    Exception = null,
            //                    Message = string.Format("\tAssociated deleted key at relative offset 0x{0:X} to active parent key at relative offset 0x{1:X}",
            //                    deletedRegistryKey.Value.NKRecord.RelativeOffset, pk.NKRecord.RelativeOffset),
            //                    MsgType = MessageEventArgs.MsgTypeEnum.Warning

            //                };

            //                OnMessage(_msgArgs);
            //            }

            //            RestoredDeletedKeyCount += 1;
            //        }
            //    }
            //}
            
            DeletedRegistryKeys = _deletedRegistryKeys.Values.ToList();
            
           

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
