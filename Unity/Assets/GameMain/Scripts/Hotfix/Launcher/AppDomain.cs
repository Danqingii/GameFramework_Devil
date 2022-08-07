using System;
using GameFramework;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityEngine;
using UnityGameFramework.Runtime;
using GameEntry = Game.Mono.GameEntry;
using ProcedureBase = Game.Mono.ProcedureBase;

namespace Game.Hotfix
{
    public class AppDomain
    {
        public Action OnStart;
        
        public static void Start()
        {
            Debug.Log("App Domain Start!");

            //删除之前构建的Mono流程 重新构建热更流程  重启也只是重启热更新流程机
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
