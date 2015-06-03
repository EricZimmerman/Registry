using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Registry.Other;

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

        public override string ToString()
        {
            var kp = Key.KeyPath;
            if (StripRootKeyName)
            {
                kp = Helpers.StripRootKeyNameFromKeyPath(kp);
            }

           if (Value != null)
           {
               return $"{kp} Value: {Value.ValueName}";
           }
           
            return kp;
        }
    }
}
