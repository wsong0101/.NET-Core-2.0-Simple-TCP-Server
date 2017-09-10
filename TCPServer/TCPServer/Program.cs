using System;
using System.Net;
using System.Net.Sockets;

namespace TCPServer
{
    class Program
    {
        static void Main(String[] _args)
        {
            String address = "localhost";
            int port = 8001;

            TCPNetworkLib.TCPServer server = new TCPNetworkLib.TCPServer(5, 2048);
            server.Init();

            // Get IPv4 address
            IPAddress ipAddress = GetIPAddress(address, AddressFamily.InterNetwork);
            IPEndPoint endPoint = new IPEndPoint(ipAddress, port);

            server.Start(endPoint);
        }
        
        private static IPAddress GetIPAddress(string _server, AddressFamily _family)
        {
            try
            {
                //System.Text.ASCIIEncoding ASCII = new System.Text.ASCIIEncoding();

                // Get server related information.
                IPHostEntry heserver = Dns.GetHostEntry(_server);

                // Loop on the AddressList
                foreach (IPAddress curAdd in heserver.AddressList)
                {
                    // Found the IPAddress that were looking for.
                    if (curAdd.AddressFamily == _family)
                    {
                        // Display the type of address family supported by the server. If the
                        // server is IPv6-enabled this value is: InternNetworkV6. If the server
                        // is also IPv4-enabled there will be an additional value of InterNetwork.
                        Console.WriteLine("AddressFamily: " + curAdd.AddressFamily.ToString());

                        // Display the ScopeId property in case of IPV6 addresses.
                        if (curAdd.AddressFamily.ToString() == ProtocolFamily.InterNetworkV6.ToString())
                            Console.WriteLine("Scope Id: " + curAdd.ScopeId.ToString());


                        // Display the server IP address in the standard format. In 
                        // IPv4 the format will be dotted-quad notation, in IPv6 it will be
                        // in in colon-hexadecimal notation.
                        Console.WriteLine("Address: " + curAdd.ToString());

                        // Display the server IP address in byte format.
                        Console.Write("AddressBytes: ");



                        Byte[] bytes = curAdd.GetAddressBytes();
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            Console.Write(bytes[i]);
                        }

                        Console.WriteLine("\r\n");

                        return curAdd;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[DoResolve] Exception: " + e.ToString());
            }

            return new IPAddress(0);
        }
    }
}