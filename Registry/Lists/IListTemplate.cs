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
        bool IsReferenceed { get; }
        /// <summary>
        /// The total number of offsets to other records this list holds.
        /// </summary>
        int NumberOfEntries { get; }

        /// <summary>
        /// The raw contents of this record
        /// </summary>
        byte[] RawBytes { get; }
        /// <summary>
        /// The offset as stored in other records to a given record
        /// <remarks>This value will be 4096 bytes (the size of the regf header) less than the AbsoluteOffset</remarks>
        /// </summary>
        long RelativeOffset { get; }
        string Signature { get; }
        // properties...

        /// <summary>
        /// The size of the hive
        /// <remarks>This value will always be positive. See IsFree to determine whether or not this cell is in use (it has a negative size)</remarks>
        /// </summary>
        int Size { get; }
    }
}
