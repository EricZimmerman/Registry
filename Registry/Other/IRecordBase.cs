using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Registry.Other
{
 public   interface IRecordBase
    {
        /// <summary>
        /// The offset in the registry hive file to a record
        /// </summary>
        long AbsoluteOffset { get; }

        string Signature { get; }
    }
}
