using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Constants;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using TDVayne.Utility;
using TDVayne.Utility.Entities;
using TDVayne.Utility.Enums;

namespace TDVayne.Skills.Tumble
{
    internal class Tumble : Skill
    {
        private float lastLaneclearTick;

        /// <summary>
        ///     The Tumble logic provider
        /// </summary>
        public TumbleLogicProvider Provider;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Tumble" /> class.
        /// </summary>
        public Tumble()
        {
            lastLaneclearTick = 0f;
            Provider = new TumbleLogicProvider();
        }

        /// <summary>
        ///     Gets the skill mode.
        /// </summary>
        /// <returns></returns>
        public SkillMode GetSkillMode()
        {
            return SkillMode.OnAfterAA;
        }

        /// <summary>
        ///     Executes the module given a target.
        /// </summary>
        /// <param name="target">The target.</param>
        public void Execute(Obj_AI_Base target)
        {
            try
            {
                if (target is AIHeroClient)
                {
                    var targetHero = target as AIHeroClient;
                    if (targetHero.IsValidTarget())
                    {
                        //If the Harass mode is agressive and the target has 1 W Stacks + 1 for current AA then we Q for the third.
                        if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass)
                            && targetHero.GetWBuff().Count != 1
                            && MenuGenerator.HarassMenu["TDVaynemixedmode"].Cast<Slider>().CurrentValue == 1)
                            return;

                        //If they are autoattacking or winding up and Harass mode is passive we AA them then Q backwards.
                        if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass)
                            && MenuGenerator.HarassMenu["TDVaynemixedmode"].Cast<Slider>().CurrentValue == 0
                            && (targetHero.Spellbook.IsAutoAttacking || targetHero.Spellbook.IsAutoAttacking)
                            && AutoAttacks.IsAutoAttack(target.LastCastedSpellName())
                            && target.LastCastedSpellTarget().IsValid && target.LastCastedSpellTarget() is AIHeroClient)
                        {
                            var backwardsPosition = ObjectManager.Player.ServerPosition.Extend(target.ServerPosition,
                                -300f);
                            if (backwardsPosition.To3D().IsSafe())
                            {
                                CastTumble(backwardsPosition.To3D(), targetHero);
                            }
                            return;
                        }

                        if (MenuGenerator.QMenu["TDVaynemisctumblesmartQ"].Cast<CheckBox>().CurrentValue)
                        {
                            var position = Provider.GetTDVayneQPosition();
                            if (position != Vector3.Zero)
                            {
                                CastTumble(position, targetHero);
                            }
                        }
                        else
                        {
                            var position = ObjectManager.Player.ServerPosition.Extend(Game.CursorPos, 300f);
                            if (position.To3D().IsSafe())
                            {
                                CastTumble(position.To3D(), targetHero);
                            }
                        }
                    }
                }
            }
            catch (NullReferenceException kappa)
            {
              
            }
        }

        /// <summary>
        ///     Executes the farm logic.
        /// </summary>
        /// <param name="target">The target.</param>
        public void ExecuteFarm(Obj_AI_Base target)
        {
            if (Environment.TickCount - lastLaneclearTick < 80)
            {
                return;
            }
            lastLaneclearTick = Environment.TickCount;

            if (Variables.Q.IsReady() && MenuGenerator.FarmMenu["useqfarm"].Cast<CheckBox>().CurrentValue)
            {
                var currentTarget = target;

                if (currentTarget is Obj_AI_Minion)
                {
                    if (GameObjects.JungleLarge.Contains(currentTarget) ||
                        GameObjects.JungleLegendary.Contains(currentTarget))
                    {
                        //It's a jungle minion, so we Q sideways.
                        var sidewaysPosition =
                            (ObjectManager.Player.ServerPosition.To2D() + 300f*ObjectManager.Player.Direction.To2D())
                                .To3D();
                        if (sidewaysPosition.IsSafe())
                        {
                            CastTumble(sidewaysPosition, currentTarget);
                            Orbwalker.ForcedTarget = currentTarget;
                            return;
                        }
                    }
                    var minionsInRange =
                        EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Both,
                            EntityManager.UnitTeam.Enemy, ObjectManager.Player.ServerPosition,
                            ObjectManager.Player.AttackRange + 65)
                            .Where(
                                m =>
                                    m.Health + 5 <=
                                    ObjectManager.Player.GetAutoAttackDamage(m) +
                                    ObjectManager.Player.GetSpellDamage(m, SpellSlot.Q))
                            .ToList();


                    if (minionsInRange.Count() > 1)
                    {
                        var firstMinion = minionsInRange.OrderBy(m => m.HealthPercent).First();
                        var afterTumblePosition = ObjectManager.Player.ServerPosition.Extend(Game.CursorPos, 300f);
                        if (afterTumblePosition.Distance(firstMinion.ServerPosition) <=
                            ObjectManager.Player.GetAutoAttackRange()
                            && afterTumblePosition.To3D().IsSafe())
                        {
                            CastTumble(Game.CursorPos, firstMinion);
                            Orbwalker.ForcedTarget = firstMinion;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Casts Q (Tumble).
        /// </summary>
        /// <param name="Position">The position.</param>
        /// <param name="target">The target.</param>
        private void CastTumble(Vector3 Position, Obj_AI_Base target)
        {
            var WallQPosition = TumbleHelper.GetQBurstModePosition();
            if (WallQPosition != null && ObjectManager.Player.ServerPosition.To2D().IsSafeEx() &&
                !(ObjectManager.Player.ServerPosition.UnderTurret(true)))
            {
                var V3WallQ = (Vector3) WallQPosition;
                CastQ(V3WallQ);
                return;
            }

            var TumbleQEPosition = Provider.GetQEPosition();
            if (TumbleQEPosition != Vector3.Zero)
            {
                CastQ(TumbleQEPosition);
                return;
            }
            Orbwalker.ForcedTarget = target;
            CastQ(Position);
            Orbwalker.ForcedTarget = null;
        }

        /// <summary>
        ///     Casts Q (Tumble) to a specified position.
        /// </summary>
        /// <param name="Position">The position.</param>
        private void CastQ(Vector3 Position)
        {
            //Orbwalking.ResetAutoAttackTimer();
            Player.CastSpell(SpellSlot.Q, Position);
        }
    }
}