using System;

namespace Crazy.ClientNet
{
	public class CCMSGConnectionFailure:ILocalMessage
	{
        public Int32 MessageId { get { return (Int32)LocalMsgId.CCMSGConnectionFailure; } }
	}

    public class CCMSGConnectionSendFailure:ILocalMessage
    {
        public Int32 MessageId { get { return (Int32)LocalMsgId.CCMSGConnectionSendFailure; } }

        public String ExceptionInfo { get; set; }
    }

    public class CCMSGConnectionRecvFailure:ILocalMessage
    {
        public Int32 MessageId { get { return (Int32)LocalMsgId.CCMSGConnectionRecvFailure; } }

        public String ExceptionInfo { get; set; }
    }
    public class CCMSGConnectionBreak : ILocalMessage
    {

        public int MessageId
        {
            get { return (Int32)LocalMsgId.CCMSGConnectionBreak; }
        }
    }
    public class CCMSGConnectionReady:ILocalMessage
    {
        public Int32 MessageId { get { return (Int32)LocalMsgId.CCMSGConnectionReady; } }
    }
}

