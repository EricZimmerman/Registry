using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NLog;
using NLog.Config;
using Registry.Abstractions;
using Registry.Cells;
using Registry.Lists;
using Registry.Other;

namespace Registry
{
	public class RegistryHiveOnDemand
	{
		private static LoggingConfiguration _nlogConfig;
		private readonly byte[] _fileBytes;
		private readonly Logger _logger = LogManager.GetCurrentClassLogger();

		public RegistryHiveOnDemand(string fileName)
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

			if (!RegistryHive.HasValidHeader(fileName))
			{
				_logger.Error("'{0}' is not a Registry hive (bad signature)", fileName);

				throw new Exception(string.Format("'{0}' is not a Registry hive (bad signature)", fileName));
			}

			var fileStream = new FileStream(Filename, FileMode.Open);
			var binaryReader = new BinaryReader(fileStream);

			binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);

			RegistryHive.FileBytes = binaryReader.ReadBytes((int) binaryReader.BaseStream.Length);
			_fileBytes = RegistryHive.FileBytes;

			binaryReader.Close();
			fileStream.Close();
			
			_logger.Debug("Set HivePath to {0}", Filename);

			var header = ReadBytesFromHive(0, 4096);

			_logger.Debug("Getting header");

			Header = new RegistryHeader(header);

			_logger.Debug("Got header. Embedded file name {0}", Header.FileName);

			var fnameBase = Path.GetFileName(Header.FileName).ToLower();

			switch (fnameBase)
			{
				case "ntuser.dat":
					HiveType = RegistryHive.HiveTypeEnum.NtUser;
					break;
				case "sam":
					HiveType = RegistryHive.HiveTypeEnum.Sam;
					break;
				case "security":
					HiveType = RegistryHive.HiveTypeEnum.Security;
					break;
				case "software":
					HiveType = RegistryHive.HiveTypeEnum.Software;
					break;
				case "system":
					HiveType = RegistryHive.HiveTypeEnum.System;
					break;
				case "usrclass.dat":
					HiveType = RegistryHive.HiveTypeEnum.UsrClass;
					break;

				default:
					HiveType = RegistryHive.HiveTypeEnum.Other;
					break;
			}
			_logger.Debug("Hive is a {0} hive", HiveType);

			var version = string.Format("{0}.{1}", Header.MajorVersion, Header.MinorVersion);

			_logger.Debug("Hive version is {0}", version);
		}

		public RegistryHive.HiveTypeEnum HiveType { get; }
		public string HivePath { get; private set; }
		public string Filename { get; }
		public RegistryHeader Header { get; }

		public static LoggingConfiguration NlogConfig
		{
			get { return _nlogConfig; }
			set
			{
				_nlogConfig = value;
				LogManager.Configuration = _nlogConfig;
			}
		}

		private List<RegistryKey> GetSubkeys(uint subkeyListsStableCellIndex, RegistryKey parent)
		{
			var keys = new List<RegistryKey>();

			//this needs to be a function so it can be reused for every call
			_logger.Debug("Looking for list record at relative offset 0x{0:X}", subkeyListsStableCellIndex);

			var rawList = GetRawRecord(subkeyListsStableCellIndex);

			var l = GetListFromRawBytes(rawList, subkeyListsStableCellIndex);
			
			switch (l.Signature)
			{
				case "lf":
				case "lh":
					var lxRecord = l as LxListRecord;

					foreach (var offset in lxRecord.Offsets)
					{
						_logger.Debug("In lf or lh, looking for nk record at relative offset 0x{0:X}", offset);
						var rawCell = GetRawRecord(offset.Key);
						var nk = new NKCellRecord(rawCell, offset.Key);
						
						_logger.Debug("In lf or lh, found nk record at relative offset 0x{0:X}. Name: {1}", offset,
							nk.Name);

						var tempKey = new RegistryKey(nk, parent);

						keys.Add(tempKey);
					}
					break;

				case "ri":
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
								var nk = new NKCellRecord(rawCell, offset1);
								
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
								var nk = new NKCellRecord(rawCell, offset3.Key);

								var tempKey = new RegistryKey(nk, parent);

								keys.Add(tempKey);
							}
						}
					}
					
					break;

				case "li":
					var liRecord = l as LIListRecord;
					
					foreach (var offset in liRecord.Offsets)
					{
						_logger.Debug("In li, looking for nk record at relative offset 0x{0:X}", offset);
						var rawCell = GetRawRecord(offset);
						var nk = new NKCellRecord(rawCell, offset);

						var tempKey = new RegistryKey(nk, parent);
						keys.Add(tempKey);
					}
					
					break;
				default:
					throw new Exception(string.Format("Unknown subkey list type {0}!", l.Signature));
			}
			
			return keys;
		}

		private List<KeyValue> GetKeyValues(uint valueListCellIndex, uint valueListCount)
		{
			var values = new List<KeyValue>();

			var offsets = new List<uint>();

			//this needs to be a function so it can be reused for every call
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
				_logger.Warn("Value count mismatch! ValueListCount is {0:N0} but NKRecord.ValueOffsets.Count is {1:N0}",
					valueListCount, offsets.Count);
			}

			foreach (var valueOffset in offsets)
			{
				_logger.Debug("Looking for vk record at relative offset 0x{0:X}", valueOffset);

				var rawVK = GetRawRecord(valueOffset);
				var vk = new VKCellRecord(rawVK, valueOffset, Header.MinorVersion);

				_logger.Debug("Found vk record at relative offset 0x{0:X}. Value name: {1}", valueOffset, vk.ValueName);
				var value = new KeyValue(vk);
				values.Add(value);
			}
			
			return values;
		}

		private IListTemplate GetListFromRawBytes(byte[] rawBytes, long relativeOffset)
		{
			var sig = Encoding.ASCII.GetString(rawBytes, 4, 2);

			switch (sig)
			{
				case "lf":
				case "lh":
					return new LxListRecord(rawBytes, relativeOffset);
				case "ri":
					return new RIListRecord(rawBytes, relativeOffset);
				case "li":
					return new LIListRecord(rawBytes, relativeOffset);
				default:
					throw new Exception(string.Format("Unknown list signature: {0}", sig));
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

			var rootNk = new NKCellRecord(rawRoot, Header.RootCellOffset);

			var rootKey = new RegistryKey(rootNk, null);

			var keyNames = keyPath.Split(new[] {'\\'}, StringSplitOptions.RemoveEmptyEntries);

			rootKey.SubKeys.AddRange(GetSubkeys(rootKey.NKRecord.SubkeyListsStableCellIndex, rootKey));

			var finalKey = rootKey;
			
			for (var i = 0; i < keyNames.Length; i++)
			{
				finalKey = finalKey.SubKeys.SingleOrDefault(r => r.KeyName == keyNames[i]);

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

			return finalKey;
		}

		private byte[] ReadBytesFromHive(long absoluteOffset, int length)
		{
			var absLen = Math.Abs(length);
			var retArray = new byte[absLen];
			Array.Copy(_fileBytes, absoluteOffset, retArray, 0, absLen);
			return retArray;
		}
	}
}