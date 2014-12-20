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
        /// <summary>
        /// The offset in the registry hive file to a record
        /// </summary>
        long AbsoluteOffset { get; }
        // properties...
        bool IsFree { get; }
        /// <summary>
        /// Set to true when a record is referenced by another referenced record.
        /// <remarks>This flag allows for determining records that are marked 'in use' by their size but never actually referenced by another record in a hive</remarks>
        /// </summary>
        bool IsReferenceed { get;  }
        byte[] RawBytes { get; }
        /// <summary>
        /// The offset as stored in other records to a given record
        /// <remarks>This value will be 4096 bytes (the size of the regf header) less than the AbsoluteOffset</remarks>
        /// </summary>
        long RelativeOffset { get; }
        string Signature { get; }
        int Size { get;  }
    }
}
