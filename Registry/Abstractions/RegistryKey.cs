using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Registry.Cells;

namespace Registry.Abstractions
{
    /// <summary>
    /// Represents a key that is associated with a Registry hive
    /// <remarks>Also contains references to low level structures related to a given key</remarks>
    /// </summary>
    public class RegistryKey
    {
        public NKCellRecord NKRecord { get; private set; }
        public List<RegistryKey> SubKeys { get; private set; }
        public List<KeyValue> Values { get; private set; }
        public string KeyName { get; private set; }
        public string KeyPath { get; private set; }
        public DateTimeOffset? LastWriteTime { get; private set; }

        public RegistryKey(NKCellRecord nk, string parentPath)
        {
            NKRecord = nk;

            KeyName = nk.Name;
            LastWriteTime = nk.LastWriteTimestamp;

            if (parentPath == null)
            {
                KeyPath = string.Format(@"{0}",KeyName);
            }
            else
            {
                KeyPath = string.Format(@"{0}\{1}", parentPath, KeyName);    
            }

            

            SubKeys = new List<RegistryKey>();
            Values = new List<KeyValue>();

        }
      
    }
}
