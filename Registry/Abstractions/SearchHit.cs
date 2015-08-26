using Registry.Other;

namespace Registry.Abstractions
{
    public class SearchHit
    {
        public SearchHit(RegistryKey key, KeyValue value, string hitstring)
        {
            Key = key;
            Value = value;
            HitString = hitstring;
        }

        public RegistryKey Key { get; }
        public KeyValue Value { get; }
        public string HitString { get; }

        public bool StripRootKeyName { get; set; }

        public override string ToString()
        {
            var kp = Key.KeyPath;
            if (StripRootKeyName)
            {
                kp = Helpers.StripRootKeyNameFromKeyPath(kp);
            }

            if (Value != null)
            {
                return $"{kp} Hit string: {HitString} Value: {Value.ValueName}";
            }

            return $"{kp} Hit string: {HitString}";
        }
    }
}