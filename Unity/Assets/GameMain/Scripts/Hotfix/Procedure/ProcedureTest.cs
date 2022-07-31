using GameFramework;
using GameFramework.Event;
using GameFramework.Resource;
using System.Collections.Generic;
using GameFramework.Procedure;
using UnityEngine;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace Game.Hotfix
{
    public class ProcedureTest : ProcedureBase
    {
        public override bool UseNativeDialog { get; }

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Debug("进入测试流程");

            foreach (var item in Config.Tables.TbItem.DataList)
            {
                Log.Debug($"{item.Id}/{item.Name}/{item.Desc}");
            }
        }
    }
}