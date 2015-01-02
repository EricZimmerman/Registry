using Registry.Cells;
using System;
using System.Collections.Generic;
using System.Linq;

// namespaces...
namespace Registry.Abstractions
{
    // public classes...
    /// <summary>
    /// Represents a value that is associated with a RegistryKey
    /// <remarks>Also contains references to low level structures related to a given value</remarks>
    /// </summary>
    public class KeyValue
    {
        // public constructors...
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValue"/> class.
        /// </summary>
        /// <param name="valueName"></param>
        /// <param name="valueType"></param>
        /// <param name="valueData"></param>
        /// <param name="valueSlack"></param>
        /// <param name="valueSlackRaw"></param>
        public KeyValue(string valueName, string valueType, string valueData, string valueSlack, byte[] valueSlackRaw, VKCellRecord vk)
        {
            VKRecord = vk;
            ValueName = valueName;
            ValueType = valueType;
            ValueData = valueData;
            ValueSlack = valueSlack;
            ValueSlackRaw = valueSlackRaw;

            InternalGUID = Guid.NewGuid().ToString();
        }

        // public properties...
        /// <summary>
        /// A unique value that can be used to find this key in a collection
        /// </summary>
        public string InternalGUID { get; set; }
        
        /// <summary>
        /// The normalized representation of the value's value.
        /// </summary>
        public string ValueData { get; private set; }
        /// <summary>
        /// The value as stored in the hive as a series of bytes
        /// </summary>
        public byte[] ValueDataRaw { get; private set; }
        public string ValueName { get; private set; }
        /// <summary>
        /// If present, the value slack as a string of bytes delimited by hyphens
        /// </summary>
        public string ValueSlack { get; private set; }
        /// <summary>
        /// The value slack as stored in the hive as a series of bytes
        /// </summary>
        public byte[] ValueSlackRaw { get; private set; }
        /// <summary>
        /// The values type (VKCellRecord.DataTypeEnum)
        /// </summary>
        public string ValueType { get; private set; }
        /// <summary>
        /// The underlying VKRecord for this Key. This allows access to all info about the VK Record 
        /// </summary>
        public VKCellRecord VKRecord { get; private set; }
    }
}
