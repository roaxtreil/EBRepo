using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using TDVayne.Skills.Tumble;
using TDVayne.Utility;

namespace TDVayne.Modules.Condemn
{
    internal class KSQ : ISOLOModule
    {
        /// <summary>
        ///     Called when the module is loaded.
        /// </summary>
        public void OnLoad()
        {
        }

        /// <summary>
        ///     Should the module get executed.
        /// </summary>
        /// <returns></returns>
        public bool ShouldGetExecuted()
        {
            return MenuGenerator.QMenu["TDVaynemisctumbleqks"].Cast<CheckBox>().CurrentValue &&
                   Variables.Q.IsReady();
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
        ///     Called when the module is executed.
        /// </summary>
        public void OnExecute()
        {
            var currentTarget = TargetSelector.GetTarget(ObjectManager.Player.GetAutoAttackRange() + 260f,
                DamageType.Physical);
            if (currentTarget.IsValidTarget())
            {
                if (currentTarget.ServerPosition.Distance(ObjectManager.Player.ServerPosition) <=
                    ObjectManager.Player.GetAutoAttackRange())
                {
                    return;
                }

                if (Prediction.Health.GetPrediction(currentTarget, (int) (250 + Game.Ping/2f + 125f)) <
                    ObjectManager.Player.GetAutoAttackDamage(currentTarget) +
                    ObjectManager.Player.GetSpellDamage(currentTarget, SpellSlot.Q) &&
                    Prediction.Health.GetPrediction(currentTarget, (int) (250 + Game.Ping/2f + 125f)) > 0)
                {
                    var extendedPosition = ObjectManager.Player.ServerPosition.Extend(
                        currentTarget.ServerPosition, 300f);
                    if (extendedPosition.To3D().IsSafe() && !extendedPosition.To3D().UnderTurret(true))
                    {
                        Orbwalker.ResetAutoAttack();
                        Player.CastSpell(SpellSlot.Q, extendedPosition.To3D());
                        Orbwalker.ForcedTarget = currentTarget;
                    }
                }
            }
        }
    }
}