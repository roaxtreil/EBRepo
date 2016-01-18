using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;
using Color = System.Drawing.Color;

namespace Shaco
{
    internal static class DamageIndicator
    {
        public delegate float DamageToUnitDelegate(AIHeroClient hero);

        public static SpellSlot Smite;
        public static SpellSlot Ignite;
        private const int BarWidth = 104;
        private const int LineThickness = 9;
        private static readonly Vector2 BarOffset = new Vector2(1, 0); // -9, 11
        private static Dictionary<DamageToUnitDelegate, Color> _spells;

        private static float QDamage(AIHeroClient hero)
        {if (ThugDogeShaco.Q.IsReady()) { 
            return ObjectManager.Player.GetSpellDamage(hero, SpellSlot.Q);
            }
            return 0;
        }

        private static float EDamage(AIHeroClient hero)
        {
            if (ThugDogeShaco.E.IsReady())
            {
                return ObjectManager.Player.GetSpellDamage(hero, SpellSlot.E);
             }
            return 0;
        }

        private static float RDamage(AIHeroClient hero)
        {
            if (ThugDogeShaco.Clone != null)
            {
                return ObjectManager.Player.GetSpellDamage(hero, SpellSlot.R);
            }
            return 0;
        }


        private static float IgniteDamage(AIHeroClient hero)
        {
            return ObjectManager.Player.GetSpellDamage(hero, Ignite);
        }

        private static float SmiteDamage(AIHeroClient hero)
        {
            return ObjectManager.Player.GetSpellDamage(hero, Ignite);
        }



        public static void Initialize()
        {
            _spells = new Dictionary<DamageToUnitDelegate, Color>
            {
                {QDamage, Color.Blue},
                {EDamage, Color.OrangeRed},
                {RDamage, Color.Aqua},
                {IgniteDamage, Color.FromArgb(255, 120, 56, 28)},
                {SmiteDamage, Color.FromArgb(255, 70, 130, 156)}
            };

            SummonerSpells.Initialize();
            Drawing.OnEndScene += Drawing_OnEndScene;
        }

        internal class SummonerSpells
        {
            private static readonly int[] SmitePurple = { 3713, 3726, 3725, 3724, 3723, 3933 };
            private static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719, 3932 };
            private static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714, 3931 };
            private static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707, 3930 };

            public static void Initialize()
            {
                SetSummonerSlots();
            }

            private static void SetSummonerSlots()
            {
                if (SmiteBlue.Any(x => Player.Instance.InventoryItems.FirstOrDefault(a => a.Id == (ItemId)x) != null))
                {
                    Smite = Player.Instance.GetSpellSlotFromName("s5_summonersmiteplayerganker");
                }
                else if (
                    SmiteRed.Any(
                        x => Player.Instance.InventoryItems.FirstOrDefault(a => a.Id == (ItemId)x) != null))
                {
                    Smite = Player.Instance.GetSpellSlotFromName("s5_summonersmiteduel");
                }
                else if (
                    SmiteGrey.Any(
                        x => Player.Instance.InventoryItems.FirstOrDefault(a => a.Id == (ItemId)x) != null))
                {
                    Smite = Player.Instance.GetSpellSlotFromName("s5_summonersmitequick");
                }
                else if (
                    SmitePurple.Any(
                        x => Player.Instance.InventoryItems.FirstOrDefault(a => a.Id == (ItemId)x) != null))
                {
                    Smite = Player.Instance.GetSpellSlotFromName("itemsmiteaoe");
                }
                else
                {
                    Smite = Player.Instance.GetSpellSlotFromName("summonersmite");
                }

                Ignite = Player.Instance.GetSpellSlotFromName("summonerdot");
            }
          }

            private static void Drawing_OnEndScene(EventArgs args)
        {

            if (!ThugDogeShaco.DrawingConfig.DrawDamageBar)
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