using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace TDVayne.Skills.Tumble
{
    internal class TumbleVariables
    {
        /// <summary>
        ///     Gets the enemies close.
        /// </summary>
        /// <value>
        ///     The enemies close.
        /// </value>
        public static IEnumerable<AIHeroClient> EnemiesClose
        {
            get
            {
                return
                    EntityManager.Heroes.Enemies.Where(
                        m =>
                            m.Distance(ObjectManager.Player, true) <= Math.Pow(1000, 2) && m.IsValidTarget(1500, false) &&
                            m.CountEnemiesInRange(m.IsMelee ? m.AttackRange*1.5f : m.AttackRange + 20*1.5f) > 0);
            }
        }
    }
}