using Crazy.Common;
using System;

namespace Crazy.ClientNet
{
    /// <summary>
    /// 连接接收的网络消息封装成本地消息
    /// </summary>
	public class CCMSGConnectionRevMsg:ILocalMessage
	{
        public MessageInfo MessageInfo;
        public Int32 MessageId { get { return (Int32)LocalMsgId.CCMSGConnectionRevMsg; } }
    }
    
}

