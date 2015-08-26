using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NFluent;
using Registry.Cells;
using Registry.Lists;

namespace Registry
{
    public class RegistrySkeleton
    {
        private readonly RegistryHive _hive;

        private readonly List<SkeletonKeyRoot> _keys;

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

            var intKey = _keys.SingleOrDefault(t => t.KeyPath == key.KeyPath);

            if (intKey == null)
            {
                if (key.KeyPath.StartsWith(_hive.Root.KeyName) == false)
                {
                    var newKeyPath = $"{_hive.Root.KeyName}\\{key.KeyPath}";
                    var tempKey = new SkeletonKeyRoot(newKeyPath,key.AddValues);
                    key = tempKey;
                }

                _keys.Add(key);
            }

            return true;
        }

        public bool RemoveEntry(SkeletonKeyRoot key)
        {
            if (key.KeyPath.StartsWith(_hive.Root.KeyName) == false)
            {
                var newKeyPath = $"{_hive.Root.KeyName}\\{key.KeyPath}";
                var tempKey = new SkeletonKeyRoot(newKeyPath, key.AddValues);
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

        private uint _relativeOffset = 0;

        private byte[] GetEmptyHbin(uint size)
        {
            var newHbin = new byte[size];

            //signature 'hbin'
            newHbin[0] = 0x68;
            newHbin[1] = 0x62;
            newHbin[2] = 0x69;
            newHbin[3] = 0x6E;

            BitConverter.GetBytes(_relativeOffset).CopyTo(newHbin,0x4);
            _relativeOffset += size;

            BitConverter.GetBytes(size).CopyTo(newHbin,0x8); //size

            BitConverter.GetBytes(DateTimeOffset.UtcNow.ToFileTime()).CopyTo(newHbin, 0x14); //last write

            return newHbin;
        }

        uint currentOffsetInHbin = 0x20;

        private byte[] _hbin = new byte[0];

        public bool Write(string outHive)
        {
            if (_keys.Count == 0)
            {
                throw new InvalidOperationException("At least one SkeletonKey must be added before calling Write");
            }

           // var b = new byte[] {0x01,0x02, 0x01, 0x02, 0x01, 0x02, 0x01, 0x02, 0x01, 0x02, 0x01, 0x02, 0x01, 0x02, 0x01, 0x02, 0x01, 0x02};

            if (File.Exists(outHive))
            {
                File.Delete(outHive);
            }

            _hbin = _hbin.Concat(GetEmptyHbin(0x1000)).ToArray();
            

            var treeKey = BuildKeyTree();
            
            ProcessSkeletonKey(treeKey,-1);


                        //mark remaining hbin as free
                        BitConverter.GetBytes(_hbin.Length - currentOffsetInHbin).CopyTo(_hbin,currentOffsetInHbin);



            //work is done, get header, update rootcelloffset, adjust its length to match new hbin length, and write it out

            var headerBytes = _hive.ReadBytesFromHive(0, 0x1000);

            BitConverter.GetBytes(_hbin.Length).CopyTo(headerBytes, 0x28);

            var outBytes = headerBytes.Concat(_hbin).ToArray();

            File.WriteAllBytes(outHive,outBytes);

            return true;
        }

        const int SecurityOffset = 0x30;
        const int SubkeyCountStableOffset = 0x18;
        const int SubkeyListStableCellIndex = 0x20;
        const int ValueListCellIndex = 0x2C;
        const int ParentCellIndex = 0x14;
        const int ValueDataOffset = 0x0C;

        private void CheckhbinSize(int recordSize)
        {
            if (currentOffsetInHbin + recordSize > _hbin.Length)
            {
                //we need to add another hbin
                //set remaining space to free record
                BitConverter.GetBytes(_hbin.Length - currentOffsetInHbin).CopyTo(_hbin, currentOffsetInHbin);

                //go to end of current _hbin
                currentOffsetInHbin = (uint)_hbin.Length;


                //this is where to check for record size and adjust size below accordingly!!
                //TODO FINISH THIS

                //add more space
                _hbin = _hbin.Concat(GetEmptyHbin(0x1000)).ToArray();

                //move pointer to next usable space
                currentOffsetInHbin += 0x20;
            }
        }

        private uint ProcessSkeletonKey(SkeletonKey treeKey, long parentIndex)
        {
            //get nk record bytes
            var key = _hive.GetKey(treeKey.KeyPath);

            
            //this is where we will be placing our record
            var nkOffset = currentOffsetInHbin;

            //move our pointer to the beginning of free space for any subsequent records
            currentOffsetInHbin += (uint) key.NKRecord.RawBytes.Length;

            var nkBytes = key.NKRecord.RawBytes;

            if ((key.NKRecord.Flags & NKCellRecord.FlagEnum.HiveEntryRootKey) != NKCellRecord.FlagEnum.HiveEntryRootKey)
            {
                //update parent offset
                BitConverter.GetBytes(parentIndex).CopyTo(nkBytes,ParentCellIndex);
            }

            //For now, Update security cell index to 0x0
            BitConverter.GetBytes(0).CopyTo(nkBytes,SecurityOffset);


            BitConverter.GetBytes(treeKey.Subkeys.Count).CopyTo(nkBytes,SubkeyCountStableOffset);

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
                subkeyOffsets.Add(offset,hash);
            }

            if (subkeyOffsets.Count > 0)
            {
                //write list and save address
                var list = BuildlfList(subkeyOffsets, currentOffsetInHbin);

                CheckhbinSize(list.RawBytes.Length);
                list.RawBytes.CopyTo(_hbin,currentOffsetInHbin);

                //update SubkeyListStableCellIndex in nkrecord
                BitConverter.GetBytes(currentOffsetInHbin).CopyTo(nkBytes,SubkeyListStableCellIndex);

                currentOffsetInHbin += (uint) list.RawBytes.Length;
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

                    if (keyValue.VKRecord.ValueName == "@C:\\Windows\\system32\\themeui.dll,-2682")
                    {
                        Debug.WriteLine(1);
                        
                    }

                    if ((keyValue.VKRecord.DataLength & DWORD_SIGN_MASK) != DWORD_SIGN_MASK)
                    {
                        //non-resident data, so write out the data and update the vkrecords pointer to said data

                        var dataraw = keyValue.ValueDataRaw.Concat(keyValue.ValueSlackRaw).ToArray();
                        
                        var datarawBytes = new byte[4 + dataraw.Length];

                        BitConverter.GetBytes(-1 * datarawBytes.Length).CopyTo(datarawBytes,0);
                        dataraw.CopyTo(datarawBytes,4);

                       CheckhbinSize(datarawBytes.Length);
                        datarawBytes.CopyTo(_hbin,currentOffsetInHbin);

                        BitConverter.GetBytes(currentOffsetInHbin).CopyTo(vkBytes, ValueDataOffset);

                        currentOffsetInHbin +=(uint)datarawBytes.Length;
                    }

                    CheckhbinSize(vkBytes.Length);
                    vkBytes.CopyTo(_hbin, (int)currentOffsetInHbin);

                    valueOffsets.Add(currentOffsetInHbin);

                    currentOffsetInHbin += (uint)vkBytes.Length;
                    
                }

                var valListSize = 4 + valueOffsets.Count*4;
                if (valListSize%8 != 0)
                {
                    valListSize += 4;
                }

                var offsetListBytes = new byte[valListSize];

                BitConverter.GetBytes(-1 * valListSize).CopyTo(offsetListBytes,0);

                var index = 4;
                foreach (var valueOffset in valueOffsets)
                {
                    BitConverter.GetBytes(valueOffset).CopyTo(offsetListBytes,index);
                    index += 4;
                }

                CheckhbinSize(offsetListBytes.Length);
                offsetListBytes.CopyTo(_hbin,currentOffsetInHbin);

                BitConverter.GetBytes(currentOffsetInHbin).CopyTo(nkBytes, ValueListCellIndex);

                currentOffsetInHbin += (uint) offsetListBytes.Length;

                Trace.Assert(offsetListBytes.Count() % 8 == 0);

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

        private LxListRecord BuildlfList(Dictionary<uint,string> subkeyInfo, long offset)
        { 
            var totalSize = 4 + 2 + 2 + subkeyInfo.Count * 8; //size + sig + num entries + bytes for list itself

            var listBytes = new byte[totalSize]; 
            
            BitConverter.GetBytes(-1 * totalSize).CopyTo(listBytes, 0);
            Encoding.ASCII.GetBytes("lf").CopyTo(listBytes, 4);
            BitConverter.GetBytes((short)subkeyInfo.Count).CopyTo(listBytes, 6);

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
                SkeletonKey current = root;

                //need to make sure root key name is at beginning of each

                var segs = keyRoot.KeyPath.Split('\\');

                var withVals = false;
                foreach (var seg in segs)
                {
                    if (seg == segs.Last())
                    {
                        withVals = keyRoot.AddValues;
                    }

                    if (root == null)
                    {
                        root = new SkeletonKey(seg,seg,withVals);
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

                    var sk = new SkeletonKey($"{current.KeyPath}\\{seg}", seg,withVals);
                    current.Subkeys.Add(sk);
                    current = sk;

                 
                }
            }

            return root;
        }
    }

    public class SkeletonKeyRoot
    {
        public SkeletonKeyRoot(string keyPath, bool addValues)
        {
            KeyPath = keyPath;
            AddValues = addValues;
        }

        public string KeyPath { get; }
        public bool AddValues { get; }
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