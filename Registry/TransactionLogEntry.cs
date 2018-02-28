using System;
using System.Collections.Generic;
using System.Text;

namespace Registry
{
    public class TransactionLogEntry
    {
        public List<DirtyPageInfo> DirtyPages{ get; }

        public TransactionLogEntry(byte[] rawBytes)
        {
            var sig = Encoding.GetEncoding(1252).GetString(rawBytes, 0, 4);

            if (sig != "HvLE")
            {
                throw new Exception("Data is not a transaction log entry (bad signature)");
            }

            var index = 4;

            Size = BitConverter.ToUInt32(rawBytes, index);
            index += 4;

            var flags = BitConverter.ToInt32(rawBytes, index);
            index += 4;

            SequenceNumber = BitConverter.ToInt32(rawBytes, index);
            index += 4;

            var hiveBinDataSize = BitConverter.ToInt32(rawBytes, index);
            index += 4;

            DirtyPageCount = BitConverter.ToInt32(rawBytes, index);
            index += 4;

            Hash1 = BitConverter.ToInt64(rawBytes, index);
            index += 8;

            Hash2 = BitConverter.ToInt64(rawBytes, index);
            index += 8;

            var dpCount = 0;

            var dpBuff = new byte[8];

            DirtyPages = new List<DirtyPageInfo>();

            while (dpCount < DirtyPageCount)
            {
                Buffer.BlockCopy(rawBytes, index, dpBuff, 0, 8);
                index += 8;

                var off = BitConverter.ToInt32(dpBuff, 0);
                var pageSize = BitConverter.ToInt32(dpBuff, 4);

                var dp = new DirtyPageInfo(off, pageSize);

                DirtyPages.Add(dp);

                dpCount += 1;
            }

            //should be sitting at hbin

            var hbinsig = Encoding.GetEncoding(1252).GetString(rawBytes, index, 4);

            if (hbinsig != "hbin")
            {
                throw new Exception($"hbin header not found at offset 0x{index}");
            }

            //from here are hbins in order

            foreach (var dirtyPageInfo in DirtyPages)
            {
                //dirtyPageInfo.Size contains how many bytes we need to overwrite in the main hive's bytes

                //from index, read size bytes, update dirtyPage

                var pageBuff = new byte[dirtyPageInfo.Size];

                Buffer.BlockCopy(rawBytes, index, pageBuff, 0, dirtyPageInfo.Size);

                dirtyPageInfo.UpdatePageBytes(pageBuff);

                index += dirtyPageInfo.Size;
            }
        }

        public int DirtyPageCount { get; }
        public long Hash1 { get; }
        public long Hash2 { get; }
        public int SequenceNumber { get; }
        public uint Size { get; }

        public override string ToString()
        {
            return
                $"Size: 0x{Size:X4}, Sequence Number: 0x{SequenceNumber:X4}, Dirty Page Count: 0x{Size:DirtyPageCount}, Hash1: 0x{Hash1:X}, Hash1: 0x{Hash1:X}";
        }
    }

    public class DirtyPageInfo
    {
        public DirtyPageInfo(int offset, int size)
        {
            Offset = offset;
            Size = size;
        }

        public int Offset { get; }
        public int Size { get; }
        public byte[] PageBytes { get; private set; }

        public void UpdatePageBytes(byte[] bytes)
        {
            PageBytes = bytes;
        }

        public override string ToString()
        {
            return $"Offset: 0x{Offset:X4}, Size: 0x{Size:X4}";
        }
    }
}