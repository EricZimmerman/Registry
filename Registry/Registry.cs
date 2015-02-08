using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public class RegistryHive
    {
        public enum HiveTypeEnum
        {
            [Description("Other")] Other = 0,
            [Description("NTUSER")] NtUser = 1,
            [Description("SAM")] Sam = 2,
            [Description("SECURITY")] Security = 3,
            [Description("SOFTWARE")] Software = 4,
            [Description("SYSTEM")] System = 5,
            [Description("USRCLASS")] UsrClass = 6
        }

        public enum VerbosityEnum
        {
            Normal,
            Full
        }

        internal static int _hardParsingErrors;
        internal static int _softParsingErrors;
        internal static byte[] FileBytes;
        public static long TotalBytesRead;
        private readonly Dictionary<string, RegistryKey> KeyPathKeyMap = new Dictionary<string, RegistryKey>();
        private readonly Dictionary<long, RegistryKey> RelativeOffsetKeyMap = new Dictionary<long, RegistryKey>();
        private MessageEventArgs _msgArgs;

        /// <summary>
        ///     Initializes a new instance of the
        ///     <see cref="Registry" />
        ///     class.
        /// </summary>
        public RegistryHive(string fileName)
        {
            Filename = fileName;

            if (Filename == null)
            {
                throw new ArgumentNullException("Filename cannot be null");
            }

            if (!File.Exists(Filename))
            {
                throw new FileNotFoundException();
            }

            HivePath = Filename;

            CellRecords = new Dictionary<long, ICellTemplate>();
            ListRecords = new Dictionary<long, IListTemplate>();
            //  DataRecords = new Dictionary<long, DataNode>();
            HBinRecords = new Dictionary<long, HBinRecord>();

            var fileStream = new FileStream(Filename, FileMode.Open);
            var binaryReader = new BinaryReader(fileStream);

            binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);

            FileBytes = binaryReader.ReadBytes((int) binaryReader.BaseStream.Length);

            binaryReader.Close();
            fileStream.Close();

            DeletedRegistryKeys = new List<RegistryKey>();
            UnassociatedRegistryValues = new List<KeyValue>();

            Verbosity = VerbosityEnum.Normal;
        }

        public HiveTypeEnum HiveType { get; private set; }
        public string HivePath { get; private set; }

        /// <summary>
        ///     Contains all recovered
        /// </summary>
        public List<RegistryKey> DeletedRegistryKeys { get; private set; }

        public List<KeyValue> UnassociatedRegistryValues { get; }

        /// <summary>
        ///     List of all NK, VK, and SK cell records, both in use and free, as found in the hive
        /// </summary>
        public Dictionary<long, ICellTemplate> CellRecords { get; }

        /// <summary>
        ///     List of all data nodes, both in use and free, as found in the hive
        /// </summary>
        //  public Dictionary<long, DataNode> DataRecords { get; private set; }
        public string Filename { get; }

        /// <summary>
        ///     The total number of record parsing errors where the records were IsFree == false
        /// </summary>
        public int HardParsingErrors
        {
            get { return _hardParsingErrors; }
        }

        public Dictionary<long, HBinRecord> HBinRecords { get; }
        public RegistryHeader Header { get; private set; }

        /// <summary>
        ///     List of all DB, LI, RI, LH, and LF list records, both in use and free, as found in the hive
        /// </summary>
        public Dictionary<long, IListTemplate> ListRecords { get; }

        public RegistryKey Root { get; private set; }

        /// <summary>
        ///     The total number of record parsing errors where the records were IsFree == true
        /// </summary>
        public int SoftParsingErrors
        {
            get { return _softParsingErrors; }
        }

        public static VerbosityEnum Verbosity { get; private set; }
        public event EventHandler<MessageEventArgs> Message;

        protected virtual void OnMessage(MessageEventArgs e)
        {
            var handler = Message;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void DumpKeyCommonFormat(RegistryKey key, StreamWriter sw, ref int keyCount,
            ref int valueCount)
        {
            foreach (var subkey in key.SubKeys)
            {
                keyCount += 1;

                sw.WriteLine("key|{0}|{1}|{2}|{3}", subkey.NKRecord.IsFree ? "U" : "A",
                    subkey.NKRecord.AbsoluteOffset, subkey.KeyName,
                    subkey.LastWriteTime.Value.UtcDateTime.ToString("o"));

                foreach (var val in subkey.Values)
                {
                    valueCount += 1;


                    sw.WriteLine(@"value|{0}|{1}|{2}|{3}|{4}|{5}", val.VKRecord.IsFree ? "U" : "A",
                        val.VKRecord.AbsoluteOffset, subkey.KeyName, val.ValueName, (int) val.VKRecord.DataType,
                        BitConverter.ToString(val.VKRecord.ValueDataRaw).Replace("-", " "));
                }

                DumpKeyCommonFormat(subkey, sw, ref keyCount, ref valueCount);
            }
        }

        private DataNode GetDataNodeFromOffset(long relativeOffset)
        {
            var dataLenBytes = ReadBytesFromHive(relativeOffset + 4096, 4);
            var dataLen = BitConverter.ToUInt32(dataLenBytes, 0);
            var size = (int) dataLen;
            size = Math.Abs(size);

            var dn = new DataNode(ReadBytesFromHive(relativeOffset + 4096, size), relativeOffset);

            return dn;
        }

        //TODO this needs refactored to remove duplicated code
        private List<RegistryKey> GetSubKeysAndValues(RegistryKey key)
        {
            RelativeOffsetKeyMap.Add(key.NKRecord.RelativeOffset, key);

            KeyPathKeyMap.Add(key.KeyPath.Replace(string.Format("{0}\\", Root.KeyName), ""), key);


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

            key.KeyFlags = RegistryKey.KeyFlagsEnum.HasActiveParent;
            //key.KeyFlags = key.KeyFlags | RegistryKey.KeyFlagsEnum.HasActiveParent;

            var keys = new List<RegistryKey>();

            if (key.NKRecord.ClassCellIndex > 0)
            {
                var d = GetDataNodeFromOffset(key.NKRecord.ClassCellIndex);
                d.IsReferenced = true;
                var clsName = Encoding.Unicode.GetString(d.Data, 0, key.NKRecord.ClassLength);
                key.ClassName = clsName;
            }

            //Build ValueOffsets for this NKRecord
            if (key.NKRecord.ValueListCellIndex > 0)
            {
                //there are values for this key, so get the offsets so we can pull them next

                var offsetList = GetDataNodeFromOffset(key.NKRecord.ValueListCellIndex);

                offsetList.IsReferenced = true;

                for (var i = 0; i < key.NKRecord.ValueListCount; i++)
                {
                    //use i * 4 so we get 4, 8, 12, 16, etc
                    var os = BitConverter.ToUInt32(offsetList.Data, i*4);

                    key.NKRecord.ValueOffsets.Add(os);
                }
            }

            if (key.NKRecord.ValueOffsets.Count != key.NKRecord.ValueListCount)
            {
                var args = new MessageEventArgs
                {
                    Detail =
                        string.Format(
                            "Value count mismatch! ValueListCount is {0:N0} but NKRecord.ValueOffsets.Count is {1:N0}",
                            key.NKRecord.ValueListCount, key.NKRecord.ValueOffsets.Count),
                    Exception = null,
                    Message = ("Value count mismatch!"),
                    MsgType = MessageEventArgs.MsgTypeEnum.Warning
                };

                OnMessage(args);
            }

            // look for values in this key 
            foreach (var valueOffset in key.NKRecord.ValueOffsets)
            {
                var vc = CellRecords[(long) valueOffset];

                var vk = vc as VKCellRecord;

                vk.IsReferenced = true;

                var dbListProcessed = false;

                foreach (var dataOffet in vk.DataOffets)
                {
                    //there is a special case when registry version > 1.4 and size > 16344
                    //if this is true, the first offset is a db record, found with the lists
                    if ((Header.MinorVersion > 4) && vk.DataLength > 16344 && dbListProcessed == false)
                    {
                        var db = ListRecords[(long) dataOffet];

                        var dbr = db as DBListRecord;
                        dbr.IsReferenced = true;
                        dbListProcessed = true;
                    }
//                    else
//                    {
//                        GetDataNodeFromOffset((long) dataOffet).IsReferenced = true;
//                    }
                }


                var value = new KeyValue(vk);


                key.Values.Add(value);
            }

            var sk = CellRecords[key.NKRecord.SecurityCellIndex] as SKCellRecord;
            sk.IsReferenced = true;

            //TODO THIS SHOULD ALSO CHECK THE # OF SUBKEYS == 0
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

                        var tempKey = new RegistryKey(nk, key);

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

                                var tempKey = new RegistryKey(nk, key);

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

                                var tempKey = new RegistryKey(nk, key);

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

                        var tempKey = new RegistryKey(nk, key);

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

        /// <summary>
        ///     Returns the length, in bytes, of the file being processed
        ///     <remarks>This is the length returned by the underlying stream used to open the file</remarks>
        /// </summary>
        /// <returns></returns>
        protected internal int HiveLength()
        {
            return FileBytes.Length;
        }

        /// <summary>
        ///     Reads 'length' bytes from the registry starting at 'offset' and returns a byte array
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        protected internal static byte[] ReadBytesFromHive(long offset, int length)
        {
            var absLen = Math.Abs(length);
            var retArray = new byte[absLen];
            Array.Copy(FileBytes, offset, retArray, 0, absLen);
            return retArray;
        }

        // public methods...


        public void ExportDataToCommonFormat(string outfile, bool deletedOnly)
        {
            var KeyCount = 0; //root key
            var ValueCount = 0;
            var KeyCountDeleted = 0;
            var ValueCountDeleted = 0;

            using (var sw = new StreamWriter(outfile, false))
            {
                sw.AutoFlush = true;

                if (!deletedOnly)
                {
                    if (Root.LastWriteTime != null)
                    {
                        KeyCount = 1;
                        sw.WriteLine("key|{0}|{1}|{2}|{3}", Root.NKRecord.IsFree ? "U" : "A",
                            Root.NKRecord.AbsoluteOffset,
                            Root.KeyPath, Root.LastWriteTime.Value.UtcDateTime.ToString("o"));
                    }

                    foreach (var val in Root.Values)
                    {
                        ValueCount += 1;
                        sw.WriteLine(@"value|{0}|{1}|{2}|{3}|{4}|{5}", val.VKRecord.IsFree ? "U" : "A",
                            val.VKRecord.AbsoluteOffset, Root.KeyPath, val.ValueName, (int) val.VKRecord.DataType,
                            BitConverter.ToString(val.VKRecord.ValueDataRaw).Replace("-", " "));
                    }


                    DumpKeyCommonFormat(Root, sw, ref KeyCount, ref ValueCount);
                }

                var theRest = CellRecords.Where(a => a.Value.IsReferenced == false);
                //may not need to if we do not care about orphaned values

                foreach (var keyValuePair in theRest)
                {
                    if (keyValuePair.Value.Signature == "vk")
                    {
                        ValueCountDeleted += 1;
                        var val = keyValuePair.Value as VKCellRecord;
                        sw.WriteLine(@"value|{0}|{1}|{2}|{3}|{4}|{5}", val.IsFree ? "U" : "A", val.AbsoluteOffset, "",
                            val.ValueName, (int) val.DataType, BitConverter.ToString(val.ValueDataRaw).Replace("-", " "));
                    }

                    if (keyValuePair.Value.Signature == "nk")
                    {
                        //this should never be once we re-enable deleted key rebuilding

                        KeyCountDeleted += 1;
                        var nk = keyValuePair.Value as NKCellRecord;
                        var key = new RegistryKey(nk, null);

                        sw.WriteLine("key|{0}|{1}|{2}|{3}", key.NKRecord.IsFree ? "U" : "A",
                            key.NKRecord.AbsoluteOffset, key.KeyName,
                            key.LastWriteTime.Value.UtcDateTime.ToString("o"));

                        DumpKeyCommonFormat(key, sw, ref KeyCountDeleted, ref ValueCountDeleted);
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
            if (KeyPathKeyMap.ContainsKey(keypath))
            {
                return KeyPathKeyMap[keypath];
            }
            return null;
        }

        public RegistryKey FindKey(long relativeOffset, RegistryKey parent)
        {
            if (RelativeOffsetKeyMap.ContainsKey(relativeOffset))
            {
                return RelativeOffsetKeyMap[relativeOffset];
            }

            return null;
        }

        public bool ParseHive()
        {
            if (Filename == null)
            {
                throw new ArgumentNullException("Filename cannot be null");
            }

            if (!File.Exists(Filename))
            {
                throw new FileNotFoundException();
            }

            var header = ReadBytesFromHive(0, 4096);

            TotalBytesRead = 0;

            TotalBytesRead += 4096;

            Header = new RegistryHeader(header);

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
                case "usrclass.dat":
                    HiveType = HiveTypeEnum.UsrClass;
                    break;

                default:
                    HiveType = HiveTypeEnum.Other;
                    break;
            }


            var version = float.Parse(string.Format("{0}.{1}", Header.MajorVersion, Header.MinorVersion));

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
                        Detail = string.Format("Percent done: {0:P}", (double) offsetInHive/hivelen),
                        Exception = null,
                        Message = string.Format("hbin header incorrect at offset 0x{0:X}!!!", offsetInHive),
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
                        Detail =
                            string.Format("Size 0x{0:X}\t\t\t\tPercent done: {1:P}", hbinSize,
                                (double) offsetInHive/hivelen),
                        Exception = null,
                        Message = string.Format("Pulling hbin at offset 0x{0:X}.", offsetInHive),
                        MsgType = MessageEventArgs.MsgTypeEnum.Info
                    };

                    OnMessage(args);
                }

                var rawhbin = ReadBytesFromHive(offsetInHive, (int) hbinSize);


                try
                {
                    var h = new HBinRecord(rawhbin, offsetInHive - 4096, version);

                    h.Message += (ss, ee) => { OnMessage(ee); };

                    var records = h.Process();

                    foreach (var record in records)
                    {
                        switch (record.Signature)
                        {
                            case "nk":
                            case "sk":
                            case "lk":
                            case "vk":
                                CellRecords.Add(record.AbsoluteOffset - 4096, (ICellTemplate) record);
                                break;

                            case "db":
                            case "li":
                            case "ri":
                            case "lh":
                            case "lf":
                                ListRecords.Add(record.AbsoluteOffset - 4096, (IListTemplate) record);
                                break;

                            case "":
                                //  DataRecords.Add(record.AbsoluteOffset - 4096, (DataNode) record);
                                break;
                            default:

                                Debug.Write(1);
                                break;
                        }
                    }


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
                Detail = ("Initial processing complete. Building tree..."),
                Exception = null,
                Message = ("Initial processing complete. Building tree..."),
                MsgType = MessageEventArgs.MsgTypeEnum.Info
            };

            OnMessage(_msgArgs);


            //The root node can be found by either looking at Header.RootCellOffset or looking for an nk record with HiveEntryRootKey flag set.
            //here we are looking for the flag
            var rootNode =
                CellRecords.Values.OfType<NKCellRecord>()
                    .SingleOrDefault(
                        (f => f.Flags.ToString().Contains(NKCellRecord.FlagEnum.HiveEntryRootKey.ToString())));

            if (rootNode == null)
            {
                throw new Exception("Root nk record not found!");
            }

            //validate what we found above via the flag method
            Check.That((long) Header.RootCellOffset).IsEqualTo(rootNode.RelativeOffset);

            rootNode.IsReferenced = true;

            _msgArgs = new MessageEventArgs
            {
                Detail = ("Found root node! Getting subkeys..."),
                Exception = null,
                Message = ("Found root node! Getting subkeys..."),
                MsgType = MessageEventArgs.MsgTypeEnum.Info
            };

            OnMessage(_msgArgs);

            Root = new RegistryKey(rootNode, null);


            var keys = GetSubKeysAndValues(Root);


            Root.SubKeys.AddRange(keys);

            _msgArgs = new MessageEventArgs
            {
                Detail = "Processing complete! Call BuildDeletedRegistryKeys to rebuild deleted record structures",
                Exception = null,
                Message = "Processing complete! Call BuildDeletedRegistryKeys to rebuild deleted record structures",
                MsgType = MessageEventArgs.MsgTypeEnum.Info
            };

            OnMessage(_msgArgs);

            //All processing is complete, so we do some tests to see if we really saw everything
            if (HiveLength() != TotalBytesRead)
            {
                var remainingHive = ReadBytesFromHive(TotalBytesRead, (int) (HiveLength() - TotalBytesRead));

                //Sometimes the remainder of the file is all zeros, which is useless, so check for that
                if (!Array.TrueForAll(remainingHive, a => a == 0))
                {
                    _msgArgs = new MessageEventArgs
                    {
                        Detail =
                            string.Format(
                                "Extra, non-zero data found beyond hive length! Check for erroneous data starting at 0x{0:x}!",
                                TotalBytesRead),
                        Exception = null,
                        Message =
                            string.Format(
                                "Extra, non-zero data found beyond hive length! Check for erroneous data starting at 0x{0:x}!",
                                TotalBytesRead),
                        MsgType = MessageEventArgs.MsgTypeEnum.Warning
                    };

                    OnMessage(_msgArgs);
                }

                //as a second check, compare Header length with what we read (taking the header into account as Header.Length is only for hbin records)
                try
                {
                    Check.That((long) Header.Length).IsEqualTo(TotalBytesRead - 0x1000);
                }
                catch (Exception)
                {
                    _msgArgs = new MessageEventArgs
                    {
                        Detail =
                            string.Format(
                                "Hive length (0x{0:x}) does not equal bytes read (0x{1:x})!! Check the end of the hive for erroneous data",
                                HiveLength(), TotalBytesRead),
                        Exception = null,
                        Message =
                            string.Format(
                                "Hive length (0x{0:x}) does not equal bytes read (0x{1:x})!! Check the end of the hive for erroneous data",
                                HiveLength(), TotalBytesRead),
                        MsgType = MessageEventArgs.MsgTypeEnum.Warning
                    };

                    OnMessage(_msgArgs);
                }
            }


            return true;
        }

        /// <summary>
        ///     Associates vk records with NK records and builds a heirarchy of nk records
        ///     <remarks>Results of this method will be available in DeletedRegistryKeys</remarks>
        /// </summary>
        public void BuildDeletedRegistryKeys()
        {
            _msgArgs = new MessageEventArgs
            {
                Detail = "Associating deleted keys and values...",
                Exception = null,
                Message = "Associating deleted keys and values...",
                MsgType = MessageEventArgs.MsgTypeEnum.Info
            };

            OnMessage(_msgArgs);

            var unreferencedNKCells = CellRecords.Where(t => t.Value.IsReferenced == false && t.Value is NKCellRecord);

            var associatedVKRecordOffsets = new List<long>();

            var _deletedRegistryKeys = new Dictionary<long, RegistryKey>();

            //Phase one is to associate any value records with key records
            foreach (var unreferencedNkCell in unreferencedNKCells)
            {
                try
                {
                    var nk = unreferencedNkCell.Value as NKCellRecord;

                    nk.IsDeleted = true;

                    var regKey = new RegistryKey(nk, null)
                    {
                        KeyFlags = RegistryKey.KeyFlagsEnum.Deleted
                    };

                    //Build ValueOffsets for this NKRecord
                    if (regKey.NKRecord.ValueListCellIndex > 0)
                    {
                        //there are values for this key, so get the offsets so we can pull them next

                        DataNode offsetList = null;

                        var size = ReadBytesFromHive(regKey.NKRecord.ValueListCellIndex + 4096, 4);

                        var sizeNum = Math.Abs(BitConverter.ToUInt32(size, 0));

                        if (sizeNum > regKey.NKRecord.ValueListCount*4 + 4)
                        {
                            //ValueListCount is the number of offsets we should be looking for. they are 4 bytes long
                            //If the size of the data record at regKey.NKRecord.ValueListCellIndex exceeds the total number of bytes plus the size (another 4 bytes), reset it to a more sane value to avoid crazy long reads
                            sizeNum = regKey.NKRecord.ValueListCount*4 + 4;
                        }


                        try
                        {
                            var rawData = ReadBytesFromHive(regKey.NKRecord.ValueListCellIndex + 4096,
                                (int) sizeNum);

                            var dr = new DataNode(rawData, regKey.NKRecord.ValueListCellIndex);

                            offsetList = dr;
                        }
                        catch (Exception)
                        {
                            //sometimes the data node doesnt have enough data to even do this, or its wrong data
                            if (Verbosity == VerbosityEnum.Full)
                            {
                                _msgArgs = new MessageEventArgs
                                {
                                    Detail =
                                        string.Format(
                                            "\t**** When getting values for nk record at relative offset 0x{0:X}, not enough/invalid data was found at offset 0x{1:X} to look for value offsets. Value recovery is not possible",
                                            nk.RelativeOffset, regKey.NKRecord.ValueListCellIndex),
                                    Exception = null,
                                    Message =
                                        string.Format(
                                            "\t**** When getting values for nk record at relative offset 0x{0:X}, not enoughinvalid/invalid data was found at offset 0x{1:X} to look for value offsets. Value recovery is not possible",
                                            nk.RelativeOffset, regKey.NKRecord.ValueListCellIndex),
                                    MsgType = MessageEventArgs.MsgTypeEnum.Warning
                                };

                                OnMessage(_msgArgs);
                            }
                        }


                        if (offsetList != null)
                        {
                            try
                            {
                                for (var i = 0; i < regKey.NKRecord.ValueListCount; i++)
                                {
                                    //use i * 4 so we get 4, 8, 12, 16, etc
                                    var os = BitConverter.ToUInt32(offsetList.Data, i*4);

                                    regKey.NKRecord.ValueOffsets.Add(os);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (Verbosity == VerbosityEnum.Full)
                                {
                                    //we are out of Data
                                    _msgArgs = new MessageEventArgs
                                    {
                                        Detail =
                                            string.Format(
                                                "\t**** When getting value offsets for nk record at relative offset 0x{0:X}, not enough data was found at offset 0x{1:X} to look for all value offsets. Only partial value recovery possible",
                                                nk.RelativeOffset, regKey.NKRecord.ValueListCellIndex),
                                        Exception = ex,
                                        Message =
                                            string.Format(
                                                "\t**** When getting value offsets for nk record at relative offset 0x{0:X}, not enough data was found at offset 0x{1:X} to look for all value offsets. Only partial value recovery possible",
                                                nk.RelativeOffset, regKey.NKRecord.ValueListCellIndex),
                                        MsgType = MessageEventArgs.MsgTypeEnum.Warning
                                    };

                                    OnMessage(_msgArgs);
                                }
                            }
                        }
                    }


                    //For each value offset, get the vk record if it exists, create a KeyValue, and assign it to the current RegistryKey
                    foreach (var valueOffset in nk.ValueOffsets)
                    {
                        if (CellRecords.ContainsKey((long) valueOffset))
                        {
                            var val = CellRecords[(long) valueOffset] as VKCellRecord;
                            //we have a value for this key

                            if (val != null)
                            {
                                //if its an in use record AND referenced, warn
                                if (val.IsFree == false && val.IsReferenced)
                                {
                                    if (Verbosity == VerbosityEnum.Full)
                                    {
                                        _msgArgs = new MessageEventArgs
                                        {
                                            Detail =
                                                string.Format(
                                                    "\t**** When getting values for nk record at relative offset 0x{0:X}, VK record at relative offset 0x{1:X} isn't free and is referenced by another nk record. Skipping!",
                                                    nk.RelativeOffset, valueOffset),
                                            Exception = null,
                                            Message =
                                                string.Format(
                                                    "\t**** When getting values for nk record at relative offset 0x{0:X}, VK record at relative offset 0x{1:X} isn't free and is referenced by another nk record. Skipping!",
                                                    nk.RelativeOffset, valueOffset),
                                            MsgType = MessageEventArgs.MsgTypeEnum.Warning
                                        };

                                        OnMessage(_msgArgs);
                                    }
                                }
                                else
                                {
                                    //TODO WHAT IS THIS FOR?
                                    associatedVKRecordOffsets.Add(val.RelativeOffset);

                       

                                    var kv = new KeyValue(val);

                                    regKey.Values.Add(kv);
                                }
                            }
                        }
                        else
                        {
                            if (Verbosity == VerbosityEnum.Full)
                            {
                                _msgArgs = new MessageEventArgs
                                {
                                    Detail =
                                        string.Format(
                                            "\t**** When getting values for nk record at relative offset 0x{0:X}, VK record at relative offset 0x{1:X} was not found",
                                            nk.RelativeOffset, valueOffset),
                                    Exception = null,
                                    Message =
                                        string.Format(
                                            "\t**** When getting values for nk record at relative offset 0x{0:X}, VK record at relative offset 0x{1:X} was not found",
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
                            Detail =
                                string.Format(
                                    "\tAssociated {0:N0} value(s) out of {1:N0} possible values for nk record at relative offset 0x{2:X}",
                                    regKey.Values.Count, nk.ValueListCount, nk.RelativeOffset),
                            Exception = null,
                            Message =
                                string.Format(
                                    "\tAssociated {0:N0} value(s) out of {1:N0} possible values for nk record at relative offset 0x{2:X}",
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

            //Phase 3 is looking at top level keys from Phase 2 and seeing if any of those can be assigned to non-deleted keys in the main tree
            foreach (var deletedRegistryKey in _deletedRegistryKeys)
            {
                if (CellRecords.ContainsKey(deletedRegistryKey.Value.NKRecord.ParentCellIndex))
                {
                    //an parent key has been located, so get it
                    var parentNk = CellRecords[deletedRegistryKey.Value.NKRecord.ParentCellIndex] as NKCellRecord;

                    if (parentNk == null)
                    {
                        //the data at that index is not an nkrecord
                        continue;
                    }

                    if (parentNk.IsReferenced && parentNk.IsFree == false)
                    {
                        //parent exists in our primary tree, so get that key
                        var pk = FindKey(deletedRegistryKey.Value.NKRecord.ParentCellIndex, Root);

                        deletedRegistryKey.Value.KeyPath = string.Format(@"{0}\{1}", pk.KeyPath,
                            deletedRegistryKey.Value.KeyName);

                        deletedRegistryKey.Value.KeyFlags |= RegistryKey.KeyFlagsEnum.HasActiveParent;


                        //right now you get the first layer of children but thats it

                        UpdateChildPaths(deletedRegistryKey.Value);

                        //foreach (var sk in deletedRegistryKey.Value.SubKeys)
                        //{
                        //    sk.KeyPath = string.Format(@"{0}\{1}", deletedRegistryKey.Value.KeyPath,
                        //        sk.KeyName);

                        //    RelativeOffsetKeyMap.Add(sk.NKRecord.RelativeOffset, sk);
                        //    
                        //    KeyPathKeyMap.Add(sk.KeyPath.Replace(string.Format("{0}\\", Root.KeyName), ""), sk);

                        //}

                        //add a copy of deletedRegistryKey under its original parent
                        pk.SubKeys.Add(deletedRegistryKey.Value);

                        RelativeOffsetKeyMap.Add(deletedRegistryKey.Value.NKRecord.RelativeOffset,
                            deletedRegistryKey.Value);

                        if (
                            KeyPathKeyMap.ContainsKey(
                                deletedRegistryKey.Value.KeyPath.Replace(string.Format("{0}\\", Root.KeyName), "")) ==
                            false)
                        {
                            KeyPathKeyMap.Add(
                                deletedRegistryKey.Value.KeyPath.Replace(string.Format("{0}\\", Root.KeyName), ""),
                                deletedRegistryKey.Value);
                        }


                        if (Verbosity == VerbosityEnum.Full)
                        {
                            _msgArgs = new MessageEventArgs
                            {
                                Detail =
                                    string.Format(
                                        "\tAssociated deleted key at relative offset 0x{0:X} to active parent key at relative offset 0x{1:X}",
                                        deletedRegistryKey.Value.NKRecord.RelativeOffset, pk.NKRecord.RelativeOffset),
                                Exception = null,
                                Message =
                                    string.Format(
                                        "\tAssociated deleted key at relative offset 0x{0:X} to active parent key at relative offset 0x{1:X}",
                                        deletedRegistryKey.Value.NKRecord.RelativeOffset, pk.NKRecord.RelativeOffset),
                                MsgType = MessageEventArgs.MsgTypeEnum.Warning
                            };

                            OnMessage(_msgArgs);
                        }
                    }
                }
            }

            DeletedRegistryKeys = _deletedRegistryKeys.Values.ToList();

            var unreferencedVk = CellRecords.Where(t => t.Value.IsReferenced == false && t.Value is VKCellRecord);

            foreach (var keyValuePair in unreferencedVk)
            {
                if (associatedVKRecordOffsets.Contains(keyValuePair.Key) == false)
                {
                    var vk = keyValuePair.Value as VKCellRecord;

                    var val = new KeyValue(vk);

                    UnassociatedRegistryValues.Add(val);
                }
            }
        }

        private void UpdateChildPaths(RegistryKey key)
        {
            foreach (var sk in key.SubKeys)
            {
                sk.KeyPath = string.Format(@"{0}\{1}", key.KeyPath,
                    sk.KeyName);

                RelativeOffsetKeyMap.Add(sk.NKRecord.RelativeOffset, sk);

                var keyNormalized = sk.KeyPath.Replace(string.Format("{0}\\", Root.KeyName), "");

                if (KeyPathKeyMap.ContainsKey(keyNormalized) == false)
                {
                    KeyPathKeyMap.Add(keyNormalized, sk);
                }


                UpdateChildPaths(sk);
            }
        }

        /// <summary>
        ///     Given a file, confirm it is a registry hive and that hbin headers are found every 4096 * (size of hbin) bytes.
        /// </summary>
        /// <returns></returns>
        public HiveMetadata Verify()
        {
            const int regfHeader = 0x66676572;
            const int hbinHeader = 0x6e696268;

            if (Filename == null)
            {
                throw new ArgumentNullException("Filename cannot be null");
            }

            if (!File.Exists(Filename))
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
                var hbinSig = BitConverter.ToUInt32(ReadBytesFromHive(offset, 4), 0);

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