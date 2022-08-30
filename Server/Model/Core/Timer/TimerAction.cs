namespace ET 
{
    public enum TimerClass
    {
        None,
        /// <summary>执行一次</summary>
        OnceTimer,      
        /// <summary>执行等待一次</summary>
        OnceWaitTimer,  
        /// <summary>重复执行</summary>
        RepeatedTimer,  
    }
    
    [ObjectSystem]
    public class TimerActionAwakeSystem: AwakeSystem<TimerAction, TimerClass, long, int, object>
    {
        public override void Awake(TimerAction self, TimerClass timerClass, long time, int type, object obj)
        {
            self.TimerClass = timerClass;
            self.Object = obj;
            self.Time = time;
            self.Type = type;
        }
    }

    [ObjectSystem]
    public class TimerActionDestroySystem: DestroySystem<TimerAction>
    {
        public override void Destroy(TimerAction self)
        {
            self.Object = null;
            self.Time = 0;
            self.TimerClass = TimerClass.None;
            self.Type = 0;
        }
    }
    
    public class TimerAction: Entity, IAwake<TimerClass, long, int, object>, IDestroy
    {
        public TimerClass TimerClass; //计时器类型
        
        public long Time;

        public int Type;     //计时器下标id索引
        
        public object Object;
    }
}