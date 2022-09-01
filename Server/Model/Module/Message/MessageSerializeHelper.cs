using System;
using System.IO;

namespace ET
{
    /// <summary>
    /// 消息序列化帮助
    /// </summary>
    public static class MessageSerializeHelper
    {
        public static object DeserializeFrom(ushort opcode, Type type, MemoryStream memoryStream)
        {
            if (opcode < OpcodeRangeDefine.PbMaxOpcode)
            {
                return ProtobufHelper.FromStream(type, memoryStream);
            }
            
            if (opcode >= OpcodeRangeDefine.JsonMinOpcode)
            {
                return JsonHelper.FromJson(type, memoryStream.GetBuffer().ToStr((int)memoryStream.Position, (int)(memoryStream.Length - memoryStream.Position)));
            }
#if NOT_UNITY
            return MongoHelper.FromStream(type, memoryStream);
#else
            throw new Exception($"client no message: {opcode}");
#endif
        }

        public static void SerializeTo(ushort opcode, object obj, MemoryStream memoryStream)
        {
            try
            {
                if (opcode < OpcodeRangeDefine.PbMaxOpcode)
                {
                    ProtobufHelper.ToStream(obj, memoryStream);
                    return;
                }

                if (opcode >= OpcodeRangeDefine.JsonMinOpcode)
                {
                    string s = JsonHelper.ToJson(obj);
                    byte[] bytes = s.ToUtf8();
                    memoryStream.Write(bytes, 0, bytes.Length);
                    return;
                }
#if NOT_UNITY
                MongoHelper.ToStream(obj, memoryStream);
#else
                throw new Exception($"client no message: {opcode}");
#endif
            }
            catch (Exception e)
            {
                throw new Exception($"SerializeTo error: {opcode}", e);
            }

        }

        public static MemoryStream GetStream(int count = 0)
        {
            MemoryStream stream;
            if (count > 0)
            {
                stream = new MemoryStream(count);
            }
            else
            {
                stream = new MemoryStream();
            }

            return stream;
        }
        
        public static (ushort, MemoryStream) MessageToStream(object message)
        {
            int headOffset = Packet.ActorIdLength; 
            
            //1.得到一个头部长度+操作长度的存储流 8 + 2
            MemoryStream stream = GetStream(headOffset + Packet.OpcodeLength);

            ushort opcode = OpcodeTypeComponent.Instance.GetOpcode(message.GetType());
            
            //2.把流的当前位置设为指定值 
            stream.Seek(headOffset + Packet.OpcodeLength, SeekOrigin.Begin);
            
            //3.设置流长度
            stream.SetLength(headOffset + Packet.OpcodeLength);
            
            //4.把头部跟操作码都写入
            stream.GetBuffer().WriteTo(headOffset, opcode);
            
            MessageSerializeHelper.SerializeTo(opcode, message, stream);
            
            stream.Seek(0, SeekOrigin.Begin);
            return (opcode, stream);
        }
    }
}