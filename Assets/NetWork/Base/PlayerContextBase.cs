using Crazy.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crazy.ClientNet;
using Log = Crazy.Common.Log;
namespace Crazy.Main
{
    /// <summary>
    /// 假的玩家上下文
    /// 注意ContextId SessionId 指向相同且要求在请求消息不能为0，由客户端自行分配Id
    /// PlayerId为玩家应用层Id目前作为玩家账号表示
    /// </summary>
    public partial class PlayerContextBase:ISession
    {
        public PlayerContextBase()
        {

        }
        public PlayerContextBase(ClientNet.Client client)
        {
            m_client = client;
        }

        public void SetClient(ClientNet.Client client)
        {
            m_client = client;
        }
        public void Update()
        {
            while (true)
            {
                var messagePair = m_client.GetMessagePair();
                if (messagePair.Key == 0 || messagePair.Value == null)
                {
                    break;
                }
                //Log.Info("找到了一条OnMessage::"+messagePair.Key);
                OnMessage(messagePair);
            }
        }
        #region TickLocalMessage

        public virtual void OnMessage(KeyValuePair<int, object> keyValuePair)
        {
            switch (keyValuePair.Key)
            {
                case (int)LocalMsgId.CCMSGConnectionBreak:
                    ClientNet.Log.Error("连接断开");
                    break;
                case (int)LocalMsgId.CCMSGConnectionFailure:
                    ClientNet.Log.Error("连接失败");
                    break;
                case (int)LocalMsgId.CCMSGConnectionReady:
                    Send(new C2S_TestMessage {Data = "新消息"});
                    ClientNet.Log.Error("连接准备完成");
                    break;
                case (int)LocalMsgId.CCMSGConnectionRecvFailure:
                    ClientNet.Log.Error("连接接收失败");
                    break;
                case (int)LocalMsgId.CCMSGConnectionSendFailure:
                    ClientNet.Log.Error("连接发送失败");
                    break;
                case (int)LocalMsgId.CCMSGConnectionRevMsg:
                    CCMSGConnectionRevMsg message = keyValuePair.Value as CCMSGConnectionRevMsg;
                    //ClientNet.Log.Info($"网络消息接收:{message.MessageInfo.Message} opcode:{message.MessageInfo.Opcode} flag:{message.MessageInfo.flag}");
                    if (!message.MessageInfo.flag)
                    {
                        MessageDispather.Instance.Handle(this, message.MessageInfo);
                    }
                    else
                    {
                        IResponse response = message.MessageInfo.Message as IResponse;
                        Log.Msg(response);
                        if(response == null)
                        {
                          
                            throw new Exception($"flag is response, but message is not! {message.MessageInfo.Opcode}");
                       
                            
                        }
                        Action<IResponse> action;
                        if (!m_requestCallback.TryGetValue(response.RpcId, out action))
                        {
                            return ;
                        }
                        m_requestCallback.Remove(response.RpcId);

                        action(response);
                    }
                    break;
                default:break;
            }
        }
        #endregion

        #region ISession

        public void Reply(IResponse message)
        {
            Send(message);
        }
        //一次发送多个消息
        public int Send(List<IMessage> messages)
        {

            foreach (var item in messages)
            {
                Send(item);
            }

            return 0;
        }

        public int Send(IMessage message)
        {
            // 包检查
            if (message == null)
                throw new ArgumentNullException("PlayerContextBase::SendPackage packageObj");

            // 客户端代理不可用
            if (m_client == null || m_client.State == ConnectionState.Closed)
                return -1;

            m_client.SendMessage(message);
            //MessageFactory.Recycle(message);//将message丢入池子中
            return 0;
        }

        internal ulong GetInstanceId()
        {
            return m_contextId; 
        }

        public Task<IResponse> Call(IRequest request)
        {
            int rpcId = ++RpcId;
            var tcs = new TaskCompletionSource<IResponse>();

            this.m_requestCallback[rpcId] = (response) =>
            {
                try
                {
                    if (ErrorCode.IsRpcNeedThrowException(response.Error))
                    {
                        throw new RpcException(response.Error, response.Message);
                    }

                    tcs.SetResult(response);
                }
                catch (Exception e)
                {
                    tcs.SetException(new Exception($"Rpc Error: {request.GetType().FullName}", e));
                }
            };

            request.RpcId = rpcId;
            this.Send(request);
            return tcs.Task;
        }

        public Dictionary<int, Action<IResponse>> GetRPCActionDic()
        {
            return m_requestCallback;
        }
        #endregion


 
        #region 属性

        public ulong ContextId { get => m_contextId; set => m_contextId = value; }

        private static int RpcId
        {
            get
            {
                if (c_rpcId >= Int32.MaxValue)
                {
                    c_rpcId = 0;

                }
                return c_rpcId;

            }
            set { c_rpcId = value; }
        }

        public ulong SessionId => m_contextId;

        public string PlayerId {  get { return m_playerId; } }

        protected string m_playerId;

        public string ContextStringName => m_playerId;

        #endregion


        #region 字段

        private ulong m_contextId = FactoryId++;

        private Dictionary<int, Action<IResponse>> m_requestCallback = new Dictionary<int, Action<IResponse>>();
        private ClientNet.Client m_client;
        private static int c_rpcId;

        #endregion

        private static ulong FactoryId = 1;

    }
}
