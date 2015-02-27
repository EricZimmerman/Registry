using System;
using System.Text;
using Registry.Cells;

// namespaces...

namespace Registry.Abstractions
{
    // public classes...
    /// <summary>
    ///     Represents a value that is associated with a RegistryKey
    ///     <remarks>Also contains references to low level structures related to a given value</remarks>
    /// </summary>
    public class KeyValue
    {
        // public constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="KeyValue" /> class.
        /// </summary>
        public KeyValue(VKCellRecord vk)
        {
            VKRecord = vk;
            InternalGUID = Guid.NewGuid().ToString();
        }

        // public properties...
        /// <summary>
        ///     A unique value that can be used to find this key in a collection
        /// </summary>
        public string InternalGUID { get; set; }

        /// <summary>
        ///     The normalized representation of the value's value.
        /// </summary>
        public string ValueData
        {
            get { return VKRecord.ValueData.ToString(); }
        }

        /// <summary>
        ///     The value as stored in the hive as a series of bytes
        /// </summary>
        public byte[] ValueDataRaw
        {
            get { return VKRecord.ValueDataRaw; }
        }

        public string ValueName
        {
            get { return VKRecord.ValueName; }
        }

        /// <summary>
        ///     If present, the value slack as a string of bytes delimited by hyphens
        /// </summary>
        public string ValueSlack
        {
            get { return BitConverter.ToString(VKRecord.ValueDataSlack); }
        }

        /// <summary>
        ///     The value slack as stored in the hive as a series of bytes
        /// </summary>
        public byte[] ValueSlackRaw
        {
            get { return VKRecord.ValueDataSlack; }
        }

        /// <summary>
        ///     The values type (VKCellRecord.DataTypeEnum)
        /// </summary>
        public string ValueType
        {
            get { return VKRecord.DataType.ToString(); }
        }

        /// <summary>
        ///     The underlying VKRecord for this Key. This allows access to all info about the VK Record
        /// </summary>
        public VKCellRecord VKRecord { get; private set; }

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Value Name: {0}", ValueName));
            sb.AppendLine(string.Format("Value Type: {0}", ValueType));
            sb.AppendLine(string.Format("Value Data: {0}", ValueData));
            sb.AppendLine(string.Format("Value Slack: {0}", ValueSlack));

            sb.AppendLine();

            sb.AppendLine(string.Format("Internal GUID: {0}", InternalGUID));
            sb.AppendLine();

            sb.AppendLine(string.Format("VK Record: {0}", VKRecord));

            return sb.ToString();
        }
    }
}