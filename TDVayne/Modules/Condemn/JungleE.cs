using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using TDVayne.Skills.Tumble;
using TDVayne.Utility;
using TDVayne.Utility.Entities;

namespace TDVayne.Modules.Condemn
{
    internal class JungleE : ISOLOModule
    {
        /// <summary>
        ///     S
        ///     Called when the module gets loaded.
        /// </summary>
        public void OnLoad()
        {
        }

        /// <summary>
        ///     Shoulds the module get executed.
        /// </summary>
        /// <returns></returns>
        public bool ShouldGetExecuted()
        {
            return MenuGenerator.FarmMenu["TDVaynelaneclearcondemnjungle"].Cast<CheckBox>().CurrentValue
                   && Variables.E.IsReady()
                   && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear)
                   && ObjectManager.Player.ManaPercent >= 40;
        }

        /// <summary>
        ///     Gets the type of the module.
        /// </summary>
        /// <returns></returns>
        public ModuleType GetModuleType()
        {
            return ModuleType.OnUpdate;
        }

        /// <summary>
        ///     Called when the module gets executed.
        /// </summary>
        public void OnExecute()
        {
            var currentTarget = Orbwalker.GetTarget();

            if (currentTarget is Obj_AI_Minion && GameObjects.JungleLarge.Contains(currentTarget))
            {
                for (var i = 0; i < 450; i += 65)
                {
                    var endPos = currentTarget.Position.Extend(ObjectManager.Player.ServerPosition, -i);

                    if (endPos.IsWall())
                    {
                        Variables.E.Cast(currentTarget as Obj_AI_Base);
                        return;
                    }
                }
            }
        }
    }
}