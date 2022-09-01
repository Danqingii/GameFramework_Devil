using System;
using System.IO;

namespace ET 
{
    public enum ChannelType
    {
        Connect,
        Accept,
    }

    

    public abstract class AChannel: IDisposable 
    {
        public abstract void Dispose();
    }
}