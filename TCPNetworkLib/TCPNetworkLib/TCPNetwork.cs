using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace TCPNetworkLib
{
    public class TCPNetwork
    {
        protected int _maxNumConnections;   // the maximum number of connections the sample is designed to handle simultaneously 
        protected int _receiveBufferSize;   // buffer size to use for each socket I/O operation 
        protected BufferManager _bufferManager;       // represents a large reusable set of buffers for all socket operations
        const int _opsToPreAlloc = 2;       // read, write (don't alloc buffer space for accepts)

        // pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations
        protected SocketAsyncEventArgsPool _readWritePool;

        public TCPNetwork(int maxNumConnections, int receiveBufferSize)
        {
            _maxNumConnections = maxNumConnections;
            _receiveBufferSize = receiveBufferSize;

            // allocate buffers such that the maximum number of sockets can have one outstanding read and 
            //write posted to the socket simultaneously  
            _bufferManager = new BufferManager(receiveBufferSize * maxNumConnections * _opsToPreAlloc,
                receiveBufferSize);

            _readWritePool = new SocketAsyncEventArgsPool(maxNumConnections);
        }

        public virtual void Init()
        {
            // Allocates one large byte buffer which all I/O operations use a piece of.  This gaurds 
            // against memory fragmentation
            _bufferManager.InitBuffer();

            // preallocate pool of SocketAsyncEventArgs objects
            SocketAsyncEventArgs readWriteEventArg;

            for (int i = 0; i < _maxNumConnections; i++)
            {
                //Pre-allocate a set of reusable SocketAsyncEventArgs
                readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                readWriteEventArg.UserToken = new AsyncUserToken();

                // assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
                _bufferManager.SetBuffer(readWriteEventArg);

                // add SocketAsyncEventArg to the pool
                _readWritePool.Push(readWriteEventArg);
            }
        }

        public virtual void Start(IPEndPoint localEndPoint)
        {
            Debug.Assert(true, "Start function must be implemented.");
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

        // This method is invoked when an asynchronous receive operation completes. 
        // If the remote host closed the connection, then the socket is closed.  
        // If data was received then the data is echoed back to the client.
        //
        protected virtual void ProcessReceive(SocketAsyncEventArgs e)
        {
            Debug.Assert(true, "ProcessReceive function must be implemented.");
        }

        // This method is invoked when an asynchronous send operation completes.  
        // The method issues another receive on the socket to read any additional 
        // data sent from the client
        //
        // <param name="e"></param>
        protected virtual void ProcessSend(SocketAsyncEventArgs e)
        {
            Debug.Assert(true, "ProcessSend function must be implemented.");
        }
    }
}
