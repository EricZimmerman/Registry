using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NFluent;
using Registry.Cells;

// namespaces...

namespace Registry
{
    // public classes...
    public class HBinRecord
    {
        // protected internal constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="HBinRecord" /> class.
        ///   <remarks>Represents a Hive Bin Record</remarks>
        /// </summary>
        protected internal HBinRecord(byte[] rawBytes)
        {
            Signature = Encoding.ASCII.GetString(rawBytes, 0, 4);

            Check.That(Signature).IsEqualTo("hbin");

            FileOffset = BitConverter.ToUInt32(rawBytes, 0x4);

            Size = BitConverter.ToUInt32(rawBytes, 0x8);

            Reserved = BitConverter.ToUInt32(rawBytes, 0xc);

            var ts = BitConverter.ToInt64(rawBytes, 0x14);

            var dt = DateTimeOffset.FromFileTime(ts);

            if (dt.Year > 1600)
            {
                LastWriteTimestamp = dt;
            }

            Spare = BitConverter.ToUInt32(rawBytes, 0xc);

            //additional cell data starts 32 bytes (0x20) in

            var recordSize = BitConverter.ToUInt32(rawBytes, 0x20);

            var readSize = (int) recordSize;

            var offset = 0x20;

            CellRecords = new List<ICellTemplate>();

            while (offset < Size)
            {
                recordSize = BitConverter.ToUInt32(rawBytes, offset);

                readSize = (int) recordSize;

                readSize = Math.Abs(readSize);
                    // if we get a negative number here the record is allocated, but we cant read negative bytes, so get absolute value

                var rawRecord = rawBytes.Skip(offset).Take(readSize).ToArray();

                var cellSignature = Encoding.ASCII.GetString(rawRecord, 4, 2);

                ICellTemplate cellRecord = null;

                try
                {
                    switch (cellSignature)
                    {
                        case "nk":
                            cellRecord = new NKCellRecord(rawRecord);

                            //    Debug.WriteLine(cellRecord);
                            break;
                        case "sk":
                            cellRecord = new SKCellRecord(rawRecord);

                            //   Debug.WriteLine(cellRecord);

                            break;

                        case "vk":
                            cellRecord = new VKCellRecord(rawRecord);

                            System.IO.File.AppendAllText(@"C:\temp\values.txt",cellRecord.ToString());

                            

                            break;

                        default:
                            Debug.WriteLine(string.Format( "Unknown cell signature: {0}", cellSignature));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }


                if (cellRecord != null)
                {
                    CellRecords.Add(cellRecord);
                }


                offset += readSize;
            }
        }

        // public properties...
        public List<ICellTemplate> CellRecords { get; private set; }
        public uint FileOffset { get; private set; }
        public DateTimeOffset? LastWriteTimestamp { get; private set; }
        public uint Reserved { get; private set; }

        /// <summary>
        ///     The signature of the hbin record. Should always be "hbin"
        /// </summary>
        public string Signature { get; private set; }

        public uint Size { get; private set; }
        public uint Spare { get; private set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Size: 0x{0:X}", Size));
            sb.AppendLine(string.Format("Signature: {0}", Signature));

            if (LastWriteTimestamp.HasValue)
            {
                sb.AppendLine(string.Format("LastWriteTimestamp: {0}", LastWriteTimestamp));
            }


            sb.AppendLine();

            sb.AppendLine(string.Format("Cell records count: {0:N0}", CellRecords.Count));
            sb.AppendLine();
            sb.AppendLine(string.Format("File offset: 0x{0:X}", FileOffset));
            sb.AppendLine();

            sb.AppendLine(string.Format("Reserved: 0x{0:X}", Reserved));
            sb.AppendLine(string.Format("Spare: 0x{0:X}", Spare));

            return sb.ToString();
        }
    }
}