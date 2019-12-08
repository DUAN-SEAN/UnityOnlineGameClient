using System;
using System.Linq;
using System.Net;
using System.Reflection;
using Assets.Game.Manager;
using Crazy.Common;
using GameServer.Configure;
using Log = Crazy.ClientNet.Log;
using Client = Crazy.ClientNet.Client;

namespace Crazy.Main
{

    public class NetWorkManager :  ITickable
    {
        public NetWorkManager(GameManager gameManager) 
        {
        }

        /// <summary>
        /// Tick当前Task列表
        /// </summary>
        public void Tick()
        {
            
        }
        public void Initialize(ServerBaseGlobalConfigure config)
        {
            //Log.Info(Application.dataPath + "/" + configPath);
            //1 初始化配置
            if (!InitNetConfigure(config))
            {
                Log.Info("读取配置文件失败");
            }

            Start();
        }
        
        private void Start()
        {
            Instance = this;


            //2 初始化网络
            Log.Info("初始化网络开始！");
            InitNetWork();
        }
        private bool InitNetConfigure(ServerBaseGlobalConfigure config)
        {
            ServerBaseGlobalConfigure gameServerGlobalConfig = config;
            m_Globalfigure = gameServerGlobalConfig as ServerBaseGlobalConfigure;
            m_serverConfig = m_Globalfigure.Global.Servers[0];

            Log.Info($"Server Ip = {m_serverConfig.EndPortIP} Port = {m_serverConfig.EndPortPort}");
            return true;
        }
        private bool InitNetWork()
        {


            //1 初始化程序集
            TypeManager.Instance.Add(DLLType.Common, Assembly.GetAssembly(typeof(TypeManager)));
            TypeManager.Instance.Add(DLLType.Client, Assembly.GetAssembly(this.GetType()));
            //TypeManager.Instance.Add(DLLType.Client, Assembly.GetAssembly(this.GetType()));
            //2 初始化Opcode
            m_opcodeTypeDictionary = new OpcodeTypeDictionary();
            if (!m_opcodeTypeDictionary.Init())
            {
                Log.Error("OpcodeTypeDictionary Initialise  Fail");
                return false;
            }
            //3 初始化Dispacher
            m_messageDispather = new MessageDispather();
            if (!MessageDispather.Instance.Init())
            {

                Log.Error("MessageDispather Initialise Fail");
                return false;
            }
            //Log.Info("handler Type  = " + m_messageDispather.Handlers.Keys.First());
            //Log.Info("hander个数:" + m_messageDispather.Handlers.Values.ToList().Count);
            //初始化对象池

            m_objectPool = new ObjectPool();
            //MessageFactory.Adapting(m_objectPool);

            //3 其他配置

            return true;
        }
        /// <summary>
        /// 客户端人员可以根据此代码的逻辑 创建玩家现场，玩家现场的逻辑供参考
        /// </summary>
        /// <returns></returns>
        public PlayerContextBase Connect()
        {

            PlayerContextBase playerContextBase = new PlayerContextBase(CreateClient());
            userTick = playerContextBase.Update;
            playerContextBase.ContextId = IdFactory++;
            return playerContextBase;
        }

        public Client CreateClient(string ip, int port)
        {
            Client client = new Client();
            if (client.Initialize(ip, port))
            {
                return client;
            }
            else
                return null;

        }

        public Client CreateClient(IPEndPoint iPEndPoint)
        {
            Client client = new Client();


            return null;
        }

        public Client CreateClient()
        {
            Client client = new Client();
            if (client.Initialize(m_serverConfig.EndPortIP, m_serverConfig.EndPortPort))
            {
                //Log.Error("连接Client成功");
                return client;
            }
            else
            {
                //Log.Error("连接Client失败");
            }

            return null;
        }

        public void Test()
        {
            Client client = CreateClient("127.0.0.1", 5500);
            //client.SendMessage(new C2S_SearchUser { });
        }

        public ServerBaseGlobalConfigure Globalfigure => m_Globalfigure;

        public Action userTick;
        /// <summary>
        /// 配置路径 在unity面板中绑定
        /// </summary>
        public string configPath;
        /// <summary>
        /// 全局配置文件
        /// </summary>
        private ServerBaseGlobalConfigure m_Globalfigure;
        /// <summary>
        /// 服务器配置文件
        /// </summary>
        private Server m_serverConfig;
        /// <summary>
        /// 协议字典
        /// </summary>
        private OpcodeTypeDictionary m_opcodeTypeDictionary;
        /// <summary>
        /// 消息分发
        /// </summary>
        private MessageDispather m_messageDispather;
        /// <summary>
        /// 单例
        /// </summary>
        public static NetWorkManager Instance;
        /// <summary>
        /// 简单的Id序列化器
        /// </summary>
        public static ulong IdFactory = 1;
        /// <summary>
        /// 对象池对象
        /// </summary>
        private ObjectPool m_objectPool;

    }
   


}
