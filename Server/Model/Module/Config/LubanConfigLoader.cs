using System.Collections.Generic;
using System.IO;
using Bright.Serialization;
using Cfg;

namespace ET 
{
    public class LubanConfigLoader 
    {
        private Tables m_Tables;
        public async ETTask LoadConfig()
        {
            if (m_Tables != null)
            {
                return;
            }
            
            m_Tables = new Tables();
            await m_Tables.LoadAsync(Load);
        }
        
        private async ETTask<ByteBuf> Load(string file) {
            byte[] bytes = await File.ReadAllBytesAsync($"../ServerConfig/Bin/{file}.bytes");
            return new ByteBuf(bytes);
        }
    }
}