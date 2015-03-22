using System;
using System.Collections.Generic;
using System.Linq;
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
        public NKCellRecord NKRecord { get; private set; }

        /// <summary>
        ///     A list of child keys that exist under this key
        /// </summary>
        public List<RegistryKey> SubKeys { get; private set; }

        /// <summary>
        ///     A list of values that exists under this key
        /// </summary>
        public List<KeyValue> Values { get; private set; }

        public string GetRegFormat(HiveTypeEnum hiveType)
        {
            var sb = new StringBuilder();

            string keyBase;

            switch (hiveType)
            {
                case HiveTypeEnum.NtUser:
                    keyBase = "HKEY_CURRENT_USER";
                    break;
                case HiveTypeEnum.Sam:
                    keyBase = "HKEY_CURRENT_USER\\SAM";
                    break;
                case HiveTypeEnum.Security:
                    keyBase = "HKEY_CURRENT_USER\\SECURITY";
                    break;
                case HiveTypeEnum.Software:
                    keyBase = "HKEY_CURRENT_USER\\SOFTWARE";
                    break;
                case HiveTypeEnum.System:
                    keyBase = "HKEY_CURRENT_USER\\SYSTEM";
                    break;
                case HiveTypeEnum.UsrClass:
                    keyBase = "HKEY_CLASSES_ROOT";
                    break;
                case HiveTypeEnum.Components:
                    keyBase = "HKEY_CURRENT_USER\\COMPONENTS";
                    break;

                default:
                    keyBase = "HKEY_CURRENT_USER\\UNKNOWN_BASEPATH";
                    break;
            }

            var keyNames = KeyPath.Split('\\');
            var normalizedKeyPath = string.Join("\\", keyNames.Skip(1));

            var keyName = normalizedKeyPath.Length > 0
                ? string.Format("[{0}\\{1}]", keyBase, normalizedKeyPath)
                : string.Format("[{0}]", keyBase);

            sb.AppendLine();
            sb.AppendLine(keyName);
            sb.AppendLine(string.Format(";Last write timestamp {0}", LastWriteTime.Value.UtcDateTime.ToString("o")));
            //sb.AppendLine($";Last write timestamp {LastWriteTime.Value.UtcDateTime.ToString("o")}");

            foreach (var keyValue in Values)
            {
                var keyNameOut = keyValue.ValueName;
                if (keyNameOut.ToLowerInvariant() == "(default)")
                {
                    keyNameOut = "@";
                }
                else
                {
                    keyNameOut = keyNameOut.Replace("\\", "\\\\");
                    keyNameOut = string.Format("\"{0}\"", keyNameOut.Replace("\"", "\\\""));
                }

                var keyValueOut = "";

                switch (keyValue.VKRecord.DataType)
                {
                    case VKCellRecord.DataTypeEnum.RegSz:
                        keyValueOut = string.Format("\"{0}\"",
                            keyValue.ValueData.Replace("\\", "\\\\").Replace("\"", "\\\""));
                        break;

                    case VKCellRecord.DataTypeEnum.RegNone:
                    case VKCellRecord.DataTypeEnum.RegDwordBigEndian:
                    case VKCellRecord.DataTypeEnum.RegFullResourceDescription:
                    case VKCellRecord.DataTypeEnum.RegMultiSz:
                    case VKCellRecord.DataTypeEnum.RegQword:
                    case VKCellRecord.DataTypeEnum.RegFileTime:
                    case VKCellRecord.DataTypeEnum.RegLink:
                    case VKCellRecord.DataTypeEnum.RegResourceRequirementsList:
                    case VKCellRecord.DataTypeEnum.RegExpandSz:

                        var prefix = string.Format("hex({0:x}):", (int) keyValue.VKRecord.DataType);

                        keyValueOut =
                            string.Format("{0}{1}", prefix,
                                BitConverter.ToString(keyValue.ValueDataRaw).Replace("-", ",")).ToLowerInvariant();

                        if (keyValueOut.Length + prefix.Length + keyNameOut.Length > 76)
                        {
                            keyValueOut = string.Format("{0}{1}", prefix,
                                FormatBinaryValueData(keyValue.ValueDataRaw, keyNameOut.Length, prefix.Length));
                        }

                        break;

                    case VKCellRecord.DataTypeEnum.RegDword:
                        keyValueOut =
                            string.Format("dword:{0:X8}", BitConverter.ToInt32(keyValue.ValueDataRaw, 0))
                                .ToLowerInvariant();
                        break;

                    case VKCellRecord.DataTypeEnum.RegBinary:

                        keyValueOut =
                            string.Format("hex:{0}", BitConverter.ToString(keyValue.ValueDataRaw).Replace("-", ","))
                                .ToLowerInvariant();

                        if (keyValueOut.Length + 5 + keyNameOut.Length > 76)
                        {
                            keyValueOut = string.Format("hex:{0}",
                                FormatBinaryValueData(keyValue.ValueDataRaw, keyNameOut.Length, 5));
                        }

                        break;
                }

                sb.AppendLine(string.Format("{0}={1}", keyNameOut, keyValueOut));
            }

            return sb.ToString().TrimEnd();
        }

        private string FormatBinaryValueData(byte[] data, int prefixLength, int nameLength)
        {
            //each line is 80 chars long max
            var tempkeyVal = new StringBuilder();

            int charsWritten;
            charsWritten = nameLength + prefixLength; //account for the name and whatever the hex prefix looks like

            var lineLength = charsWritten;

            var dataIndex = 0;

            while (dataIndex < data.Length)
            {
                tempkeyVal.Append(string.Format("{0:x2},", data[dataIndex]));
                dataIndex += 1;
                charsWritten += 3; //2 hex chars plus a comma
                lineLength += 3;

                if (lineLength >= 76)
                {
                    tempkeyVal.AppendLine("\\");
                    tempkeyVal.Append("  ");
                    charsWritten += 2;
                    lineLength = 2;
                }
            }

            var ret = tempkeyVal.ToString();
            ret = ret.Trim();

            ret = ret.TrimEnd('\\');
            ret = ret.TrimEnd(',');
            ret = ret.TrimEnd('\\');

            return ret.ToLowerInvariant();
        }

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