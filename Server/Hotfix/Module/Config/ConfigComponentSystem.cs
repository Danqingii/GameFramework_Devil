using System.IO;
using Bright.Serialization;
using Cfg;

namespace ET
{
	[ObjectSystem]
    public class ConfigAwakeSystem : AwakeSystem<ConfigComponent>
    {
        public override void Awake(ConfigComponent self)
        {
	        ConfigComponent.Instance = self;
	        self.Tables = new Tables();
        }
    }
    
    [ObjectSystem]
    public class ConfigDestroySystem : DestroySystem<ConfigComponent>
    {
	    public override void Destroy(ConfigComponent self)
	    {
		    self.Tables = null;
		    ConfigComponent.Instance = null;
	    }
    }
    
    [FriendClass(typeof(ConfigComponent))]
    public static class ConfigComponentSystem
	{
		public static async ETTask LoadAsync(this ConfigComponent self)
		{
			await self.Tables.LoadAsync(Load);
		}
		
		private static async ETTask<ByteBuf> Load(string file) {
			byte[] bytes = await File.ReadAllBytesAsync($"../ServerConfig/Bin/{file}.bytes");
			return new ByteBuf(bytes);
		}
	}
}