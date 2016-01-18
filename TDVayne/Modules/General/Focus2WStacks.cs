using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using TDVayne.Utility.Entities;

namespace TDVayne.Modules.General
{
    internal class Focus2WStacks : ISOLOModule
    {
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
            return
                EntityManager.Heroes.Enemies.Any(
                    m => m.IsValidTarget(ObjectManager.Player.GetAutoAttackRange() + 200) && m.Has2WStacks())
                &&
                !EntityManager.Heroes.Enemies.Any(
                    m =>
                        m.IsValidTarget(ObjectManager.Player.GetAutoAttackRange() + 200) &&
                        m.Health + 15 <
                        ObjectManager.Player.GetAutoAttackDamage(m)*3 +
                        ObjectManager.Player.GetSpellDamage(m, SpellSlot.W));
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
            if (ShouldGetExecuted())
            {
                Orbwalker.ForcedTarget = EntityManager.Heroes.Enemies.FirstOrDefault(
                    m => m.IsValidTarget(ObjectManager.Player.GetAutoAttackRange() + 200) && m.Has2WStacks());
            }
        }
    }
}