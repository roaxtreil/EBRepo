using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Constants;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using HoolaRiven.Utility;

namespace HoolaRiven
{
    public class Program
    {
        private const string IsFirstR = "RivenFengShuiEngine";
        private const string IsSecondR = "rivenizunablade";
        private static readonly AIHeroClient Player = ObjectManager.Player;
        private static readonly Spell.Active Q = new Spell.Active(SpellSlot.Q);
        private static readonly Spell.Active W = new Spell.Active(SpellSlot.W);
        private static readonly Spell.Active E = new Spell.Active(SpellSlot.E, 300);
        private static readonly Spell.Skillshot R = new Spell.Skillshot(SpellSlot.R, 900, SkillShotType.Cone, 250, 1600,
            45)
        {
            MinimumHitChance = HitChance.High,
            AllowedCollisionCount = -1
        };

        private static readonly Spell.Active R2 = new Spell.Active(SpellSlot.R, 0);
        public static Spell.Skillshot Flash;
        private static int QStack = 1;
        private static bool forceQ;
        private static bool forceW;
        private static bool forceR;
        private static bool forceR2;
        private static bool forceItem;
        private static float LastQ;
        private static float LastR;
        private static AttackableUnit QTarget;

        private static int GetWRange
        {
            get { return Player.HasBuff("RivenFengShuiEngine") ? 330 : 265; }
        }

        private static int ItemHue
        {
            get
            {
                return Item.CanUseItem(3077) && Item.HasItem(3077)
                    ? 3077
                    : Item.CanUseItem(3074) && Item.HasItem(3074) ? 3074 : 0;
            }
        }

        public static bool FlashIsReady
        {
            get { return Flash != null && Flash.IsReady(); }
        }

        private static void Main()
        {
            Loading.OnLoadingComplete += OnGameLoad;
        }

        private static void OnGameLoad(EventArgs args)
        {
            var slot = Player.GetSpellSlotFromName("summonerflash");
            if (slot != SpellSlot.Unknown)
            {
                Flash = new Spell.Skillshot(slot, 450, SkillShotType.Linear, 0, int.MaxValue, 55);
            }

            HoolaMenu.InitializeMenu();
            Game.OnUpdate += OnTick;
            Obj_AI_Base.OnProcessSpellCast += OnCast;
            Obj_AI_Base.OnSpellCast += OnDoCast;
            Obj_AI_Base.OnSpellCast += OnDoCastLC;
            Obj_AI_Base.OnPlayAnimation += OnPlay;
            Obj_AI_Base.OnProcessSpellCast += OnCasting;
            Interrupter.OnInterruptableSpell += Interrupt;
        }

        private static void OnTick(EventArgs args)
        {
            ForceSkill();
            UseRMaxDam();
            AutoUseW();
            Killsteal();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                Combo();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                Jungleclear();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                Harass();
            if (HoolaMenu.KeybindValue(HoolaMenu.ComboMenu, "FastHarass"))
            {
                FastHarass();
            }
            else
            {
                Orbwalker.ForcedTarget = null;
            }
            if (HoolaMenu.KeybindValue(HoolaMenu.ComboMenu, "Burst"))
            {
                Burst();
            }
            else
            {
                Orbwalker.ForcedTarget = null;
            }       
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
                Flee();
            if (Utils.GameTimeTickCount - LastQ >= 3650 && QStack != 1 && !Player.IsRecalling() &&
                HoolaMenu.BoolValue(HoolaMenu.MiscMenu, "KeepQ") && Q.IsReady())
                EloBuddy.Player.CastSpell(SpellSlot.Q, Game.CursorPos);
        }

        private static void UseRMaxDam()
        {
            if (HoolaMenu.BoolValue(HoolaMenu.MiscMenu, "RMaxDam") && R.IsReady() && R.Name == IsSecondR)
            {
                var targets = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(R.Range) && !x.IsZombie);
                foreach (var target in targets)
                {
                    if (target.Health/target.MaxHealth <= 0.25 &&
                        (!target.HasBuff("kindrednodeathbuff") || !target.HasBuff("Undying Rage") ||
                         !target.HasBuff("JudicatorIntervention")))
                        R.Cast(target.Position);
                }
            }
        }

        private static void Killsteal()
        {
            if (HoolaMenu.BoolValue(HoolaMenu.MiscMenu, "killstealw") && W.IsReady())
            {
                var targets = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(R.Range) && !x.IsZombie);
                foreach (var target in targets)
                {
                    if (target.Health < Player.GetSpellDamage(target, SpellSlot.W) && InWRange(target))
                        W.Cast();
                }
            }
            if (HoolaMenu.BoolValue(HoolaMenu.MiscMenu, "killstealr") && R.IsReady() && R.Name == IsSecondR)
            {
                var targets = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(R.Range) && !x.IsZombie);
                foreach (var target in targets)
                {
                    if (target.Health < Rdame(target, Prediction.Health.GetPrediction(target, 250)) &&
                        (!target.HasBuff("kindrednodeathbuff") && !target.HasBuff("Undying Rage") &&
                         !target.HasBuff("JudicatorIntervention")))
                        R.Cast(target.Position);
                }
            }
        }

        private static void AutoUseW()
        {
            if (HoolaMenu.SliderValue(HoolaMenu.MiscMenu, "AutoW") > 0)
            {
                if (Player.CountEnemiesInRange(GetWRange) >= HoolaMenu.SliderValue(HoolaMenu.MiscMenu, "AutoW"))
                {
                    ForceW();
                }
            }
        }

        private static void ForceSkill()
        {
            if (forceQ && QTarget != null && QTarget.IsValidTarget(E.Range + Player.BoundingRadius + 70) && Q.IsReady())
                EloBuddy.Player.CastSpell(SpellSlot.Q, QTarget.Position);
            if (forceW) W.Cast();
            if (forceR && R.Name == IsFirstR) R2.Cast();
            if (forceItem && Item.CanUseItem(ItemHue) && Item.HasItem(ItemHue) && ItemHue != 0) Item.UseItem(ItemHue);
            if (forceR2 && R.Name == IsSecondR)
            {
                var target = TargetSelector.SelectedTarget;
                if (target != null) R.Cast(target.Position);
            }
        }

        private static void Combo()
        {
            var targetR = TargetSelector.GetTarget(250 + Player.AttackRange + 70, DamageType.Physical);
            if (!targetR.IsValidTarget() && targetR == null) return;

            if (R.IsReady() && R.Name == IsFirstR && Player.IsInAutoAttackRange(targetR) &&
                HoolaMenu.KeybindValue(HoolaMenu.ComboMenu, "AlwaysR") && targetR != null) ForceR();
            if (R.IsReady() && R.Name == IsFirstR && W.IsReady() && InWRange(targetR) &&
                HoolaMenu.BoolValue(HoolaMenu.ComboMenu, "ComboW") &&
                HoolaMenu.KeybindValue(HoolaMenu.ComboMenu, "AlwaysR") && targetR != null)
            {
                ForceR();
                Core.DelayAction(ForceW, 1);
            }
            if (W.IsReady() && InWRange(targetR) && HoolaMenu.BoolValue(HoolaMenu.ComboMenu, "ComboW") &&
                targetR != null) W.Cast();
            if (HoolaMenu.KeybindValue(HoolaMenu.ComboMenu, "UseHoola") && R.IsReady() && R.Name == IsFirstR &&
                W.IsReady() && targetR != null && E.IsReady() && targetR.IsValidTarget() && !targetR.IsZombie &&
                (IsKillableR(targetR) || HoolaMenu.KeybindValue(HoolaMenu.ComboMenu, "AlwaysR")))
            {
                if (!InWRange(targetR))
                {
                    EloBuddy.Player.CastSpell(SpellSlot.E, targetR.Position);
                    ForceR();
                    Core.DelayAction(ForceW, 200);
                    Core.DelayAction(() => ForceCastQ(targetR), 305);
                }
            }
            else if (!HoolaMenu.KeybindValue(HoolaMenu.ComboMenu, "UseHoola") && R.IsReady() && R.Name == IsFirstR &&
                     W.IsReady() && targetR != null && E.IsReady() && targetR.IsValidTarget() && !targetR.IsZombie &&
                     (IsKillableR(targetR) || HoolaMenu.KeybindValue(HoolaMenu.ComboMenu, "AlwaysR")))
            {
                if (!InWRange(targetR))
                {
                    EloBuddy.Player.CastSpell(SpellSlot.E, targetR.Position);
                    ForceR();
                    Core.DelayAction(ForceW, 200);
                }
            }
            else if (HoolaMenu.KeybindValue(HoolaMenu.ComboMenu, "UseHoola") && W.IsReady() && E.IsReady())
            {
                if (targetR.IsValidTarget() && targetR != null && !targetR.IsZombie && !InWRange(targetR))
                {
                    EloBuddy.Player.CastSpell(SpellSlot.E, targetR.Position);
                    Core.DelayAction(ForceItem, 10);
                    Core.DelayAction(ForceW, 200);
                    Core.DelayAction(() => ForceCastQ(targetR), 305);
                }
            }
            else if (!HoolaMenu.KeybindValue(HoolaMenu.ComboMenu, "UseHoola") && W.IsReady() && targetR != null &&
                     E.IsReady())
            {
                if (targetR.IsValidTarget() && targetR != null && !targetR.IsZombie && !InWRange(targetR))
                {
                    EloBuddy.Player.CastSpell(SpellSlot.E, targetR.Position);
                    Core.DelayAction(ForceItem, 10);
                    Core.DelayAction(ForceW, 240);
                }
            }
            else if (E.IsReady())
            {
                if (targetR.IsValidTarget() && !targetR.IsZombie && !InWRange(targetR))
                {
                    EloBuddy.Player.CastSpell(SpellSlot.E, targetR.Position);
                }
            }
        }

        private static void Interrupt(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            if (sender.IsEnemy && W.IsReady() && sender.IsValidTarget() && !sender.IsZombie &&
                HoolaMenu.BoolValue(HoolaMenu.MiscMenu, "Winterrupt"))
            {
                if (sender.IsValidTarget(125 + Player.BoundingRadius + sender.BoundingRadius)) W.Cast();
            }
        }

        private static void OnDoCastLC(Obj_AI_Base Sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!Sender.IsMe || !AutoAttacks.IsAutoAttack(args.SData.Name)) return;
            QTarget = (Obj_AI_Base) args.Target;
            if (args.Target is Obj_AI_Minion)
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                {
                    var Minions =
                        EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Both,
                            EntityManager.UnitTeam.Enemy, Player.Position, 70 + 120 + Player.BoundingRadius)
                            .OrderByDescending(i => i.MaxHealth)
                            .ToList();
                    if (!Minions.Any())
                        return;
                    if (HasTitan())
                    {
                        CastTitan();
                        return;
                    }
                    if (Q.IsReady() && HoolaMenu.BoolValue(HoolaMenu.LaneClear, "LaneQ"))
                    {
                        ForceItem();
                        Core.DelayAction(() => ForceCastQ(Minions[0]), 1);
                    }
                    if ((!Q.IsReady() || (Q.IsReady() && !HoolaMenu.BoolValue(HoolaMenu.LaneClear, "LaneQ"))) &&
                        W.IsReady() && HoolaMenu.SliderValue(HoolaMenu.LaneClear, "LaneW") != 0 &&
                        Minions.Count >= HoolaMenu.SliderValue(HoolaMenu.LaneClear, "LaneW"))
                    {
                        ForceItem();
                        Core.DelayAction(ForceW, 1);
                    }
                    if ((!Q.IsReady() || (Q.IsReady() && !HoolaMenu.BoolValue(HoolaMenu.LaneClear, "LaneQ"))) &&
                        (!W.IsReady() || (W.IsReady() && HoolaMenu.SliderValue(HoolaMenu.LaneClear, "LaneW") == 0) ||
                         Minions.Count() < HoolaMenu.SliderValue(HoolaMenu.LaneClear, "LaneW")) &&
                        E.IsReady() && HoolaMenu.BoolValue(HoolaMenu.LaneClear, "LaneE"))
                    {
                        EloBuddy.Player.CastSpell(SpellSlot.E, Minions[0].Position);
                        Core.DelayAction(ForceItem, 1);
                    }
                }
            }
        }

        private static void OnCasting(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsEnemy && sender.Type == Player.Type &&
                (HoolaMenu.BoolValue(HoolaMenu.MiscMenu, "AutoShield") ||
                 (HoolaMenu.BoolValue(HoolaMenu.MiscMenu, "Shield") &&
                  Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))))
            {
                var epos = Player.ServerPosition +
                           (Player.ServerPosition - sender.ServerPosition).Normalized()*300;

                if (Player.Distance(sender.ServerPosition) <= args.SData.CastRange)
                {
                    switch (args.SData.TargettingType)
                    {
                        case SpellDataTargetType.Unit:

                            if (args.Target.NetworkId == Player.NetworkId)
                            {
                                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit) &&
                                    !args.SData.Name.Contains("NasusW"))
                                {
                                    if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E, epos);
                                }
                            }

                            break;
                        case SpellDataTargetType.SelfAoe:

                            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
                            {
                                if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E, epos);
                            }

                            break;
                    }
                    if (args.SData.Name.Contains("IreliaEquilibriumStrike"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady() && InWRange(sender)) W.Cast();
                            else if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E, epos);
                        }
                    }
                    if (args.SData.Name.Contains("TalonCutthroat"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("RenektonPreExecute"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("GarenRPreCast"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E, epos);
                        }
                    }
                    if (args.SData.Name.Contains("GarenQAttack"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E, epos);
                        }
                    }
                    if (args.SData.Name.Contains("XenZhaoThrust3"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("RengarQ"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E, epos);
                        }
                    }
                    if (args.SData.Name.Contains("RengarPassiveBuffDash"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E, epos);
                        }
                    }
                    if (args.SData.Name.Contains("RengarPassiveBuffDashAADummy"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E, epos);
                        }
                    }
                    if (args.SData.Name.Contains("TwitchEParticle"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E, epos);
                        }
                    }
                    if (args.SData.Name.Contains("FizzPiercingStrike"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E, epos);
                        }
                    }
                    if (args.SData.Name.Contains("HungeringStrike"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E, epos);
                        }
                    }
                    if (args.SData.Name.Contains("YasuoDash"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E, epos);
                        }
                    }
                    if (args.SData.Name.Contains("KatarinaRTrigger"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady() && InWRange(sender)) W.Cast();
                            else if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E, epos);
                        }
                    }
                    if (args.SData.Name.Contains("YasuoDash"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E, epos);
                        }
                    }
                    if (args.SData.Name.Contains("KatarinaE"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingQAttack"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E);
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingSpinToWin"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E);
                            else if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingQAttack"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E);
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingQAttack"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E);
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingQAttack"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) EloBuddy.Player.CastSpell(SpellSlot.E);
                        }
                    }
                }
            }
        }

        private static void Burst()
        {
            var target = TargetSelector.GetTarget(1500, DamageType.Physical);
            if (target != null && target.IsValidTarget() && !target.IsZombie)
            {
                Orbwalker.ForcedTarget = target;
                Orbwalker.OrbwalkTo(target.ServerPosition);
                if (R.IsReady() && R.Name == IsFirstR && W.IsReady() && E.IsReady() &&
                    Player.Distance(target.Position) <= 250 + 70 + Player.AttackRange)
                {
                    EloBuddy.Player.CastSpell(SpellSlot.E, target.Position);
                    CastYoumuu();
                    ForceR();
                    Core.DelayAction(ForceW, 100);
                }
                else if (R.IsReady() && R.Name == IsFirstR && E.IsReady() && W.IsReady() && Q.IsReady() &&
                         Player.Distance(target.Position) <= 400 + 70 + Player.AttackRange)
                {
                    EloBuddy.Player.CastSpell(SpellSlot.E, target.Position);
                    CastYoumuu();
                    ForceR();
                    Core.DelayAction(() => ForceCastQ(target), 150);
                    Core.DelayAction(ForceW, 160);
                }
                else if (FlashIsReady
                         && R.IsReady() && R.Name == IsFirstR && (Player.Distance(target.Position) <= 800) &&
                         (!HoolaMenu.BoolValue(HoolaMenu.MiscMenu, "FirstHydra") ||
                          (HoolaMenu.BoolValue(HoolaMenu.MiscMenu, "FirstHydra") && !HasItem())))
                {
                    EloBuddy.Player.CastSpell(SpellSlot.E, target.Position);
                    CastYoumuu();
                    ForceR();
                    Core.DelayAction(FlashW, 180);
                }
                else if (FlashIsReady
                         && R.IsReady() && E.IsReady() && W.IsReady() && R.Name == IsFirstR &&
                         (Player.Distance(target.Position) <= 800) &&
                         HoolaMenu.BoolValue(HoolaMenu.MiscMenu, "FirstHydra") && HasItem())
                {
                    EloBuddy.Player.CastSpell(SpellSlot.E, target.Position);
                    ForceR();
                    Core.DelayAction(ForceItem, 100);
                    Core.DelayAction(FlashW, 210);
                }
            }
        }

        private static void FastHarass()
        {
            var target = TargetSelector.SelectedTarget;
            if (target == null || !target.IsValidTarget())
            {
                target = TargetSelector.GetTarget(450 + Player.AttackRange + 70, DamageType.Physical);
                if (target == null || !target.IsValidTarget()) return;
                Orbwalker.ForcedTarget = target;
                Orbwalker.OrbwalkTo(target.ServerPosition);
                if (Q.IsReady() && E.IsReady())
                {
                    if (target.IsValidTarget() && !target.IsZombie)
                    {
                        if (!Player.IsInAutoAttackRange(target) && !InWRange(target))
                            EloBuddy.Player.CastSpell(SpellSlot.E, target.Position);
                        Core.DelayAction(ForceItem, 10);
                        Core.DelayAction(() => ForceCastQ(target), 170);
                    }
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(400, DamageType.Physical);
            if (target == null || !target.IsValidTarget()) return;
            if (Q.IsReady() && W.IsReady() && E.IsReady() && QStack == 1)
            {
                if (target.IsValidTarget() && !target.IsZombie)
                {
                    ForceCastQ(target);
                    Core.DelayAction(ForceW, 1);
                }
            }
            if (Q.IsReady() && E.IsReady() && QStack == 3 && !Orbwalker.CanAutoAttack && Orbwalker.CanMove)
            {
                var epos = Player.ServerPosition +
                           (Player.ServerPosition - target.ServerPosition).Normalized()*300;
                EloBuddy.Player.CastSpell(SpellSlot.E, epos);
                Core.DelayAction(() => EloBuddy.Player.CastSpell(SpellSlot.Q, epos), 190);
            }
        }

        private static void Flee()
        {
            var enemy =
                EntityManager.Heroes.Enemies.Where(
                    hero =>
                        hero.IsValidTarget(Player.HasBuff("RivenFengShuiEngine")
                            ? 70 + 195 + Player.BoundingRadius
                            : 70 + 120 + Player.BoundingRadius) && W.IsReady());
            var x = Player.Position.Extend(Game.CursorPos, 300);
            if (W.IsReady() && enemy.Any()) foreach (var target in enemy) if (InWRange(target)) W.Cast();
            if (Q.IsReady() && !Player.IsDashing()) EloBuddy.Player.CastSpell(SpellSlot.Q, Game.CursorPos);
            if (E.IsReady() && !Player.IsDashing()) EloBuddy.Player.CastSpell(SpellSlot.E, x);
            ;
        }

        private static void Jungleclear()
        {
            var Mobs =
                EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Position, 250 + Player.AttackRange + 70)
                    .OrderBy(i => i.MaxHealth).ToList();

            if (Mobs.Count <= 0)
                return;

            if (W.IsReady() && E.IsReady() && !Player.IsInAutoAttackRange(Mobs[0]))
            {
                EloBuddy.Player.CastSpell(SpellSlot.E, Mobs[0].Position);
                Core.DelayAction(ForceItem, 1);
                Core.DelayAction(ForceW, 200);
            }
        }

        private static void OnPlay(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (!sender.IsMe) return;

            switch (args.Animation)
            {
                case "Spell1a":
                    LastQ = Utils.GameTimeTickCount;
                    if (HoolaMenu.BoolValue(HoolaMenu.MiscMenu, "Qstrange") &&
                        !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None)) Chat.Say("/d");
                    QStack = 2;
                    if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None) &&
                        !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit) &&
                        !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
                        Core.DelayAction(Reset, (HoolaMenu.SliderValue(HoolaMenu.MiscMenu, "QD")*10) + 1);
                    break;
                case "Spell1b":
                    LastQ = Utils.GameTimeTickCount;
                    if (HoolaMenu.BoolValue(HoolaMenu.MiscMenu, "Qstrange") &&
                        !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None)) Chat.Say("/d");
                    QStack = 3;
                    if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None) &&
                        !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit) &&
                        !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
                        Core.DelayAction(Reset, (HoolaMenu.SliderValue(HoolaMenu.MiscMenu, "QD")*10) + 1);
                    break;
                case "Spell1c":
                    LastQ = Utils.GameTimeTickCount;
                    if (HoolaMenu.BoolValue(HoolaMenu.MiscMenu, "Qstrange") &&
                        !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None)) Chat.Say("/d");
                    QStack = 1;
                    if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None) &&
                        !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit) &&
                        !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
                        Core.DelayAction(Reset, (HoolaMenu.SliderValue(HoolaMenu.MiscMenu, "QLD")*10) + 3);
                    break;
                case "Spell3":
                    if ((HoolaMenu.KeybindValue(HoolaMenu.ComboMenu, "Burst") ||
                         Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) ||
                         HoolaMenu.KeybindValue(HoolaMenu.ComboMenu, "FastHarass") ||
                         Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee)) &&
                        HoolaMenu.BoolValue(HoolaMenu.MiscMenu, "youmuu"))
                        CastYoumuu();
                    break;
                case "Spell4a":
                    LastR = Utils.GameTimeTickCount;
                    break;
                case "Spell4b":
                    var target = TargetSelector.SelectedTarget;
                    if (Q.IsReady() && target.IsValidTarget()) ForceCastQ(target);
                    break;
            }
        }

        private static void OnCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;

            if (args.SData.Name.Contains("ItemTiamatCleave")) forceItem = false;
            if (args.SData.Name.Contains("RivenTriCleave")) forceQ = false;
            if (args.SData.Name.Contains("RivenMartyr")) forceW = false;
            if (args.SData.Name == IsFirstR) forceR = false;
            if (args.SData.Name == IsSecondR) forceR2 = false;
        }

        private static void Reset()
        {
            Chat.Say("/d");
            Orbwalker.ResetAutoAttack();
            EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo,
                Player.Position.Extend(Game.CursorPos, Player.Distance(Game.CursorPos) + 10));
        }

        private static void OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe || !AutoAttacks.IsAutoAttack(args.SData.Name)) return;
            QTarget = (Obj_AI_Base) args.Target;

            if (args.Target is Obj_AI_Minion)
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                {
                    var Mobs = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                        Player.Position, 120 + 70 + Player.BoundingRadius).OrderByDescending(i => i.MaxHealth).ToArray();
                    if (Mobs.Any())
                    {
                        if (HasTitan())
                        {
                            CastTitan();
                            return;
                        }
                        if (Q.IsReady())
                        {
                            ForceItem();
                            Core.DelayAction(() => ForceCastQ(Mobs[0]), 1);
                        }
                        else if (W.IsReady())
                        {
                            ForceItem();
                            DelayAction.Add(1, ForceW);
                        }
                        else if (E.IsReady())
                        {
                            EloBuddy.Player.CastSpell(SpellSlot.E, Mobs[0].Position);
                        }
                    }
                }
            }

            if (args.Target is Obj_AI_Turret)
                if (args.Target.IsValid && args.Target != null && Q.IsReady() &&
                    HoolaMenu.BoolValue(HoolaMenu.LaneClear, "LaneQ") &&
                    Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                    ForceCastQ((Obj_AI_Base) args.Target);
            if (args.Target is AIHeroClient)
            {
                var target = (AIHeroClient) args.Target;
                if (HoolaMenu.BoolValue(HoolaMenu.MiscMenu, "killstealr") && R.IsReady() && R.Name == IsSecondR)
                    if (target.Health < (Rdame(target, Prediction.Health.GetPrediction(target, 250)) + Player.GetAutoAttackDamage(target)) &&
                        target.Health > Player.GetAutoAttackDamage(target)) R.Cast(target.Position);
                if (HoolaMenu.BoolValue(HoolaMenu.MiscMenu, "killstealw") && W.IsReady())
                    if (target.Health <
                        (Player.GetSpellDamage(target, SpellSlot.W) + Player.GetAutoAttackDamage(target)) &&
                        target.Health > Player.GetAutoAttackDamage(target)) W.Cast();
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    if (Q.IsReady())
                    {
                        ForceItem();
                        Core.DelayAction(() => ForceCastQ(target), 1);
                    }
                    else if (W.IsReady() && InWRange(target))
                    {
                        //ForceItem();
                        Core.DelayAction(ForceW, 1);
                    }
                    else if (E.IsReady() && !Player.IsInAutoAttackRange(target))
                        EloBuddy.Player.CastSpell(SpellSlot.E, target.Position);
                }
                if (HoolaMenu.KeybindValue(HoolaMenu.ComboMenu, "FastHarass"))
                {
                    if (HasTitan())
                    {
                        CastTitan();
                        return;
                    }
                    if (W.IsReady() && InWRange(target))
                    {
                        ForceItem();
                        Core.DelayAction(ForceW, 1);
                        Core.DelayAction(() => ForceCastQ(target), 2);
                    }
                    else if (Q.IsReady())
                    {
                        ForceItem();
                        Core.DelayAction(() => ForceCastQ(target), 1);
                    }
                    else if (E.IsReady() && !Player.IsInAutoAttackRange(target) && !InWRange(target))
                    {
                        EloBuddy.Player.CastSpell(SpellSlot.E, target.Position);
                    }
                }

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                {
                    if (HasTitan())
                    {
                        CastTitan();
                        return;
                    }
                    if (QStack == 2 && Q.IsReady())
                    {
                        ForceItem();
                        Core.DelayAction(() => ForceCastQ(target), 1);
                    }
                }

                if (HoolaMenu.KeybindValue(HoolaMenu.ComboMenu, "Burst"))
                {
                    if (HasTitan())
                    {
                        CastTitan();
                        return;
                    }
                    if (R.IsReady() && R.Name == IsSecondR)
                    {
                        ForceItem();
                        Core.DelayAction(ForceR2, 1);
                    }
                    else if (Q.IsReady())
                    {
                        ForceItem();
                        Core.DelayAction(() => ForceCastQ(target), 1);
                    }
                }
            }
        }

        public static bool IsKillableR(AIHeroClient target)
        {
            if (HoolaMenu.BoolValue(HoolaMenu.ComboMenu, "RKillable") && target.IsValidTarget() &&
                (totaldame(target) >= target.Health
                 && basicdmg(target) <= target.Health) ||
                Player.CountEnemiesInRange(900) >= 2 &&
                (!target.HasBuff("kindrednodeathbuff") && !target.HasBuff("Undying Rage") &&
                 !target.HasBuff("JudicatorIntervention")))
            {
                return true;
            }
            return false;
        }

        private static double totaldame(Obj_AI_Base target)
        {
            if (target != null)
            {
                double dmg = 0;
                double passivenhan = 0;
                if (Player.Level >= 18)
                {
                    passivenhan = 0.5;
                }
                else if (Player.Level >= 15)
                {
                    passivenhan = 0.45;
                }
                else if (Player.Level >= 12)
                {
                    passivenhan = 0.4;
                }
                else if (Player.Level >= 9)
                {
                    passivenhan = 0.35;
                }
                else if (Player.Level >= 6)
                {
                    passivenhan = 0.3;
                }
                else if (Player.Level >= 3)
                {
                    passivenhan = 0.25;
                }
                else
                {
                    passivenhan = 0.2;
                }
                if (HasItem()) dmg = dmg + Player.GetAutoAttackDamage(target)*0.7;
                if (W.IsReady()) dmg = dmg + ObjectManager.Player.GetSpellDamage(target, SpellSlot.W);
                if (Q.IsReady())
                {
                    var qnhan = 4 - QStack;
                    dmg = dmg + ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q)*qnhan +
                          Player.GetAutoAttackDamage(target)*qnhan*(1 + passivenhan);
                }
                dmg = dmg + Player.GetAutoAttackDamage(target)*(1 + passivenhan);
                if (R.IsReady())
                {
                    var rdmg = Rdame(target, Prediction.Health.GetPrediction(target, 250) - dmg*1.2);
                    return dmg*1.2 + rdmg;
                }
                return dmg;
            }
            return 0;
        }

        private static double basicdmg(Obj_AI_Base target)
        {
            if (target != null)
            {
                double dmg = 0;
                double passivenhan = 0;
                if (Player.Level >= 18)
                {
                    passivenhan = 0.5;
                }
                else if (Player.Level >= 15)
                {
                    passivenhan = 0.45;
                }
                else if (Player.Level >= 12)
                {
                    passivenhan = 0.4;
                }
                else if (Player.Level >= 9)
                {
                    passivenhan = 0.35;
                }
                else if (Player.Level >= 6)
                {
                    passivenhan = 0.3;
                }
                else if (Player.Level >= 3)
                {
                    passivenhan = 0.25;
                }
                else
                {
                    passivenhan = 0.2;
                }
                if (HasItem()) dmg = dmg + Player.GetAutoAttackDamage(target)*0.7;
                if (W.IsReady()) dmg = dmg + ObjectManager.Player.GetSpellDamage(target, SpellSlot.W);
                if (Q.IsReady())
                {
                    var qnhan = 4 - QStack;
                    dmg = dmg + ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q)*qnhan +
                          Player.GetAutoAttackDamage(target)*qnhan*(1 + passivenhan);
                }
                dmg = dmg + Player.GetAutoAttackDamage(target)*(1 + passivenhan);
                return dmg;
            }
            return 0;
        }

        private static bool HasItem()
        {
            return Item.HasItem(ItemId.Tiamat_Melee_Only) && Item.CanUseItem(ItemId.Tiamat_Melee_Only) ||
                   Item.CanUseItem(ItemId.Ravenous_Hydra_Melee_Only);
        }

        private static bool InWRange(GameObject target)
        {
            if (target == null || !target.IsValid) return false;
            return (Player.HasBuff("RivenFengShuiEngine"))
                ? 330 >= Player.Distance(target.Position)
                : 265 >= Player.Distance(target.Position);
        }

        private static bool HasTitan()
        {
            return (Item.HasItem(3748) && Item.CanUseItem(3748));
        }

        private static void CastTitan()
        {
            if (Item.HasItem(3748) && Item.CanUseItem(3748))
            {
                Item.UseItem(3748);
                Orbwalker.ResetAutoAttack();
            }
        }

        private static void CastYoumuu()
        {
            if (Item.HasItem(ItemId.Youmuus_Ghostblade) && Item.CanUseItem(ItemId.Youmuus_Ghostblade))
                Item.UseItem(ItemId.Youmuus_Ghostblade);
        }

        private static void ForceItem()
        {
            if (Item.CanUseItem(ItemHue) && Item.HasItem(ItemHue) && ItemHue != 0) forceItem = true;
            Core.DelayAction(() => forceItem = false, 500);
        }

        private static void ForceR()
        {
            forceR = (R.IsReady() && R.Name == IsFirstR);
            Core.DelayAction(() => forceR = false, 500);
        }

        private static void ForceR2()
        {
            forceR2 = R.IsReady() && R.Name == IsSecondR;
            Core.DelayAction(() => forceR2 = false, 500);
        }

        private static void ForceW()
        {
            forceW = W.IsReady();
            Core.DelayAction(() => forceW = false, 500);
        }

        private static void ForceCastQ(AttackableUnit target)
        {
            forceQ = true;
            QTarget = target;
        }

        private static void FlashW()
        {
            var target = TargetSelector.SelectedTarget;
            if (target == null || !target.IsValidTarget())
                target = TargetSelector.GetTarget(450 + Player.AttackRange + 70, DamageType.Physical);
            if (target == null || !target.IsValidTarget()) return;
            if (target.IsValidTarget() && !target.IsZombie)
            {
                W.Cast();
                Chat.Print("Zk");
                Core.DelayAction(() => EloBuddy.Player.CastSpell(Flash.Slot, target.Position), 10);
            }
        }

        private static double Rdame(Obj_AI_Base target, double health)
        {
            if (target != null)
            {
                var missinghealth = (target.MaxHealth - health)/target.MaxHealth > 0.75
                    ? 0.75
                    : (target.MaxHealth - health)/target.MaxHealth;
                var pluspercent = missinghealth*(8/3);
                var rawdmg = new double[] {80, 120, 160}[R.Level - 1] + 0.6*Player.FlatPhysicalDamageMod;
                return Player.CalculateDamageOnUnit(target, DamageType.Physical, (float) (rawdmg*(1 + pluspercent)));
            }
            return 0;
        }
    }
}