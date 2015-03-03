using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog.Config;
using Registry.Abstractions;
using Registry.Other;

namespace Registry
{
    public interface IRegistry
    {
        byte[] FileBytes { get; }
        LoggingConfiguration NlogConfig { get; set; }

         HiveTypeEnum HiveType { get;   }

        string HivePath { get; }

        RegistryHeader Header { get;  set; }

        byte[] ReadBytesFromHive(long offset, int length);

    }

    public enum HiveTypeEnum
    {
        [Description("Other")]
        Other = 0,
        [Description("NTUSER")]
        NtUser = 1,
        [Description("SAM")]
        Sam = 2,
        [Description("SECURITY")]
        Security = 3,
        [Description("SOFTWARE")]
        Software = 4,
        [Description("SYSTEM")]
        System = 5,
        [Description("USRCLASS")]
        UsrClass = 6,
        [Description("COMPONENTS")]
        Components = 7
    }
}
