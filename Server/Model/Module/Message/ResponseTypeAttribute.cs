using System;

namespace ET
{
    /// <summary>
    /// 回复消息特性
    /// </summary>
    public class ResponseTypeAttribute: BaseAttribute
    {
        public string Type { get; }

        public ResponseTypeAttribute(string type)
        {
            this.Type = type;
        }
    }
}