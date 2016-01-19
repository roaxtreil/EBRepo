using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Constants;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using HoolaRiven.Utility;

namespace HoolaRiven
{
    public class Program
    {
        private const string IsFirstR = "RivenFengShuiEngine";
        private const string IsSecondR = "rivenizunablade";
        public static Orbwalking.Orbwalker Orbwalker;
        private static readonly AIHeroClient Player = ObjectManager.Player;
        private static readonly Spell.Active Q = new Spell.Active(SpellSlot.Q);
        private static readonly Spell.Active W = new Spell.Active(SpellSlot.W);
        private static readonly Spell.Targeted E = new Spell.Targeted(SpellSlot.E, 300);
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
       
        private static void Main()
        {
            Loading.OnLoadingComplete += OnGameLoad;
        }

        public static Menu MenuPrincipal, ComboMenu, LaneClear, MiscMenu;

        public static bool BoolValue(Menu menu, string menuvalue)
        {
            return menu[menuvalue].Cast<CheckBox>().CurrentValue;
        }

        public static bool KeybindValue(Menu menu, string menuvalue)
        {
            return menu[menuvalue].Cast<KeyBind>().CurrentValue;
        }

        public static int SliderValue(Menu menu, string menuvalue)
        {
            return menu[menuvalue].Cast<Slider>().CurrentValue;
        }
        public static void InitializeMenu()
        {
            MenuPrincipal = MainMenu.AddMenu("Hoola Riven", "hoola");
            var orbwalker = MainMenu.AddMenu("Hoola Orbwalker", "rorb");
            Orbwalker = new Orbwalking.Orbwalker(orbwalker);
            ComboMenu = MenuPrincipal.AddSubMenu("Combo Settings");
            ComboMenu.Add("AlwaysR",
                new KeyBind("Always Use R (Toggle)", false, KeyBind.BindTypes.PressToggle, "H".ToCharArray()[0]));
            ComboMenu.Add("UseHoola",
                new KeyBind("Use Hoola Combo Logic (Toggle)", true, KeyBind.BindTypes.PressToggle, "L".ToCharArray()[0]));
            ComboMenu.Add("ComboW", new CheckBox("Always use W"));
            ComboMenu.Add("RKillable", new CheckBox("Use R when the target is killable"));

            LaneClear = MenuPrincipal.AddSubMenu("Clear Settings");
            LaneClear.Add("LaneQ", new CheckBox("Use Q"));
            LaneClear.Add("LaneW", new Slider("Use W in X minions", 5, 0, 5));
            LaneClear.Add("LaneE", new CheckBox("Use E"));

            MiscMenu = MenuPrincipal.AddSubMenu("Misc Settings");
            MiscMenu.Add("youmuu", new CheckBox("Use Youmuus when E"));
            MiscMenu.Add("FirstHydra", new CheckBox("Flash burst Hydra before W"));
            MiscMenu.Add("Qstrange", new CheckBox("Strange Q"));
            MiscMenu.Add("Winterrupt", new CheckBox("W to interrupt spells"));
            MiscMenu.Add("AutoW", new Slider("Auto W when x enemies", 5, 0, 5));
            MiscMenu.Add("RMaxDam", new CheckBox("Use second R for max damage"));
            MiscMenu.Add("killstealw", new CheckBox("KillSteal with W"));
            MiscMenu.Add("killstealr", new CheckBox("KillSteal with second R"));
            MiscMenu.Add("AutoShield", new CheckBox("Auto E Shield"));
            MiscMenu.Add("Shield", new CheckBox("Auto E in lasthit mode"));
            MiscMenu.Add("KeepQ", new CheckBox("Keep Q alive"));
            MiscMenu.Add("QD", new Slider("First and second Q Delay", Game.Ping));
            MiscMenu.Add("QLD", new Slider("Third Q Delay", Game.Ping));
        }

        private static void OnGameLoad(EventArgs args)
        {
            SpellDataInst flash = Player.Spellbook.Spells.Where(s => s.Name.Contains("summonerflash")).Any()
                ? Player.Spellbook.Spells.First(spell => spell.Name.Contains("summonerflash")) : null;
            if (flash.Slot != SpellSlot.Unknown)
            {
                Flash = new Spell.Skillshot(flash.Slot, 425, SkillShotType.Linear);
            }

            InitializeMenu();
            Game.OnUpdate += OnTick;
            Obj_AI_Base.OnProcessSpellCast += OnCast;
            Obj_AI_Base.OnSpellCast += OnDoCast;
            Obj_AI_Base.OnSpellCast += OnDoCastLC;
            Obj_AI_Base.OnPlayAnimation += OnPlay;
            Obj_AI_Base.OnProcessSpellCast += OnCasting;
            Interrupter.OnInterruptableSpell += Interrupt;
        }
        public static void DisableOrbwalker()
        {
            EloBuddy.SDK.Orbwalker.DisableAttacking = true;
            EloBuddy.SDK.Orbwalker.DisableMovement = true;
        }

        public static void EnableOrbwalker()
        {
            EloBuddy.SDK.Orbwalker.DisableAttacking = false;
            EloBuddy.SDK.Orbwalker.DisableMovement = false;
        }
        private static void OnTick(EventArgs args)
        {
            if (EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.Combo))
            {
                DisableOrbwalker();
            }
            else
            {
                EnableOrbwalker();
            }
            ForceSkill();
            UseRMaxDam();
            AutoUseW();
            Killsteal();
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) Combo();
            if (EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.LaneClear)) Jungleclear();
            if (EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.Harass)) Harass();
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.FastHarass) FastHarass();
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Burst) Burst();
            if (EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.Flee)) Flee();

            if (Utils.GameTimeTickCount - LastQ >= 3650 && QStack != 1 && !Player.IsRecalling() &&
                BoolValue(MiscMenu, "KeepQ") && Q.IsReady())
                EloBuddy.Player.CastSpell(SpellSlot.Q, Game.CursorPos);
        }

        private static void UseRMaxDam()
        {
            if (BoolValue(MiscMenu, "RMaxDam") && R.IsReady() && R.Name == IsSecondR)
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
            if (BoolValue(MiscMenu, "killstealw") && W.IsReady())
            {
                var targets = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(R.Range) && !x.IsZombie);
                foreach (var target in targets)
                {
                    if (target.Health < Player.GetSpellDamage(target, SpellSlot.W) && InWRange(target))
                        W.Cast();
                }
            }
            if (BoolValue(MiscMenu, "killstealr") && R.IsReady() && R.Name == IsSecondR)
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
            if (SliderValue(MiscMenu, "AutoW") > 0)
            {
                if (Player.CountEnemiesInRange(GetWRange) >= SliderValue(MiscMenu, "AutoW"))
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
                KeybindValue(ComboMenu, "AlwaysR") && targetR != null) ForceR();
            if (R.IsReady() && R.Name == IsFirstR && W.IsReady() && InWRange(targetR) &&
                BoolValue(ComboMenu, "ComboW") &&
                KeybindValue(ComboMenu, "AlwaysR") && targetR != null)
            {
                ForceR();
                Core.DelayAction(ForceW, 1);
            }
            if (W.IsReady() && InWRange(targetR) && BoolValue(ComboMenu, "ComboW") &&
                targetR != null) W.Cast();
            if (KeybindValue(ComboMenu, "UseHoola") && R.IsReady() && R.Name == IsFirstR &&
                W.IsReady() && targetR != null && E.IsReady() && targetR.IsValidTarget() && !targetR.IsZombie &&
                (IsKillableR(targetR) || KeybindValue(ComboMenu, "AlwaysR")))
            {
                if (!InWRange(targetR))
                {
                    E.Cast(targetR.Position);
                    ForceR();
                    Core.DelayAction(ForceW, 200);
                    Core.DelayAction(() => ForceCastQ(targetR), 305);
                }
            }
            else if (!KeybindValue(ComboMenu, "UseHoola") && R.IsReady() && R.Name == IsFirstR &&
                     W.IsReady() && targetR != null && E.IsReady() && targetR.IsValidTarget() && !targetR.IsZombie &&
                     (IsKillableR(targetR) || KeybindValue(ComboMenu, "AlwaysR")))
            {
                if (!InWRange(targetR))
                {
                    E.Cast(targetR.Position);
                    ForceR();
                    Core.DelayAction(ForceW, 200);
                }
            }
            else if (KeybindValue(ComboMenu, "UseHoola") && W.IsReady() && E.IsReady())
            {
                if (targetR.IsValidTarget() && targetR != null && !targetR.IsZombie && !InWRange(targetR))
                {
                    E.Cast(targetR.Position);
                    Core.DelayAction(ForceItem, 10);
                    Core.DelayAction(ForceW, 200);
                    Core.DelayAction(() => ForceCastQ(targetR), 305);
                }
            }
            else if (!KeybindValue(ComboMenu, "UseHoola") && W.IsReady() && targetR != null &&
                     E.IsReady())
            {
                if (targetR.IsValidTarget() && targetR != null && !targetR.IsZombie && !InWRange(targetR))
                {
                    E.Cast(targetR.Position);
                    Core.DelayAction(ForceItem, 10);
                    Core.DelayAction(ForceW, 240);
                }
            }
            else if (E.IsReady())
            {
                if (targetR.IsValidTarget() && !targetR.IsZombie && !InWRange(targetR))
                {
                    E.Cast(targetR.Position);
                }
            }
        }

        private static void Interrupt(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            if (sender.IsEnemy && W.IsReady() && sender.IsValidTarget() && !sender.IsZombie &&
                BoolValue(MiscMenu, "Winterrupt"))
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
                if (EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.LaneClear))
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
                    if (Q.IsReady() && BoolValue(LaneClear, "LaneQ"))
                    {
                        ForceItem();
                        Core.DelayAction(() => ForceCastQ(Minions[0]), 1);
                    }
                    if ((!Q.IsReady() || (Q.IsReady() && !BoolValue(LaneClear, "LaneQ"))) &&
                        W.IsReady() && SliderValue(LaneClear, "LaneW") != 0 &&
                        Minions.Count >= SliderValue(LaneClear, "LaneW"))
                    {
                        ForceItem();
                        Core.DelayAction(ForceW, 1);
                    }
                    if ((!Q.IsReady() || (Q.IsReady() && !BoolValue(LaneClear, "LaneQ"))) &&
                        (!W.IsReady() || (W.IsReady() && SliderValue(LaneClear, "LaneW") == 0) ||
                         Minions.Count() < SliderValue(LaneClear, "LaneW")) &&
                        E.IsReady() && BoolValue(LaneClear, "LaneE"))
                    {
                        E.Cast(Minions[0].Position);
                        Core.DelayAction(ForceItem, 1);
                    }
                }
            }
        }

        private static void OnCasting(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsEnemy && sender.Type == Player.Type &&
                (BoolValue(MiscMenu, "AutoShield") ||
                 (BoolValue(MiscMenu, "Shield") &&
                  Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)))
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
                                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit &&
                                    !args.SData.Name.Contains("NasusW"))
                                {
                                    if (E.IsReady()) E.Cast(epos);
                                }
                            }

                            break;
                        case SpellDataTargetType.SelfAoe:

                            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
                            {
                                if (E.IsReady()) E.Cast(epos);
                            }

                            break;
                    }
                    if (args.SData.Name.Contains("IreliaEquilibriumStrike"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady() && InWRange(sender)) W.Cast();
                            else if (E.IsReady()) E.Cast(epos);
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
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("GarenQAttack"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
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
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("RengarPassiveBuffDash"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("RengarPassiveBuffDashAADummy"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("TwitchEParticle"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("FizzPiercingStrike"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("HungeringStrike"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("YasuoDash"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("KatarinaRTrigger"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady() && InWRange(sender)) W.Cast();
                            else if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("YasuoDash"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
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
            var target = TargetSelector.SelectedTarget;
            if (target != null && target.IsValidTarget() && !target.IsZombie)
            {
                if (R.IsReady() && R.Name == IsFirstR && W.IsReady() && E.IsReady() &&
                    Player.Distance(target.Position) <= 250 + 70 + Player.AttackRange)
                {
                    E.Cast(target.Position);
                    CastYoumuu();
                    ForceR();
                    Core.DelayAction(ForceW, 100);                
                }
                else if (R.IsReady() && R.Name == IsFirstR && E.IsReady() && W.IsReady() && Q.IsReady() &&
                         Player.Distance(target.Position) <= 400 + 70 + Player.AttackRange)
                {
                    E.Cast(target.Position);
                    CastYoumuu();
                    ForceR();
                    Core.DelayAction(() => ForceCastQ(target), 150);
                    if (InWRange(target))
                    {
                        Core.DelayAction(ForceW, 160);
                    }
                }
                else if (Flash.IsReady()
                         && R.IsReady() && R.Name == IsFirstR && (Player.Distance(target.Position) <= 800) &&
                         (!BoolValue(MiscMenu, "FirstHydra") ||
                          (BoolValue(MiscMenu, "FirstHydra") && !HasItem())))
                {
                    E.Cast(target.Position);
                    CastYoumuu();
                    ForceR();
                    Core.DelayAction(FlashW, 180);
                }
                else if (Flash.IsReady()
                         && R.IsReady() && E.IsReady() && W.IsReady() && R.Name == IsFirstR &&
                         (Player.Distance(target.Position) <= 800) &&
                         BoolValue(MiscMenu, "FirstHydra") && HasItem())
                {
                    E.Cast(target.Position);
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
                if (Q.IsReady() && E.IsReady())
                {
                    if (target.IsValidTarget() && !target.IsZombie)
                    {
                        if (!Player.IsInAutoAttackRange(target) && !InWRange(target))
                            E.Cast(target.Position);
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
            if (Q.IsReady() && E.IsReady() && QStack == 3 && !Orbwalking.CanAttack() && Orbwalking.CanMove(5))
            {
                var epos = Player.ServerPosition +
                           (Player.ServerPosition - target.ServerPosition).Normalized()*300;
                E.Cast(epos);
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
            if (E.IsReady() && !Player.IsDashing()) E.Cast(x);
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
                E.Cast(Mobs[0].Position);
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
                    if (BoolValue(MiscMenu, "Qstrange") &&
                        !EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.None)) Chat.Say("/d");
                    QStack = 2;
                    if (!EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.None) &&
                        !EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.LastHit) &&
                        !EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.None))
                        Core.DelayAction(Reset, (SliderValue(MiscMenu, "QD")*10) + 1);
                    break;
                case "Spell1b":
                    LastQ = Utils.GameTimeTickCount;
                    if (BoolValue(MiscMenu, "Qstrange") &&
                        !EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.None)) Chat.Say("/d");
                    QStack = 3;
                    if (!EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.None) &&
                        !EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.LastHit) &&
                        !EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.None))
                        Core.DelayAction(Reset, (SliderValue(MiscMenu, "QD")*10) + 1);
                    break;
                case "Spell1c":
                    LastQ = Utils.GameTimeTickCount;
                    if (BoolValue(MiscMenu, "Qstrange") &&
                        !EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.None)) Chat.Say("/d");
                    QStack = 1;
                    if (!EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.None) &&
                        !EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.LastHit) &&
                        !EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.None))
                        Core.DelayAction(Reset, (SliderValue(MiscMenu, "QLD")*10) + 3);
                    break;
                case "Spell3":
                    if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Burst ||
                         Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo ||
                         Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.FastHarass ||
                         EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.Flee)) &&
                        BoolValue(MiscMenu, "youmuu"))
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
            Orbwalking.LastAATick = 0;
            EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo,
                Player.Position.Extend(Game.CursorPos, Player.Distance(Game.CursorPos) + 10));
        }

        private static void OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe || !AutoAttacks.IsAutoAttack(args.SData.Name)) return;
            QTarget = (Obj_AI_Base) args.Target;

            if (args.Target is Obj_AI_Minion)
            {
                if (EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.LaneClear))
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
                            E.Cast(Mobs[0].Position);
                        }
                    }
                }
            }

            if (args.Target is Obj_AnimatedBuilding)
                if (args.Target.IsValid && args.Target != null && Q.IsReady() &&
                    BoolValue(LaneClear, "LaneQ") &&
                    EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.LaneClear))
                    ForceCastQ((Obj_AI_Base) args.Target);
            if (args.Target is AIHeroClient)
            {
                var target = (AIHeroClient) args.Target;
                if (BoolValue(MiscMenu, "killstealr") && R.IsReady() && R.Name == IsSecondR)
                    if (target.Health < (Rdame(target, Prediction.Health.GetPrediction(target, 250)) + Player.GetAutoAttackDamage(target)) &&
                        target.Health > Player.GetAutoAttackDamage(target)) R.Cast(target.Position);
                if (BoolValue(MiscMenu, "killstealw") && W.IsReady())
                    if (target.Health <
                        (Player.GetSpellDamage(target, SpellSlot.W) + Player.GetAutoAttackDamage(target)) &&
                        target.Health > Player.GetAutoAttackDamage(target)) W.Cast();
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
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
                        E.Cast(target.Position);
                }
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.FastHarass)
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
                        E.Cast(target.Position);
                    }
                }

                if (EloBuddy.SDK.Orbwalker.ActiveModesFlags.HasFlag(EloBuddy.SDK.Orbwalker.ActiveModes.Harass))
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

                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Burst)
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
            if (BoolValue(ComboMenu, "RKillable") && target.IsValidTarget() &&
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
                Orbwalking.LastAATick = 0;
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
            {
                if (target == null || !target.IsValidTarget()) return;
                W.Cast();
                Core.DelayAction(() => Flash.Cast(target.ServerPosition), 10);
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