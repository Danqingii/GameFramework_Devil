using System;

namespace ET {
    
    /// <summary>
    /// 消息码特性
    /// </summary>
    public class MessageAttribute : Attribute 
    {
        public ushort Opcode 
        {
            get;
        }

        public MessageAttribute(ushort opcode) 
        {
            this.Opcode = opcode;
        }
    }
}