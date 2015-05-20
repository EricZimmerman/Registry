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

       public SearchHit(RegistryKey key, KeyValue value)
       {
           Key = key;
           Value = value;
       }

       public override string ToString()
       {
           if (Value != null)
           {
               return $"{Key.KeyPath}::{Value.ValueName}";
           }
           
            return $"{Key.KeyPath}";
        }
    }
}
