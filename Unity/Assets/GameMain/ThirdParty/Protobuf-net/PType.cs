using System;

namespace ProtoBuf
{
    public static class PType
    {
        private static Func<string, Type> getILRuntimeTypeFunc;

        public static Type FindType(string metaName)
        {
            if (getILRuntimeTypeFunc == null)
            {
                return null;
            }
            return getILRuntimeTypeFunc.Invoke(metaName);
        }

        public static object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type);
        }

        public static Type GetPType(object o)
        {
            return o.GetType();
        }
    }
}