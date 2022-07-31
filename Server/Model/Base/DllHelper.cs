using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Game
{
    public static class DllHelper
    {
        private static AssemblyLoadContext s_AssemblyLoadContext;

        public static Assembly GetHotfixAssembly()
        {
            s_AssemblyLoadContext?.Unload();
            System.GC.Collect();
            s_AssemblyLoadContext = new AssemblyLoadContext("Hotfix",true);
            byte[] dllBytes = File.ReadAllBytes("./Server.Hotfix.dll");
            byte[] pdbBytes = File.ReadAllBytes("./Server.Hotfix.pdb");
            Assembly assembly = s_AssemblyLoadContext.LoadFromStream(new MemoryStream(dllBytes),new MemoryStream(pdbBytes));
            return assembly;
        }
    }
}