using NFluent;
using Registry.Cells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// namespaces...
namespace Registry
{
    // public classes...
    public class HBinRecord
    {
        // protected internal constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="HBinRecord"/> class.
        /// </summary>
        protected internal HBinRecord(byte[] rawBytes)
        {
            Signature = Encoding.ASCII.GetString(rawBytes, 0, 4);

            Check.That(Signature).IsEqualTo("hbin");

            FileOffset = BitConverter.ToUInt32(rawBytes, 0x4);

            Size = BitConverter.ToUInt32(rawBytes, 0x8);

            Reserved = BitConverter.ToUInt32(rawBytes, 0xc);

            var ts = BitConverter.ToInt64(rawBytes, 0x14);

            LastWriteTimestamp = DateTimeOffset.FromFileTime(ts);

            Spare = BitConverter.ToUInt32(rawBytes, 0xc);

            //additional cell data starts 32 bytes (0x20) in

            var recordSize =  BitConverter.ToUInt32(rawBytes, 0x20);

            var readSize = (int)recordSize;

            var offset = 0x20;

            CellRecords = new List<ICellTemplate>();

            while (offset < Size)
            {
                recordSize = BitConverter.ToUInt32(rawBytes, offset);

                readSize = (int)recordSize;

                readSize = Math.Abs(readSize); // if we get a negative number here the record is allocated, but we cant read negative bytes, so get absolute value

                var rawRecord = rawBytes.Skip(offset).Take(readSize).ToArray();

                var cellSignature = Encoding.ASCII.GetString(rawRecord, 4, 2);

                ICellTemplate cellRecord = null;

                switch (cellSignature)
                {
                    case "nk":
                        cellRecord = new NKCellRecord(rawRecord);

                        //    Debug.WriteLine(cellRecord);
                        break;
                    case "sk":
                        //http://amnesia.gtisc.gatech.edu/~moyix/suzibandit.ltd.uk/MSc/Registry%20Structure%20-%20Main%20V4.pdf
                        //4.18.2 Permissions Settings 
                        cellRecord = new SKCellRecord(rawRecord);

                        //   Debug.WriteLine(cellRecord);

                        break;

                    case "vk":
                        cellRecord = new VKCellRecord(rawRecord);
                        break;

                    default:
                        //Debug.WriteLine("Unknown cell signature: {0}", cellSignature);
                        break;
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
        public DateTimeOffset LastWriteTimestamp { get; private set; }
        public uint Reserved { get; private set; }
        /// <summary>
        /// The signature of the hbin record. Should always be "hbin"
        /// </summary>
        public string Signature { get; private set; }
        public uint Size { get; private set; }
        public uint Spare { get; private set; }
    }
}
