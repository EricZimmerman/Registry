using NFluent;
using Registry.Cells;
using Registry.Lists;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// namespaces...
namespace Registry.Other
{
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
                ;

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

            var recordSize = BitConverter.ToUInt32(rawBytes, 0x20);

            int readSize;

            var offsetInHbin = 0x20;



            while (offsetInHbin < Size)
            {
                recordSize = BitConverter.ToUInt32(rawBytes, offsetInHbin);

                readSize = (int)recordSize;

                readSize = Math.Abs(readSize);
                // if we get a negative number here the record is allocated, but we cant read negative bytes, so get absolute value

                var rawRecord = rawBytes.Skip(offsetInHbin).Take(readSize).ToArray();

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

                //only process records with 2 letter signatures. this avoids wasting time on data cells
                if (foundMatch && RegistryHive.VerboseOutput)
                {
                    Console.WriteLine("\tprocessing {0} record at offset 0x{1:X} (Absolute offset: 0x{2:X})",
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

                            //  Debug.WriteLine(listRecord);

                            break;

                        case "li":
                            listRecord = new LIListRecord(rawRecord, offsetInHbin + relativeOffset);

                            //   Debug.WriteLine(listRecord);

                            break;

                        case "ri":
                            listRecord = new RIListRecord(rawRecord, offsetInHbin + relativeOffset);

                            //  Debug.WriteLine(listRecord);
                            break;

                        case "db":
                            listRecord = new DBListRecord(rawRecord, offsetInHbin + relativeOffset);

                            //    Debug.WriteLine(listRecord);
                            break;

                        case "lk":

                            //    Debug.WriteLine(cellRecord);
                            break;

                        case "nk":
                            cellRecord = new NKCellRecord(rawRecord, offsetInHbin + relativeOffset);

                            //    Debug.WriteLine(cellRecord);
                            break;
                        case "sk":
                            cellRecord = new SKCellRecord(rawRecord, offsetInHbin + relativeOffset);

                            //   Debug.WriteLine(cellRecord);

                            break;

                        case "vk":
                            cellRecord = new VKCellRecord(rawRecord, offsetInHbin + relativeOffset);

                            //  System.IO.File.AppendAllText(@"C:\temp\values.txt",cellRecord.ToString());

                            break;

                        default:
                            dataRecord = new DataNode(rawRecord, offsetInHbin + relativeOffset);

                            //     Debug.WriteLine(string.Format( "Unknown cell signature: {0}", cellSignature));

                            break;
                    }
                }
                catch (Exception ex)
                {
                    //check size and see if its free. if so, dont worry about it. too small to be of value, but store it somewhere else
                    //TODO store it somewhere else

                    var _size = BitConverter.ToInt32(rawRecord, 0);

                    if (_size < 0)
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
                }

                if (listRecord != null)
                {
                    RegistryHive.ListRecords.Add(listRecord.RelativeOffset, listRecord);
                }

                if (dataRecord != null)
                {
                    RegistryHive.DataRecords.Add(dataRecord.RelativeOffset, dataRecord);
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
        public uint FileOffset { get; private set; }
        public DateTimeOffset? LastWriteTimestamp { get; private set; }
        /// <summary>
        /// The offset to this record as stored by other records
        /// </summary>
        public long RelativeOffset { get; private set; }
        public uint Reserved { get; private set; }
        /// <summary>
        /// The signature of the hbin record. Should always be "hbin"
        /// </summary>
        public string Signature { get; private set; }
        public uint Size { get; private set; }
        public uint Spare { get; private set; }

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Size: 0x{0:X}", Size));
            sb.AppendLine(string.Format("RelativeOffset: 0x{0:X}", RelativeOffset));
            sb.AppendLine(string.Format("AbsoluteOffset: 0x{0:X}", AbsoluteOffset));

            sb.AppendLine(string.Format("Signature: {0}", Signature));

            if (LastWriteTimestamp.HasValue)
            {
                sb.AppendLine(string.Format("LastWriteTimestamp: {0}", LastWriteTimestamp));
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
