using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Registry.Abstractions
{
   public class SearchHit
    {
        public RegistryKey Key { get; }
        public KeyValue Value { get; }

        public bool StripRootKeyName { get; set; }

       public SearchHit(RegistryKey key, KeyValue value)
       {
           Key = key;
           Value = value;
       }

        public static string StripRootKeyNameFromKeyPath(string keyPath)
        {
            var pos = keyPath.IndexOf("\\", StringComparison.Ordinal);
            return keyPath.Substring(pos + 1);
        }

        public override string ToString()
        {
            var kp = Key.KeyPath;
            if (StripRootKeyName)
            {
                kp = StripRootKeyNameFromKeyPath(kp);
            }

           if (Value != null)
           {
               return $"{kp}::{Value.ValueName}";
           }
           
            return kp;
        }
    }
}
