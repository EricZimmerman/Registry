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
        public string InternalGUID { get; }

        /// <summary>
        ///     The normalized representation of the value's value.
        /// </summary>
        public string ValueData
        {
            get
            {
                if (VKRecord.ValueData is byte[])
                {
                    return BitConverter.ToString((byte[]) VKRecord.ValueData);
                }
                return VKRecord.ValueData.ToString();
            }
        }

        /// <summary>
        ///     The value as stored in the hive as a series of bytes
        /// </summary>
        public byte[] ValueDataRaw => VKRecord.ValueDataRaw;

        public string ValueName => VKRecord.ValueName;

        /// <summary>
        ///     If present, the value slack as a string of bytes delimited by hyphens
        /// </summary>
        public string ValueSlack => BitConverter.ToString(VKRecord.ValueDataSlack);

        /// <summary>
        ///     The value slack as stored in the hive as a series of bytes
        /// </summary>
        public byte[] ValueSlackRaw => VKRecord.ValueDataSlack;

        /// <summary>
        ///     The values type (VKCellRecord.DataTypeEnum)
        /// </summary>
        public string ValueType => VKRecord.DataType.ToString();

        /// <summary>
        ///     The underlying VKRecord for this Key. This allows access to all info about the VK Record
        /// </summary>
        public VKCellRecord VKRecord { get; }

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

//            sb.AppendLine($"Value Name: {ValueName}");
//            sb.AppendLine($"Value Type: {ValueType}");
//            sb.AppendLine($"Value Data: {ValueData}");
//            sb.AppendLine($"Value Slack: {ValueSlack}");
//
//            sb.AppendLine();

//            sb.AppendLine(string.Format("Internal GUID: {0}", InternalGUID));
//            sb.AppendLine();

            sb.AppendLine($"VK Record: {VKRecord}");

            return sb.ToString();
        }
    }
}