using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;
using Color = System.Drawing.Color;

namespace Kappalista
{
    internal static class DamageIndicator
    {
        public delegate float DamageToUnitDelegate(AIHeroClient hero);
        public static SpellSlot Ignite;
        private const int BarWidth = 104;
        private const int LineThickness = 9;
        private static readonly Vector2 BarOffset = new Vector2(1, 0); // -9, 11
        private static Dictionary<DamageToUnitDelegate, Color> _spells;

        private static float QDamage(AIHeroClient hero)
        {
            if (Kappalista.Q.Ready())
            {
                return ObjectManager.Player.GetSpellDamage(hero, SpellSlot.Q);
            }
            return 0;
        }

        private static float EDamage(AIHeroClient hero)
        {
            if (Kappalista.E.Ready() && hero.HasRendBuff())
            {
                return Utility.GetRendDamage(hero);
            }
            return 0;
        }
        private static float IgniteDamage(AIHeroClient hero)
        {
            return ObjectManager.Player.GetSpellDamage(hero, Ignite);
        }



        public static void Initialize()
        {
            _spells = new Dictionary<DamageToUnitDelegate, Color>
            {
                {QDamage, Color.Blue},
                {EDamage, Color.Red},
                {IgniteDamage, Color.FromArgb(255, 120, 56, 28)},
            };

            SummonerSpells.Initialize();
            Drawing.OnEndScene += Drawing_OnEndScene;
        }

        internal class SummonerSpells
        {
            public static void Initialize()
            {
                SetSummonerSlots();
            }

            private static void SetSummonerSlots()
            {
                Ignite = Player.Instance.GetSpellSlotFromName("summonerdot");
            }
        }

        private static void Drawing_OnEndScene(EventArgs args)
        {

            if (!KappalistaMenu.GetBoolValue(KappalistaMenu.Drawings, "draw.damageindicator"))
            {
                return;
            }

            foreach (
                var enemy in EntityManager.Heroes.Enemies.Where(enemy => enemy.IsValidTarget(2200) && enemy.IsHPBarRendered)
                )
            {
                var damage = _spells.Sum(v => v.Key(enemy));
                if (damage <= 0)
                {
                    continue;
                }

                foreach (var spell in _spells)
                {
                    var damagePercentage = ((enemy.Health - damage) > 0 ? (enemy.Health - damage) : 0) /
                                           (enemy.MaxHealth + enemy.AllShield + enemy.AttackShield + enemy.MagicShield);
                    var healthPercentage = enemy.Health /
                                           (enemy.MaxHealth + enemy.AllShield + enemy.AttackShield + enemy.MagicShield);
                    var startPoint = new Vector2(
                        (int)(enemy.HPBarPosition.X + BarOffset.X + damagePercentage * BarWidth),
                        (int)(enemy.HPBarPosition.Y + BarOffset.Y) - 5);
                    var endPoint =
                        new Vector2((int)(enemy.HPBarPosition.X + BarOffset.X + healthPercentage * BarWidth) + 1,
                            (int)(enemy.HPBarPosition.Y + BarOffset.Y) - 5);
                    Drawing.DrawLine(startPoint, endPoint, LineThickness, spell.Value);

                    damage -= spell.Key(enemy);
                }
            }
        }
    }
}