using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using TDVayne.Skills.Condemn;
using TDVayne.Skills.Tumble;
using TDVayne.Utility;

namespace TDVayne.Modules.Condemn
{
    internal class AutoE : ISOLOModule
    {
        private readonly CondemnLogicProvider MyProvider = new CondemnLogicProvider();

        /// <summary>
        ///     Called when the module is loaded.
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
            return MenuGenerator.EMenu["TDVaynemisccondemnautoe"].Cast<CheckBox>().CurrentValue &&
                   Variables.E.IsReady() && (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo));
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
        ///     Called when the modules gets executed.
        /// </summary>
        public void OnExecute()
        {
            foreach (var target in EntityManager.Heroes.Enemies.Where(h => h.IsValidTarget(Variables.E.Range) && CondemnLogicProvider.IsCondemnable(h)))
            {
                Variables.E.Cast(target);
                
            }
        }
    }
}