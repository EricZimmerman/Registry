using System;
using System.Collections.Generic;
using System.Linq;

// namespaces...
namespace Registry
{
    // internal classes...
    //TODO DO I NEED THIS?
    internal class DataNode
    {
        // private fields...
        private readonly int _size;

        // public constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="DataNode"/> class.
        /// </summary>
        public DataNode(byte[] rawBytes)
        {
            RawBytes = rawBytes;

            _size = BitConverter.ToInt32(rawBytes, 0);

            IsFree = _size > 0;


            Data = BitConverter.ToUInt16(rawBytes, 0x2);
        }

        // public properties...
        public ushort Data { get; private set; }
        public bool IsFree { get; private set; }
        public byte[] RawBytes { get; private set; }
        public int Size
        {
            get
            {
                return _size;
            }
        }
    }
}
