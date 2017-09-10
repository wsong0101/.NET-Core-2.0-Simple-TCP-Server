using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace TCPNetworkLib
{
    public class AsyncUserToken
    { 
        public int Id { get; set; }
        public Socket Socket { get; set; }
    }
}
