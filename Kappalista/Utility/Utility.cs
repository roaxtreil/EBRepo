using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using SharpDX;

namespace Kappalista
{
    static class Utility
    {
        public static Vector3 Extend(this Vector3 vector3, Vector3 toVector3, float distance)
        {
            return vector3 + (distance * (toVector3 - vector3).Normalized());
        }
        internal static bool IsKillableAndValidTarget(this Obj_AI_Minion target, double calculatedDamage,
            DamageType damageType, float distance = float.MaxValue)
        {
            if (target == null || !target.IsValidTarget(distance) || target.Health <= 0 ||
                target.HasBuffOfType(BuffType.SpellImmunity) || target.HasBuffOfType(BuffType.SpellShield) ||
                target.CharData.BaseSkinName == "gangplankbarrel")
                return false;

            if (ObjectManager.Player.HasBuff("summonerexhaust"))
                calculatedDamage *= 0.6;

            var dragonSlayerBuff = ObjectManager.Player.GetBuff("s5test_dragonslayerbuff");
            if (dragonSlayerBuff != null)
            {
                if (dragonSlayerBuff.Count >= 4)
                    calculatedDamage += dragonSlayerBuff.Count == 5 ? calculatedDamage * 0.30 : calculatedDamage * 0.15;

                if (target.CharData.BaseSkinName.ToLowerInvariant().Contains("dragon"))
                    calculatedDamage *= 1 - dragonSlayerBuff.Count * 0.07;
            }

            if (target.CharData.BaseSkinName.ToLowerInvariant().Contains("baron") &&
                ObjectManager.Player.HasBuff("barontarget"))
                calculatedDamage *= 0.5;

            return target.Health + target.HPRegenRate +
                   (damageType == DamageType.Physical ? target.AttackShield : target.MagicShield) <
                   calculatedDamage - 2;
        }
        public static int TickCount
        {
            get
            {
                return Environment.TickCount & int.MaxValue;
            }
        }
        internal static bool IsKillableAndValidTarget(this AIHeroClient target, double calculatedDamage,
           DamageType damageType, float distance = float.MaxValue)
        {
            if (target == null || !target.IsValidTarget(distance) || target.CharData.BaseSkinName == "gangplankbarrel")
                return false;

            if (target.HasBuff("kindredrnodeathbuff"))
            {
                return false;
            }
            if (target.HasBuff("Undying Rage"))
            {
                return false;
            }
            if (target.HasBuff("JudicatorIntervention"))
            {
                return false;
            }
            if (target.HasBuff("BansheesVeil"))
            {
                return false;
            }
            if (target.HasBuff("SivirE"))
            {
                return false;
            }
            if (target.HasBuff("NocturneW"))
            {
                return false;
            }

            if (ObjectManager.Player.HasBuff("summonerexhaust"))
                calculatedDamage *= 0.6;

            if (target.ChampionName == "Blitzcrank")
                if (!target.HasBuff("manabarriercooldown"))
                    if (target.Health + target.HPRegenRate +
                        (damageType == DamageType.Physical ? target.AttackShield : target.MagicShield) +
                        target.Mana * 0.6 + target.PARRegenRate < calculatedDamage)
                        return true;

            if (target.ChampionName == "Garen")
                if (target.HasBuff("GarenW"))
                    calculatedDamage *= 0.7;

            if (target.HasBuff("FerociousHowl"))
                calculatedDamage *= 0.3;

            return target.Health + target.HPRegenRate +
                   (damageType == DamageType.Physical ? target.AttackShield : target.MagicShield) <
                   calculatedDamage - 2;
        }
        
        public static float GetRendDamage(Obj_AI_Base target, int customStacks = -1)
        {
            return Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical, GetRawRendDamage(target, customStacks));
        }
        private static readonly float[] RawRendDamage = { 20, 30, 40, 50, 60 };
        private static readonly float[] RawRendDamageMultiplier = { 0.6f, 0.6f, 0.6f, 0.6f, 0.6f };
        private static readonly float[] RawRendDamagePerSpear = { 10, 14, 19, 25, 32 };
        private static readonly float[] RawRendDamagePerSpearMultiplier = { 0.2f, 0.225f, 0.25f, 0.275f, 0.3f };

        public static float GetRawRendDamage(Obj_AI_Base target, int customStacks = -1)
        {
            var stacks = (customStacks > -1 ? customStacks : target.HasRendBuff() ? target.GetRendBuff().Count : 0) - 1;
            if (stacks > -1)
            {
                var index = Kappalista.E.Level - 1;
                return RawRendDamage[index] + stacks * RawRendDamagePerSpear[index] +
                       Player.Instance.TotalAttackDamage * (RawRendDamageMultiplier[index] + stacks * RawRendDamagePerSpearMultiplier[index]);
            }

            return 0;
        }
        public static bool HasRendBuff(this Obj_AI_Base target)
        {
            return target.GetRendBuff() != null;
        }
        public static BuffInstance GetRendBuff(this Obj_AI_Base target)
        {
            return target.Buffs.Find(b => b.Caster.IsMe && b.IsValid() && b.DisplayName == "KalistaExpungeMarker");
        }
        public static AIHeroClient GetTargetNoCollision(Spell.Skillshot spell,
           bool ignoreShield = true,
           IEnumerable<AIHeroClient> ignoredChamps = null,
           Vector3? rangeCheckFrom = null)
        {
            var t = TargetSelector.GetTarget(spell.Range,
                DamageType.Physical);
            if (t != null)
            if (spell.AllowedCollisionCount < 100 && spell.GetPrediction(t).HitChance != HitChance.Collision)
            {
                return t;
            }

            return null;
        }
        public static bool Ready(this Spell.Active spell)
        {
            return spell != null && spell.Slot != SpellSlot.Unknown && spell.State != SpellState.Cooldown && spell.State != SpellState.Disabled && spell.State != SpellState.NoMana && spell.State != SpellState.NotLearned && spell.State != SpellState.Surpressed && spell.State != SpellState.Unknown && spell.State == SpellState.Ready;
        }
        public static bool Ready(this Spell.Targeted spell)
        {
            return spell != null && spell.Slot != SpellSlot.Unknown && spell.State != SpellState.Cooldown && spell.State != SpellState.Disabled && spell.State != SpellState.NoMana && spell.State != SpellState.NotLearned && spell.State != SpellState.Surpressed && spell.State != SpellState.Unknown && spell.State == SpellState.Ready;
        }
        public static bool Ready(this Spell.Skillshot spell)
        {
            return spell != null && spell.Slot != SpellSlot.Unknown && spell.State != SpellState.Cooldown && spell.State != SpellState.Disabled && spell.State != SpellState.NoMana && spell.State != SpellState.NotLearned && spell.State != SpellState.Surpressed && spell.State != SpellState.Unknown && spell.State == SpellState.Ready;
        }
    }
}
