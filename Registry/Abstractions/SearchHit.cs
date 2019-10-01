using Registry.Other;

namespace Registry.Abstractions
{
    public class SearchHit
    {
        public enum HitType
        {
            KeyName = 0,
            ValueName = 1,
            ValueData = 2,
            ValueSlack = 3,
            LastWrite = 5,
            Base64 = 6
        }
        public SearchHit(RegistryKey key, KeyValue value, string hitstring, string decodedValue, HitType hitLocation)
        {
            Key = key;
            Value = value;
            HitString = hitstring;
            DecodedValue = decodedValue;
            HitLocation = hitLocation;
        }

        public RegistryKey Key { get; }
        public KeyValue Value { get; }
        public string HitString { get; }
        public string DecodedValue { get; }

        public HitType HitLocation { get; set; }

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