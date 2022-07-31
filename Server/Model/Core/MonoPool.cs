using System;
using System.Collections.Generic;

namespace Game
{
    public class MonoPool: IDisposable
    {
        private readonly Dictionary<Type, Queue<object>> m_Pool = new Dictionary<Type, Queue<object>>();
        
        public static MonoPool Instance = new MonoPool();
        
        private MonoPool()
        {
        }

        public object Fetch(Type type)
        {
            Queue<object> queue = null;
            if (!m_Pool.TryGetValue(type, out queue))
            {
                return Activator.CreateInstance(type);
            }

            if (queue.Count == 0)
            {
                return Activator.CreateInstance(type);
            }
            return queue.Dequeue();
        }

        public void Recycle(object obj)
        {
            Type type = obj.GetType();
            Queue<object> queue = null;
            if (!m_Pool.TryGetValue(type, out queue))
            {
                queue = new Queue<object>();
                m_Pool.Add(type, queue);
            }
            queue.Enqueue(obj);
        }

        public void Dispose()
        {
            this.m_Pool.Clear();
        }
    }
}