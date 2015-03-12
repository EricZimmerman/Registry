using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NFluent;
using NLog;
using Registry.Cells;
using Registry.Lists;
using static Registry.Other.Helpers;

// namespaces...

namespace Registry.Other
{
    // public classes...
    public class HBinRecord
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly bool _recoverDeleted;
        private readonly int _minorVersion;
        private byte[] _rawBytes;

        private RegistryHive _registryHive;

        // protected internal constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="HBinRecord" /> class.
        ///     <remarks>Represents a Hive Bin Record</remarks>
        /// </summary>
        protected internal HBinRecord(byte[] rawBytes, long relativeOffset, int minorVersion, bool recoverDeleted, RegistryHive reg)
        {
            RelativeOffset = relativeOffset;

            _registryHive = reg;

            _recoverDeleted = recoverDeleted;

            _minorVersion = minorVersion;

            _rawBytes = rawBytes;

            Signature = Encoding.ASCII.GetString(rawBytes, 0, 4);

            var sig = BitConverter.ToInt32(rawBytes, 0);

            Check.That(sig).IsEqualTo(HbinSignature);

            _logger.Debug("Got valid hbin signature for hbin at absolute offset 0x{0:X}", AbsoluteOffset);

            FileOffset = BitConverter.ToUInt32(rawBytes, 0x4);

            Size = BitConverter.ToUInt32(rawBytes, 0x8);

            Reserved = BitConverter.ToUInt32(rawBytes, 0xc);

            var ts = BitConverter.ToInt64(rawBytes, 0x14);

            try
            {
                var dt = DateTimeOffset.FromFileTime(ts).ToUniversalTime();

                if (dt.Year > 1601)
                {
                    LastWriteTimestamp = dt;
                }
            }
            catch (Exception)   //ncrunch: no coverage
            {                   //ncrunch: no coverage
                //very rarely you get a 'Not a valid Win32 FileTime' error, so trap it if thats the case
            }                   //ncrunch: no coverage

            Spare = BitConverter.ToUInt32(rawBytes, 0xc);
        }

        // public properties...
        /// <summary>
        ///     The offset to this record from the beginning of the hive, in bytes
        /// </summary>
        public long AbsoluteOffset
        {
            get { return RelativeOffset + 4096; }
        }

        // public properties...
        /// <summary>
        ///     The relative offset to this record
        /// </summary>
        public uint FileOffset { get; }

        /// <summary>
        ///     The last write time of this key
        /// </summary>
        public DateTimeOffset? LastWriteTimestamp { get; }

        /// <summary>
        ///     The offset to this record as stored by other records
        ///     <remarks>This value will be 4096 bytes (the size of the regf header) less than the AbsoluteOffset</remarks>
        /// </summary>
        public long RelativeOffset { get; }

        public uint Reserved { get; }

        /// <summary>
        ///     The signature of the hbin record. Should always be "hbin"
        /// </summary>
        public string Signature { get; }

        /// <summary>
        ///     The size of the hive
        ///     <remarks>
        ///         This value will always be positive. See IsFree to determine whether or not this cell is in use (it has a
        ///         negative size)
        ///     </remarks>
        /// </summary>
        public uint Size { get; }

        public uint Spare { get; }

        public List<IRecordBase> Process()
        {
            var records = new List<IRecordBase>();

            //additional cell data starts 32 bytes (0x20) in
            var offsetInHbin = 0x20;

            RegistryHive.TotalBytesRead += 0x20;

            while (offsetInHbin < Size)
            {
                var recordSize = BitConverter.ToUInt32(_rawBytes, offsetInHbin);

                var readSize = (int) recordSize;

                if (!_recoverDeleted && readSize > 0)
                {
                    //since we do not want to get deleted stuff, if the cell size > 0, its free, so skip it
                    offsetInHbin += readSize;
                    continue;
                }

                // if we get a negative number here the record is allocated, but we cant read negative bytes, so get absolute value
                readSize = Math.Abs(readSize);

                var rawRecord = new ArraySegment<byte>(_rawBytes, offsetInHbin, readSize).ToArray();//  new byte[readSize];

                RegistryHive.TotalBytesRead += readSize;

                var cellSignature = Encoding.ASCII.GetString(rawRecord, 4, 2);
                var cellSignature2 = BitConverter.ToInt16(rawRecord, 4);

                //ncrunch: no coverage start
                if (_logger.IsDebugEnabled)
                {
                    var foundMatch = false;
               
                        foundMatch = Regex.IsMatch(cellSignature, @"\A[a-z]{2}\z");

                    //only process records with 2 letter signatures. this avoids crazy output for data cells
                    if (foundMatch)
                    {
                        _logger.Debug("Processing {0} record at hbin relative offset 0x{1:X} (Absolute offset: 0x{2:X})",
                            cellSignature, offsetInHbin, offsetInHbin + RelativeOffset + 0x1000);
                    }
                    else
                    {
                        _logger.Debug("Processing data record at hbin relative offset 0x{0:X} (Absolute offset: 0x{1:X})",
                            offsetInHbin, offsetInHbin + RelativeOffset + 0x1000);
                    }  
                }
                //ncrunch: no coverage end

                ICellTemplate cellRecord = null;
                IListTemplate listRecord = null;
                DataNode dataRecord = null;

                try
                {
                    switch (cellSignature2)
                    {
                        case LfSignature:
                        case LhSignature:
                            listRecord = new LxListRecord(rawRecord, offsetInHbin + RelativeOffset);
                            break;

                        case LiSignature:
                            listRecord = new LIListRecord(rawRecord, offsetInHbin + RelativeOffset);

                            break;

                        case RiSignature:
                            listRecord = new RIListRecord(rawRecord, offsetInHbin + RelativeOffset);
                            break;

                        case DbSignature:
                            listRecord = new DBListRecord(rawRecord, offsetInHbin + RelativeOffset);
                            break;

                        case LkSignature:
                            cellRecord = new LKCellRecord(rawRecord, offsetInHbin + RelativeOffset);
                            break;   //ncrunch: no coverage

                        case NkSignature:
                            if (rawRecord.Length >= 0x30) // the minimum length for a recoverable record
                            {
                                cellRecord = new NKCellRecord(rawRecord.Length, offsetInHbin + RelativeOffset, _registryHive);
                            }

                            break;
                        case SkSignature:
                            cellRecord = new SKCellRecord(rawRecord, offsetInHbin + RelativeOffset);
                            break;

                        case VkSignature:
                            if (rawRecord.Length >= 0x18) // the minimum length for a recoverable record
                            {
                                cellRecord = new VKCellRecord(rawRecord.Length, offsetInHbin + RelativeOffset, _minorVersion, _registryHive);
                            }
                            break;

                        default:
                            dataRecord = new DataNode(rawRecord, offsetInHbin + RelativeOffset);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    //check size and see if its free. if so, dont worry about it. too small to be of value, but store it somewhere else?
                

                    var size = BitConverter.ToInt32(rawRecord, 0);

                    if (size < 0)
                    {                                               //ncrunch: no coverage
                        RegistryHive._hardParsingErrors += 1;       //ncrunch: no coverage

                        _logger.Error(                             //ncrunch: no coverage     
                            string.Format(                         
                                "Hard error processing record with cell signature {0} at Absolute Offset: 0x{1:X} with raw data: {2}",  
                                cellSignature, offsetInHbin + RelativeOffset + 4096, BitConverter.ToString(rawRecord)),                
                            ex);

                        //TODO store it somewhere else as a placeholder if its in use. include relative offset and other critical stuff

                    }                                              //ncrunch: no coverage                     
                    else
                    {
                        _logger.Warn(
                            string.Format(
                                "Soft error processing record with cell signature {0} at Absolute Offset: 0x{1:X} with raw data: {2}",
                                cellSignature, offsetInHbin + RelativeOffset + 4096, BitConverter.ToString(rawRecord)),
                            ex);
                        //This record is marked 'Free' so its not as important of an error
                        RegistryHive._softParsingErrors += 1;
                    }
                }

                List<IRecordBase> carvedRecords = null;

                if (cellRecord != null)
                {
                    if (cellRecord.IsFree)
                    {
                        carvedRecords = ExtractRecordsFromSlack(cellRecord.RawBytes, cellRecord.RelativeOffset);
                    }
                    else
                    {
                        records.Add((IRecordBase) cellRecord);
                    }
                }

                if (listRecord != null)
                {
                    if (_recoverDeleted)
                    {
                        carvedRecords = ExtractRecordsFromSlack(listRecord.RawBytes, listRecord.RelativeOffset);
                    }

                    records.Add((IRecordBase) listRecord);
                }

                if (dataRecord != null && _recoverDeleted)
                {
                    carvedRecords = ExtractRecordsFromSlack(dataRecord.RawBytes, dataRecord.RelativeOffset);
                }

                if (carvedRecords != null)
                {
                    if (carvedRecords.Count > 0)
                    {
                        records.AddRange(carvedRecords);
                    }
                }

                offsetInHbin += readSize;
            }

            _rawBytes = null;
            return records;
        }

        private List<IRecordBase> ExtractRecordsFromSlack(byte[] remainingData, long relativeoffset)
        {
            var records = new List<IRecordBase>();

            var offsetList2 = new List<int>();

            byte[] raw = null;

            _logger.Debug("Looking for cell signatures at absolute offset 0x{0:X}", relativeoffset + 0x1000);

            for (int i = 0; i < remainingData.Length; i++)
            {
                if (remainingData[i] == 0x6b) //6b == k
                {
                    if (remainingData[i - 1] == 0x6e || remainingData[i - 1] == 0x76) //6e = n, 76 = v
                    {
                        //if we are here we have a good signature, nk or vk
                        //check what is before that to see if its 0x00 or 0xFF
                        if (remainingData[i - 2] == 0x00 || remainingData[i - 2] == 0xFF)
                        {
                            //winner! since we initially hit on ZZ, substract 5 to get to the beginning of the record, XX XX XX XX YY ZZ
                            offsetList2.Add(i - 5); 
                        }
                    }
                }
            }

            //offsetList2 now has offset of every record signature we are interested in
            foreach (var i in offsetList2)
            {
                try
                {
                    var actualStart = i;

                    var size = BitConverter.ToUInt32(remainingData, actualStart);

                    if (size <= 3 || remainingData.Length - actualStart < size)
                    {
                        //if its empty or the size is beyond the data that is left, bail
                        continue;
                    }

                    raw = new ArraySegment<byte>(remainingData, actualStart, Math.Abs((int)size)).ToArray();

                    if (raw.Length<6)
                        continue; // since we need 4 bytes for the size and 2 for sig, if its smaller than 6, go to next one

                    var sig2 = BitConverter.ToInt16(raw, 4);

                    switch (sig2)
                    {
                        case NkSignature:
                            if (raw.Length <= 0x30)
                            {
                                continue;
                            }

                            var nk = new NKCellRecord(raw.Length, relativeoffset + actualStart, _registryHive);
                            if (nk.LastWriteTimestamp.Year > 1700)
                            {
                                _logger.Debug("Found nk record in slack at absolute offset 0x{0:X}",
                                    relativeoffset + actualStart + 0x1000);
                                records.Add(nk);
                            }

                            break;
                        case VkSignature:
                            if (raw.Length < 0x18)
                            {
                                //cant have a record shorter than this, even when no name is present
                                continue;
                            }
                            var vk = new VKCellRecord(raw.Length, relativeoffset + actualStart, _minorVersion, _registryHive);
                            _logger.Debug("Found vk record in slack at absolute offset 0x{0:X}",
                                relativeoffset + actualStart + 0x1000);
                            records.Add(vk);

                            break;
                    }

                }
                catch (Exception ex)                //ncrunch: no coverage
                {                                   //ncrunch: no coverage
                    // this is a corrupted/unusable record
                    _logger.Warn(                       //ncrunch: no coverage
                        string.Format(
                            "When recovering from slack at absolute offset 0x{0:X8}, an error happened! raw Length: 0x{1:x}",
                            relativeoffset + i + 0x1000, raw.Length), ex);

                    RegistryHive._softParsingErrors += 1;   //ncrunch: no coverage
                }                                   //ncrunch: no coverage
            }


            return records;
        }

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Size: 0x{0:X}", Size));
            sb.AppendLine(string.Format("Relative Offset: 0x{0:X}", RelativeOffset));
            sb.AppendLine(string.Format("Absolute Offset: 0x{0:X}", AbsoluteOffset));

            sb.AppendLine(string.Format("Signature: {0}", Signature));

            if (LastWriteTimestamp.HasValue)
            {
                sb.AppendLine(string.Format("Last Write Timestamp: {0}", LastWriteTimestamp));
            }

            sb.AppendLine();

            sb.AppendLine();
            sb.AppendLine(string.Format("File offset: 0x{0:X}", FileOffset));
            sb.AppendLine();

            sb.AppendLine(string.Format("Reserved: 0x{0:X}", Reserved));
            sb.AppendLine(string.Format("Spare: 0x{0:X}", Spare));

            return sb.ToString();
        }
    }
}