using Bright.Serialization;
using cfg;
using ET;

namespace Game.Hotfix
{
    /// <summary>
    /// 控制热更流程的自定义组件
    /// </summary>
    public class Launcher
    {
        private static Tables m_Tables;
        public static Tables Tables => m_Tables;

        static Launcher()
        {
            m_Tables = new Tables();
        }
        
        public static async ETTask LoadConfig()
        {
            if (m_Tables != null)
            {
                return;
            }
            
            m_Tables = new Tables();

            async ETTask<ByteBuf> Load(string file)
            {
                await ETTask.CompletedTask;
#if UNITY_EDITOR
                return new ByteBuf(System.IO.File.ReadAllBytes($"Assets/GameMain/DataTables/Bin/{file}.bytes"));
#else
               //TODO 使用GF加载 
                return null;
#endif
            }

            await m_Tables.LoadAsync(Load);
        }
    }
}