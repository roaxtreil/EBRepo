using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using Leblanc;
using SharpDX;
using Color = System.Drawing.Color;

namespace REBURANKU.Utility
{
    internal static class OnEndScene
    {
        public static Spell.Targeted Q = Program.Q;
        public static Spell.Skillshot W = Program.W;
        public static Spell.Skillshot E = Program.E;
        public static Spell.Targeted R = Program.R;
        private const int BarWidth = 104;
        private const int LineThickness = 9;
        public delegate float DamageToUnitDelegate(AIHeroClient hero);
        private static readonly Vector2 BarOffset = new Vector2(1, 0); // -9, 11
        private static Dictionary<DamageToUnitDelegate, Color> _combodamage;

        static float getComboDamage(Obj_AI_Base enemy)
        {
            if (enemy != null)
            {
                float damage = 0;

                if (Q.IsReady())
                    if (R.IsReady() || W.IsReady() || E.IsReady())
                    {
                        damage += (ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.Q) * 2);
                    }
                    else
                    {
                        damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.Q);
                    }
                if (W.IsReady())
                    damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.W);
                if (E.IsReady())
                    damage += (ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.E));
                if (R.IsReady())
                    if (Q.IsReady())
                    {
                        damage += (ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.Q) * 1.5f * 2f);
                    }
                    else
                    {
                        damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.R);
                    }
                if (!ObjectManager.Player.Spellbook.IsAutoAttacking)
                    damage += (float)ObjectManager.Player.GetAutoAttackDamage(enemy, true);
                return damage;
            }
            return 0;
        }

        public static void Drawing_OnEndScene(EventArgs args)
        {
            _combodamage = new Dictionary<DamageToUnitDelegate, Color>
            {
                {getComboDamage, Color.DarkRed},
            };

            if (ObjectManager.Player.IsDead && Program.Config.DrawDamageBarIndicator) return;
            foreach (
                var enemy in EntityManager.Heroes.Enemies.Where(enemy => enemy.IsValidTarget(2200) && enemy.IsHPBarRendered)
                )
            {
                var damage = _combodamage.Sum(v => v.Key(enemy));
                if (damage <= 0)
                {
                    continue;
                }

                foreach (var spell in _combodamage)
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