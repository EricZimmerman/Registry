using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using NFluent;
using NLog;
using Registry.Cells;
using Registry.Lists;

// namespaces...

namespace Registry.Other
{
    // public classes...
    public class HBinRecord
    {
        // this is static because its a lot faster than when its not!
        private static readonly Regex _recordPattern = new Regex("(00|FF)-(6E|73|76)-6B", RegexOptions.Compiled);

        private readonly List<string> _goodSigs = new List<string>
        {
            "lf",
            "lh",
            "li",
            "ri",
            "db",
            "lk",
            "nk",
            "sk",
            "vk"
        };

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly bool _recoverDeleted;
        private readonly int _minorVersion;
        private byte[] _rawBytes;
        // protected internal constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="HBinRecord" /> class.
        ///     <remarks>Represents a Hive Bin Record</remarks>
        /// </summary>
        protected internal HBinRecord(byte[] rawBytes, long relativeOffset, int minorVersion, bool recoverDeleted)
        {
            RelativeOffset = relativeOffset;

            _recoverDeleted = recoverDeleted;

            _minorVersion = minorVersion;

            _rawBytes = rawBytes;

            Signature = Encoding.ASCII.GetString(rawBytes, 0, 4);

            // Debug.WriteLine("hbin abs 0x{0:x8}",AbsoluteOffset);

            //if (AbsoluteOffset == 0x533000)
            //   Debug.WriteLine("");

            Check.That(Signature).IsEqualTo("hbin");

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
            catch (Exception)
            {
                //very rarely you get a 'Not a valid Win32 FileTime' error, so trap it if thats the case
            }

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
        public event EventHandler<MessageEventArgs> Message;

        protected virtual void OnMessage(MessageEventArgs e)
        {
            var handler = Message;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public List<IRecordBase> Process()
        {
            var records = new List<IRecordBase>();


            //additional cell data starts 32 bytes (0x20) in
            var offsetInHbin = 0x20;

            RegistryHive.TotalBytesRead += 0x20;

//            if (AbsoluteOffset == 0x6000)
//            Debug.Write(1);


            while (offsetInHbin < Size)
            {
//                Debug.WriteLine("offsetInHbin is 0x{0:X8}", offsetInHbin);
//                Debug.WriteLine("abs off is 0x{0:X8}", offsetInHbin + RelativeOffset + 0x1000);
                //if (offsetInHbin == 0x00005B08)
                //    Debug.WriteLine("offsetInHbin is 0x{0:X8}", offsetInHbin);

                //                if (offsetInHbin + RelativeOffset == 0x70e3020)
                //                {
                //                    Debug.Write(1);
                //                }


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

                var rawRecord = new byte[readSize];

                Array.Copy(_rawBytes, offsetInHbin, rawRecord, 0, readSize);

                //var rawRecord = rawBytes.Skip(offsetInHbin).Take(readSize).ToArray();

                RegistryHive.TotalBytesRead += readSize;

                var cellSignature = Encoding.ASCII.GetString(rawRecord, 4, 2);

                var foundMatch = false;
                try
                {
                    foundMatch = Regex.IsMatch(cellSignature, @"\A[a-z]{2}\z");
                }
                catch (ArgumentException)
                {
                    // Syntax error in the regular expression
                }

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

                ICellTemplate cellRecord = null;
                IListTemplate listRecord = null;
                DataNode dataRecord = null;

                try
                {
                    switch (cellSignature)
                    {
                        case "lf":
                        case "lh":
                            listRecord = new LxListRecord(rawRecord, offsetInHbin + RelativeOffset);

                            break;

                        case "li":
                            listRecord = new LIListRecord(rawRecord, offsetInHbin + RelativeOffset);

                            break;

                        case "ri":
                            listRecord = new RIListRecord(rawRecord, offsetInHbin + RelativeOffset);

                            break;

                        case "db":
                            listRecord = new DBListRecord(rawRecord, offsetInHbin + RelativeOffset);

                            break;

                        case "lk":
                            cellRecord = new LKCellRecord(rawRecord, offsetInHbin + RelativeOffset);

                            break;

                        case "nk":
                            if (rawRecord.Length >= 0x30) // the minimum length for a recoverable record
                            {
                                cellRecord = new NKCellRecord(rawRecord, offsetInHbin + RelativeOffset);
                            }

                            break;
                        case "sk":
                            cellRecord = new SKCellRecord(rawRecord, offsetInHbin + RelativeOffset);

                            break;

                        case "vk":
                            if (rawRecord.Length >= 0x18) // the minimum length for a recoverable record
                            {
                                cellRecord = new VKCellRecord(rawRecord, offsetInHbin + RelativeOffset, _minorVersion);
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
                    //TODO store it somewhere else as a placeholder if its in use. include relative offset and other critical stuff

                    var size = BitConverter.ToInt32(rawRecord, 0);

                    if (size < 0)
                    {
                        RegistryHive._hardParsingErrors += 1;

                        _logger.Error(
                            string.Format(
                                "Hard error processing record with cell signature {0} at Absolute Offset: 0x{1:X} with raw data: {2}",
                                cellSignature, offsetInHbin + RelativeOffset + 4096, BitConverter.ToString(rawRecord)),
                            ex);
                    }
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
            // a list of our known signatures, so we can only show when these are found vs data cells

            var records = new List<IRecordBase>();

            var offsetList = new List<int>();

            var valString = BitConverter.ToString(remainingData);

            var sig = string.Empty;
            byte[] raw = null;

            _logger.Debug("Looking for cell signatures at absolute offset 0x{0:X}", relativeoffset + 0x1000);
            try
            {
                var matchResult = _recordPattern.Match(valString);
                while (matchResult.Success)
                {
                    offsetList.Add(matchResult.Index);
                    matchResult = matchResult.NextMatch();
                }
            }
            catch (ArgumentException)
            {
                // Syntax error in the regular expression
            }

//            if (offsetList.Count == 0)
//            {
//                //its a data record
//
//                //var dr1 = new DataNode(remainingData, relativeoffset);
//
//              //  records.Add(dr1);
//            }
//            else
//            {
//                //is this a strange case where there are records at the end of a data block?
//                var actualStart = (offsetList.First()/3) - 3;
//                if (actualStart > 0)
//                {
//                 //   var dr = new DataNode(remainingData, relativeoffset);
//                 //   records.Add(dr);
//                }
//            }

            //resultList now has offset of every record signature we are interested in
            foreach (var i in offsetList)
            {
                try
                {
                    //we found one, but since we converted it to a string, divide by 3 to get to the proper offset
                    //finaly go back 3 to get to the start of the record
                    var actualStart = (i/3) - 3;


                    var size = BitConverter.ToUInt32(remainingData, actualStart);

                    if (size < 3 || remainingData.Length - actualStart < size)
                    {
                        //if its empty or the size is beyond the data that is left, bail
                        continue;
                    }

                    raw = new byte[Math.Abs((int) size)];

                    Array.Copy(remainingData, actualStart, raw, 0, Math.Abs((int) size));

                    sig = Encoding.ASCII.GetString(raw, 4, 2);


                    switch (sig)
                    {
                        case "nk":
                            if (raw.Length <= 0x30)
                            {
                                continue;
                            }

                            var nk = new NKCellRecord(raw, relativeoffset + actualStart);
                            if (nk.LastWriteTimestamp.Year > 1700)
                            {
                                _logger.Debug("Found nk record in slack at absolute offset 0x{0:X}",
                                    relativeoffset + actualStart + 0x1000);
                                records.Add(nk);
                            }

                            break;
                        case "vk":
                            if (raw.Length < 0x18)
                            {
                                //cant have a record shorter than this, even when no name is present
                                continue;
                            }
                            var vk = new VKCellRecord(raw, relativeoffset + actualStart, _minorVersion);
                            _logger.Debug("Found vk record in slack at absolute offset 0x{0:X}",
                                relativeoffset + actualStart + 0x1000);
                            records.Add(vk);

                            break;
                        case "ri":
                            var ri = new RIListRecord(raw, relativeoffset + actualStart);
                            _logger.Debug("Found ri record in slack at absolute offset 0x{0:X}",
                                relativeoffset + actualStart + 0x1000);
                            records.Add(ri);

                            break;

                        case "sk":
                            var sk = new SKCellRecord(raw, relativeoffset + actualStart);
                            _logger.Debug("Found sk record in slack at absolute offset 0x{0:X}",
                                relativeoffset + actualStart + 0x1000);
                            records.Add(sk);

                            break;

                        case "lf":
                            var lf = new LxListRecord(raw, relativeoffset + actualStart);
                            _logger.Debug("Found lf record in slack at absolute offset 0x{0:X}",
                                relativeoffset + actualStart + 0x1000);
                            records.Add(lf);

                            break;

                        default:
                            //we know about these signatures, so remove them. if we see others, tell someone so support can be added
                            var goodSigs2 = _goodSigs;
                            goodSigs2.Remove("nk");
                            goodSigs2.Remove("vk");
                            goodSigs2.Remove("sk");
                            goodSigs2.Remove("li");
                            goodSigs2.Remove("ri");

                            if (goodSigs2.Contains(sig))
                            {
                                throw new Exception(
                                    "Found a good signature when expecting a data node! please send this hive to saericzimmerman@gmail.com so support can be added");
                            }

                            //var dr = new DataNode(raw, relativeoffset + actualStart);

//                            records.Add(dr);

                            break;
                    }

                    // System.Diagnostics.Debug.WriteLine("Found rel offset at 0x{0:X}", relativeoffset + actualStart);
                }
                catch (Exception ex)
                {
                    // this is a corrupted/unusable record
                    //TODO do we add a placeholder here? probably not since its free

                    _logger.Warn(
                        string.Format(
                            "When recovering from slack, cell signature {0}, at absolute offset 0x{1:X8}, an error happened! Length: 0x{2:x}",
                            sig, relativeoffset + (i/3) - 3 + 0x1000, raw.Length), ex);

                    RegistryHive._softParsingErrors += 1;
                }
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