using Crazy.Common;
using System;

namespace Crazy.ClientNet
{
    /// <summary>
    /// ���ӽ��յ�������Ϣ��װ�ɱ�����Ϣ
    /// </summary>
	public class CCMSGConnectionRevMsg:ILocalMessage
	{
        public MessageInfo MessageInfo;
        public Int32 MessageId { get { return (Int32)LocalMsgId.CCMSGConnectionRevMsg; } }
    }
    
}

