using System;

namespace Crazy.ClientNet
{
    /// <summary>
    /// 本地消息定义
    /// </summary>
    public enum LocalMsgId
    {
        /// <summary>
        /// 连接准备完成
        /// </summary>
        CCMSGConnectionReady = 9001,
        /// <summary>
        /// 连接断开
        /// </summary>
        CCMSGConnectionBreak = 9002,
        /// <summary>
        /// 连接失败
        /// </summary>
        CCMSGConnectionFailure = 9003,
        /// <summary>
        /// 连接发送失败
        /// </summary>
        CCMSGConnectionSendFailure = 9004,
        /// <summary>
        /// 连接接收失败
        /// </summary>
        CCMSGConnectionRecvFailure = 9005,
        /// <summary>
        /// 网络消息
        /// </summary>
        CCMSGConnectionRevMsg = 9006,
    }
}
