using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ET
{
	[ObjectSystem]
    public class ConfigAwakeSystem : AwakeSystem<ConfigComponent>
    {
        public override void Awake(ConfigComponent self)
        {
	        ConfigComponent.Instance = self;
	        self.ConfigLoader = new LubanConfigLoader();
        }
    }
    
    [ObjectSystem]
    public class ConfigDestroySystem : DestroySystem<ConfigComponent>
    {
	    public override void Destroy(ConfigComponent self)
	    {
		    self.ConfigLoader = null;
		    ConfigComponent.Instance = null;
	    }
    }
    
    [FriendClass(typeof(ConfigComponent))]
    public static class ConfigComponentSystem
	{
		public static async ETTask LoadAsync(this ConfigComponent self)
		{
			await self.ConfigLoader.LoadConfig();
		}
	}
}