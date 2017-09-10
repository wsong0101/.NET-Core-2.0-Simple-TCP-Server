using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;

namespace TCPNetworkLib
{
    // Implements the connection logic for the socket server.  
    // After accepting a connection, all data read from the client 
    // is sent back to the client. The read and echo back to the client pattern 
    // is continued until the client disconnects.
    public class TCPServer : TCPNetwork
    {
        Socket _listenSocket;            // the socket used to listen for incoming connection requests
        ConcurrentDictionary<int, SocketAsyncEventArgs> _connectedClients;

        int _totalBytesRead = 0;            // counter of the total # bytes received by the server
        int _numConnectedSockets = 0;       // the total number of clients connected to the server 
        int _clinetId = 0;
        Semaphore _maxNumberAcceptedClients;

        // Create an uninitialized server instance.  
        // To start the server listening for connection requests
        // call the Init method followed by Start method 
        //
        // <param name="maxNumConnections">the maximum number of connections the sample is designed to handle simultaneously</param>
        // <param name="receiveBufferSize">buffer size to use for each socket I/O operation</param>
        public TCPServer(int maxNumConnections, int receiveBufferSize) : base(maxNumConnections, receiveBufferSize)
        {
            _maxNumberAcceptedClients = new Semaphore(maxNumConnections, maxNumConnections);
            _connectedClients = new ConcurrentDictionary<int, SocketAsyncEventArgs>();
        }

        // Starts the server such that it is listening for 
        // incoming connection requests.    
        //
        // <param name="localEndPoint">The endpoint which the server will listening 
        // for connection requests on</param>
        public override void Start(IPEndPoint localEndPoint)
        {
            // create the socket which listens for incoming connections
            _listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(localEndPoint);
            // start the server with a listen backlog of 100 connections
            _listenSocket.Listen(100);

            // post accepts on the listening socket
            StartAccept(null);

            //Console.WriteLine("{0} connected sockets with one outstanding receive posted to each....press any key", m_outstandingReadCount);
            Console.WriteLine("Press any key to terminate the server process....");
            Console.ReadKey();
        }
        
        // Begins an operation to accept a connection request from the client 
        //
        // <param name="acceptEventArg">The context object to use when issuing 
        // the accept operation on the server's listening socket</param>
        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }

            _maxNumberAcceptedClients.WaitOne();
            bool willRaiseEvent = _listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        // This method is the callback method associated with Socket.AcceptAsync 
        // operations and is invoked when an accept operation is complete
        //
        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            Interlocked.Increment(ref _numConnectedSockets);
            Console.WriteLine("Client connection accepted. There are {0} clients connected to the server",
                _numConnectedSockets);

            // Get the socket for the accepted client connection and put it into the 
            //ReadEventArg object user token
            SocketAsyncEventArgs readEventArgs = _readWritePool.Pop();
            AsyncUserToken token = readEventArgs.UserToken as AsyncUserToken;
            token.Socket = e.AcceptSocket;

            // add connected client to the list
            Interlocked.Increment(ref _clinetId); // increase the client id each time to make an unique id
            token.Id = _clinetId;
            _connectedClients.TryAdd(_clinetId, readEventArgs);

            // As soon as the client is connected, post a receive to the connection
            bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(readEventArgs);
            if (!willRaiseEvent)
            {
                ProcessReceive(readEventArgs);
            }

            // Accept the next connection request
            StartAccept(e);
        }

        // This method is called whenever a receive or send operation is completed on a socket 
        //
        // <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }

        }

        protected override void ProcessReceive(SocketAsyncEventArgs e)
        {
            bool willRaiseEvent;
            // check if the remote host closed the connection
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                //increment the count of the total bytes receive by the server
                Interlocked.Add(ref _totalBytesRead, e.BytesTransferred);
                Console.WriteLine("The server has read a total of {0} bytes", _totalBytesRead);
                
                foreach (KeyValuePair<int, SocketAsyncEventArgs> val in _connectedClients)
                {
                    // copy received message to every other client's send buffer
                    SocketAsyncEventArgs args = val.Value;
                    SocketAsyncEventArgs sendArgs = _readWritePool.Pop();
                    AsyncUserToken argsToken = args.UserToken as AsyncUserToken;
                    AsyncUserToken sendArgsToken = sendArgs.UserToken as AsyncUserToken;

                    sendArgsToken.Id = argsToken.Id;
                    sendArgsToken.Socket = argsToken.Socket;
                    Array.Copy(e.Buffer, e.Offset, sendArgs.Buffer, sendArgs.Offset, e.Count);
                    sendArgs.SetBuffer(sendArgs.Offset, e.BytesTransferred);

                    // send message
                    AsyncUserToken token = sendArgs.UserToken as AsyncUserToken;
                    willRaiseEvent = token.Socket.SendAsync(sendArgs);
                    if (!willRaiseEvent)
                    {
                        ProcessSend(sendArgs);
                    }
                }
                
                // set receive for next message
                willRaiseEvent = (e.UserToken as AsyncUserToken).Socket.ReceiveAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        protected override void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // reset the buffer, so that the socket can receive up to the max buffer size.
                e.SetBuffer(e.Offset, _receiveBufferSize);

                _readWritePool.Push(e);
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;

            // close the socket associated with the client
            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            // throws if client process has already closed
            catch (Exception) { }
            token.Socket.Close();

            // decrement the counter keeping track of the total number of clients connected to the server
            Interlocked.Decrement(ref _numConnectedSockets);
            _maxNumberAcceptedClients.Release();
            Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", _numConnectedSockets);

            // remove the client from the connected list
            _connectedClients.TryRemove(token.Id, out e);

            // Free the SocketAsyncEventArg so they can be reused by another client
            _readWritePool.Push(e);
        }

    }
}