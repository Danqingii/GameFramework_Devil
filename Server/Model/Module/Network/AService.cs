using System;
using System.IO;
using System.Net;

namespace ET 
{
    public enum ServiceType 
    {
        Outer,
        Inner,
    }
    
    public abstract class AService: IDisposable 
    {
        public ServiceType ServiceType { get; protected set; }
        
        public ThreadSynchronizationContext ThreadSynchronizationContext;
        
        // localConn放在低32bit
        private long connectIdGenerater = int.MaxValue;
        public long CreateConnectChannelId(uint localConn)
        {
            return (--this.connectIdGenerater << 32) | localConn;
        }
        
        public uint CreateRandomLocalConn()
        {
            return (1u << 30) | RandomHelper.RandUInt32();
        }
        
        // localConn放在低32bit
        private long acceptIdGenerater = 1;
        public long CreateAcceptChannelId(uint localConn)
        {
            return (++this.acceptIdGenerater << 32) | localConn;
        }
        
        //在NetThreadComponentUpdate组件中LateUpdate每帧轮询自身服务的Update
        public abstract void Update();

        public abstract void Remove(long id);
        
        public abstract bool IsDispose();

        protected abstract void Get(long id, IPEndPoint address);

        public abstract void Dispose();

        protected abstract void Send(long channelId, long actorId, MemoryStream stream);
        
        
        public Action<long, IPEndPoint> AcceptCallback; //连接回调
        public Action<long, MemoryStream> ReadCallback; //读取回调
        public Action<long, int> ErrorCallback;         //错误回调
        
        protected void OnAccept(long channelId, IPEndPoint ipEndPoint)
        {
            this.AcceptCallback.Invoke(channelId, ipEndPoint);
        }

        public void OnRead(long channelId, MemoryStream memoryStream)
        {
            this.ReadCallback.Invoke(channelId, memoryStream);
        }

        public void OnError(long channelId, int e)
        {
            this.Remove(channelId);
            this.ErrorCallback?.Invoke(channelId, e);
        }
        
        
        public void Destroy()
        {
            this.Dispose();
        }

        public void RemoveChannel(long channelId)
        {
            this.Remove(channelId);
        }

        public void SendStream(long channelId, long actorId, MemoryStream stream)
        {
            this.Send(channelId, actorId, stream);
        }

        public void GetOrCreate(long id, IPEndPoint address)
        {
            this.Get(id, address);
        }
    }
}