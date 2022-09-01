using System;
using System.Collections.Generic;

namespace ET 
{
    [ObjectSystem]
    public class OpcodeTypeComponentAwakeSystem : AwakeSystem<OpcodeTypeComponent>
    {
        public override void Awake(OpcodeTypeComponent self) 
        {
            OpcodeTypeComponent.Instance = self;
            
            List<Type> types = Game.EventSystem.GetTypes(typeof (MessageAttribute));
            foreach (Type type in types) 
            {
                //获取所有派生于该类型的类
                object[] attrs = type.GetCustomAttributes(typeof(MessageAttribute), false);
                if (attrs.Length == 0) 
                {
                    continue;
                }
                
                MessageAttribute messageAttribute = attrs[0] as MessageAttribute;
                if (messageAttribute == null) 
                {
                    continue;
                }
                
                self.OpcodeTypes.Add(messageAttribute.Opcode,type);
                self.TypeOpcodes.Add(type,messageAttribute.Opcode);
                
                //如果当前操作码是其他操作码 并且是内网消息就注册成outr
                if (OpcodeHelper.IsOuterMessage(messageAttribute.Opcode) && typeof (IActorMessage).IsAssignableFrom(type))
                {
                    self.OutrActorMessage.Add(messageAttribute.Opcode);
                }

                //如果该类型是派生于请求接口
                if (typeof(IRequest).IsAssignableFrom(type)) 
                {
                    //如果该类型还派生于内网消息
                    if (typeof (IActorLocationMessage).IsAssignableFrom(type))
                    {
                        self.RequestResponse.Add(type, typeof(ActorResponse));
                        continue;
                    }
                    
                    //记录一下回复特性  也就是回复该类型的类型
                    attrs = type.GetCustomAttributes(typeof (ResponseTypeAttribute), false);
                    if (attrs.Length == 0)
                    {
                        Log.Error($"[OpcodeTypeComponentAwakeSystem.Awake] not found responseType: {type}");
                        continue;
                    }
                    
                    //符合要求 把 请求跟回复 保存进入键值对
                    ResponseTypeAttribute responseTypeAttribute = attrs[0] as ResponseTypeAttribute;
                    self.RequestResponse.Add(type, Game.EventSystem.GetType($"ET.{responseTypeAttribute.Type}"));
                }
            }
        }
    }
    
    [ObjectSystem]
    public class OpcodeTypeComponentDestroySystem: DestroySystem<OpcodeTypeComponent>
    {
        public override void Destroy(OpcodeTypeComponent self)
        {
            OpcodeTypeComponent.Instance = null;
        }
    }

    [FriendClass(typeof(OpcodeTypeComponent))]
    public static class OpcodeTypeComponentSystem 
    {
        public static bool IsOutrActorMessage(this OpcodeTypeComponent self, ushort opcode)
        {
            return self.OutrActorMessage.Contains(opcode);
        }

        public static ushort GetOpcode(this OpcodeTypeComponent self, Type type)
        {
            return self.TypeOpcodes[type];
        }

        public static Type GetType(this OpcodeTypeComponent self, ushort opcode)
        {
            return self.OpcodeTypes[opcode];
        }

        public static Type GetResponseType(this OpcodeTypeComponent self, Type request)
        {
            if (!self.RequestResponse.TryGetValue(request, out Type response))
            {
                throw new Exception($"not found response type, request type: {request.GetType().Name}");
            }
            return response;
        }
    }
    
    [ComponentOf(typeof(Scene))]
    public class OpcodeTypeComponent : Entity,IAwake,IDestroy
    {
        public static OpcodeTypeComponent Instance;

        //内网Actor消息集合
        public HashSet<ushort> OutrActorMessage = new HashSet<ushort>();

        //key=操作码 value=类型
        public readonly Dictionary<ushort, Type> OpcodeTypes = new Dictionary<ushort, Type>();
        
        //key=类型 value=操作码
        public readonly Dictionary<Type, ushort> TypeOpcodes = new Dictionary<Type, ushort>();
        
        //消息请求响应键值对 key=请求类型 value=回复类型
        public readonly Dictionary<Type, Type> RequestResponse = new Dictionary<Type, Type>();
    }
}