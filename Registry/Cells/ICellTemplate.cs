using System;
using System.Collections.Generic;
using System.Linq;

// namespaces...
namespace Registry
{
    // public interfaces...
    public interface ICellTemplate
    {
        // properties...
        bool IsFree { get; }
        byte[] RawBytes { get; }
        string Signature { get; }
        int Size { get;  }
        long AbsoluteOffset { get; }
    }
}
