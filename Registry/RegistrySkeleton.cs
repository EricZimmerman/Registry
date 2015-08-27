using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Registry.Abstractions;
using Registry.Cells;
using Registry.Lists;

namespace Registry
{
    public class RegistrySkeleton
    {
        private const int SecurityOffset = 0x30;
        private const int SubkeyCountStableOffset = 0x18;
        private const int SubkeyListStableCellIndex = 0x20;
        private const int ValueListCellIndex = 0x2C;
        private const int ParentCellIndex = 0x14;
        private const int ValueDataOffset = 0x0C;
        private const int HeaderMinorVersion = 0x18;
        private const int CheckSumOffset = 0x1fc;

        private readonly RegistryHive _hive;

        private readonly List<SkeletonKeyRoot> _keys;

        private uint _currentOffsetInHbin = 0x20;

        private byte[] _hbin = new byte[0];

        private uint _relativeOffset;

        private readonly Dictionary<long, uint> _skMap = new Dictionary<long, uint>();

        public RegistrySkeleton(RegistryHive hive)
        {
            if (hive == null)
            {
                throw new NullReferenceException();
            }
            _hive = hive;
            _keys = new List<SkeletonKeyRoot>();
        }

        public ReadOnlyCollection<SkeletonKeyRoot> Keys => _keys.AsReadOnly();

        /// <summary>
        ///     Adds a SkeletonKey to the SkeletonHive
        /// </summary>
        /// <remarks>Returns true if key is already in list or it is added</remarks>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool AddEntry(SkeletonKeyRoot key)
        {
            var hiveKey = _hive.GetKey(key.KeyPath);

            if (hiveKey == null)
            {
                return false;
            }

            if (key.KeyPath.StartsWith(_hive.Root.KeyName) == false)
            {
                var newKeyPath = $"{_hive.Root.KeyName}\\{key.KeyPath}";
                var tempKey = new SkeletonKeyRoot(newKeyPath, key.AddValues, key.Recursive);
                key = tempKey;
            }

            var intKey = _keys.SingleOrDefault(t => t.KeyPath == key.KeyPath);

            if (intKey == null)
            {
                _keys.Add(key);

                if (key.Recursive)
                {
                    // for each subkey in hivekey, create another skr and add it
                    var subs = GetSubkeyNames(hiveKey);

                    foreach (var sub in subs)
                    {
                        var subsk = new SkeletonKeyRoot(sub, true, false);
                        _keys.Add(subsk);
                    }
                }
            }

            return true;
        }

        private List<string> GetSubkeyNames(RegistryKey key)
        {
            var l = new List<string>();

            foreach (var registryKey in key.SubKeys)
            {
                l.AddRange(GetSubkeyNames(registryKey));

                l.Add(registryKey.KeyPath);
            }

            return l;
        }

        public bool RemoveEntry(SkeletonKeyRoot key)
        {
            if (key.KeyPath.StartsWith(_hive.Root.KeyName) == false)
            {
                var newKeyPath = $"{_hive.Root.KeyName}\\{key.KeyPath}";
                var tempKey = new SkeletonKeyRoot(newKeyPath, key.AddValues, key.Recursive);
                key = tempKey;
            }

            var intKey = _keys.SingleOrDefault(t => t.KeyPath == key.KeyPath);

            if (intKey == null)
            {
                return false;
            }

            _keys.Remove(intKey);

            return true;
        }

        private byte[] GetEmptyHbin(uint size)
        {
            var newHbin = new byte[size];

            //signature 'hbin'
            newHbin[0] = 0x68;
            newHbin[1] = 0x62;
            newHbin[2] = 0x69;
            newHbin[3] = 0x6E;

            BitConverter.GetBytes(_relativeOffset).CopyTo(newHbin, 0x4);
            _relativeOffset += size;

            BitConverter.GetBytes(size).CopyTo(newHbin, 0x8); //size

            BitConverter.GetBytes(DateTimeOffset.UtcNow.ToFileTime()).CopyTo(newHbin, 0x14); //last write

            return newHbin;
        }

        public bool Write(string outHive)
        {
            if (_keys.Count == 0)
            {
                throw new InvalidOperationException("At least one SkeletonKey must be added before calling Write");
            }

            if (File.Exists(outHive))
            {
                File.Delete(outHive);
            }

            _hbin = _hbin.Concat(GetEmptyHbin(0x1000)).ToArray();


            var treeKey = BuildKeyTree();

            ProcessSkeletonKey(treeKey, -1);

            //mark any remaining hbin as free
            var freeSize = _hbin.Length - _currentOffsetInHbin;
            if (freeSize > 0)
            {
                BitConverter.GetBytes(freeSize).CopyTo(_hbin, _currentOffsetInHbin);
            }

            //work is done, get header, update rootcelloffset, adjust its length to match new hbin length, and write it out

            var headerBytes = _hive.ReadBytesFromHive(0, 0x1000);

            BitConverter.GetBytes(_hbin.Length).CopyTo(headerBytes, 0x28);
            BitConverter.GetBytes(5).CopyTo(headerBytes, HeaderMinorVersion);

            //update checksum
            var index = 0;
            var xsum = 0;
            while (index <= 0x1fb)
            {
                xsum ^= BitConverter.ToInt32(headerBytes, index);
                index += 0x04;
            }
            var newcs = xsum;

            BitConverter.GetBytes(newcs).CopyTo(headerBytes, CheckSumOffset);

            var outBytes = headerBytes.Concat(_hbin).ToArray();

            File.WriteAllBytes(outHive, outBytes);

            return true;
        }

        private void CheckhbinSize(int recordSize)
        {
            if (_currentOffsetInHbin + recordSize > _hbin.Length)
            {
                //we need to add another hbin

                //set remaining space to free record
                var freeSize = _hbin.Length - _currentOffsetInHbin;
                if (freeSize > 0)
                {
                    BitConverter.GetBytes(freeSize).CopyTo(_hbin, _currentOffsetInHbin);
                }

                //go to end of current _hbin
                _currentOffsetInHbin = (uint) _hbin.Length;

                //we have to make our hbin at least as big as the data that needs to go in it, so figure that out
                var hbinBaseSize = (int) Math.Ceiling(recordSize/(double) 4096);
                var hbinSize = hbinBaseSize*0x1000;

                //add more space
                _hbin = _hbin.Concat(GetEmptyHbin((uint) hbinSize)).ToArray();

                //move pointer to next usable space
                _currentOffsetInHbin += 0x20;
            }
        }

        private uint ProcessSkeletonKey(SkeletonKey treeKey, long parentIndex)
        {
            //get nk record bytes
            var key = _hive.GetKey(treeKey.KeyPath);


            //this is where we will be placing our record
            var nkOffset = _currentOffsetInHbin;

            //move our pointer to the beginning of free space for any subsequent records
            _currentOffsetInHbin += (uint) key.NKRecord.RawBytes.Length;

            var nkBytes = key.NKRecord.RawBytes;

            if ((key.NKRecord.Flags & NKCellRecord.FlagEnum.HiveEntryRootKey) != NKCellRecord.FlagEnum.HiveEntryRootKey)
            {
                //update parent offset
                BitConverter.GetBytes(parentIndex).CopyTo(nkBytes, ParentCellIndex);
            }

            //Get Security record
            if (_hive.CellRecords.ContainsKey(key.NKRecord.SecurityCellIndex))
            {
                var sk = _hive.CellRecords[key.NKRecord.SecurityCellIndex] as SKCellRecord;

                if (_skMap.ContainsKey(sk.RelativeOffset))
                {
                    //sk is already in _hbin
                    var skOffset = _skMap[sk.RelativeOffset];
                    BitConverter.GetBytes(skOffset).CopyTo(nkBytes, SecurityOffset);
                }
                else
                {
                    CheckhbinSize(sk.RawBytes.Length);
                    sk.RawBytes.CopyTo(_hbin, _currentOffsetInHbin);

                    var skOffset = _currentOffsetInHbin;
                    _skMap.Add(sk.RelativeOffset, skOffset);
                    _currentOffsetInHbin += (uint) sk.RawBytes.Length;
                    BitConverter.GetBytes(skOffset).CopyTo(nkBytes, SecurityOffset);
                }
            }

            BitConverter.GetBytes(treeKey.Subkeys.Count).CopyTo(nkBytes, SubkeyCountStableOffset);

            var subkeyOffsets = new Dictionary<uint, string>();

            foreach (var skeletonKey in treeKey.Subkeys)
            {
                //write out subkeys, keep record of offsets
                var offset = ProcessSkeletonKey(skeletonKey, nkOffset);

                var hash = skeletonKey.KeyName;
                if (skeletonKey.KeyName.Length >= 4)
                {
                    hash = skeletonKey.KeyName.Substring(0, 4);
                }

                //generate list for key offsets
                subkeyOffsets.Add(offset, hash);
            }

            if (subkeyOffsets.Count > 0)
            {
                //TODO this should generate an ri list pointing to lh lists when the number of subkeys > 500. each lh should be 500 in size
                //TODO test this with some hive that has a ton of keys

                //write list and save address
                var list = BuildlfList(subkeyOffsets, _currentOffsetInHbin);

                CheckhbinSize(list.RawBytes.Length);
                list.RawBytes.CopyTo(_hbin, _currentOffsetInHbin);

                //update SubkeyListStableCellIndex in nkrecord
                BitConverter.GetBytes(_currentOffsetInHbin).CopyTo(nkBytes, SubkeyListStableCellIndex);

                _currentOffsetInHbin += (uint) list.RawBytes.Length;
            }


            //zero out the pointer to values unless its enabled for this key
            BitConverter.GetBytes(0).CopyTo(nkBytes, ValueListCellIndex);
            if (treeKey.AddValues)
            {
                const uint DWORD_SIGN_MASK = 0x80000000;

                var valueOffsets = new List<uint>();

                foreach (var keyValue in key.Values)
                {
                    var vkBytes = keyValue.VKRecord.RawBytes;

//                    if (keyValue.VKRecord.ValueName == "@C:\\Windows\\system32\\themeui.dll,-2682")
//                    {
//                        Debug.WriteLine("Debug trap");
//                    }

                    if ((keyValue.VKRecord.DataLength & DWORD_SIGN_MASK) != DWORD_SIGN_MASK)
                    {
                        //non-resident data, so write out the data and update the vkrecords pointer to said data

                        if (keyValue.VKRecord.DataLength > 16344)
                        {
                            //big data case
                            //get data and slack
                            //split into 16344 chunks
                            //add 4 bytes of padding at the end of each chunk
                            //this makes each chunk 16348 of data plus 4 bytes at front for size (16352 total)
                            //write out data chunks, keeping a record of where they went
                            //build db list
                            //point vk record ValueDataOffset to this location

                            var dataraw = keyValue.ValueDataRaw.Concat(keyValue.ValueSlackRaw).ToArray();

                            var pos = 0;

                            var chunks = new List<byte[]>();

                            while (pos < dataraw.Length)
                            {
                                if (dataraw.Length - pos < 16344)
                                {
                                    //we are out of data
                                    chunks.Add(dataraw.Skip(pos).Take(dataraw.Length - pos).ToArray());
                                    pos = dataraw.Length;
                                }

                                chunks.Add(dataraw.Skip(pos).Take(16344).ToArray());
                                pos += 16344;
                            }

                            var dbOffsets = new List<uint>();

                            foreach (var chunk in chunks)
                            {
                                var rawChunk = chunk.Concat(new byte[4]).ToArray(); //add our extra 4 bytes at the end
                                var toWrite = BitConverter.GetBytes(-1*(rawChunk.Length + 4)).Concat(rawChunk).ToArray();
                                    //add the size

                                CheckhbinSize(toWrite.Length);
                                toWrite.CopyTo(_hbin, _currentOffsetInHbin);

                                dbOffsets.Add(_currentOffsetInHbin);

                                _currentOffsetInHbin += (uint) toWrite.Length;
                            }


                            //next is the list itself of offsets to the data chunks

                            var offsetSize = 4 + (dbOffsets.Count*4); //size itself plus a slot for each offset

                            if ((4 + offsetSize)%8 != 0)
                            {
                                offsetSize += 4;
                            }

                            var offsetList =
                                BitConverter.GetBytes(-1*offsetSize).Concat(new byte[(dbOffsets.Count*4)]).ToArray();

                            var i = 1;
                            foreach (var dbo in dbOffsets)
                            {
                                BitConverter.GetBytes(dbo).CopyTo(offsetList, i*4);
                                i += 1;
                            }

                            //write offsetList to hbin
                            CheckhbinSize(offsetList.Length);

                            var offsetOffset = _currentOffsetInHbin;

                            offsetList.CopyTo(_hbin, offsetOffset);
                            _currentOffsetInHbin += (uint) offsetList.Length;


                            //all the data is written, build a dblist to reference it
                            //db list is just an offset to offsets
                            //size db #entries offset

                            var dbRaw =
                                BitConverter.GetBytes(-16)
                                    .Concat(Encoding.ASCII.GetBytes("db"))
                                    .Concat(
                                        BitConverter.GetBytes((short) dbOffsets.Count)
                                            .Concat(BitConverter.GetBytes(offsetOffset))).Concat(new byte[4])
                                    .ToArray();

                            var dbOffset = _currentOffsetInHbin;
                            CheckhbinSize(dbRaw.Length);
                            dbRaw.CopyTo(_hbin, dbOffset);

                            _currentOffsetInHbin += (uint) dbRaw.Length;

                            BitConverter.GetBytes(dbOffset).CopyTo(vkBytes, ValueDataOffset);
                        }
                        else
                        {
                            var dataraw = keyValue.ValueDataRaw.Concat(keyValue.ValueSlackRaw).ToArray();

                            var datarawBytes = new byte[4 + dataraw.Length];

                            BitConverter.GetBytes(-1*datarawBytes.Length).CopyTo(datarawBytes, 0);
                            dataraw.CopyTo(datarawBytes, 4);

                            CheckhbinSize(datarawBytes.Length);
                            datarawBytes.CopyTo(_hbin, _currentOffsetInHbin);

                            BitConverter.GetBytes(_currentOffsetInHbin).CopyTo(vkBytes, ValueDataOffset);

                            _currentOffsetInHbin += (uint) datarawBytes.Length;
                        }
                    }

                    CheckhbinSize(vkBytes.Length);
                    vkBytes.CopyTo(_hbin, (int) _currentOffsetInHbin);

                    valueOffsets.Add(_currentOffsetInHbin);

                    _currentOffsetInHbin += (uint) vkBytes.Length;
                }

                var valListSize = 4 + valueOffsets.Count*4;
                if (valListSize%8 != 0)
                {
                    valListSize += 4;
                }

                var offsetListBytes = new byte[valListSize];

                BitConverter.GetBytes(-1*valListSize).CopyTo(offsetListBytes, 0);

                var index = 4;
                foreach (var valueOffset in valueOffsets)
                {
                    BitConverter.GetBytes(valueOffset).CopyTo(offsetListBytes, index);
                    index += 4;
                }

                CheckhbinSize(offsetListBytes.Length);
                offsetListBytes.CopyTo(_hbin, _currentOffsetInHbin);

                BitConverter.GetBytes(_currentOffsetInHbin).CopyTo(nkBytes, ValueListCellIndex);

                _currentOffsetInHbin += (uint) offsetListBytes.Length;

                Trace.Assert(offsetListBytes.Count()%8 == 0);

                //foreach value in key.values
                //get vk record
                //write out vk data
                //update vk pointer to data if non-resident
                //write out vk
                //add pointer to vk to list

                //when done, make a list for each value (its just a data record)
                //write out list, record offset to list
                //update nk record ValueListCellIndex to this offset
                //???
                //profit 
            }

            //commit our nk record to hbin
            CheckhbinSize(nkBytes.Length);
            nkBytes.CopyTo(_hbin, nkOffset);

            return nkOffset;
        }

        private LxListRecord BuildlfList(Dictionary<uint, string> subkeyInfo, long offset)
        {
            var totalSize = 4 + 2 + 2 + subkeyInfo.Count*8; //size + sig + num entries + bytes for list itself

            var listBytes = new byte[totalSize];

            BitConverter.GetBytes(-1*totalSize).CopyTo(listBytes, 0);
            Encoding.ASCII.GetBytes("lf").CopyTo(listBytes, 4);
            BitConverter.GetBytes((short) subkeyInfo.Count).CopyTo(listBytes, 6);

            var index = 0x8;

            foreach (var entry in subkeyInfo)
            {
                BitConverter.GetBytes(entry.Key).CopyTo(listBytes, index);
                index += 4;
                Encoding.ASCII.GetBytes(entry.Value).CopyTo(listBytes, index);
                index += 4;
            }

            return new LxListRecord(listBytes, offset);
        }

        private SkeletonKey BuildKeyTree()
        {
            SkeletonKey root = null;

            foreach (var keyRoot in _keys)
            {
                var current = root;

                //need to make sure root key name is at beginning of each

                var segs = keyRoot.KeyPath.Split('\\');

                var withVals = keyRoot.AddValues;
                foreach (var seg in segs)
                {
                    if (seg == segs.Last())
                    {
                        withVals = keyRoot.AddValues;
                    }

                    if (root == null)
                    {
                        root = new SkeletonKey(seg, seg, withVals);
                        current = root;
                        continue;
                    }

                    if (current.KeyName == segs.First() && seg == segs.First())
                    {
                        continue;
                    }

                    if (current.Subkeys.Any(t => t.KeyName == seg))
                    {
                        current = current.Subkeys.Single(t => t.KeyName == seg);
                        continue;
                    }

                    if (seg == segs.Last())
                    {
                        withVals = keyRoot.AddValues;
                    }

                    var sk = new SkeletonKey($"{current.KeyPath}\\{seg}", seg, withVals);
                    current.Subkeys.Add(sk);
                    current = sk;
                }
            }

            return root;
        }
    }

    public class SkeletonKeyRoot
    {
        public SkeletonKeyRoot(string keyPath, bool addValues, bool recursive)
        {
            KeyPath = keyPath;
            AddValues = addValues;
            Recursive = recursive;
        }

        public string KeyPath { get; }
        public bool AddValues { get; }
        public bool Recursive { get; }
    }

    public class SkeletonKey
    {
        public SkeletonKey(string keyPath, string keyName, bool addValues)
        {
            KeyPath = keyPath;
            KeyName = keyName;
            AddValues = addValues;
            Subkeys = new List<SkeletonKey>();
        }

        public string KeyName { get; }
        public string KeyPath { get; }
        public bool AddValues { get; }
        public List<SkeletonKey> Subkeys { get; }
    }
}