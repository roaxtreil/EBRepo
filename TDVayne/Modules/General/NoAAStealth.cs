using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using TDVayne.Skills.Tumble;
using TDVayne.Utility;

namespace TDVayne.Modules.General
{
    internal class NoAAStealth : ISOLOModule
    {
        /// <summary>
        ///     Called when the module is loaded.
        /// </summary>
        public void OnLoad()
        {
            Orbwalker.OnPreAttack += BeforeAttack;
        }

        /// <summary>
        ///     Shoulds the module get executed.
        /// </summary>
        /// <returns></returns>
        public bool ShouldGetExecuted()
        {
            return ObjectManager.Player.GetEnemiesInRange(1200f).Any()
                   && Player.HasBuff("vaynetumblefade")
                   && MenuGenerator.MiscMenu["TDVaynemiscmiscellaneousnoaastealth"].Cast<CheckBox>().CurrentValue
                   && !ObjectManager.Player.IsUnderTurret()
                   &&
                   !(ObjectManager.Get<Obj_AI_Base>()
                       .Count(
                           m =>
                               string.Equals(m.BaseSkinName, "VisionWard", StringComparison.CurrentCultureIgnoreCase) &&
                               m.IsValidTarget(650f)) > 0);
        }

        /// <summary>
        ///     Gets the type of the module.
        /// </summary>
        /// <returns></returns>
        public ModuleType GetModuleType()
        {
            return ModuleType.Other;
        }

        /// <summary>
        ///     Called when the module gets executed.
        /// </summary>
        public void OnExecute()
        {
        }

        /// <summary>
        ///     Called Before the orbwalker attacks.
        /// </summary>
        /// <param name="args">The <see cref="Orbwalking.BeforeAttackEventArgs" /> instance containing the event data.</param>
        private void BeforeAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (ShouldGetExecuted())
            {
                args.Process = false;
            }
        }
    }
}