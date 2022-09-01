using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace ET 
{
    [ObjectSystem]
    public class SessionAwakeSystem: AwakeSystem<Session, AService>
    {
        public override void Awake(Session self, AService aService)
        {
            self.AService = aService;
            long timeNow = TimeHelper.ClientNow();
            self.LastRecvTime = timeNow;
            self.LastSendTime = timeNow;
            self.RequestCallbacks.Clear();
            
            Log.Info($"[SessionAwakeSystem.Awake] session create: zone: {self.DomainZone()} id: {self.Id} {timeNow} ");
        }
    }
    
    [ObjectSystem]
    public class SessionDestroySystem: DestroySystem<Session>
    {
        public override void Destroy(Session self)
        {
            self.AService.RemoveChannel(self.Id);
            
            foreach (RpcInfo responseCallback in self.RequestCallbacks.Values.ToArray())
            {
                responseCallback.Tcs.SetException(new RpcException(self.Error, $"session dispose: {self.Id} {self.RemoteAddress}"));
            }

            Log.Info($"[SessionDestroySystem.Destroy] session dispose: {self.RemoteAddress} id: {self.Id} ErrorCode: {self.Error}, please see ErrorCode.cs! {TimeHelper.ClientNow()}");
            
            self.RequestCallbacks.Clear();
        }
    }
    
    [FriendClass(typeof(Session))]
    public static class SessionSystem 
    {
        public static void OnRead(this Session self, ushort opcode, IResponse response)
        {
            OpcodeHelper.LogMsg(self.DomainZone(), opcode, response);
            
            if (!self.RequestCallbacks.TryGetValue(response.RpcId, out var action))
            {
                return;
            }

            self.RequestCallbacks.Remove(response.RpcId);
            if (ErrorCore.IsRpcNeedThrowException(response.Error))
            {
                action.Tcs.SetException(new Exception($"[SessionSystem.OnRead] Rpc error, request: {action.Request} response: {response}"));
                return;
            }
            action.Tcs.SetResult(response);
        }
        
        public static async ETTask<IResponse> Call(this Session self, IRequest request, ETCancellationToken cancellationToken)
        {
            int rpcId = ++Session.RpcId;
            RpcInfo rpcInfo = new RpcInfo(request);
            self.RequestCallbacks[rpcId] = rpcInfo;
            request.RpcId = rpcId;

            self.Send(request);
            
            //c#闭包写法 token令牌
            void CancelAction()
            {
                if (!self.RequestCallbacks.TryGetValue(rpcId, out RpcInfo action))
                {
                    return;
                }

                //如果调用了取消令牌,直接创建一个回复类来返回,不执行我们保存的回复类
                self.RequestCallbacks.Remove(rpcId);
                Type responseType = OpcodeTypeComponent.Instance.GetResponseType(action.Request.GetType());
                IResponse response = (IResponse) Activator.CreateInstance(responseType);
                response.Error = ErrorCore.ERR_Cancel;
                action.Tcs.SetResult(response);
            }

            IResponse ret;
            try
            {
                cancellationToken?.Add(CancelAction);
                ret = await rpcInfo.Tcs;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);
            }
            return ret;
        }
        
        public static async ETTask<IResponse> Call(this Session self, IRequest request)
        {
            int rpcId = ++Session.RpcId;
            RpcInfo rpcInfo = new RpcInfo(request);
            self.RequestCallbacks[rpcId] = rpcInfo;
            request.RpcId = rpcId;
            self.Send(request);
            return await rpcInfo.Tcs;
        }
        
        public static void Reply(this Session self, IResponse message)
        {
            self.Send(0, message);
        }

        public static void Send(this Session self, IMessage message)
        {
            self.Send(0, message);
        }
        
        public static void Send(this Session self, long actorId, IMessage message)
        {
            //序列化过程! 非常重要,决定了解包过程
            (ushort opcode, MemoryStream stream) = MessageSerializeHelper.MessageToStream(message);
            OpcodeHelper.LogMsg(self.DomainZone(), opcode, message);
            self.Send(actorId, stream);
        }
        
        public static void Send(this Session self, long actorId, MemoryStream memoryStream)
        {
            self.LastSendTime = TimeHelper.ClientNow();
            self.AService.SendStream(self.Id, actorId, memoryStream);
        }
    }
    
    public readonly struct RpcInfo
    {
        public readonly IRequest Request;
        public readonly ETTask<IResponse> Tcs;

        public RpcInfo(IRequest request)
        {
            this.Request = request;
            this.Tcs = ETTask<IResponse>.Create(true);
        }
    }
    
    public class Session : Entity, IAwake<AService>, IDestroy 
    {
        public AService AService;
        
        //每个包都有一个RpcId 这个id在发送的时候是自增的
        public static int RpcId
        {
            get;
            set;
        }

        //key=RpcId value=回复包键值对,含有一个请求包和一个回复包
        public readonly Dictionary<int, RpcInfo> RequestCallbacks = new Dictionary<int, RpcInfo>();

        //上一次接收时间
        public long LastRecvTime;

        //上一次发送时间
        public long LastSendTime;

        public int Error;

        //远程地址
        public IPEndPoint RemoteAddress;
    }
}