using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace TCPNetworkLib
{
    public class TCPClient : TCPNetwork
    {
        Socket _socket;

        public TCPClient(int maxNumConnections, int receiveBufferSize) : base(maxNumConnections, receiveBufferSize)
        {
        }

        public override void Start(IPEndPoint localEndPoint)
        {
            _socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(localEndPoint);

            // Connection to server is successful.
            if(_socket.Connected)
            {
                Console.WriteLine("Conntion to server({0}) is successful.", localEndPoint.ToString());

                // Get an EventArg from the pool, and make it to receive async.
                SocketAsyncEventArgs readEventArgs = _readWritePool.Pop();
                ((AsyncUserToken)readEventArgs.UserToken).Socket = _socket;
                bool willRaiseEvent = _socket.ReceiveAsync(readEventArgs);
                if (!willRaiseEvent)
                {
                    ProcessReceive(readEventArgs);
                }
            }
        }

        public void Send(string message)
        {
            // send message async to server.
            SocketAsyncEventArgs writeEventArgs = _readWritePool.Pop();
            byte[] buffer = writeEventArgs.Buffer;
            Array.Clear(buffer, writeEventArgs.Offset, writeEventArgs.BytesTransferred);

            byte[] byteMessage = Encoding.UTF8.GetBytes(message);
            byteMessage.CopyTo(buffer, writeEventArgs.Offset);

            writeEventArgs.SetBuffer(writeEventArgs.Offset, byteMessage.Length);

            AsyncUserToken token = (AsyncUserToken)writeEventArgs.UserToken;
            token.Socket = _socket;
            bool willRaiseEvent = token.Socket.SendAsync(writeEventArgs);
            if (!willRaiseEvent)
            {
                ProcessSend(writeEventArgs);
            }
        }

        protected override void ProcessReceive(SocketAsyncEventArgs e)
        {
            // check if the remote host closed the connection
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                Console.WriteLine("The server has sent {0} bytes", e.BytesTransferred);
                string receivedMsg = Encoding.UTF8.GetString(e.Buffer, e.Offset, e.BytesTransferred);
                Console.WriteLine("Msg : {0}", receivedMsg);

                bool willRaiseEvent = _socket.ReceiveAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseSocket(e);
            }
        }

        protected override void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // done sending data to server.
                Console.WriteLine("Sent {0} bytes to server.", e.BytesTransferred);
                _readWritePool.Push(e);
            }
            else
            {
                CloseSocket(e);
            }
        }

        private void CloseSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;

            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception) { }
            token.Socket.Close();

            Console.WriteLine("Connection to Server is closed.");
            
            _readWritePool.Push(e);
        }
    }
}
