using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Registry.Abstractions
{
  public  class ValueBySizeInfo
    {
        public RegistryKey Key { get; }
        public KeyValue Value { get; }

      public ValueBySizeInfo(RegistryKey key, KeyValue value)
      {
          Key = key;
          Value = value;
      }

        public  string StripRootKeyNameFromKeyPath()
        {
            var pos = Key.KeyPath.IndexOf("\\", StringComparison.Ordinal);
            return Key.KeyPath.Substring(pos + 1);
        }
    }
}
