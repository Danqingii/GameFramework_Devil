using System;
using System.Collections.Generic;
using Cfg;

namespace ET
{
    /// <summary>
    /// ET的 Config组件会扫描所有的有ConfigAttribute标签的配置,加载进来
    /// 而Luban直接配置生成就已经记录就全部的类型了 所以不用配置特效
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ConfigComponent: Entity, IAwake, IDestroy
    {
        public static ConfigComponent Instance;
        public Tables Tables;
    }
}