using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NFluent;
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

        private readonly byte[] _rawBytes;
        private readonly float _version;
        private MessageEventArgs _msgArgs;

        public event EventHandler<MessageEventArgs> Message;

        protected virtual void OnMessage(MessageEventArgs e)
        {
            var handler = Message;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        // protected internal constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="HBinRecord" /> class.
        ///     <remarks>Represents a Hive Bin Record</remarks>
        /// </summary>
        protected internal HBinRecord(byte[] rawBytes, long relativeOffset, float version)
        {
            RelativeOffset = relativeOffset;

            _version = version;

            _rawBytes = rawBytes;

            Signature = Encoding.ASCII.GetString(rawBytes, 0, 4);

            // Debug.WriteLine("hbin abs 0x{0:x8}",AbsoluteOffset);

            //if (AbsoluteOffset == 0x533000)
            //   Debug.WriteLine("");

            Check.That(Signature).IsEqualTo("hbin");

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
        public uint FileOffset { get; private set; }

        /// <summary>
        ///     The last write time of this key
        /// </summary>
        public DateTimeOffset? LastWriteTimestamp { get; private set; }

        /// <summary>
        ///     The offset to this record as stored by other records
        ///     <remarks>This value will be 4096 bytes (the size of the regf header) less than the AbsoluteOffset</remarks>
        /// </summary>
        public long RelativeOffset { get; private set; }

        public uint Reserved { get; private set; }

        /// <summary>
        ///     The signature of the hbin record. Should always be "hbin"
        /// </summary>
        public string Signature { get; private set; }

        /// <summary>
        ///     The size of the hive
        ///     <remarks>
        ///         This value will always be positive. See IsFree to determine whether or not this cell is in use (it has a
        ///         negative size)
        ///     </remarks>
        /// </summary>
        public uint Size { get; private set; }

        public uint Spare { get; private set; }

        public List<IRecordBase> Process()
        {
            var records = new List<IRecordBase>();


            //additional cell data starts 32 bytes (0x20) in
            var offsetInHbin = 0x20;

            RegistryHive.TotalBytesRead += 0x20;

            while (offsetInHbin < Size)
            {
                //Debug.WriteLine("offsetInHbin is 0x{0:X8}", offsetInHbin);
                //if (offsetInHbin == 0x00005B08)
                //    Debug.WriteLine("offsetInHbin is 0x{0:X8}", offsetInHbin);


                var recordSize = BitConverter.ToUInt32(_rawBytes, offsetInHbin);

                var readSize = (int) recordSize;

                readSize = Math.Abs(readSize);
                // if we get a negative number here the record is allocated, but we cant read negative bytes, so get absolute value

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
                if (foundMatch && RegistryHive.Verbosity == RegistryHive.VerbosityEnum.Full)
                {
                    var args = new MessageEventArgs
                    {
                        Detail =
                            string.Format("\tProcessing {0} record at offset 0x{1:X} (Absolute offset: 0x{2:X})",cellSignature, offsetInHbin, offsetInHbin + RelativeOffset),
                        Exception = null,
                        Message = string.Format("\tProcessing {0} record at offset 0x{1:X} (Absolute offset: 0x{2:X})", cellSignature, offsetInHbin, offsetInHbin + RelativeOffset),
                        MsgType = MessageEventArgs.MsgTypeEnum.Info
                    };

                    OnMessage(args);

                    //Console.WriteLine("\tProcessing {0} record at offset 0x{1:X} (Absolute offset: 0x{2:X})",
                    //    cellSignature, offsetInHbin, offsetInHbin + RelativeOffset);
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
                            cellRecord = new NKCellRecord(rawRecord, offsetInHbin + RelativeOffset);

                            break;
                        case "sk":
                            cellRecord = new SKCellRecord(rawRecord, offsetInHbin + RelativeOffset);

                            break;

                        case "vk":
                            if (rawRecord.Length >= 0x18) // the minimum length for a recoverable record
                            {
                                cellRecord = new VKCellRecord(rawRecord, offsetInHbin + RelativeOffset, _version);
                            }


                            break;

                        default:
                            dataRecord = new DataNode(rawRecord, offsetInHbin + RelativeOffset);

                            break;
                    }
                }
                catch (Exception ex)
                {
                    //check size and see if its free. if so, dont worry about it. too small to be of value, but store it somewhere else
                    //TODO store it somewhere else as a placeholder if its in use. include relative offset and other critical stuff

                    var size = BitConverter.ToInt32(rawRecord, 0);

                    if (size < 0)
                    {
                        RegistryHive._hardParsingErrors += 1;
                        //  Debug.WriteLine("Cell signature: {0}, Error: {1}, Stack: {2}. Hex: {3}", cellSignature, ex.Message, ex.StackTrace, BitConverter.ToString(rawRecord));

                        var args = new MessageEventArgs
                        {
                            Detail =
                                string.Format(
                            "Cell signature: {0}, Absolute Offset: 0x{1:X}, Error: {2}, Stack: {3}. Hex: {4}",
                            cellSignature, offsetInHbin + RelativeOffset + 4096, ex.Message, ex.StackTrace,
                            BitConverter.ToString(rawRecord)),
                            Exception = ex,
                            Message = string.Format(
                            "Cell signature: {0}, Absolute Offset: 0x{1:X}, Error: {2}, Stack: {3}. Hex: {4}",
                            cellSignature, offsetInHbin + RelativeOffset + 4096, ex.Message, ex.StackTrace,
                            BitConverter.ToString(rawRecord)),
                            MsgType = MessageEventArgs.MsgTypeEnum.Error
                        };

                        OnMessage(args);


                        //Console.WriteLine(
                        //    "Cell signature: {0}, Absolute Offset: 0x{1:X}, Error: {2}, Stack: {3}. Hex: {4}",
                        //    cellSignature, offsetInHbin + RelativeOffset + 4096, ex.Message, ex.StackTrace,
                        //    BitConverter.ToString(rawRecord));

                        Console.WriteLine();
                        Console.WriteLine();
                        //Console.WriteLine("Press a key to continue");

                        //Console.ReadKey();
                    }
                    else
                    {
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
                        //  RegistryHive.CellRecords.Add(cellRecord.RelativeOffset, cellRecord);
                    }
                }

                if (listRecord != null)
                {
                    records.Add((IRecordBase) listRecord);
                    // RegistryHive.ListRecords.Add(listRecord.RelativeOffset, listRecord);
                    carvedRecords = ExtractRecordsFromSlack(listRecord.RawBytes, listRecord.RelativeOffset);
                }

                if (dataRecord != null)
                {
                    carvedRecords = ExtractRecordsFromSlack(dataRecord.RawBytes, dataRecord.RelativeOffset);
                }

                if (carvedRecords != null)
                {
                    records.AddRange(carvedRecords);
                }

                offsetInHbin += readSize;
            }

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


            try
            {
                var matchResult = _recordPattern.Match(valString);
                while (matchResult.Success)
                {
                    offsetList.Add(matchResult.Index);
                    matchResult = matchResult.NextMatch();
                }
            }
            catch (ArgumentException ex)
            {
                // Syntax error in the regular expression
            }

            if (offsetList.Count == 0)
            {
                //its a data record

                var dr1 = new DataNode(remainingData, relativeoffset);

                records.Add(dr1);

            }
            else
            {
                //is this a strange case where there are records at the end of a data block?
                var actualStart = (offsetList.First()/3) - 3;
                if (actualStart > 0)
                {
                    var dr = new DataNode(remainingData, relativeoffset);
                    records.Add(dr);

                }
            }

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
                            var nk = new NKCellRecord(raw, relativeoffset + actualStart);
                            if (nk.LastWriteTimestamp.Year > 1700)
                            {
                                records.Add(nk);
                            }

                            break;
                        case "vk":
                            if (raw.Length < 0x18)
                            {
                                //cant have a record shorter than this, even when no name is present
                                continue;
                            }
                            var vk = new VKCellRecord(raw, relativeoffset + actualStart, _version);
                            records.Add(vk);

                            break;
                        case "ri":
                            var ri = new RIListRecord(raw, relativeoffset + actualStart);
                            records.Add(ri);

                            break;

                        case "sk":
                            var sk = new SKCellRecord(raw, relativeoffset + actualStart);
                            records.Add(sk);
                            
                            break;

                        case "lf":
                            var lf = new LxListRecord(raw, relativeoffset + actualStart);
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

                            var dr = new DataNode(raw, relativeoffset + actualStart);

                            records.Add(dr);

                            break;
                    }

                    // System.Diagnostics.Debug.WriteLine("Found rel offset at 0x{0:X}", relativeoffset + actualStart);
                }
                catch (Exception ex)
                {
                    // this is a corrupted/unusable record
                    //TODO do we add a placeholder here? probably not since its free
                    //Console.WriteLine("{0}: At relativeoffset 0x{1:X8}, an error happened: {2}. LENGTH: 0x{3:x}", sig,
                    //    relativeoffset + (i/3) - 3, ex.Message, raw.Length);

                    var args = new MessageEventArgs
                    {
                        Detail =
                            string.Format("{0}: At relativeoffset 0x{1:X8}, an error happened: {2}. LENGTH: 0x{3:x}", sig,
                        relativeoffset + (i / 3) - 3, ex.Message, raw.Length),
                        Exception = ex,
                        Message = string.Format("{0}: At relativeoffset 0x{1:X8}, an error happened: {2}. LENGTH: 0x{3:x}", sig,
                        relativeoffset + (i/3) - 3, ex.Message, raw.Length),
                        MsgType = MessageEventArgs.MsgTypeEnum.Error
                    };

                    OnMessage(args);
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