using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using TDVayne.Skills.Condemn;
using TDVayne.Utility;
using TDVayne.Utility.Entities;
using TDVayne.Utility.Enums;

namespace TDVayne.Skills.Tumble
{
    internal class Condemn : Skill
    {
        public CondemnLogicProvider Provider = new CondemnLogicProvider();

        /// <summary>
        ///     Gets the skill mode.
        /// </summary>
        /// <returns></returns>
        public SkillMode GetSkillMode()
        {
            return SkillMode.OnUpdate;
        }

        /// <summary>
        ///     Executes logic given the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        public void Execute(Obj_AI_Base target)
        {
            try
            {
                if (Variables.E.IsReady() && MenuGenerator.EMenu["useecombo"].Cast<CheckBox>().CurrentValue)
                {
                    var CondemnTarget = CondemnLogicProvider.GetCondemnableTarget();
                    if (target != null)
                    {
                        Variables.E.Cast(CondemnTarget);
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        /// <summary>
        ///     Executes the farm logic.
        /// </summary>
        /// <param name="target">The target.</param>
        public void ExecuteFarm(Obj_AI_Base target)
        {
            if (target is Obj_AI_Minion
                && ObjectManager.Player.ManaPercent > 40
                && ObjectManager.Player.CountEnemiesInRange(2000f) == 0 && MenuGenerator.FarmMenu["useqfarm"].Cast<CheckBox>().CurrentValue)
            {
                if (GameObjects.JungleLarge.Contains(target) &&
                    Provider.IsCondemnable(target, ObjectManager.Player.ServerPosition))
                {
                    Variables.E.Cast(target);
                }
            }
        }
    }
}