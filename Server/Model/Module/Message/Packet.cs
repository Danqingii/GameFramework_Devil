using System.IO;

namespace ET
{
    /// <summary>
    /// 客户端包定义 
    /// </summary>
    public struct Packet
    {
        public const int MinPacketSize = 2;   //包体最小byte
        public const int OpcodeIndex = 8;     //默认操作码处在包中第8个位置
        public const int KcpOpcodeIndex = 0;  //kcp操作码处在包第0个位置
        public const int OpcodeLength = 2;    //操作码固定长度为2 因为是ushort是2个字节
        public const int ActorIdIndex = 0;    //actorId从第0位开始读取
        public const int ActorIdLength = 8;   //actor长度为8 long是8个字节
        public const int MessageIndex = 10;   //消息体从第10位开始读取

        public ushort Opcode;
        public long ActorId;
        public MemoryStream MemoryStream;
    }
}