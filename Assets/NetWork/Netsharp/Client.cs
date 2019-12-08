using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Debug = System.Diagnostics.Debug;
using Crazy.Common;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif


namespace Crazy.ClientNet
{
    /// <summary>
    /// Client states definition.
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>
        /// Idle状态
        /// </summary>
        None = 0,
        /// <summary>
        /// 连接中
        /// </summary>
        Connecting = 1,
        /// <summary>
        /// 已连接
        /// </summary>
        Established = 2,
        /// <summary>
        /// 断开中
        /// </summary>
        Disconnecting = 3,
        /// <summary>
        /// 已关闭
        /// </summary>
        Closed = 4,
    }

    /// <summary>
    /// Client const value definition.
    /// </summary>
    public enum ConnectionConst
    {
        CACHE_MAX_MESSAGE = 1000,
        CACHE_MAX_BYTE = ProtoConst.MAX_PACKAGE_LENGTH * 2,
    }

    public class RecvStateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const Int32 BufferSize = ProtoConst.MAX_PACKAGE_LENGTH;
        // receive buffer.
        public Byte[] buffer = new Byte[BufferSize];
    }

    public class SendStateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // The data size that should be sent;
        public Int32 messageSize = 0;
    }

    public class Client
    {
#if UNITY_5_3_OR_NEWER && UNITY_IOS
        [DllImport("__Internal")]
        private static extern string getIPv6(string mHost, string mPort);
#endif
        /// <summary>
        /// 获取服务器ip对应的ipv6的地址表示字符串，默认为"192.168.1.1&&ipv4"
        /// </summary>
        /// <param name="mHost"></param>
        /// <param name="mPort"></param>
        /// <returns></returns>
        public static string GetIPv6String(string mHost, string mPort)
        {
#if UNITY_5_3_OR_NEWER && UNITY_IOS
                string mIPv6 = getIPv6(mHost, mPort);
                return mIPv6;
#endif
            return mHost + "&&ipv4";
        }

        /// <summary>
        /// 将服务器ip转换为ipv6网络中的新地址
        /// </summary>
        /// <param name="serverIp"></param>
        /// <param name="serverPorts"></param>
        /// <param name="newServerIp"></param>
        /// <param name="mIPType"></param>
        void getIPType(String serverIp, String serverPorts, out String newServerIp, out AddressFamily mIPType)
        {
            mIPType = AddressFamily.InterNetwork;
            newServerIp = serverIp;
            try
            {
                string mIPv6 = GetIPv6String(serverIp, serverPorts);
                if (!string.IsNullOrEmpty(mIPv6))
                {
                    string[] m_StrTemp = System.Text.RegularExpressions.Regex.Split(mIPv6, "&&");
                    if (m_StrTemp != null && m_StrTemp.Length >= 2)
                    {
                        string IPType = m_StrTemp[1];
                        if (IPType == "ipv6")
                        {
                            newServerIp = m_StrTemp[0];
                            mIPType = AddressFamily.InterNetworkV6;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public Client(Func<Stream, Type, int, object> deserializeMessageAction = null)
        {
            m_lockState = new object();
            State = ConnectionState.None;
         
            m_messageDeserializeAction = deserializeMessageAction;
            ConnSocket = null;
        }

        /// <summary>
        /// Initialize net component and connect to remote server.
        /// </summary>
        /// <param name="remoteAddress">The address of remote server to connect.</param>
        /// <param name="remotePort">The port of remote server to connect.</param>
        public Boolean Initialize(String remoteAddress, Int32 remotePort)
        {
            // 客户端处理IPV6的时候逻辑，需要用到
#if UNITY_5_3_OR_NEWER
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                String newServerIp = "";
                AddressFamily newAddressFamily = AddressFamily.InterNetwork;
                getIPType(remoteAddress, remotePort.ToString(), out newServerIp, out newAddressFamily);
                if (!string.IsNullOrEmpty(newServerIp)) { remoteAddress = newServerIp; }
            }
#endif

            // analyze the ip address and port info
            IPAddress myIp;
            if (IPAddress.TryParse(remoteAddress, out myIp))
            {
                //strMsg = String.Format("BuildPeer TryParse {0} success", remoteAddress);
                //Debug.WriteLine(strMsg);

                m_ipEndPoint= new IPEndPoint(myIp, remotePort);
            }
            else
            {
                //strMsg = String.Format("BuildPeer GetHostEntry {0} fail", remoteAddress);
                //Debug.WriteLine(strMsg);
                return false;
            }

            // Do some cleanup if necessery
            if (State != ConnectionState.None)
                Close();

            // Clean all messages of last connection
            RecvQueue.Clear();

            // Clean all event args of last connection
            m_connEventArg = new SocketAsyncEventArgs();
            m_receiveEventArg = new SocketAsyncEventArgs();

            // init event args
            m_connEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnCompletedForConnect);
            m_connEventArg.RemoteEndPoint = m_ipEndPoint;
            m_receiveEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnCompletedForReceive);
            m_receiveEventArg.SetBuffer(new byte[ProtoConst.MAX_PACKAGE_LENGTH], 0, ProtoConst.MAX_PACKAGE_LENGTH);

            // Change state
            State = ConnectionState.Connecting;

            // Create work resource
            RecvCache = new MessageBlock((int)ConnectionConst.CACHE_MAX_BYTE);
            try
            {
                StartConnectionWork();
            }catch(Exception e)
            {
                Log.Error(e);
                return true;
            }
            return true;
            
        }

        ///// <summary>
        ///// shutdown the socket
        ///// </summary>
        //public void Shutdown()
        //{
        //    try
        //    {
        //        lock (_lockSocket)
        //        {
        //            ////Console.WriteLine("Shutdown::lock (_lockPeer)");
        //            ConnSocket.Shutdown(SocketShutdown.Both);
        //        }

        //    }
        //    catch (Exception)
        //    {
        //        //String msg = String.Format("{0}:{1}", ex.GetType().ToString(), ex.Message);
        //        //Debug.WriteLine(msg);
        //        //Console.WriteLine(msg);
        //        //throw ex;
        //    }
        //    //Console.WriteLine("Client SHUTDOWN!");
        //}

        /// <summary>
        /// 断开soket
        /// </summary>
        private void SocketDisconnect()
        {
            try
            {
                if (ConnSocket != null)
                {
                    lock (_lockSocket)
                    {
                        // 启动断开连接的timer
                        if (m_disconnectTimer == null)
                        {
                            m_disconnectTimer = new Timer((c) =>
                            {
                                if (ConnSocket.Connected) return;
                                if (State != ConnectionState.Disconnecting) return;

                                // 记录日志
                                Debug.WriteLine(string.Format("SocketDisconnect Disconnected timer start..."));
                                FireEventOnLogPrint("Disconnect.Timer" + "state=" + State);

                                // 设置状态
                                State = ConnectionState.Closed;

                                // 发送连接断开的消息
                                var vMsg = new CCMSGConnectionBreak();
                                RecvQueue.Enqueue(new KeyValuePair<int, object>(vMsg.MessageId, vMsg));

                                // 清理timer
                                if (m_disconnectTimer != null)
                                {
                                    m_disconnectTimer.Dispose();
                                    m_disconnectTimer = null;
                                }
                            }, null, 500, 1000); // 500毫秒后启动， 1000毫秒检查一次
                        }

                        ConnSocket.Shutdown(SocketShutdown.Both);
                        //ConnSocket.Disconnect(false);
                    }
                }
            }
            catch (Exception ex)
            {
                String msg = String.Format("{0}:{1}", ex.GetType().ToString(), ex.Message);
                Debug.WriteLine(msg);
                //throw ex;
            }
        }

        /// <summary>
        /// 清理关闭soket
        /// Close all work and release all resource.
        /// </summary>
        private void SocketClose()
        {
            try
            {
                if (ConnSocket != null)
                {
                    lock (_lockSocket)
                    {
                        ConnSocket.Close();
                    }
                }

                // 删除两个IDispose数据，避免connect释放不了的问题
                if (m_connEventArg != null)
                {
                    m_connEventArg.Dispose();
                    m_connEventArg = null;
                }

                if (m_receiveEventArg != null)
                {
                    m_receiveEventArg.Dispose();
                    m_receiveEventArg = null;
                }

                // 清理timer
                if (m_disconnectTimer != null)
                {
                    m_disconnectTimer.Dispose();
                    m_disconnectTimer = null;
                }
            }
            catch (Exception ex)
            {
                String msg = String.Format("{0}:{1}", ex.GetType().ToString(), ex.Message);
                Debug.WriteLine(msg);
                //throw ex;
            }
        }

        /// <summary>
        /// 主动断开连接
        /// </summary>
        public void Disconnect()
        {
            SocketDisconnect();
            State = ConnectionState.Disconnecting;

        }

        /// <summary>
        /// 关闭连接，初始化状态
        /// </summary>
        public void Close()
        {
            SocketClose();
            State = ConnectionState.None;
        }

        /// <summary>
        /// Get underline protocol message.
        /// </summary>
        /// <returns>If exist message return it ,or reutrn null.</returns>
        public KeyValuePair<int, object> GetMessagePair()
        {
            KeyValuePair<int, object> msgPair = new KeyValuePair<int, object>(0, null);
            lock (RecvQueue)
            {
                if (RecvQueue.Count > 0)
                {
                    var vMsg = RecvQueue.Dequeue();
                    return vMsg;
                }
            }
            return msgPair;
        }

        /// <summary>
        /// Use underline socket to send protocol message async.
        /// </summary>
        /// <param name="vMessage">The protocol message to be sended.</param>
        public void SendMessage(System.Object vMsg)
        {
            Log.Info("一个消息需要发送");
            if (State != ConnectionState.Established)
            {
                Log.Info("一个消息需要发送 1");
                Debug.WriteLine(String.Format("SendMessage Error:in State {0}", State));
                return;
            }
            if (vMsg == null)
            {
                Log.Info("一个消息需要发送 2");
                Debug.WriteLine(String.Format("SendMessage Error:vMsg is null"));
                return;
            }

            ArraySegment<Byte> sndBuf = ProtoHelper.EncodeMessage(vMsg,vMsg is IResponse);


            SocketAsyncEventArgs sendEventArgs = new SocketAsyncEventArgs();

            sendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnCompletedForSend);

            sendEventArgs.SetBuffer(sndBuf.Array, sndBuf.Offset, sndBuf.Count);

            Debug.WriteLine(string.Format("SendMessage Send {0}", vMsg.GetType().Name));
            Log.Info(string.Format("SendMessage Send {0}", vMsg.GetType().Name));
            if (!ConnSocket.SendAsync(sendEventArgs))
            {
                OnCompletedForSendImpl(sendEventArgs);
                sendEventArgs.Dispose();
            }
        }

        private void OnCompletedForSend(object sender, SocketAsyncEventArgs e)
        {
            OnCompletedForSendImpl(e);
            e.Dispose();
        }

        //The callback of BeginSend on fuction "SendMessage"
        private void OnCompletedForSendImpl(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    return;
                }
                else//发生故障了 就抛一个异常进行处理 
                {
                    throw new Exception("The result of SendAsync is not correct");
                }
            }
            catch (Exception ex)
            {
                // Client exception means connection is disconnected
                CCMSGConnectionSendFailure failureMsg = new CCMSGConnectionSendFailure();
                failureMsg.ExceptionInfo = ex.ToString();
                lock (RecvQueue)
                {
                    RecvQueue.Enqueue(new KeyValuePair<int, object>(failureMsg.MessageId, failureMsg));
                }
                goto BREAK_CONNECT;
            }
        BREAK_CONNECT:
            lock (RecvQueue)
            {
                if (State == ConnectionState.Established)
                {
                    State = ConnectionState.Closed;
                    var vMsg = new CCMSGConnectionBreak();
                    RecvQueue.Enqueue(new KeyValuePair<int, object>(vMsg.MessageId, vMsg));
                }
            }
        }

        //Main work when initializing, including async build socket connection
        private void StartConnectionWork()
        {
            // Make connection to remote
            try
            {
                lock (_lockSocket)
                {
                    if (State == ConnectionState.Connecting)
                    {
                        ConnSocket = new Socket(m_ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                        // if the return value of ConnectAsync is false, the connection was finished synchronously, just call OnCompletedForConnectImpl directly
                        if (!ConnSocket.ConnectAsync(m_connEventArg))
                        {
                            OnCompletedForConnectImpl(m_connEventArg);
                        }
                    }
                }
            }
            catch (SocketException)
            {
                throw;
            }
            catch
            {
                throw;
            }
        }

        // the callback when socket ConnectAsync finished
        private void OnCompletedForConnect(object sender, SocketAsyncEventArgs e)
        {
            OnCompletedForConnectImpl(e);
        }
        
        private void OnCompletedForConnectImpl(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    /// Change connection state to ready
                    State = ConnectionState.Established;

                    // as soon as the client is connected, post a receive to the connection
                    if (!ConnSocket.ReceiveAsync(m_receiveEventArg))
                    {
                        OnCompletedForReceiveImpl(m_receiveEventArg);
                    }

                    /// Make a connection ready notification
                    CCMSGConnectionReady ccReadyMsg = new CCMSGConnectionReady();
                    RecvQueue.Enqueue(new KeyValuePair<int, object>(ccReadyMsg.MessageId, ccReadyMsg));
                    return;
                }
                else
                {
                    goto FAIL_CONNECT;
                }
            }
            catch
            {
                goto FAIL_CONNECT;
            }
            
        FAIL_CONNECT:
            CCMSGConnectionFailure ccFailMsg = new CCMSGConnectionFailure();
            lock (RecvQueue)
            {
                RecvQueue.Enqueue(new KeyValuePair<int, object>(ccFailMsg.MessageId, ccFailMsg));
            }
            State = ConnectionState.Closed;
        }

        // the callback when socket ReceiveAsync finished
        private void OnCompletedForReceive(object sender, SocketAsyncEventArgs e)
        {
            OnCompletedForReceiveImpl(e);
        }

        private void OnCompletedForReceiveImpl(SocketAsyncEventArgs e)
        {
            //Log.Info("接收到消息 Count = "+ e.BytesTransferred);
           // Log.Info("sadsadsadsa " + e.BytesTransferred);
            try
            {
                //Log.Info($"SocketError :{ e.SocketError}");
                if (e.SocketError == SocketError.Success && e.BytesTransferred != 0)
                {
                   // Log.Info("开始处理消息");
                    // at first, write state.buffer to recvcache
                    RecvCache.Write(e.Buffer, e.BytesTransferred);

                    //Debug.WriteLine(string.Format("OnCompletedForReceiveImpl Receive {0} RecvCache.Length={1}", e.BytesTransferred, RecvCache.Length));

                    // second, push the msg to recvqueue and decode message
                    while (true)
                    {
                        ushort msgId = 0;
                        bool flag;
                        object msg = ProtoHelper.DecodeMessage(RecvCache, out msgId,out flag);
                        //Log.Msg(msg.GetType());
                        if (msg == null)
                        {
                            
                            break;
                        }
                        lock (RecvQueue)
                        {
                            var eMsg = new CCMSGConnectionRevMsg();

                            eMsg.MessageInfo.Message = msg as IMessage;
                            eMsg.MessageInfo.Opcode= (ushort)msgId;
                            eMsg.MessageInfo.flag = flag;
                       //     Log.Info("接收到消息  并且丢进了队列");
                            RecvQueue.Enqueue(new KeyValuePair<int, object>(eMsg.MessageId ,eMsg));
                        }

                        // all cache is handled
                        if (RecvCache.Length == 0)
                        {
                            break;
                        }
                    }
                    //Debug.WriteLine(string.Format("OnCompletedForReceiveImpl Receive End, RecvCache.Length={0}", RecvCache.Length));
                    RecvCache.Crunch();

                    // third, restart the async receive process
                    m_receiveEventArg.SetBuffer(0, ProtoConst.MAX_PACKAGE_LENGTH);
                    if (!ConnSocket.ReceiveAsync(m_receiveEventArg))
                    {
                        OnCompletedForReceiveImpl(m_receiveEventArg);
                    }
                    return;
                }
                else
                {
                    throw new Exception(string.Format("The result of ReceiveAsync is not correct, SocketError={0} BytesTransferred={1}", e.SocketError, e.BytesTransferred));
                }
            }
            catch(Exception ex)
            {
                // Client exception means connection is disconnected
                // Or, there is an bad message format in byte stream
                CCMSGConnectionRecvFailure eMsg = new CCMSGConnectionRecvFailure();
                eMsg.ExceptionInfo = ex.ToString();
                lock (RecvQueue)
                {
                    RecvQueue.Enqueue(new KeyValuePair<int, object>(eMsg.MessageId, eMsg));
                }
                goto BREAK_CONNECT;
            }

        BREAK_CONNECT:
            // when receive action is over, which represents that socket can't receive any data
            lock (RecvQueue)
            {
                if (State == ConnectionState.Established || 
                    State == ConnectionState.Disconnecting)  // 这个状态是为了让客户端主动断开服务器的时候
                {
                    State = ConnectionState.Closed;
                    var vMsg = new CCMSGConnectionBreak();
                    RecvQueue.Enqueue(new KeyValuePair<int, object>(vMsg.MessageId, vMsg));
                }
            }
        }

        private void FireEventOnLogPrint(string callFun, string log = "")
        {
            if (string.IsNullOrEmpty(log))
            {
                log = string.Format("m_ipEndPoint={0}:{1} CCMSGConnectionBreak happen. callFun={2}",
                    m_ipEndPoint.Address.ToString(), m_ipEndPoint.Port, callFun);
            }

            if (EventOnLogPrint != null)
            {
                EventOnLogPrint(log);
            }
        }

        /// <summary>
        /// 获取端口
        /// </summary>
        public int GetEndpoint()
        {
            if (m_ipEndPoint == null)
            {
                return 0;
            }

            return m_ipEndPoint.Port;
        }

        /// <summary>
        /// The connection state store and management
        /// </summary>
        private ConnectionState _state;
        public ConnectionState State
        {
            get
            {
                lock (m_lockState)
                {
                    return _state;
                }
            }

            set
            {
                lock (m_lockState)
                {
                    _state = value;
                }
            }
        }

   

        /// <summary>
        /// Underline socket intance
        /// </summary>
        private Socket ConnSocket { get; set; }
        /// <summary>
        /// Underline socket remote address
        /// </summary>
        private IPEndPoint Remote { get; set; }

        private Queue<ILocalMessage> LocalMsgQueue = new Queue<ILocalMessage>();
        /// <summary>
        /// The protocol message to be processed
        /// </summary>
        private Queue<KeyValuePair<int, System.Object>> RecvQueue = new Queue<KeyValuePair<int, System.Object>>();
        /// <summary>
        /// Local byte cahce for recv operation
        /// </summary>
        private MessageBlock RecvCache { get; set; }
        /// <summary>
        /// The lock-object for State
        /// </summary>
        private object m_lockState;
        /// <summary>
        /// remote address
        /// </summary>
        private IPEndPoint m_ipEndPoint;
        /// <summary>
        /// The lock-object for Peer
        /// </summary>
        private object _lockSocket = new object();
        /// <summary>
        /// The eventArg for socket connect action
        /// </summary>
        private SocketAsyncEventArgs m_connEventArg;
        /// <summary>
        /// The eventArg for socket receive action
        /// </summary>
        private SocketAsyncEventArgs m_receiveEventArg;
        /// <summary>
        /// sokect断开后的timer，用来处理可能收不到后续Fin包的情况
        /// </summary>
        private Timer m_disconnectTimer;

        /// <summary>
        /// 对协议二进制数据进行反序列化的方法，客户端会使用不同于服务器的方法
        /// </summary>
        private Func<Stream, Type, int, object> m_messageDeserializeAction;

        public event Action<string> EventOnLogPrint;
    }
}


