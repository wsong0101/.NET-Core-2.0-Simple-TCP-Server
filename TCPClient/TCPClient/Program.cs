using System;
using System.Net;
using System.Text;
using TCPNetworkLib;

namespace TestTCPClient
{
    class Program
    {
        static void Main(string[] args)
        {
            String address = "127.0.0.1";
            int port = 8001;

            TCPClient tcpClient = new TCPClient(2, 2048);
            tcpClient.Init();
            
            IPAddress ipAddress = IPAddress.Parse(address);
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);
            tcpClient.Start(ipEndPoint);

            string inputMsg;
            while(true)
            {
                inputMsg = Console.ReadLine();

                if(inputMsg.Length > 0)
                {
                    if(inputMsg == "exit")
                    {
                        break;
                    }

                    tcpClient.Send(inputMsg);
                }
            }
        }
    }
}
