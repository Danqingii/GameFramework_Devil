using System;
using System.Collections.Generic;

namespace ET 
{
    [ObjectSystem]
    public class TimerComponentAwakeSystem : AwakeSystem<TimerComponent>
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

            int count = self.EveryFrameTimeIdr.Count;
            for (int i = 0; i < count; i++) {
                long timerId = self.EveryFrameTimeIdr.Dequeue();
                TimerAction timerAction = self.GetChild<TimerAction>(timerId);
                if (timerAction == null) 
                {
                    continue;
                }
                self.Run(timerAction);
            }
            
            #endregion
            
            if (self.TimeId.Count == 0) {
                return;
            }
            
            self.TimeNow = TimeHelper.ServerNow();
            
            //这一步就是核查全部的Time 合法性
            self.TimeId.ForEachFunc(self.ForeachFunc);

            while (self.TimeOutTime.Count > 0) 
            {
                long time = self.TimeOutTime.Dequeue();
                List<long> list = self.TimeId[time];
                for (int i = 0; i < list.Count; i++) 
                {
                    long timerId = list[i];
                    self.TimeOutTimerIds.Enqueue(timerId);  //找到具体的全部的定时器
                }

                self.TimeId.Remove(time);
            }

            while (self.TimeOutTimerIds.Count > 0) 
            {
                long timerId = self.TimeOutTimerIds.Dequeue();
                TimerAction timerAction = self.GetChild<TimerAction>(timerId);
                if (timerAction == null) 
                {
                    continue;
                }
                self.Run(timerAction);
            }
        }
    }
    
    [ObjectSystem]
    public class TimerComponentLoadSystem : LoadSystem<TimerComponent>
    {
        public override void Load(TimerComponent self)
        {
            self.Init();
        }
    }

    [ObjectSystem]
    public class TimerComponentDestroySystem : DestroySystem<TimerComponent>
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
                    int index = timerAction.Type;
                    long tillTime = TimeHelper.ServerNow() + timerAction.Time;
                    self.AddTimer(tillTime, timerAction);
                    
                    ITimer timer = self.TimerActions[index];
                    if (timer == null)
                    {
                        Log.Error($"[TimerComponentSystem.Run] not found timer action: {index}");
                        return;
                    }
                    timer.Handle(timerAction.Object);
                    break;
                }
            }
        }

        private static void AddTimer(this TimerComponent self,long tillTime, TimerAction timerAction) 
        {
            if (timerAction.TimerClass == TimerClass.RepeatedTimer && timerAction.Time == 0) 
            {
                self.EveryFrameTimeIdr.Enqueue(timerAction.Id);
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

        public static async ETTask<bool> WaitTillAsync(this TimerComponent self, long tillTime, ETCancellationToken cancellationToken = null)
        {
            //如果当前的时间已经大于了  直到时间
            if (self.TimeNow >= tillTime) 
            {
                return true;
            }
            
            ETTask<bool> tcs = ETTask<bool>.Create(true);
            TimerAction timer = self.AddChild<TimerAction, TimerClass, long, int, object>(TimerClass.OnceWaitTimer, tillTime - self.TimeNow, 0, tcs, true);
            self.AddTimer(tillTime,timer);
            long timerId = timer.Id;
            
            //c#闭包写法 该方法为取消函数token
            void CancelAction()
            {
                if (self.Remove(timerId))
                {
                    tcs.SetResult(false);
                }
            }
            
            //运行中如果token不为空 我们就把取消函数注册进入令牌
            //那么如果外界主动调用token 我们就会执行取消函数,函数会直接返回false,即不等待了
            bool ret;
            try
            {
                cancellationToken?.Add(CancelAction);
                ret = await tcs;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);    
            }
            return ret;
        }
        
        public static async ETTask<bool> WaitFrameAsync(this TimerComponent self, ETCancellationToken cancellationToken = null)
        {
            bool ret = await self.WaitAsync(1, cancellationToken);
            return ret;
        }
        
        public static async ETTask<bool> WaitAsync(this TimerComponent self, long time, ETCancellationToken cancellationToken = null)
        {
            if (time == 0)
            {
                return true;
            }
            long tillTime = TimeHelper.ServerNow() + time;

            ETTask<bool> tcs = ETTask<bool>.Create(true);
            
            TimerAction timer = self.AddChild<TimerAction, TimerClass, long, int, object>(TimerClass.OnceWaitTimer, time, 0, tcs, true);
            self.AddTimer(tillTime, timer);
            long timerId = timer.Id;

            //c#闭包写法 该方法为取消函数token
            void CancelAction()
            {
                if (self.Remove(timerId))
                {
                    tcs.SetResult(false);
                }
            }
            
            //运行中如果token不为空 我们就把取消函数注册进入令牌
            //那么如果外界主动调用token 我们就会执行取消函数,函数会直接返回false,即不等待了
            bool ret;
            try
            {
                cancellationToken?.Add(CancelAction);
                ret = await tcs;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);    
            }
            return ret;
        }
        
        // 用这个优点是可以热更，缺点是回调式的写法，逻辑不连贯。WaitTillAsync不能热更，优点是逻辑连贯。
        // wait时间短并且逻辑需要连贯的建议WaitTillAsync
        // wait时间长不需要逻辑连贯的建议用NewOnceTimer
        public static long NewOnceTimer(this TimerComponent self, long tillTime, int type, object args)
        {
            if (tillTime < TimeHelper.ServerNow())
            {
                Log.Warning($"[TimerComponentSystem.NewOnceTimer] new once time too small: {tillTime}");
            }
            TimerAction timer = self.AddChild<TimerAction, TimerClass, long, int, object>(TimerClass.OnceTimer, tillTime, type, args, true);
            self.AddTimer(tillTime, timer);
            return timer.Id;
        }
        
        public static long NewFrameTimer(this TimerComponent self, int type, object args)
        {
#if NOT_UNITY
            return self.NewRepeatedTimerInner(100, type, args);
#else
            return self.NewRepeatedTimerInner(0, type, args);
#endif
        }

        /// <summary>
        /// 创建一个RepeatedTimer
        /// </summary>
        private static long NewRepeatedTimerInner(this TimerComponent self, long time, int type, object args)
        {
#if NOT_UNITY
            if (time < 100)
            { 
                throw new Exception($"[TimerComponentSystem.NewRepeatedTimerInner] repeated timer < 100, timerType: time: {time}");
            }
#endif
            long tillTime = TimeHelper.ServerNow() + time;
            TimerAction timer = self.AddChild<TimerAction, TimerClass, long, int, object>(TimerClass.RepeatedTimer, time, type, args, true);

            // 每帧执行的不用加到timerId中，防止遍历
            self.AddTimer(tillTime, timer);
            return timer.Id;
        }

        public static long NewRepeatedTimer(this TimerComponent self, long time, int type, object args)
        {
            if (time < 100)
            {
                Log.Error($"[TimerComponentSystem.NewRepeatedTimer] time too small: {time}");
                return 0;
            }
            return self.NewRepeatedTimerInner(time, type, args);
        }
    }

    [ComponentOf(typeof(Scene))]
    public class TimerComponent : Entity,IAwake,IUpdate,ILoad,IDestroy 
    {
        public static TimerComponent Instance;

        public long TimeNow; //服务器现在时间
        public Func<long, List<long>, bool> ForeachFunc; //迭代器
        
        //全部计时器 key:time  value:list<timerId>
        public readonly MultiMap<long, long> TimeId = new MultiMap<long, long>();

        //全部计时器key 不包括每帧运行的
        public readonly Queue<long> TimeOutTime = new Queue<long>();

        //全部timerId  不包括每帧运行的
        public readonly Queue<long> TimeOutTimerIds = new Queue<long>();
        
        //每帧执行的timerId
        public readonly Queue<long> EveryFrameTimeIdr = new Queue<long>();

        // 记录最小时间，不用每次都去MultiMap取第一个值
        public long MinTime;

        public const int TimeTypeMax = 10000;

        //index=TimerType  value=定时器
        public ITimer[] TimerActions;
    }
}