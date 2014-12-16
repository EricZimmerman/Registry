using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Registry.Lists
{
    public interface IListTemplate
    {
        // properties...
        int Size { get; }
        int NumberOfEntries { get; }
        byte[] RawBytes { get; }
        string Signature { get; }
        bool IsFree { get; }
    }
}
