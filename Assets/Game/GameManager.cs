using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Crazy.ClientNet;
using Crazy.Common;
using Crazy.Main;
using GameServer.Configure;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Log = Crazy.ClientNet.Log;

namespace Assets.Game.Manager
{
    public interface ITickable
    {
        void Tick();
    }
    
    public class GameManager:MonoBehaviour
    {
        

        public void Start()
        {
            Instance = this;

            _netWorkManager = new NetWorkManager(this);

            Initialize();

            Test();

            DontDestroyOnLoad(this);
            
        }

        void Test()
        {
            var plx = CreatePlayerContext<PlayerContextBase>();
        }
        /// <summary>
        /// 初始化各个管理器
        /// 1 加载配置文件 配置类型和服务器同步 commonLib
        /// 2 创建一个玩家现场 （玩家现场由当前项目决定）
        /// </summary>
        private void Initialize()
        {
            InitConfig(configPath);

            _netWorkManager.Initialize(m_Globalfigure);

        }

        private void InitConfig(string path)
        {

            //GameServerGlobalConfig gameServerGlobalConfig = Util.Deserialize<GameServer.Configure.GameServerGlobalConfig>(path);
            path = Path.Combine(Application.streamingAssetsPath, configPath);
            // 1 www加载 过时
            //{
            //    WWW www = new WWW(path);
            //    XmlSerializer xmlSerializer =
            //        new XmlSerializer(typeof(GameServerGlobalConfig), new XmlAttributeOverrides());
            //    GameServerGlobalConfig obj;
            //    using (Stream stream = new MemoryStream(www.bytes))
            //    {
            //        obj = xmlSerializer.Deserialize(stream) as GameServerGlobalConfig;
            //    }

            //    m_gameServerGlobalConfig = obj;
            //}

            // 2 UnityWebRequest加载 异步不可用
            //StartCoroutine(LoadStreamingAssets(path,
            //    () => { Log.Info($"Server Ip = {m_serverConfig.EndPortIP} Port = {m_serverConfig.EndPortPort}"); }));

            // 3 本地加载，安卓不可用
            //{
            //GameServerGlobalConfig gameServerGlobalConfig = Util.Deserialize<GameServer.Configure.GameServerGlobalConfig>(path);
            //m_gameServerGlobalConfig = gameServerGlobalConfig ;
            //}

            // 4 UnityWebRequest 同步加载 可用
            UnityWebRequest wr = new UnityWebRequest(path);
            DownloadHandlerBuffer bufferHandler = new DownloadHandlerBuffer();
            wr.downloadHandler = bufferHandler;
            var operation = wr.SendWebRequest();
            while (!operation.isDone)
            {
                Thread.Sleep(100);
            }
            print(bufferHandler.data.Length);
            print(bufferHandler.text);

            XmlSerializer xmlSerializer =
                new XmlSerializer(typeof(SampleGameServerGlobalConfig), new XmlAttributeOverrides());
            SampleGameServerGlobalConfig obj;
            using (Stream stream = new MemoryStream(bufferHandler.data))
            {
                obj = xmlSerializer.Deserialize(stream) as SampleGameServerGlobalConfig;
            }

            m_Globalfigure = obj;
            m_gameServerGlobalConfig = obj;
            m_serverConfig = m_gameServerGlobalConfig.Global.Servers[0];
            Log.Info($"Server Ip = {m_serverConfig.EndPortIP} Port = {m_serverConfig.EndPortPort}");

        }

        /// <summary>
        /// Tick各个Task管理器
        /// </summary>
        public void Update()
        {

            _netWorkManager?.Tick();
            _currentSpacePlayerContext?.Update();//polling 本地消息或网络消息

        }
        /// <summary>
        /// 创建一个玩家现场
        /// </summary>
        public T CreatePlayerContext<T>() where T :PlayerContextBase,new()
        {
            var client = _netWorkManager.CreateClient();

            T playerContext = new T();

            playerContext.SetClient(client);

            _currentSpacePlayerContext = playerContext;
            return playerContext;
        }

        #region UnityObject

        [SerializeField] private string configPath;//Resources 下的相对路径（一般为名称）

        public void LoadScenceFromTask(string scenceName,Action action = null)
        {
            StartCoroutine(LoadScence(scenceName, action));
        }
        public IEnumerator LoadScence(string scenceName, Action action = null)
        {
            AsyncOperation async = SceneManager.LoadSceneAsync(scenceName);
            yield return async;
            Debug.Log("加载场景成功：" + scenceName);
            action?.Invoke();
        }
        #endregion

        #region 字段
       
        /// <summary>
        /// 网络管理器
        /// </summary>
        private NetWorkManager _netWorkManager;
        /// <summary>
        /// 当前玩家现场
        /// </summary>
        private PlayerContextBase _currentSpacePlayerContext;

        public static GameManager Instance;

        /// <summary>
        /// 服务器全局配置
        /// </summary>
        private ServerBaseGlobalConfigure m_Globalfigure;
        /// <summary>
        /// 服务器实例配置 
        /// </summary>
        private Server m_serverConfig;



        /// <summary>
        /// 新程序自定义 外围配置 包含服务器全局配置
        /// </summary>
        private SampleGameServerGlobalConfig m_gameServerGlobalConfig;

        #endregion

    }
}
