using System;
using System.Collections.Generic;
using System.Linq;

// namespaces...
namespace Registry.Lists
{
    // public interfaces...
    public interface IListTemplate
    {
        // properties...
        bool IsFree { get; }
        int NumberOfEntries { get; }
        byte[] RawBytes { get; }
        string Signature { get; }
        // properties...
        int Size { get; }
        long AbsoluteOffset { get; }
    }
}
