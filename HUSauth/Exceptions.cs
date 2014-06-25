using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HUSauth
{

    public class UnknownException : Exception
    {
        public UnknownException() { }

        public UnknownException(string message) : base(message) { }

        public UnknownException(string message, Exception inner) : base(message) { }
    }

    public class ReceivedCorruptDataException : Exception
    {
        public ReceivedCorruptDataException() { }

        public ReceivedCorruptDataException(string message) : base(message) { }

        public ReceivedCorruptDataException(string message, Exception inner) : base(message) { }
    }

    public class ServerBusyException : Exception
    {
        public ServerBusyException() { }

        public ServerBusyException(string message) : base(message) { }

        public ServerBusyException(string message, Exception inner) : base(message) { }
    }
}
