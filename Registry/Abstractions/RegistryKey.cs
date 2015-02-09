using System;
using System.Collections.Generic;
using System.Text;
using Registry.Cells;

// namespaces...

namespace Registry.Abstractions
{
    // public classes...
    /// <summary>
    ///     Represents a key that is associated with a Registry hive
    ///     <remarks>Also contains references to low level structures related to a given key</remarks>
    /// </summary>
    public class RegistryKey
    {
        [Flags]
        public enum KeyFlagsEnum
        {
            Deleted = 1,
            HasActiveParent = 2
        }

        private string _keyPath;
        // public constructors...
        public RegistryKey(NKCellRecord nk, RegistryKey parent)
        {
            NKRecord = nk;
            
            Parent = parent;

            InternalGUID = Guid.NewGuid().ToString();

            SubKeys = new List<RegistryKey>();
            Values = new List<KeyValue>();

            ClassName = string.Empty;
        }

        // public properties...
        public string ClassName { get; set; }
        public RegistryKey Parent { get; set; }

        /// <summary>
        ///     A unique value that can be used to find this key in a collection
        /// </summary>
        public string InternalGUID { get; set; }

        public KeyFlagsEnum KeyFlags { get; set; }


        /// <summary>
        ///     The name of this key. For the full path, see KeyPath
        /// </summary>
        public string KeyName
        {
            get { return NKRecord.Name; }
        }

        /// <summary>
        ///     The full path to the  key, including its KeyName
        /// </summary>
        public string KeyPath
        {
            get
            {
                if (_keyPath != null)
                {
                    //sometimes we have to update the path elsewhere, so if that happens, return it
                    return _keyPath;
                }

                if (Parent == null)
                {
                    //This is the root key
                    return string.Format("{0}", KeyName);
                }

              return string.Format(@"{0}\{1}", Parent.KeyPath, KeyName);
            }

            set { _keyPath = value; }
        }

        /// <summary>
        ///     The last write time of this key
        /// </summary>
        public DateTimeOffset? LastWriteTime
        {
            get { return NKRecord.LastWriteTimestamp; }
        }

        /// <summary>
        ///     The underlying NKRecord for this Key. This allows access to all info about the NK Record
        /// </summary>
        public NKCellRecord NKRecord { get; }

        /// <summary>
        ///     A list of child keys that exist under this key
        /// </summary>
        public List<RegistryKey> SubKeys { get; }

        /// <summary>
        ///     A list of values that exists under this key
        /// </summary>
        public List<KeyValue> Values { get; }

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Key Name: {0}", KeyName));
            sb.AppendLine(string.Format("Key Path: {0}", KeyPath));
            sb.AppendLine();

            sb.AppendLine(string.Format("Last Write Time: {0}", LastWriteTime));
            sb.AppendLine();

            // sb.AppendLine(string.Format("Is Deleted: {0}", IsDeleted));

            sb.AppendLine(string.Format("Key flags: {0}", KeyFlags));

            sb.AppendLine();

            sb.AppendLine(string.Format("Internal GUID: {0}", InternalGUID));
            sb.AppendLine();

            sb.AppendLine(string.Format("NK Record: {0}", NKRecord));

            sb.AppendLine();

            sb.AppendLine(string.Format("SubKey count: {0:N0}", SubKeys.Count));

            var i = 0;
            foreach (var sk in SubKeys)
            {
                sb.AppendLine(string.Format("------------ SubKey #{0} ------------", i));
                sb.AppendLine(sk.ToString());
                i += 1;
            }

            sb.AppendLine();

            sb.AppendLine(string.Format("Value count: {0:N0}", Values.Count));

            i = 0;
            foreach (var value in Values)
            {
                sb.AppendLine(string.Format("------------ Value #{0} ------------", i));
                sb.AppendLine(value.ToString());
                i += 1;
            }

            sb.AppendLine();


            return sb.ToString();
        }
    }
}