namespace ET
{
    public interface ITimer 
    {
        void Handle(object ages);
    }

    public abstract class ATimer<T> : ITimer where T : class 
    {
        public void Handle(object ages) 
        {
            Run(ages as T);
        }

        public abstract void Run(T t);
    }
}