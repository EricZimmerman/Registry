using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Registry
{
    public class MessageEventArgs : EventArgs
    {
        public enum MsgTypeEnum
        {
            Info,
            Warning,
            Error
        }

        public string Message { get; set; }

        public string Detail { get; set; }

        public MsgTypeEnum MsgType { get; set; }

        public Exception Exception { get; set; }
    }
}
