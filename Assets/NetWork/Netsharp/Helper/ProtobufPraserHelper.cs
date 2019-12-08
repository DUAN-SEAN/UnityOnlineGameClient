using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crazy.Common;

namespace Crazy.ClientNet
{
    public static class ProtobufPraserHelper
    {
        public static ProtobufPacker s_protobufPacker;
        static ProtobufPraserHelper()
        {
            s_protobufPacker = new ProtobufPacker();
        }
    }
}
