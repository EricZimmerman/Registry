using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NFluent;
using NLog;
using Registry.Abstractions;
using Registry.Cells;
using Registry.Lists;
using Registry.Other;
using static Registry.Other.Helpers;

namespace Registry
{
    // public classes...
    public class RegistryHive : RegistryBase
    {
        internal static int _hardParsingErrors;
        internal static int _softParsingErrors;
        private readonly Dictionary<string, RegistryKey> KeyPathKeyMap = new Dictionary<string, RegistryKey>();
        private readonly Dictionary<long, RegistryKey> RelativeOffsetKeyMap = new Dictionary<long, RegistryKey>();
        private bool _parsed;

        /// <summary>
        ///     If true, CellRecords and ListRecords will be purged to free memory
        /// </summary>
        public bool FlushRecordListsAfterParse = true;

        /// <summary>
        ///     Initializes a new instance of the
        ///     <see cref="Registry" />
        ///     class.
        /// </summary>
        public RegistryHive(string hivePath) : base(hivePath)
        {
            CellRecords = new Dictionary<long, ICellTemplate>();
            ListRecords = new Dictionary<long, IListTemplate>();

            DeletedRegistryKeys = new List<RegistryKey>();
            UnassociatedRegistryValues = new List<KeyValue>();
        }

        public RegistryHive(byte[] rawBytes) : base(rawBytes)
        {
            CellRecords = new Dictionary<long, ICellTemplate>();
            ListRecords = new Dictionary<long, IListTemplate>();

            DeletedRegistryKeys = new List<RegistryKey>();
            UnassociatedRegistryValues = new List<KeyValue>();
        }

        public bool RecoverDeleted { get; set; }

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
        ///     The total number of record parsing errors where the records were IsFree == false
        /// </summary>
        public int HardParsingErrors => _hardParsingErrors;

        public uint HBinRecordTotalSize { get; private set; } //Dictionary<long, HBinRecord>
        public int HBinRecordCount { get; private set; } //Dictionary<long, HBinRecord>

        /// <summary>
        ///     List of all DB, LI, RI, LH, and LF list records, both in use and free, as found in the hive
        /// </summary>
        public Dictionary<long, IListTemplate> ListRecords { get; }

        public RegistryKey Root { get; private set; }

        /// <summary>
        ///     The total number of record parsing errors where the records were IsFree == true
        /// </summary>
        public int SoftParsingErrors => _softParsingErrors;

        private void DumpKeyCommonFormat(RegistryKey key, StreamWriter sw, ref int keyCount,
            ref int valueCount)
        {
            if (((key.KeyFlags & RegistryKey.KeyFlagsEnum.HasActiveParent) == RegistryKey.KeyFlagsEnum.HasActiveParent) &&
                ((key.KeyFlags & RegistryKey.KeyFlagsEnum.Deleted) == RegistryKey.KeyFlagsEnum.Deleted))
            {
                return;
            }

            foreach (var subkey in key.SubKeys)
            {
                if (((subkey.KeyFlags & RegistryKey.KeyFlagsEnum.HasActiveParent) ==
                     RegistryKey.KeyFlagsEnum.HasActiveParent) &&
                    ((subkey.KeyFlags & RegistryKey.KeyFlagsEnum.Deleted) == RegistryKey.KeyFlagsEnum.Deleted))
                {
                    return;
                }

                keyCount += 1;

                sw.WriteLine("key|{0}|{1}|{2}|{3}", subkey.NKRecord.IsFree ? "U" : "A",
                    subkey.NKRecord.AbsoluteOffset, subkey.KeyPath,
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

            KeyPathKeyMap.Add(key.KeyPath.ToLowerInvariant(), key);

            _logger.Debug("Getting subkeys for {0}", key.KeyPath);

            key.KeyFlags = RegistryKey.KeyFlagsEnum.HasActiveParent;

            var keys = new List<RegistryKey>();

            if (key.NKRecord.ClassCellIndex > 0)
            {
                _logger.Debug("Getting Class cell information at relative offset 0x{0:X}", key.NKRecord.ClassCellIndex);
                var d = GetDataNodeFromOffset(key.NKRecord.ClassCellIndex);
                d.IsReferenced = true;
                var clsName = Encoding.Unicode.GetString(d.Data, 0, key.NKRecord.ClassLength);
                key.ClassName = clsName;
                _logger.Debug("Class name found {0}", clsName);
            }

            //Build ValueOffsets for this NKRecord
            if (key.NKRecord.ValueListCellIndex > 0)
            {
                //there are values for this key, so get the offsets so we can pull them next

                _logger.Debug("Getting value list offset at relative offset 0x{0:X}. Value count is {1:N0}",
                    key.NKRecord.ValueListCellIndex, key.NKRecord.ValueListCount);

                var offsetList = GetDataNodeFromOffset(key.NKRecord.ValueListCellIndex);

                offsetList.IsReferenced = true;

                for (var i = 0; i < key.NKRecord.ValueListCount; i++)
                {
                    //use i * 4 so we get 4, 8, 12, 16, etc
                    var os = BitConverter.ToUInt32(offsetList.Data, i*4);
                    _logger.Debug("Got value offset 0x{0:X}", os);
                    key.NKRecord.ValueOffsets.Add(os);
                }
            }

            if (key.NKRecord.ValueOffsets.Count != key.NKRecord.ValueListCount)
            {
//ncrunch: no coverage
                _logger.Warn(
                    "Value count mismatch! ValueListCount is {0:N0} but NKRecord.ValueOffsets.Count is {1:N0}",
                    //ncrunch: no coverage
                    key.NKRecord.ValueListCount, key.NKRecord.ValueOffsets.Count);
            } //ncrunch: no coverage

            // look for values in this key 
            foreach (var valueOffset in key.NKRecord.ValueOffsets)
            {
                _logger.Debug("Looking for vk record at relative offset 0x{0:X}", valueOffset);
                var vc = CellRecords[(long) valueOffset];

                var vk = vc as VKCellRecord;

                _logger.Debug("Found vk record at relative offset 0x{0:X}. Value name: {1}", valueOffset, vk.ValueName);

                vk.IsReferenced = true;

                var value = new KeyValue(vk);

                key.Values.Add(value);
            }

            _logger.Debug("Looking for sk record at relative offset 0x{0:X}", key.NKRecord.SecurityCellIndex);

//            var sk = CellRecords[key.NKRecord.SecurityCellIndex] as SKCellRecord;
//            sk.IsReferenced = true;

                        if (CellRecords.ContainsKey(key.NKRecord.SecurityCellIndex))
                        {
                            var sk = CellRecords[key.NKRecord.SecurityCellIndex] as SKCellRecord;
                            if (sk != null)
                            {
                                sk.IsReferenced = true;
                            }
                        }


            //TODO THIS SHOULD ALSO CHECK THE # OF SUBKEYS == 0
            if (ListRecords.ContainsKey(key.NKRecord.SubkeyListsStableCellIndex) == false)
            {
                return keys;
            }

            _logger.Debug("Looking for list record at relative offset 0x{0:X}", key.NKRecord.SubkeyListsStableCellIndex);
            var l = ListRecords[key.NKRecord.SubkeyListsStableCellIndex];

            var sig = BitConverter.ToInt16(l.RawBytes, 4);

            switch (sig)
            {
                case LfSignature:
                case LhSignature:
                    var lxRecord = l as LxListRecord;
                    lxRecord.IsReferenced = true;

                    foreach (var offset in lxRecord.Offsets)
                    {
                        _logger.Debug("In lf or lh, looking for nk record at relative offset 0x{0:X}", offset.Key);
                        var cell = CellRecords[offset.Key];

                        var nk = cell as NKCellRecord;
                        nk.IsReferenced = true;

                        _logger.Debug("In lf or lh, found nk record at relative offset 0x{0:X}. Name: {1}", offset.Key,
                            nk.Name);

                        var tempKey = new RegistryKey(nk, key);

                        var sks = GetSubKeysAndValues(tempKey);
                        tempKey.SubKeys.AddRange(sks);

                        keys.Add(tempKey);
                    }
                    break;

                case RiSignature:
                    var riRecord = l as RIListRecord;
                    riRecord.IsReferenced = true;

                    foreach (var offset in riRecord.Offsets)
                    {
                        _logger.Debug("In ri, looking for list record at relative offset 0x{0:X}", offset);
                        var tempList = ListRecords[offset];

                        //templist is now an li or lh list 

                        if (tempList.Signature == "li")
                        {
                            var sk3 = tempList as LIListRecord;

                            foreach (var offset1 in sk3.Offsets)
                            {
                                _logger.Debug("In ri/li, looking for nk record at relative offset 0x{0:X}", offset1);
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
                                _logger.Debug("In ri/li, looking for nk record at relative offset 0x{0:X}", offset3.Key);
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

                case LiSignature:
                    var liRecord = l as LIListRecord;
                    liRecord.IsReferenced = true;

                    foreach (var offset in liRecord.Offsets)
                    {
                        _logger.Debug("In li, looking for nk record at relative offset 0x{0:X}", offset);
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
                    throw new Exception($"Unknown subkey list type {l.Signature}!");
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
        ///     Exports contents of Registry to text format.
        /// </summary>
        /// <remarks>Be sure to set FlushRecordListsAfterParse to FALSE if you want deleted records included</remarks>
        /// <param name="outfile">The outfile.</param>
        /// <param name="deletedOnly">if set to <c>true</c> [deleted only].</param>
        public void ExportDataToCommonFormat(string outfile, bool deletedOnly)
        {
            var KeyCount = 0; //root key
            var ValueCount = 0;
            var KeyCountDeleted = 0;
            var ValueCountDeleted = 0;

            var header = new StringBuilder();
            header.AppendLine("## Registry common export format");
            header.AppendLine("## Key format");
            header.AppendLine(
                "## key|Is Free (A for in use, U for unused)|Absolute offset in decimal|KeyPath|LastWriteTime in UTC");
            header.AppendLine("## Value format");
            header.AppendLine(
                "## value|Is Free (A for in use, U for unused)|Absolute offset in decimal|KeyPath|Value name|Data type (as decimal integer)|Value data as bytes separated by a singe space");
            header.AppendLine("##");
            header.AppendLine(
                "## Comparison of deleted keys/values is done to compare recovery of vk and nk records, not the algorithm used to associate deleted keys to other keys and their values.");
            header.AppendLine(
                "## When including deleted keys, only the recovered key name should be included, not the full path to the deleted key.");
            header.AppendLine("## When including deleted values, do not include the parent key information.");
            header.AppendLine("##");
            header.AppendLine("## The following totals should also be included");
            header.AppendLine("##");
            header.AppendLine("## total_keys|total in use key count");
            header.AppendLine("## total_values|total in use value count");
            header.AppendLine("## total_deleted_keys|total recovered free key count");
            header.AppendLine("## total_deleted_values|total recovered free value count");
            header.AppendLine("##");
            header.AppendLine(
                "## Before comparison with other common export implementations, the files should be sorted");
            header.AppendLine("##");

            using (var sw = new StreamWriter(outfile, false))
            {
                sw.AutoFlush = true;

                sw.Write(header.ToString());

                if (deletedOnly == false)
                {
                    //dump active stuff
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
                    try
                    {
                        if (keyValuePair.Value.Signature == "vk")
                        {
                            ValueCountDeleted += 1;
                            var val = keyValuePair.Value as VKCellRecord;

                            sw.WriteLine(@"value|{0}|{1}|{2}|{3}|{4}|{5}", val.IsFree ? "U" : "A", val.AbsoluteOffset,
                                "",
                                val.ValueName, (int) val.DataType,
                                BitConverter.ToString(val.ValueDataRaw).Replace("-", " "));
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
                    catch (Exception ex)
                    {
                        _logger.Warn("There was an error exporting free record at offset 0x{0:X}. Error: {1}",
                            keyValuePair.Value.AbsoluteOffset, ex.Message);
                    }
                }

                sw.WriteLine("total_keys|{0}", KeyCount);
                sw.WriteLine("total_values|{0}", ValueCount);
                sw.WriteLine("total_deleted_keys|{0}", KeyCountDeleted);
                sw.WriteLine("total_deleted_values|{0}", ValueCountDeleted);
            }
        }

        public RegistryKey GetDeletedKey(string keyPath, string lastwritetimestamp)
        {
            var segs = keyPath.Split('\\');

            //get a list that contains all matching root level unassociated keys
            var keys = DeletedRegistryKeys.Where(t => t.KeyPath == segs[0]);

            //drill down into each until we find the right one based on last write time
            foreach (var registryKey in keys)
            {
                var foo = registryKey;

                var startKey = registryKey;

                for (var i = 1; i < segs.Length; i++)
                {
                    foo = startKey.SubKeys.SingleOrDefault(t => t.KeyName == segs[i]);
                    if (foo != null)
                    {
                        startKey = foo;
                    }
                }

                if (foo == null)
                {
                    continue;
                }

                if (foo.LastWriteTime.ToString() != lastwritetimestamp)
                {
                    continue;
                }
                return foo;

                //  break;
            }

            return null;
        }

        public RegistryKey GetKey(string keyPath)
        {
            keyPath = keyPath.ToLowerInvariant();

            if (KeyPathKeyMap.ContainsKey(keyPath))
            {
                return KeyPathKeyMap[keyPath];
            }

            //handle case where someone doesn't pass in ROOT keyname
            var newPath = $"{Root.KeyName}\\{keyPath}".ToLowerInvariant();

            if (KeyPathKeyMap.ContainsKey(newPath))
            {
                return KeyPathKeyMap[newPath];
            }

            return null;
        }

        public RegistryKey GetKey(long relativeOffset)
        {
            if (RelativeOffsetKeyMap.ContainsKey(relativeOffset))
            {
                return RelativeOffsetKeyMap[relativeOffset];
            }

            return null;
        }

        public bool ParseHive()
        {
            if (_parsed)
            {
                throw new Exception("ParseHive already called");
            }
            TotalBytesRead = 0;

            TotalBytesRead += 4096;

            _softParsingErrors = 0;
            _hardParsingErrors = 0;

            ////Look at first hbin, get its size, then read that many bytes to create hbin record
            long offsetInHive = 4096;

            var hiveLength = Header.Length + 0x1000;
            if (hiveLength < FileBytes.Length)
            {
                _logger.Warn($"Header length is smaller than the size of the file.");
                hiveLength = (uint) FileBytes.Length;
            }

                //keep reading the file until we reach the end
                while (offsetInHive < hiveLength)
               {
     

                    var hbinSize = BitConverter.ToUInt32(ReadBytesFromHive(offsetInHive + 8, 4), 0);

                if (hbinSize == 0)
                {
                    _logger.Info("Found hbin with size 0 at absolute offset 0x{0:X}", offsetInHive);
                    // Go to end if we find a 0 size block (padding?)
                    offsetInHive = HiveLength();
                    continue;
                }

                var hbinSig = BitConverter.ToInt32(ReadBytesFromHive(offsetInHive, 4), 0);

                if (hbinSig != HbinSignature)
                {
                    _logger.Error("hbin header incorrect at absolute offset 0x{0:X}!!! Percent done: {1:P}",
                        offsetInHive,
                        (double) offsetInHive/hiveLength);

//                    if (RecoverDeleted) //TODO ? always or only if recoverdeleted
//                    {
//                        //TODO need to try to recover records from the bad chunk
//                    }

                    break;
                }

                Check.That(hbinSig).IsEqualTo(HbinSignature);

                _logger.Debug(
                    "Processing hbin at absolute offset 0x{0:X} with size 0x{1:X} Percent done: {2:P}",
                    offsetInHive, hbinSize,
                    (double) offsetInHive/hiveLength);

                var rawhbin = ReadBytesFromHive(offsetInHive, (int) hbinSize);

                try
                {
                    var h = new HBinRecord(rawhbin, offsetInHive - 0x1000, Header.MinorVersion, RecoverDeleted, this);

                    _logger.Trace("hbin info: {0}", h.ToString());

                    _logger.Debug("Getting records from hbin at absolute offset 0x{0:X}", offsetInHive);

                    var records = h.Process();

                    _logger.Debug("Found {0:N0} records from hbin at absolute offset 0x{1:X}", records.Count,
                        offsetInHive);

                    foreach (var record in records)
                    {
                        //TODO change this to compare against constants?
                        switch (record.Signature)
                        {
                            case "nk":
                            case "sk":
                            case "lk":
                            case "vk":
                                _logger.Debug("Adding cell record with signature {0} at absolute offset 0x{1:X}",
                                    record.Signature, record.AbsoluteOffset);

                                CellRecords.Add(record.AbsoluteOffset - 4096, (ICellTemplate) record);
                                break;

                            case "db":
                            case "li":
                            case "ri":
                            case "lh":
                            case "lf":
                                _logger.Debug("Adding list record with signature {0} at absolute offset 0x{1:X}",
                                    record.Signature, record.AbsoluteOffset);

                                ListRecords.Add(record.AbsoluteOffset - 4096, (IListTemplate) record);
                                break;
                        }
                    }

                    HBinRecordCount += 1;
                    HBinRecordTotalSize += hbinSize;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error processing hbin at absolute offset 0x{offsetInHive:X}.");
                }

                offsetInHive += hbinSize;
            }

            _logger.Info("Initial processing complete. Building tree...");

            //The root node can be found by either looking at Header.RootCellOffset or looking for an nk record with HiveEntryRootKey flag set.
            //here we are looking for the flag
            var rootNode =
                CellRecords.Values.OfType<NKCellRecord>()
                    .SingleOrDefault(
                        (f =>
                            (f.Flags & NKCellRecord.FlagEnum.HiveEntryRootKey) == NKCellRecord.FlagEnum.HiveEntryRootKey));

            if (rootNode == null)
            {
                _logger.Warn("Unable to find root key based on flag HiveEntryRootKey. Looking for root key via Header.RootCellOffset value...");
                rootNode =
                CellRecords.Values.OfType<NKCellRecord>()
                    .SingleOrDefault(
                        (f => f.RelativeOffset == (long) Header.RootCellOffset));

                if (rootNode == null)
                {
                    throw new KeyNotFoundException("Root nk record not found!");
                }

                    
            }

            //validate what we found above via the flag method
            Check.That((long) Header.RootCellOffset).IsEqualTo(rootNode.RelativeOffset);

            rootNode.IsReferenced = true;

            _logger.Info("Found root node! Getting subkeys...");

            Root = new RegistryKey(rootNode, null);
            _logger.Debug("Created root node object. Getting subkeys.");


            var keys = GetSubKeysAndValues(Root);

            Root.SubKeys.AddRange(keys);

            _logger.Info("Hive processing complete!");

            //All processing is complete, so we do some tests to see if we really saw everything
            if (RecoverDeleted && HiveLength() != TotalBytesRead)
            {
                var remainingHive = ReadBytesFromHive(TotalBytesRead, (int) (HiveLength() - TotalBytesRead));

                //Sometimes the remainder of the file is all zeros, which is useless, so check for that
                if (!Array.TrueForAll(remainingHive, a => a == 0))
                {
                    _logger.Warn(
                        "Extra, non-zero data found beyond hive length! Check for erroneous data starting at 0x{0:x}!",
                        TotalBytesRead);
                }

                //as a second check, compare Header length with what we read (taking the header into account as Header.Length is only for hbin records)

                if (Header.Length != TotalBytesRead - 0x1000)
                {
//ncrunch: no coverage
                    _logger.Warn( //ncrunch: no coverage
                        "Hive length (0x{0:x}) does not equal bytes read (0x{1:x})!! Check the end of the hive for erroneous data",
                        HiveLength(), TotalBytesRead);
                } //ncrunch: no coverage
            }

            if (RecoverDeleted)
            {
                BuildDeletedRegistryKeys();
            }

            if (FlushRecordListsAfterParse)
            {
                _logger.Info("Flushing record lists...");
                ListRecords.Clear();

                var toRemove = CellRecords.Where(pair => pair.Value is NKCellRecord || pair.Value is VKCellRecord)
                    .Select(pair => pair.Key)
                    .ToList();

                foreach (var key in toRemove)
                {
                    CellRecords.Remove(key);
                }
            }
            _parsed = true;
            return true;
        }

        /// <summary>
        ///     Associates vk records with NK records and builds a hierarchy of nk records
        ///     <remarks>Results of this method will be available in DeletedRegistryKeys</remarks>
        /// </summary>
        private void BuildDeletedRegistryKeys()
        {
            _logger.Info("Associating deleted keys and values...");

            var unreferencedNKCells = CellRecords.Where(t => t.Value.IsReferenced == false && t.Value is NKCellRecord);

            var associatedVKRecordOffsets = new List<long>();

            var _deletedRegistryKeys = new Dictionary<long, RegistryKey>();

            //Phase one is to associate any value records with key records
            foreach (var unreferencedNkCell in unreferencedNKCells)
            {
                try
                {
                    var nk = unreferencedNkCell.Value as NKCellRecord;

                    _logger.Debug("Processing deleted nk record at absolute offset 0x{0:X}", nk.AbsoluteOffset);

                    nk.IsDeleted = true;

                    var regKey = new RegistryKey(nk, null)
                    {
                        KeyFlags = RegistryKey.KeyFlagsEnum.Deleted
                    };

                    //some sanity checking on things
                    if (regKey.NKRecord.Size < 0x50 + regKey.NKRecord.NameLength)
                    {
                        continue;
                    }

                    //Build ValueOffsets for this NKRecord
                    if (regKey.NKRecord.ValueListCellIndex > 0)
                    {
                        //there are values for this key, so get the offsets so we can pull them next

                        _logger.Debug("Processing deleted nk record values for nk at absolute offset 0x{0:X}",
                            nk.AbsoluteOffset);

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
                        catch (Exception) //ncrunch: no coverage
                        {
//ncrunch: no coverage
                            //sometimes the data node doesn't have enough data to even do this, or its wrong data
                            _logger.Warn( //ncrunch: no coverage
                                "When getting values for nk record at absolute offset 0x{0:X}, not enough/invalid data was found at offset 0x{1:X}to look for value offsets. Value recovery is not possible",
                                nk.AbsoluteOffset, regKey.NKRecord.ValueListCellIndex);
                        } //ncrunch: no coverage

                        if (offsetList != null)
                        {
                            _logger.Debug("Found offset list for nk at absolute offset 0x{0:X}. Processing.",
                                nk.AbsoluteOffset);
                            try
                            {
                                for (var i = 0; i < regKey.NKRecord.ValueListCount; i++)
                                {
                                    //use i * 4 so we get 4, 8, 12, 16, etc
                                    var os = BitConverter.ToUInt32(offsetList.Data, i*4);

                                    regKey.NKRecord.ValueOffsets.Add(os);
                                }
                            }
                            catch (Exception) //ncrunch: no coverage
                            {
//ncrunch: no coverage
                                _logger.Warn( //ncrunch: no coverage
                                    "When getting value offsets for nk record at absolute offset 0x{0:X}, not enough data was found at offset 0x{1:X} to look for all value offsets. Only partial value recovery possible",
                                    nk.AbsoluteOffset, regKey.NKRecord.ValueListCellIndex);
                            } //ncrunch: no coverage
                        }
                    }

                    _logger.Debug("Looking for vk records for nk record at absolute offset 0x{0:X}", nk.AbsoluteOffset);

                    //For each value offset, get the vk record if it exists, create a KeyValue, and assign it to the current RegistryKey
                    foreach (var valueOffset in nk.ValueOffsets)
                    {
                        if (CellRecords.ContainsKey((long) valueOffset))
                        {
                            _logger.Debug(
                                "Found vk record at relative offset 0x{0:X} for nk record at absolute offset 0x{1:X}",
                                valueOffset, nk.AbsoluteOffset);

                            var val = CellRecords[(long) valueOffset] as VKCellRecord;
                            //we have a value for this key

                            if (val != null)
                            {
                                //if its an in use record AND referenced, warn
                                if (val.IsFree == false && val.IsReferenced)
                                {
                                    _logger.Warn(
                                        "When getting values for nk record at absolute offset 0x{0:X}, VK record at relative offset 0x{1:X} isn't free and is referenced by another nk record. Skipping!",
                                        nk.AbsoluteOffset, valueOffset);
                                }
                                else
                                {
                                    associatedVKRecordOffsets.Add(val.RelativeOffset);

                                    var kv = new KeyValue(val);

                                    regKey.Values.Add(kv);
                                    _logger.Debug(
                                        "Added vk record at relative offset 0x{0:X} for nk record at absolute offset 0x{1:X}",
                                        valueOffset, nk.AbsoluteOffset);
                                }
                            }
                        }
                        else
                        {
                            _logger.Debug(
                                "vk record at relative offset 0x{0:X} not found for nk record at absolute offset 0x{1:X}",
                                valueOffset, nk.AbsoluteOffset);
                        }
                    }

                    _logger.Debug(
                        "Associated {0:N0} value(s) out of {1:N0} possible values for nk record at absolute offset 0x{2:X}",
                        regKey.Values.Count, nk.ValueListCount, nk.AbsoluteOffset);


                    _deletedRegistryKeys.Add(nk.RelativeOffset, regKey);
                }
                catch (Exception ex) //ncrunch: no coverage
                {
//ncrunch: no coverage
                    _logger.Error( //ncrunch: no coverage
                        ex,
                        $"Error while processing deleted nk record at absolute offset 0x{unreferencedNkCell.Value.AbsoluteOffset:X}");
                } //ncrunch: no coverage
            }

            _logger.Debug("Building tree of key/subkeys for deleted keys");

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

                        _logger.Debug(
                            "Found subkey at absolute offset 0x{0:X} for parent key at absolute offset 0x{1:X}",
                            deletedRegistryKey.Value.NKRecord.AbsoluteOffset, parent.NKRecord.AbsoluteOffset);

                        deletedRegistryKey.Value.KeyPath = $@"{parent.KeyPath}\{deletedRegistryKey.Value.KeyName}";

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

            _logger.Debug("Associating top level deleted keys to active Registry keys");

            //Phase 3 is looking at top level keys from Phase 2 and seeing if any of those can be assigned to non-deleted keys in the main tree
            foreach (var deletedRegistryKey in _deletedRegistryKeys)
            {
                if (CellRecords.ContainsKey(deletedRegistryKey.Value.NKRecord.ParentCellIndex))
                {
                    //an parent key has been located, so get it
                    var parentNk = CellRecords[deletedRegistryKey.Value.NKRecord.ParentCellIndex] as NKCellRecord;

                    _logger.Debug(
                        "Found possible parent key at absolute offset 0x{0:X} for deleted key at absolute offset 0x{1:X}",
                        deletedRegistryKey.Value.NKRecord.ParentCellIndex + 0x1000,
                        deletedRegistryKey.Value.NKRecord.AbsoluteOffset);

                    if (parentNk == null)
                    {
                        //the data at that index is not an nkrecord
                        continue;
                    }

                    if (parentNk.IsReferenced && parentNk.IsFree == false)
                    {
                        //parent exists in our primary tree, so get that key
                        var pk = GetKey(deletedRegistryKey.Value.NKRecord.ParentCellIndex);

                        _logger.Debug(
                            "Copying subkey at absolute offset 0x{0:X} for parent key at absolute offset 0x{1:X}",
                            deletedRegistryKey.Value.NKRecord.AbsoluteOffset, pk.NKRecord.AbsoluteOffset);

                        deletedRegistryKey.Value.KeyPath = $@"{pk.KeyPath}\{deletedRegistryKey.Value.KeyName}";

                        deletedRegistryKey.Value.KeyFlags |= RegistryKey.KeyFlagsEnum.HasActiveParent;

                        UpdateChildPaths(deletedRegistryKey.Value);

                        //add a copy of deletedRegistryKey under its original parent
                        pk.SubKeys.Add(deletedRegistryKey.Value);

                        RelativeOffsetKeyMap.Add(deletedRegistryKey.Value.NKRecord.RelativeOffset,
                            deletedRegistryKey.Value);

                        if (KeyPathKeyMap.ContainsKey(deletedRegistryKey.Value.KeyPath.ToLowerInvariant()) == false)
                        {
                            KeyPathKeyMap.Add(deletedRegistryKey.Value.KeyPath.ToLowerInvariant(),
                                deletedRegistryKey.Value);
                        }

                        _logger.Debug(
                            "Associated deleted key at absolute offset 0x{0:X} to active parent key at absolute offset 0x{1:X}",
                            deletedRegistryKey.Value.NKRecord.AbsoluteOffset, pk.NKRecord.AbsoluteOffset);
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
            _logger.Trace("Updating child paths or key {0}", key.KeyPath);
            foreach (var sk in key.SubKeys)
            {
                sk.KeyPath = $@"{key.KeyPath}\{sk.KeyName}";

                RelativeOffsetKeyMap.Add(sk.NKRecord.RelativeOffset, sk);

                if (KeyPathKeyMap.ContainsKey(sk.KeyPath.ToLowerInvariant()) == false)
                {
                    KeyPathKeyMap.Add(sk.KeyPath.ToLowerInvariant(), sk);
                }

                UpdateChildPaths(sk);
            }
        }

        /// <summary>
        ///     Given a file, confirm that hbin headers are found every 4096 * (size of hbin) bytes.
        /// </summary>
        /// <returns></returns>
        public HiveMetadata Verify()
        {
            var hiveMetadata = new HiveMetadata();

            hiveMetadata.HasValidHeader = true;

            long offset = 4096;

            while (offset < Header.Length)
            {
                var hbinSig = BitConverter.ToUInt32(ReadBytesFromHive(offset, 4), 0);

                if (hbinSig == HbinSignature)
                {
                    hiveMetadata.NumberofHBins += 1;
                }

                var hbinSize = BitConverter.ToUInt32(ReadBytesFromHive(offset + 8, 4), 0);

                offset += hbinSize;
            }

            return hiveMetadata;
        }

        public IEnumerable<ValueBySizeInfo> FindByValueSize(int minimumSizeInBytes)
        {
            foreach (var registryKey in KeyPathKeyMap)
            {
                foreach (var keyValue in registryKey.Value.Values)
                {
                    if (keyValue.ValueDataRaw.Length >= minimumSizeInBytes)
                    {
                        yield return new ValueBySizeInfo(registryKey.Value, keyValue);
                    }
                }
            }
        }

        public IEnumerable<SearchHit> FindInKeyName(string searchTerm, bool useRegEx = false)
        {
            foreach (var registryKey in KeyPathKeyMap)
            {
                if (useRegEx)
                {
                    if (Regex.IsMatch(registryKey.Value.KeyName, searchTerm, RegexOptions.IgnoreCase))
                    {
                        yield return new SearchHit(registryKey.Value, null, searchTerm);
                    }
                }
                else
                {
                    if (registryKey.Value.KeyName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        yield return new SearchHit(registryKey.Value, null, searchTerm);
                    }
                }
            }
        }

        public IEnumerable<SearchHit> FindByLastWriteTime(DateTimeOffset? start, DateTimeOffset? end)
        {
            foreach (var registryKey in KeyPathKeyMap)
            {
                if (start != null && end != null)
                {
                    if (start <= registryKey.Value.LastWriteTime && registryKey.Value.LastWriteTime <= end)
                    {
                        yield return new SearchHit(registryKey.Value, null, null);
                    }
                }
                else if (end != null)
                {
                    if (registryKey.Value.LastWriteTime < end)
                    {
                        yield return new SearchHit(registryKey.Value, null, null);
                    }
                }
                else if (start != null)
                {
                    if (registryKey.Value.LastWriteTime > start)
                    {
                        yield return new SearchHit(registryKey.Value, null, null);
                    }
                }
            }
        }

        public IEnumerable<SearchHit> FindInValueName(string searchTerm, bool useRegEx = false)
        {
            foreach (var registryKey in KeyPathKeyMap)
            {
                foreach (var keyValue in registryKey.Value.Values)
                {
                    if (useRegEx)
                    {
                        if (Regex.IsMatch(keyValue.ValueName, searchTerm, RegexOptions.IgnoreCase))
                        {
                            yield return new SearchHit(registryKey.Value, keyValue, searchTerm);
                        }
                    }
                    else
                    {
                        if (keyValue.ValueName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            yield return new SearchHit(registryKey.Value, keyValue, searchTerm);
                        }
                    }
                }
            }
        }

        public IEnumerable<SearchHit> FindInValueData(string searchTerm, bool useRegEx = false, bool literal = false)
        {
         //   var _logger = LogManager.GetLogger("FFFFFFF");

            foreach (var registryKey in KeyPathKeyMap)
            {
             //   _logger.Debug($"Iterating key {registryKey.Value.KeyName} in path {registryKey.Value.KeyPath}. searchTerm: {searchTerm}, regex: {useRegEx}, literal: {literal}");

                foreach (var keyValue in registryKey.Value.Values)
                {
                   // _logger.Debug($"Searching value {keyValue.ValueName}");

                    if (useRegEx)
                    {
                        if (Regex.IsMatch(keyValue.ValueData, searchTerm, RegexOptions.IgnoreCase))
                        {
                            yield return new SearchHit(registryKey.Value, keyValue, searchTerm);
                        }
                    }
                    else
                    {
                        //plaintext matching
                        if (keyValue.ValueData.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            yield return new SearchHit(registryKey.Value, keyValue, searchTerm);
                        }

                        if (literal)
                        {
                            continue;
                        }

                    //    _logger.Debug($"After literal match");

                        var asAscii = keyValue.ValueData;
                        var asUnicode = keyValue.ValueData;

                        if (keyValue.VKRecord.DataType == VKCellRecord.DataTypeEnum.RegBinary)
                        {
                            //this takes the raw bytes and converts it to a string, which we can then search
                            //the regex will find us the hit with exact capitalization, which we can then convert to a byte string
                            //and match against the raw data
                            asAscii = Encoding.GetEncoding(1252).GetString(keyValue.ValueDataRaw);
                            asUnicode = Encoding.Unicode.GetString(keyValue.ValueDataRaw);

                            var hitString = string.Empty;
                            try
                            {
                                hitString = Regex.Match(asAscii, searchTerm, RegexOptions.IgnoreCase).Value;
                            }
                            catch (ArgumentException)
                            {
                                // Syntax error in the regular expression
                            }

                    //        _logger.Debug($"hitstring 1: {hitString}");

                            if (hitString.Length > 0)
                            {
                                var asciihex = Encoding.GetEncoding(1252).GetBytes(hitString);

                                var asciiHit = BitConverter.ToString(asciihex);
                                yield return new SearchHit(registryKey.Value, keyValue, asciiHit);
                            }

                            hitString = string.Empty;
                            try
                            {
                                hitString = Regex.Match(asUnicode, searchTerm, RegexOptions.IgnoreCase).Value;
                            }
                            catch (ArgumentException)
                            {
                                // Syntax error in the regular expression
                            }

                        //    _logger.Debug($"hitstring 2: {hitString}");

                            if (hitString.Length <= 0)
                            {
                                continue;
                            }

                       //     _logger.Debug($"before unicodehex");

                            var unicodehex = Encoding.Unicode.GetBytes(hitString);

                            var unicodeHit = BitConverter.ToString(unicodehex);
                            yield return new SearchHit(registryKey.Value, keyValue, unicodeHit);
                        }
                    }
                }
            }
        }

        public IEnumerable<SearchHit> FindInValueDataSlack(string searchTerm, bool useRegEx = false,
            bool literal = false)
        {
            foreach (var registryKey in KeyPathKeyMap)
            {
                foreach (var keyValue in registryKey.Value.Values)
                {
                    if (useRegEx)
                    {
                        if (Regex.IsMatch(keyValue.ValueSlack, searchTerm, RegexOptions.IgnoreCase))
                        {
                            yield return new SearchHit(registryKey.Value, keyValue, searchTerm);
                        }
                    }
                    else
                    {
                        if (literal)
                        {
                            if (keyValue.ValueSlack.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                yield return new SearchHit(registryKey.Value, keyValue, searchTerm);
                            }
                        }
                        else
                        {
                            //this takes the raw bytes and converts it to a string, which we can then search
                            //the regex will find us the hit with exact capitalization, which we can then convert to a byte string
                            //and match against the raw data
                            var asAscii = Encoding.GetEncoding(1252).GetString(keyValue.ValueSlackRaw);
                            var asUnicode = Encoding.Unicode.GetString(keyValue.ValueSlackRaw);

                            var hitString = string.Empty;
                            try
                            {
                                hitString = Regex.Match(asAscii, searchTerm, RegexOptions.IgnoreCase).Value;
                            }
                            catch (ArgumentException)
                            {
                                // Syntax error in the regular expression
                            }

                            if (hitString.Length > 0)
                            {
                                var asciihex = Encoding.GetEncoding(1252).GetBytes(hitString);

                                var asciiHit = BitConverter.ToString(asciihex);
                                yield return new SearchHit(registryKey.Value, keyValue, asciiHit);
                            }

                            hitString = string.Empty;
                            try
                            {
                                hitString = Regex.Match(asUnicode, searchTerm, RegexOptions.IgnoreCase).Value;
                            }
                            catch (ArgumentException)
                            {
                                // Syntax error in the regular expression
                            }

                            if (hitString.Length <= 0)
                            {
                                continue;
                            }
                            var unicodehex = Encoding.Unicode.GetBytes(hitString);

                            var unicodeHit = BitConverter.ToString(unicodehex);
                            yield return new SearchHit(registryKey.Value, keyValue, unicodeHit);
                        }
                    }
                }
            }
        }
    }
}