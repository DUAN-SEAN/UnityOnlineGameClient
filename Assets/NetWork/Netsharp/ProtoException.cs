using System;

namespace Crazy.ClientNet
{
	public class ProtoException : Exception
	{
		public ProtoException (String sDesc) : base(sDesc)
		{
		}
	}
}

