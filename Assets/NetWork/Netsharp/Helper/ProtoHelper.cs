using System;
using System.IO;
using System.Diagnostics;
using Crazy.Common;
namespace Crazy.ClientNet
{
    public static class ProtoHelper
    {
        /// <summary>
        /// Decodes the message.
        /// </summary>
        /// <returns>The message.</returns>
		/// <param name="dataBlock">Data block.</param>
		/// <param name="dataOffset">Data offset.</param>
		/// <param name="dataLen">Data length.</param>
		/// <param name="newMsg">New message.</param>
        /// <returns> 
        /// 	!= null     : A protocol message
        ///     == null     : No full message exist
        ///     Exception   : ProtoException to indicate HACK stream
        /// </returns>
        public static Object DecodeMessage(MessageBlock rCache, out ushort msgId,out bool isRpc)
        {
            msgId = 0;
            isRpc = false;
            // get the message head's length
            int headLength = sizeof(ushort) * 2;

            // Not enough data for header
            if (rCache.Length < headLength)
            {
                //Debug.WriteLine(String.Format("DecodeMessage Not enough data for header. rCache.Length={0} < headLength={1}", rCache.Length, headLength));
                return null;
            }

            // get message length
            ushort pakFullLength = rCache.ReadUInt16();

            // Bad stream
            if (pakFullLength < headLength)
                throw new ProtoException(string.Format("Hack stream, TotalLength={0}", pakFullLength));

            int pakBodyLength = pakFullLength - headLength;

            //Debug.WriteLine(String.Format("DecodeMessage Before rCache.Length={0} pakBodyLength={1} ", rCache.Length, pakBodyLength));

            // Not enough data for body
            // 注意这里需要考虑后面一个包头的长度
            if (rCache.Length < pakBodyLength + sizeof(ushort))
            {
                // Move read ptr to back
                // 此处只处理了UInt16长度的数据，所以回滚需要一致，不能使用headLength
                rCache.ReadPtr(-(sizeof(ushort)));
                //Debug.WriteLine(String.Format("DecodeMessage Not enough data for body rCache.Length={0} < pakBodyLength={1}", rCache.Length, pakBodyLength));
                return null;
            }

            // get message id field
            ushort pakMessageIdField = rCache.ReadUInt16();
            // get compressed tag
            isRpc = pakMessageIdField >> (sizeof(ushort) * 8 - 1) == 1;
            // get the protocol id
            msgId = (ushort)(pakMessageIdField & 0x7FFF);

            // deserialize message, we should decompress message body data if needed
            Type pakType = OpcodeTypeDictionary.Instance.GetTypeById(msgId);
            // Use ProtoBuf to deserialize message		
            //object ccMsg =  MessageFactory.CreateMessage(pakType);//2019.7.17改为对象池
            object ccMsg = Activator.CreateInstance(pakType);

            //Debug.WriteLine(String.Format("DecodeMessage msgId={0} pakBodyLength={1} isCompressed={2} pakType={3}", msgId, pakBodyLength, isCompressed, pakType.ToString()));
            try
            {
                using (MemoryStream readStream = rCache.GetStream(pakBodyLength))
                {
                    //将消息序列化，根据所提供的序列化器进行序列化和反序列化             
                    ProtobufPraserHelper.s_protobufPacker.DeserializeFrom(ccMsg, readStream);
                }
                
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("msgId={0}, isRpc={1} pakBodyLength={2}",
                    msgId, isRpc, pakBodyLength), ex);
            }

            //Debug.WriteLine(String.Format("DecodeMessage rCacheInfo Length={0} WrPtr={1} RdPtr={2}", rCache.Length, rCache.WrPtr, rCache.RdPtr));
            return ccMsg;
        }

        ///// <summary>
        ///// 打印byte数组
        ///// </summary>
        ///// <param name="array"></param>
        ///// <returns></returns>
        //private static string printByteArray(byte[] array)
        //{
        //    if (array == null || array.Length == 0) return string.Empty;

        //    System.Text.StringBuilder sb = new System.Text.StringBuilder();
        //    foreach (var c in array)
        //    {
        //        sb.AppendFormat("{0} ", c);
        //    }
        //    return sb.ToString();
        //}

        /// <summary>
        /// Encodes the message through protobuff, the message's format is : messageSize(UInt16)-ProtocolId(UInt16, including the compressTag and the protocolId value)-ProtocolData
        /// </summary>
        /// <returns>The ecoded message stream.</returns>
        /// <param name="vMsg">The message.</param>
        public static ArraySegment<byte> EncodeMessage(Object vMsg,bool rpc = false)
        {
            byte[] fullBuf = new byte[(Int32)ProtoConst.MAX_PACKAGE_LENGTH];
            MemoryStream wrStream = new MemoryStream(fullBuf);
            BinaryWriter bnWriter = new BinaryWriter(wrStream);

            // get the message head's length
            int headLength = sizeof(ushort) * 2;

            // Total length, placehold only now
            bnWriter.Write((ushort)0);
            // Message ID, place hold only now
            bnWriter.Write((ushort)0);
            // write message body data to stream
            ProtobufPraserHelper.s_protobufPacker.SerializeTo(vMsg, wrStream);
            
            
            ushort fullLength = 0;
            fullLength = (ushort)wrStream.Position;
            bnWriter.Seek(0, SeekOrigin.Begin);
            bnWriter.Write(fullLength);
            ushort protocolId = (ushort)(OpcodeTypeDictionary.Instance.GetIdByType(vMsg.GetType()));
            protocolId =rpc? (ushort)(protocolId | 0x8000):(ushort)(protocolId & 0x7FFF);
            bnWriter.Write(protocolId);
            //Log.Info($"{vMsg.GetType()} Count = " + fullLength);
            return new ArraySegment<byte>(fullBuf, 0, fullLength);
        }
    }

 
}

