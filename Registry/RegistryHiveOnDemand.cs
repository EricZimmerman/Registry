using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Registry.Abstractions;
using Registry.Cells;
using Registry.Lists;
using Registry.Other;
using static Registry.Other.Helpers;

namespace Registry
{
    public class RegistryHiveOnDemand : RegistryBase
    {
        public RegistryHiveOnDemand(string hivePath) : base(hivePath)
        {
        }

        public RegistryHiveOnDemand(byte[] rawBytes) : base(rawBytes)
        {
        }

        private List<RegistryKey> GetSubkeys(uint subkeyListsStableCellIndex, RegistryKey parent)
        {
            var keys = new List<RegistryKey>();

            _logger.Debug("Looking for list record at relative offset 0x{0:X}", subkeyListsStableCellIndex);

            var rawList = GetRawRecord(subkeyListsStableCellIndex);

            var l = GetListFromRawBytes(rawList, subkeyListsStableCellIndex);

            var sig = BitConverter.ToInt16(l.RawBytes, 4);

            switch (sig)
            {
                case LfSignature:
                case LhSignature:
                    var lxRecord = l as LxListRecord;

                    foreach (var offset in lxRecord.Offsets)
                    {
                        _logger.Debug("In lf or lh, looking for nk record at relative offset 0x{0:X}", offset);
                        var rawCell = GetRawRecord(offset.Key);
                        var nk = new NKCellRecord(rawCell.Length, offset.Key, this);

                        _logger.Debug("In lf or lh, found nk record at relative offset 0x{0:X}. Name: {1}", offset,
                            nk.Name);

                        var tempKey = new RegistryKey(nk, parent);

                        keys.Add(tempKey);
                    }
                    break;

                case RiSignature:
                    var riRecord = l as RIListRecord;

                    foreach (var offset in riRecord.Offsets)
                    {
                        _logger.Debug("In ri, looking for list record at relative offset 0x{0:X}", offset);
                        rawList = GetRawRecord(offset);

                        var tempList = GetListFromRawBytes(rawList, offset);

                        //templist is now an li or lh list 

                        if (tempList.Signature == "li")
                        {
                            var sk3 = tempList as LIListRecord;

                            foreach (var offset1 in sk3.Offsets)
                            {
                                _logger.Debug("In ri/li, looking for nk record at relative offset 0x{0:X}", offset1);
                                var rawCell = GetRawRecord(offset1);
                                var nk = new NKCellRecord(rawCell.Length, offset1, this);

                                var tempKey = new RegistryKey(nk, parent);

                                keys.Add(tempKey);
                            }
                        }
                        else
                        {
                            var lxRecord_ = tempList as LxListRecord;

                            foreach (var offset3 in lxRecord_.Offsets)
                            {
                                _logger.Debug("In ri/li, looking for nk record at relative offset 0x{0:X}", offset3);
                                var rawCell = GetRawRecord(offset3.Key);
                                var nk = new NKCellRecord(rawCell.Length, offset3.Key, this);

                                var tempKey = new RegistryKey(nk, parent);

                                keys.Add(tempKey);
                            }
                        }
                    }

                    break;

                //this is a safety net, but li's are typically only seen in RI lists. as such, don't use it in metrics

                case LiSignature:
                    var liRecord = l as LIListRecord;

                    foreach (var offset in liRecord.Offsets)
                    {
                        _logger.Debug("In li, looking for nk record at relative offset 0x{0:X}", offset);
                        var rawCell = GetRawRecord(offset);
                        var nk = new NKCellRecord(rawCell.Length, offset, this);

                        var tempKey = new RegistryKey(nk, parent);
                        keys.Add(tempKey);
                    }

                    break;
                default:
                    throw new Exception($"Unknown subkey list type {l.Signature}!");
            }

            return keys;
        }

        private List<KeyValue> GetKeyValues(uint valueListCellIndex, uint valueListCount)
        {
            var values = new List<KeyValue>();

            var offsets = new List<uint>();

            if (valueListCellIndex > 0)
            {
                _logger.Debug("Getting value list offset at relative offset 0x{0:X}. Value count is {1:N0}",
                    valueListCellIndex, valueListCount);

                var offsetList = GetDataNodeFromOffset(valueListCellIndex);

                for (var i = 0; i < valueListCount; i++)
                {
                    //use i * 4 so we get 4, 8, 12, 16, etc
                    var os = BitConverter.ToUInt32(offsetList.Data, i*4);
                    _logger.Debug("Got value offset 0x{0:X}", os);
                    offsets.Add(os);
                }
            }

            if (offsets.Count != valueListCount)
            {
                //ncrunch: no coverage
                _logger.Warn(
                    "Value count mismatch! ValueListCount is {0:N0} but NKRecord.ValueOffsets.Count is {1:N0}",
                    //ncrunch: no coverage
                    valueListCount, offsets.Count);
            } //ncrunch: no coverage

            foreach (var valueOffset in offsets)
            {
                _logger.Debug("Looking for vk record at relative offset 0x{0:X}", valueOffset);

                var rawVK = GetRawRecord(valueOffset);
                var vk = new VKCellRecord(rawVK.Length, valueOffset, Header.MinorVersion, this);

                _logger.Debug("Found vk record at relative offset 0x{0:X}. Value name: {1}", valueOffset, vk.ValueName);
                var value = new KeyValue(vk);
                values.Add(value);
            }

            return values;
        }

        private IListTemplate GetListFromRawBytes(byte[] rawBytes, long relativeOffset)
        {
            var sig = BitConverter.ToInt16(rawBytes, 4);

            switch (sig)
            {
                case LfSignature:
                case LhSignature:
                    return new LxListRecord(rawBytes, relativeOffset);
                case RiSignature:
                    return new RIListRecord(rawBytes, relativeOffset);
                case LiSignature:
                    return new LIListRecord(rawBytes, relativeOffset);
                default:
                    throw new Exception($"Unknown list signature: {sig}"); //ncrunch: no coverage
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

        private byte[] GetRawRecord(long relativeOFfset)
        {
            var absOffset = relativeOFfset + 0x1000;

            var rawSize = ReadBytesFromHive(absOffset, 4);

            var size = BitConverter.ToInt32(rawSize, 0);
            size = Math.Abs(size);

            return ReadBytesFromHive(absOffset, size);
        }

        public RegistryKey GetKey(string keyPath)
        {
            var rawRoot = GetRawRecord(Header.RootCellOffset);

            var rootNk = new NKCellRecord(rawRoot.Length, Header.RootCellOffset, this);

            var newPath = keyPath.ToLowerInvariant();

            // when getting child keys, the name may start with the root key name. if so, strip it
            if (newPath.StartsWith(rootNk.Name.ToLowerInvariant()))
            {
                var segs = keyPath.Split('\\');
                newPath = string.Join("\\", segs.Skip(1));
            }

            var rootKey = new RegistryKey(rootNk, null);

            var keyNames = newPath.Split(new[] {'\\'}, StringSplitOptions.RemoveEmptyEntries);

            rootKey.SubKeys.AddRange(GetSubkeys(rootKey.NKRecord.SubkeyListsStableCellIndex, rootKey));

            var finalKey = rootKey;

            for (var i = 0; i < keyNames.Length; i++)
            {
                finalKey =
                    finalKey.SubKeys.SingleOrDefault(r => r.KeyName.ToLowerInvariant() == keyNames[i].ToLowerInvariant());

                if (finalKey == null)
                {
                    return null;
                }

                if (finalKey.NKRecord.SubkeyListsStableCellIndex > 0)
                {
                    finalKey.SubKeys.AddRange(GetSubkeys(finalKey.NKRecord.SubkeyListsStableCellIndex, finalKey));
                }
            }

            finalKey.Values.AddRange(GetKeyValues(finalKey.NKRecord.ValueListCellIndex, finalKey.NKRecord.ValueListCount));

            if (finalKey.NKRecord.ClassCellIndex > 0)
            {
                _logger.Debug("Getting Class cell information at relative offset 0x{0:X}",
                    finalKey.NKRecord.ClassCellIndex);
                var d = GetDataNodeFromOffset(finalKey.NKRecord.ClassCellIndex);
                d.IsReferenced = true;
                var clsName = Encoding.Unicode.GetString(d.Data, 0, finalKey.NKRecord.ClassLength);
                finalKey.ClassName = clsName;
                _logger.Debug("Class name found {0}", clsName);
            }

            return finalKey;
        }
    }
}