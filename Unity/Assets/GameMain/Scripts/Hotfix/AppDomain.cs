using GameFramework;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Game.Hotfix
{
    public class AppDomain
    {
        public static void Start()
        {
            Debug.Log("App Domain Start!");
            
            //删除之前构建的状态机 重新构建热更流程
            GameEntry.Fsm.DestroyFsm<IProcedureManager>();
            IProcedureManager procedureManager = GameFrameworkEntry.GetModule<IProcedureManager>();
            IFsmManager fsmManager = GameFrameworkEntry.GetModule<IFsmManager>();
            procedureManager.Initialize(fsmManager, new ProcedureBase[]
            {
                new ProcedurePreload(),
                new ProcedureTest(),
            });

            //LubanHelp.Init();
            
            procedureManager.StartProcedure<ProcedurePreload>();
        }
    }
}
