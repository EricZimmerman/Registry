using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NFluent;
using Registry.Cells;
using Registry.Lists;

// namespaces...
namespace Registry.Other
{

    //TODO get rid of calls to Console.WriteLine in favor of an event
    //May need to move processing out of constructor and add a "ParseBytes" or "initialize" method

    // public classes...
    public class HBinRecord
    {
        // protected internal constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="HBinRecord"/> class.
        /// <remarks>Represents a Hive Bin Record</remarks>
        /// </summary>
        protected internal HBinRecord(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;

            Signature = Encoding.ASCII.GetString(rawBytes, 0, 4);

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

            //additional cell data starts 32 bytes (0x20) in
            var offsetInHbin = 0x20;

            RegistryHive.TotalBytesRead += 0x20;

            while (offsetInHbin < Size)
            {
                var recordSize = BitConverter.ToUInt32(rawBytes, offsetInHbin);

                var readSize = (int)recordSize;

                readSize = Math.Abs(readSize);
                // if we get a negative number here the record is allocated, but we cant read negative bytes, so get absolute value

                var rawRecord = rawBytes.Skip(offsetInHbin).Take(readSize).ToArray();

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
                    Console.WriteLine("\tProcessing {0} record at offset 0x{1:X} (Absolute offset: 0x{2:X})",
                        cellSignature, offsetInHbin, offsetInHbin + relativeOffset);
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
                            listRecord = new LxListRecord(rawRecord, offsetInHbin + relativeOffset);

                            break;

                        case "li":
                            listRecord = new LIListRecord(rawRecord, offsetInHbin + relativeOffset);

                            break;

                        case "ri":
                            listRecord = new RIListRecord(rawRecord, offsetInHbin + relativeOffset);

                            break;

                        case "db":
                            listRecord = new DBListRecord(rawRecord, offsetInHbin + relativeOffset);

                            break;

                        case "lk":
                            cellRecord = new LKCellRecord(rawRecord, offsetInHbin + relativeOffset);

                            break;

                        case "nk":
                            cellRecord = new NKCellRecord(rawRecord, offsetInHbin + relativeOffset);

                            break;
                        case "sk":
                            cellRecord = new SKCellRecord(rawRecord, offsetInHbin + relativeOffset);

                            break;

                        case "vk":
                            cellRecord = new VKCellRecord(rawRecord, offsetInHbin + relativeOffset);

                            break;

                        default:
                            dataRecord = new DataNode(rawRecord, offsetInHbin + relativeOffset);

                            break;
                    }
                }
                catch (Exception ex)
                {
                    //check size and see if its free. if so, dont worry about it. too small to be of value, but store it somewhere else
                    //TODO store it somewhere else

                    var size = BitConverter.ToInt32(rawRecord, 0);

                    if (size < 0)
                    {
                        RegistryHive._hardParsingErrors += 1;
                        //  Debug.WriteLine("Cell signature: {0}, Error: {1}, Stack: {2}. Hex: {3}", cellSignature, ex.Message, ex.StackTrace, BitConverter.ToString(rawRecord));

                        Console.WriteLine("Cell signature: {0}, Absolute Offset: 0x{1:X}, Error: {2}, Stack: {3}. Hex: {4}", cellSignature, offsetInHbin + relativeOffset + 4096, ex.Message, ex.StackTrace, BitConverter.ToString(rawRecord));

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


                if (cellRecord != null)
                {
                    RegistryHive.CellRecords.Add(cellRecord.RelativeOffset, cellRecord);

                    //   Debug.WriteLine(cellRecord);
                }

                if (listRecord != null)
                {
                    RegistryHive.ListRecords.Add(listRecord.RelativeOffset, listRecord);
                    //   Debug.WriteLine(listRecord);
                }

                if (dataRecord != null)
                {
                    if (dataRecord.IsFree)
                    {
                        //if the record is free, we have to do more to ensure we find other recoverable records
                        //we do not need to add the record to the DataRecords collection as ExtractRecordsFromSlack does that for us
                        Helpers.ExtractRecordsFromSlack(dataRecord.RawBytes, dataRecord.RelativeOffset);
                    }
                    else
                    {
                        RegistryHive.DataRecords.Add(dataRecord.RelativeOffset, dataRecord);
                    }

                    //   Debug.WriteLine(dataRecord);
                }

                offsetInHbin += readSize;
            }
        }

        // public properties...
        /// <summary>
        /// The offset to this record from the beginning of the hive, in bytes
        /// </summary>
        public long AbsoluteOffset
        {
            get
            {
                return RelativeOffset + 4096;
            }
        }

        // public properties...
        /// <summary>
        /// The relative offset to this record
        /// </summary>
        public uint FileOffset { get; private set; }
        /// <summary>
        /// The last write time of this key
        /// </summary>
        public DateTimeOffset? LastWriteTimestamp { get; private set; }
        /// <summary>
        /// The offset to this record as stored by other records
        /// <remarks>This value will be 4096 bytes (the size of the regf header) less than the AbsoluteOffset</remarks>
        /// </summary>
        public long RelativeOffset { get; private set; }
        public uint Reserved { get; private set; }
        /// <summary>
        /// The signature of the hbin record. Should always be "hbin"
        /// </summary>
        public string Signature { get; private set; }
        /// <summary>
        /// The size of the hive
        /// <remarks>This value will always be positive. See IsFree to determine whether or not this cell is in use (it has a negative size)</remarks>
        /// </summary>
        public uint Size { get; private set; }
        public uint Spare { get; private set; }

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
