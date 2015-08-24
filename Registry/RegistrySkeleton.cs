using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Registry
{
    public class RegistrySkeleton
    {
        private readonly RegistryHive _hive;

        private readonly List<SkeletonKey> _keys;

        public RegistrySkeleton(RegistryHive hive)
        {
            if (hive == null)
            {
                throw new NullReferenceException();
            }
            _hive = hive;
            _keys = new List<SkeletonKey>();
        }

        public ReadOnlyCollection<SkeletonKey> Keys => _keys.AsReadOnly();

        /// <summary>
        ///     Adds a SkeletonKey to the SkeletonHive
        /// </summary>
        /// <remarks>Returns true if key is already in list or it is added</remarks>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool AddEntry(SkeletonKey key)
        {
            var hiveKey = _hive.GetKey(key.KeyPath);

            if (hiveKey == null)
            {
                return false;
            }

            var intKey = _keys.SingleOrDefault(t => t.KeyPath == key.KeyPath);

            if (intKey == null)
            {
                _keys.Add(key);
            }

            return true;
        }

        public bool RemoveEntry(SkeletonKey key)
        {
            var intKey = _keys.SingleOrDefault(t => t.KeyPath == key.KeyPath);

            if (intKey == null)
            {
                return false;
            }

            _keys.Remove(intKey);

            return true;
        }

        public bool Write(string outHive)
        {
            if (_keys.Count == 0)
            {
                throw new InvalidOperationException("At least one SkeletonKey must be added before calling Write");
            }

            var b = new byte[] {0x01,0x02, 0x01, 0x02, 0x01, 0x02, 0x01, 0x02, 0x01, 0x02, 0x01, 0x02, 0x01, 0x02, 0x01, 0x02, 0x01, 0x02};

            if (File.Exists(outHive))
            {
                File.Delete(outHive);
            }

            File.WriteAllBytes(outHive,b);

            return true;
        }
    }

    public class SkeletonKey
    {
        public SkeletonKey(string keyPath, bool addValues)
        {
            KeyPath = keyPath;
            AddValues = addValues;
        }

        public string KeyPath { get; }
        public bool AddValues { get; }
    }
}