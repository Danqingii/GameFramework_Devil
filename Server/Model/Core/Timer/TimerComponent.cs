using System;
using System.Collections.Generic;

namespace ET 
{
    public class TimerComponentAwakeSystem:AwakeSystem<TimerComponent>
    {
        public override void Awake(TimerComponent self) 
        {
            TimerComponent.Instance = self;
            self.Init();
        }
    }

    [ObjectSystem]
    public class TimerComponentUpdateSystem : UpdateSystem<TimerComponent> 
    {
        public override void Update(TimerComponent self) 
        {
            #region 每帧执行的timer，不用foreach TimeId，减少GC

            int count = self.EveryFrameTimer.Count;
            for (int i = 0; i < count; i++) {
                long timerId = self.EveryFrameTimer.Dequeue();
                TimerAction timerAction = self.GetChild<TimerAction>(timerId);
                if (timerAction == null) {
                    continue;
                }
                self.Run(timerAction);
            }

            #endregion
        }
    }
    
    [ObjectSystem]
    public class TimerComponentLoadSystem: LoadSystem<TimerComponent>
    {
        public override void Load(TimerComponent self)
        {
            self.Init();
        }
    }

    [ObjectSystem]
    public class TimerComponentDestroySystem: DestroySystem<TimerComponent>
    {
        public override void Destroy(TimerComponent self)
        {
            TimerComponent.Instance = null;
        }
    }

    public static class TimerComponentSystem 
    {
        public static void Init(this TimerComponent self) 
        {
            self.ForeachFunc = (time,list) => 
            {
                if (time > self.TimeNow) {
                    self.MinTime = time;
                    return false;
                }

                self.TimeOutTime.Enqueue(time);
                return true;
            };

            self.TimerActions = new ITimer[TimerComponent.TimeTypeMax];
            
            List<Type> types = Game.EventSystem.GetTypes(typeof (TimerAttribute));

            foreach (Type type in types) 
            {
                ITimer iTimer = Activator.CreateInstance(type) as ITimer;
                if (iTimer == null) 
                {
                    Log.Error($"[TimerComponentSystem.Init] 计时器 {type.Name} 需要继承 ITimer");
                    continue;
                }
                
                object[] attrs = type.GetCustomAttributes(typeof(TimerAttribute), false);
                if (attrs.Length == 0)
                {
                    continue;
                }

                foreach (object attr in attrs)
                {
                    TimerAttribute timerAttribute = attr as TimerAttribute;
                    self.TimerActions[timerAttribute.Type] = iTimer;
                }
            }
        }

        public static void Run(this TimerComponent self, TimerAction timerAction) 
        {
            switch (timerAction.TimerClass) 
            {
                case TimerClass.OnceTimer: 
                {
                    int index = timerAction.Type;
                    ITimer timer = self.TimerActions[index];
                    if (timer == null)
                    {
                        Log.Error($"[TimerComponentSystem.Run] not found timer action: {index}");
                        return;
                    }
                    timer.Handle(timerAction.Object);
                    break;
                }
                case TimerClass.OnceWaitTimer: 
                {
                    ETTask<bool> tcs = timerAction.Object as ETTask<bool>;
                    self.Remove(timerAction.Id);
                    tcs.SetResult(true);    
                    break;
                }
                case TimerClass.RepeatedTimer: 
                {
                    int type = timerAction.Type;
                    long tillTime = TimeHelper.ServerNow() + timerAction.Time;
                    self.AddTimer(tillTime, timerAction);
                    break;
                }
            }
        }

        private static void AddTimer(this TimerComponent self,long tillTime, TimerAction timerAction) 
        {
            if (timerAction.TimerClass == TimerClass.RepeatedTimer && timerAction.Time == 0) 
            {
                self.EveryFrameTimer.Enqueue(timerAction.Id);
                return;
            }
            self.TimeId.Add(tillTime, timerAction.Id);
            if (tillTime < self.MinTime)
            {
                self.MinTime = tillTime;
            }
        }
        
        public static bool Remove(this TimerComponent self, ref long id)
        {
            long i = id;
            id = 0;
            return self.Remove(i);
        }

        private static bool Remove(this TimerComponent self, long id) 
        {
            if (id == 0) 
            {
                return false;
            }
            
            TimerAction timerAction = self.GetChild<TimerAction>(id);
            if (timerAction == null)
            {
                return false;
            }
            timerAction.Dispose();
            return true;
        }
    }

    [ComponentOf(typeof(Scene))]
    public class TimerComponent : Entity,IAwake,IUpdate,ILoad,IDestroy 
    {
        public static TimerComponent Instance;

        public float TimeNow; //现在时间
        public Func<long, List<long>, bool> ForeachFunc; //迭代器
        
        /// <summary>
        /// key: time, value: timerAction.id
        /// </summary>
        public readonly MultiMap<long, long> TimeId = new MultiMap<long, long>();

        public readonly Queue<long> TimeOutTime = new Queue<long>();

        public readonly Queue<long> TimeOutTimerIds = new Queue<long>();
        
        public readonly Queue<long> EveryFrameTimer = new Queue<long>();

        // 记录最小时间，不用每次都去MultiMap取第一个值
        public long MinTime;

        public const int TimeTypeMax = 10000;

        //index=TimerType  value=定时器
        public ITimer[] TimerActions;
    }
}