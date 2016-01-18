using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using REBURANKU.Utility;
using SharpDX;
using Color = System.Drawing.Color;

namespace Leblanc
{
    internal class Program
    {
        private static readonly List<Slide> ExistingSlide = new List<Slide>();
        public static Spell.Targeted Q, R;
        public static Spell.Skillshot W, E;
        private static ComboType vComboType = ComboType.ComboQR;
        private static ComboKill vComboKill = ComboKill.FullCombo;
        private static bool _isComboCompleted = true;
        public static SpellSlot IgniteSlot;
        public static Item Fqc = new Item(3092, 750);
        public static Menu LeblancMenu, ComboMenu, HarassMenu, KillStealMenu, RunMenu, ExtrasMenu, DrawingsMenu;
        public static bool LeBlancClone { get; set; }

        private static float GetRQDamage
        {
            get
            {
                var xDmg = 0f;
                var perDmg = new[] {100f, 200f, 300};

                xDmg += ((ObjectManager.Player.BaseAbilityDamage + ObjectManager.Player.FlatMagicDamageMod)*.65f) +
                        perDmg[R.Level - 1];
                var t = TargetSelector.GetTarget(2000, DamageType.Magical);
                if (t.IsValidTarget(2000))
                    if (vComboType == ComboType.ComboQR)
                    {
                        xDmg += QDamage(t);
                    }
                if (vComboType != ComboType.ComboQR)
                {
                    xDmg += EDamage(t);
                }
                return xDmg;
            }
        }

        private static float GetRWDamage
        {
            get
            {
                var xDmg = 0f;
                var perDmg = new[] {150f, 300f, 450f};
                xDmg += ((ObjectManager.Player.BaseAbilityDamage + ObjectManager.Player.FlatMagicDamageMod)*.98f) +
                        perDmg[R.Level - 1];

                var t = TargetSelector.GetTarget(2000, DamageType.Magical);
                if (t.IsValidTarget(2000))
                    xDmg += WDamage(t);

                return xDmg;
            }
        }

        private static Obj_AI_Base EnemyHaveSoulShackle
        {
            get
            {
                return
                    (from hero in
                        ObjectManager.Get<Obj_AI_Base>().Where(hero => ObjectManager.Player.Distance(hero) <= 1100)
                        where hero.IsEnemy
                        from buff in hero.Buffs
                        where buff.Name.Contains("LeblancSoulShackle")
                        select hero).FirstOrDefault();
            }
        }

        private static bool DrawEnemySoulShackle
        {
            get
            {
                return
                    (from hero in
                        ObjectManager.Get<Obj_AI_Base>().Where(hero => ObjectManager.Player.Distance(hero) <= 1100)
                        where hero.IsEnemy
                        from buff in hero.Buffs
                        select (buff.Name.Contains("LeblancSoulShackle"))).FirstOrDefault();
            }
        }

        public static bool LeBlancStillJumped
        {
            get
            {
                return !W.IsReady() || ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "leblancslidereturn";
            }
        }

        private static HitChance GetEHitChance
        {
            get
            {
                HitChance hitChance;
                var eHitChance = Config.ComboEHitchance;
                switch (eHitChance)
                {
                    case 0:
                    {
                        hitChance = HitChance.Low;
                        break;
                    }
                    case 1:
                    {
                        hitChance = HitChance.Medium;
                        break;
                    }
                    case 2:
                    {
                        hitChance = HitChance.High;
                        break;
                    }
                    case 3:
                    {
                        hitChance = HitChance.Dashing;
                        break;
                    }
                    case 4:
                    {
                        hitChance = HitChance.Immobile;
                        break;
                    }
                    default:
                    {
                        hitChance = HitChance.High;
                        break;
                    }
                }
                return hitChance;
            }
        }

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != "Leblanc") return;
            Q = new Spell.Targeted(SpellSlot.Q, 720);
            W = new Spell.Skillshot(SpellSlot.W, 760, SkillShotType.Circular, int.MaxValue, 1450, 220);
            E = new Spell.Skillshot(SpellSlot.E, 900, SkillShotType.Linear, 0, 1650, 55);
            R = new Spell.Targeted(SpellSlot.R, 720);
            IgniteSlot = ObjectManager.Player.GetSpellSlotFromName("summonerdot");
            LeblancMenu = MainMenu.AddMenu("Lebranku", "Lebranku");
            ComboMenu = LeblancMenu.AddSubMenu("Combo", "Combo");
            ComboMenu.Add("showcomboinfo", new CheckBox("Show Combo Info"));
            ComboMenu.AddSeparator();
            StringList(ComboMenu, "ComboSetOption", "Combo", new[] {"Auto", "Q-R Combo", "W-R Combo", "E-R Combo"}, 1);
            StringList(ComboMenu, "ComboSetEHitCh", "E Hit", new[] {"Low", "Medium", "High", "Dashing", "Immobile"}, 0);
            ComboMenu.Add("ComboDblStun",
                new KeyBind("Double Stun", false, KeyBind.BindTypes.HoldActive, "T".ToCharArray()[0]));

            HarassMenu = LeblancMenu.AddSubMenu("Harass", "harass");
            HarassMenu.Add("harassshowinfo", new CheckBox("Show Harass info"));
            HarassMenu.AddSeparator();
            HarassMenu.Add("useqharass", new CheckBox("Use Q"));
            HarassMenu.Add("HarassUseTQ",
                new KeyBind("Use Q (toggle)", false, KeyBind.BindTypes.PressToggle, "J".ToCharArray()[0]));
            HarassMenu.Add("HarassManaQ", new Slider("Q Min. Mana Percent", 50, 100, 0));
            HarassMenu.Add("usewharass", new CheckBox("Use W"));
            HarassMenu.Add("HarassUseTW",
                new KeyBind("Use W (toggle)", false, KeyBind.BindTypes.PressToggle, "K".ToCharArray()[0]));
            HarassMenu.Add("HarassManaW", new Slider("W Min. Mana Percent", 50, 100, 0));
            HarassMenu.Add("useeharass", new CheckBox("Use E"));
            HarassMenu.Add("HarassUseTE",
                new KeyBind("Use E (toggle)", false, KeyBind.BindTypes.PressToggle, "L".ToCharArray()[0]));
            HarassMenu.Add("HarassManaE", new Slider("E Min. Mana Percent", 50, 100, 0));

            KillStealMenu = LeblancMenu.AddSubMenu("Kill Steal", "killsteal");
            KillStealMenu.AddSeparator();
            KillStealMenu.Add("KUse_q", new CheckBox("Use Q", false));
            KillStealMenu.Add("KUse_w", new CheckBox("Use W", false));
            KillStealMenu.Add("KUse_e", new CheckBox("Use E", false));
            KillStealMenu.AddSeparator();
            KillStealMenu.Add("KUse_q2", new CheckBox("Use RQ", false));
            KillStealMenu.Add("KUse_w2", new CheckBox("Use RW", false));
            KillStealMenu.Add("KUse_wr", new CheckBox("Use WR", false));
            KillStealMenu.Add("KUse_e2", new CheckBox("Use RE", false));

            DrawingsMenu = LeblancMenu.AddSubMenu("Drawings", "drawings");
            DrawingsMenu.Add("drawq", new CheckBox("Draw Q"));
            DrawingsMenu.Add("draww", new CheckBox("Draw W"));
            DrawingsMenu.Add("drawe", new CheckBox("Draw E"));
            DrawingsMenu.Add("drawdamagebar", new CheckBox("Draw Damage Bar indicator"));
            DrawingsMenu.Add("activeerange", new CheckBox("Draw Active E range"));
            DrawingsMenu.Add("wqrange", new CheckBox("W+Q Range"));

            RunMenu = LeblancMenu.AddSubMenu("Run", "Runmenu");
            RunMenu.Add("RunUseW", new CheckBox("Use W"));
            RunMenu.Add("RunUseR", new CheckBox("Use R"));
            ExtrasMenu = LeblancMenu.AddSubMenu("Extras", "Extras");
            ExtrasMenu.Add("interruptspells", new CheckBox("Interrupt Spells"));

            Game.OnTick += Game_OnTick;
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnEndScene += OnEndScene.Drawing_OnEndScene;
            Drawing.OnDraw += Drawing_OnDraw;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Interrupter.OnInterruptableSpell += Interrupter_OnPosibleToInterrupt;
        }

        public static void StringList(Menu menu, string uniqueId, string displayName, string[] values, int defaultValue)
        {
            var mode = menu.Add(uniqueId, new Slider(displayName, defaultValue, 0, values.Length - 1));
            mode.DisplayName = displayName + ": " + values[mode.CurrentValue];
            mode.OnValueChange +=
                delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
                {
                    sender.DisplayName = displayName + ": " + values[args.NewValue];
                };
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Config.ComboDoubleStun)
            {
                Drawing.DrawText(Drawing.Width*0.45f, Drawing.Height*0.78f, Color.Red, "Double Stun Active!");
            }
            var t = TargetSelector.GetTarget(W.Range*2, DamageType.Physical);
            var xComboText = "Combo Kill";
            if (t.IsValidTarget(W.Range))
            {
                if (t.Health < GetComboDamage(t))
                {
                    vComboKill = ComboKill.FullCombo;
                    Drawing.DrawText(t.HPBarPosition.X + 145, t.HPBarPosition.Y + 20, Color.Red, xComboText);
                }
            }

            else if (t.IsValidTarget(W.Range*2 - 30))
            {
                if (t.Health < GetComboDamage(t) - ObjectManager.Player.GetSpellDamage(t, SpellSlot.W))
                {
                    vComboKill = ComboKill.WithoutW;
                    xComboText = "Jump + " + xComboText;
                    Drawing.DrawText(t.HPBarPosition.X + 145, t.HPBarPosition.Y + 20, Color.Beige, xComboText);
                }
            }
            if (Config.ShowComboInfo)
            {
                var xComboStr = "Combo Mode: ";
                var xCombo = Config.ComboOption;
                switch (xCombo)
                {
                    case 0:
                        xComboStr += "Auto";
                        break;

                    case 1: //Q-R
                        xComboStr += "Q-R";
                        break;

                    case 2: //W-R
                        xComboStr += "W-R";
                        break;

                    case 3: //E-R
                        xComboStr += "E-R";
                        break;
                }
                Drawing.DrawText(Drawing.Width*0.45f, Drawing.Height*0.80f, Color.Green, xComboStr);
            }

            if (Config.ShowHarassInfo)
            {
                var xHarassInfo = "";
                if (Config.HarassUseQToggle)
                    xHarassInfo += "Q - ";

                if (Config.HarassUseWToggle)
                    xHarassInfo += "W - ";

                if (Config.HarassUseEToggle)
                    xHarassInfo += "E - ";
                if (xHarassInfo.Length < 1)
                {
                    xHarassInfo = "Harass Toggle: OFF   ";
                }
                else
                {
                    xHarassInfo = "Harass Toggle: " + xHarassInfo;
                }
                xHarassInfo = xHarassInfo.Substring(0, xHarassInfo.Length - 3);
                Drawing.DrawText(Drawing.Width*0.44f, Drawing.Height*0.82f, Color.DarkCyan, xHarassInfo);
            }

            var color = new ColorBGRA(255, 255, 255, 100);
            if (Config.DrawQ && Q.IsReady())
            {
                Circle.Draw(color, Q.Range, ObjectManager.Player.Position);
            }

            if (Config.DrawW && W.IsReady())
            {
                Circle.Draw(color, W.Range, ObjectManager.Player.Position);
            }
            if (Config.DrawE && E.IsReady())
            {
                Circle.Draw(color, E.Range, ObjectManager.Player.Position);
            }

            if (Config.DrawWplusQRange && Q.IsReady() && W.IsReady())
            {
                Circle.Draw(color, W.Range + Q.Range, ObjectManager.Player.Position);
            }

            if (Config.DrawActiveERange && EnemyHaveSoulShackle != null)
            {
                Circle.Draw(color, 1100f, ObjectManager.Player.Position);
            }
        }

        private static void Game_OnTick(EventArgs args)
        {
            if (Config.UseQKS && Q.IsReady())
            {
                var QTarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (QTarget == null) return;
                if (QTarget.Health <= QDamage(QTarget))
                {
                    Q.Cast(QTarget);
                    return;
                }
            }
            if (Config.UseWKS && W.IsReady() &&
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "LeblancSlide")
            {
                var WTarget = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                if (WTarget == null) return;
                if (WTarget.Health <= WDamage(WTarget))
                {
                    W.Cast(WTarget.ServerPosition);
                    return;
                }
            }
            if (Config.UseEKS && E.IsReady())
            {
                var ETarget = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                if (ETarget == null) return;
                if (ETarget.Health <= EDamage(ETarget))
                {
                    var pred = E.GetPrediction(ETarget);
                    if (pred.HitChance == GetEHitChance)
                    {
                        var predictE = Prediction.Position.PredictLinearMissile(ETarget, E.Range, 55, 250, 1600, 0);
                        E.Cast(predictE.CastPosition);
                    }
                    return;
                }
            }
            if (Config.UseRQKS && R.IsReady() &&
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancChaosOrbM")
            {
                var QTarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (QTarget == null) return;
                if (QTarget.Health <= RQDamage(QTarget))
                {
                    R.Cast(QTarget);
                    return;
                }
            }
            if (Config.UseRWKS && R.IsReady() &&
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSlideM")
            {
                var WTarget = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                if (WTarget == null) return;
                if (WTarget.Health <= WDamage(WTarget))
                {
                    R.Cast(WTarget.ServerPosition);
                    return;
                }
            }

            if (Config.UseWRKS && R.IsReady() && E.IsReady())
            {
                var WTarget = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                if (WTarget == null) return;
                if (WTarget.Health <= WDamage(WTarget)*2)
                {
                    W.Cast(WTarget.ServerPosition);
                    if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSlideM")
                    {
                        R.Cast(WTarget.ServerPosition);
                    }
                    return;
                }
            }
            if (Config.UseREKS && R.IsReady() &&
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSoulShackleM")
            {
                var ETarget = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                if (ETarget == null) return;
                if (ETarget.Health <= ObjectManager.Player.GetSpellDamage(ETarget, SpellSlot.E))
                {
                    var pred = E.GetPrediction(ETarget);
                    if (pred.HitChance == HitChance.Low)
                    {
                        R.Cast(ETarget);
                    }
                }
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
                return;

            RefreshComboType();

            _isComboCompleted = !R.IsReady();

            if (Config.ComboDoubleStun)
                DoubleStun();


            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
                Run();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                Combo();

            DoToggleHarass();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                Harass();
        }

        public static float QDamage(Obj_AI_Base target)
        {
            return ObjectManager.Player.CalculateDamageOnUnit(target, DamageType.Magical,
                new[] {55, 80, 105, 130, 155}[Q.Level - 1] +
                0.4f*ObjectManager.Player.FlatMagicDamageMod);
        }

        public static float WDamage(Obj_AI_Base target)
        {
            return ObjectManager.Player.CalculateDamageOnUnit(target, DamageType.Magical,
                new[] {85, 125, 165, 205, 245}[W.Level - 1] +
                0.6f*ObjectManager.Player.FlatMagicDamageMod);
        }

        public static float EDamage(Obj_AI_Base target)
        {
            return ObjectManager.Player.CalculateDamageOnUnit(target, DamageType.Magical,
                new[] {40, 65, 90, 115, 140}[E.Level - 1] +
                0.5f*ObjectManager.Player.FlatMagicDamageMod);
        }

        public static float E2Damage(Obj_AI_Base target)
        {
            return ObjectManager.Player.CalculateDamageOnUnit(target, DamageType.Magical,
                new[] {40, 65, 90, 115, 140}[E.Level - 1] +
                0.5f*ObjectManager.Player.FlatMagicDamageMod);
        }

        public static float RQDamage(Obj_AI_Base target)
        {
            return ObjectManager.Player.CalculateDamageOnUnit(target, DamageType.Magical,
                new[] {100, 200, 300}[R.Level - 1] +
                0.65f*ObjectManager.Player.FlatMagicDamageMod);
        }

        private static void DoToggleHarass()
        {
            if (Config.HarassUseQToggle)
            {
                var t = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (t != null && Q.IsReady() &&
                    ObjectManager.Player.ManaPercent >=
                    Config.HarassMinManaQ)
                    Q.Cast(t);
            }

            if (Config.HarassUseWToggle)
            {
                var t = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                if (t != null && W.IsReady() && !LeBlancStillJumped &&
                    ObjectManager.Player.ManaPercent >=
                    Config.HarassMinManaW)
                    W.Cast(t);
            }

            if (Config.HarassUseEToggle)
            {
                var t = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                if (t != null && E.IsReady() &&
                    ObjectManager.Player.ManaPercent >=
                    Config.HarassMinManaE)
                {
                    var pred = E.GetPrediction(t);
                    if (pred.HitChance >= GetEHitChance)
                    {
                        var predictE = Prediction.Position.PredictLinearMissile(t, E.Range, 55, 250, 1600, 0);
                        E.Cast(predictE.CastPosition);
                    }
                }
            }
        }

        private static void RefreshComboType()
        {
            var xCombo = Config.ComboOption;
            switch (xCombo)
            {
                case 0:
                    vComboType = Q.Level > W.Level ? ComboType.ComboQR : ComboType.ComboWR;
                    break;
                case 1: //Q-R
                    vComboType = ComboType.ComboQR;
                    break;
                case 2: //W-R
                    vComboType = ComboType.ComboWR;
                    break;
                case 3: //E-R
                    vComboType = ComboType.ComboER;
                    break;
            }
        }

        private static void Combo()
        {
            var cdQEx = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).CooldownExpires;
            var cdWEx = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).CooldownExpires;
            var cdEEx = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).CooldownExpires;

            var cdQ = Game.Time < cdQEx ? cdQEx - Game.Time : 0;
            var cdW = Game.Time < cdWEx ? cdWEx - Game.Time : 0;
            var cdE = Game.Time < cdEEx ? cdEEx - Game.Time : 0;

            var t = TargetSelector.GetTarget(Q.Range*2, DamageType.Magical);

            if (!t.IsValidTarget())
                return;

            if (vComboKill == ComboKill.WithoutW && !LeBlancStillJumped)
            {
                W.Cast(t.ServerPosition);
            }

            if (R.IsReady())
            {
                if (vComboType == ComboType.Auto)
                {
                    if (Q.Level > W.Level)
                    {
                        if (Q.IsReady())
                            ExecuteCombo();
                    }
                    else
                    {
                        if (W.IsReady())
                            ExecuteCombo();
                    }
                }
                else if ((vComboType == ComboType.ComboQR && Q.IsReady()) ||
                         (vComboType == ComboType.ComboWR && W.IsReady()) ||
                         (vComboType == ComboType.ComboER && E.IsReady()))
                    ExecuteCombo();
                else
                {
                    if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancChaosOrbM") // R-Q
                    {
                        t = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                        if (t.IsValidTarget(Q.Range) &&
                            t.Health < GetRQDamage + QDamage(t))
                            R.Cast(t);
                    }
                    if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSlideM") // R-W
                    {
                        t = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                        if (t.IsValidTarget(W.Range) &&
                            t.Health < GetRQDamage + QDamage(t))
                            R.Cast(t);
                        ObjectManager.Player.Spellbook.CastSpell(SpellSlot.R, t, false);
                    }
                    if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSoulShackleM") // R-E
                    {
                        t = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                        if (t.IsValidTarget(E.Range) &&
                            t.Health < GetRQDamage + QDamage(t))
                        {
                            var pred = E.GetPrediction(t);
                            if (pred.HitChance >= GetEHitChance)
                            {
                                var predictE = Prediction.Position.PredictLinearMissile(t, E.Range, 55, 250, 1600, 0);
                                R.Cast(predictE.CastPosition);
                            }
                        }
                    }
                    _isComboCompleted = true;
                }
                return;
            }

            if (Q.IsReady() && t.IsValidTarget(Q.Range) && _isComboCompleted)
            {
                if (vComboType == ComboType.ComboQR)
                {
                    if (!R.IsReady())
                        Q.Cast(t);
                }
                else
                {
                    Q.Cast(t);
                }
            }

            if (W.IsReady() && t.IsValidTarget(W.Range) && !LeBlancStillJumped && _isComboCompleted)
            {
                if (vComboType == ComboType.ComboWR)
                {
                    if (!R.IsReady())

                        W.Cast(t);
                }
                else
                {
                    W.Cast(t);
                }
            }

            if (E.IsReady() && t.IsValidTarget(E.Range) && _isComboCompleted)
            {
                if (vComboType == ComboType.ComboER)
                {
                    if (!R.IsReady())
                    {
                        var pred = E.GetPrediction(t);
                        if (pred.HitChance >= GetEHitChance)
                        {
                            var predictE = Prediction.Position.PredictLinearMissile(t, E.Range, 55, 250, 1600, 0);
                            E.Cast(predictE.CastPosition);
                        }
                    }
                }
                else
                {
                    var pred = E.GetPrediction(t);
                    if (pred.HitChance >= GetEHitChance)
                    {
                        var predictE = Prediction.Position.PredictLinearMissile(t, E.Range, 55, 250, 1600, 0);
                        E.Cast(predictE.CastPosition);
                    }
                }
            }

            if (t != null && IgniteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (ObjectManager.Player.Distance(t) < 650 &&
                    ObjectManager.Player.GetSummonerSpellDamage(t, DamageLibrary.SummonerSpells.Ignite) >= t.Health)
                {
                    ObjectManager.Player.Spellbook.CastSpell(IgniteSlot, t);
                }
            }
        }

        private static void ExecuteCombo()
        {
            if (!R.IsReady())
                return;

            _isComboCompleted = false;

            Obj_AI_Base t;
            var cdQEx = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).CooldownExpires;
            var cdWEx = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).CooldownExpires;
            var cdEEx = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).CooldownExpires;

            var cdQ = Game.Time < cdQEx ? cdQEx - Game.Time : 0;
            var cdW = Game.Time < cdWEx ? cdWEx - Game.Time : 0;
            var cdE = Game.Time < cdEEx ? cdEEx - Game.Time : 0;

            if (vComboType == ComboType.ComboQR && Q.IsReady())
            {
                t = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (t == null)
                    return;

                Q.Cast(t);
                R.Cast(t);
            }

            if (vComboType == ComboType.ComboWR && W.IsReady())
            {
                t = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                if (t == null)
                    return;

                if (!LeBlancStillJumped)
                    W.Cast(t);
                R.Cast(t);
            }

            if (vComboType == ComboType.ComboER && E.IsReady())
            {
                t = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                if (t == null)
                    return;

                E.Cast(t);
                R.Cast(t);
            }
            _isComboCompleted = true;

            t = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            UserSummoners(t);
        }

        private static void Harass()
        {
            var qTarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var wTarget = TargetSelector.GetTarget(W.Range, DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range, DamageType.Magical);

            var useQ = Config.HarassUseQ &&
                       ObjectManager.Player.ManaPercent >= Config.HarassMinManaQ;
            var useW = Config.HarassUseW &&
                       ObjectManager.Player.ManaPercent >= Config.HarassMinManaW;
            var useE = Config.HarassUseE &&
                       ObjectManager.Player.ManaPercent >= Config.HarassMinManaE;

            if (useQ && qTarget != null && Q.IsReady())
                Q.Cast(qTarget);

            if (useW && wTarget != null && W.IsReady())
                W.Cast(wTarget);

            if (useE && eTarget != null && E.IsReady())
            {
                var pred = E.GetPrediction(eTarget);
                if (pred.HitChance >= GetEHitChance)
                {
                    var predictE = Prediction.Position.PredictLinearMissile(eTarget, E.Range, 55, 250, 1600, 0);
                    E.Cast(predictE.CastPosition);
                }
            }
        }

        private static void Run()
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, ObjectManager.Player.Position.Extend(Game.CursorPos, 600).To3D());

            var useW = Config.RunUseW;
            var useR = Config.RunUseR;

            if (useW && W.IsReady() && !LeBlancStillJumped)
                W.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, 600).To3D());

            if (useR && R.IsReady() && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSlideM")
                R.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, 600).To3D());
        }

        private static void DoubleStun()
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            if (Config.ComboDoubleStun)
            {
                foreach (var enemy in
                    ObjectManager.Get<Obj_AI_Base>()
                        .Where(
                            enemy =>
                                enemy.IsEnemy && !enemy.IsDead && enemy.IsVisible && !enemy.IsMinion &&
                                ObjectManager.Player.Distance(enemy) < E.Range + 200 && !xEnemyHaveSoulShackle(enemy)))
                {
                    if (E.IsReady() && ObjectManager.Player.Distance(enemy) < E.Range)
                    {
                        var pred = E.GetPrediction(enemy);
                        if (pred.HitChance >= GetEHitChance)
                        {
                            var predictE = Prediction.Position.PredictLinearMissile(enemy, E.Range, 55, 250, 1600, 0);
                            E.Cast(predictE.CastPosition);
                        }
                    }
                    else if (R.IsReady() && ObjectManager.Player.Distance(enemy) < E.Range &&
                             ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSoulShackleM")
                    {
                        var pred = E.GetPrediction(enemy);
                        if (pred.HitChance >= GetEHitChance)
                        {
                            var predictE = Prediction.Position.PredictLinearMissile(enemy, E.Range, 55, 250, 1600, 0);
                            R.Cast(predictE.CastPosition);
                        }
                    }
                }
            }
        }

        private static bool xEnemyHaveSoulShackle(Obj_AI_Base vTarget)
        {
            return (vTarget.HasBuff("LeblancSoulShackle"));
        }

        private static float GetComboDamage(Obj_AI_Base t)
        {
            var fComboDamage = 0f;

            if (!t.IsValidTarget(2000))
                return 0f;

            fComboDamage += Q.IsReady() ? QDamage(t) : 0;

            fComboDamage += W.IsReady() ? WDamage(t) : 0;

            fComboDamage += E.IsReady() ? EDamage(t) : 0;

            if (R.IsReady())
            {
                if (vComboType == ComboType.ComboQR || vComboType == ComboType.ComboER)
                {
                    fComboDamage += GetRQDamage;
                }

                if (vComboType == ComboType.ComboWR)
                {
                    fComboDamage += GetRWDamage;
                }
            }

            fComboDamage += IgniteSlot != SpellSlot.Unknown &&
                            ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready
                ? ObjectManager.Player.GetSummonerSpellDamage(t, DamageLibrary.SummonerSpells.Ignite)
                : 0f;

            fComboDamage += Item.CanUseItem(3092)
                ? ObjectManager.Player.GetItemDamage(t, ItemId.Frost_Queens_Claim)
                : 0;

            return fComboDamage;
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit,
            Interrupter.InterruptableSpellEventArgs spell)
        {
            if (!Config.ExtrasInterruptSpells)
                return;

            var isValidTarget = unit.IsValidTarget(E.Range) && spell.DangerLevel == DangerLevel.High;

            if (E.IsReady() && isValidTarget)
            {
                var pred = E.GetPrediction(unit);
                if (pred.HitChance >= GetEHitChance)
                {
                    var predictE = Prediction.Position.PredictLinearMissile(unit, E.Range, 55, 250, 1600, 0);
                    E.Cast(predictE.CastPosition);
                }
            }
            else if (R.IsReady() && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSoulShackleM" &&
                     isValidTarget)
            {
                R.Cast(unit);
            }
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            LeBlancClone = sender.Name.Contains("LeBlanc_MirrorImagePoff.troy");

            if (sender.Name.Contains("displacement_blink_indicator"))
            {
                ExistingSlide.Add(
                    new Slide
                    {
                        Object = sender,
                        NetworkId = sender.NetworkId,
                        Position = sender.Position,
                        ExpireTime = Game.Time + 4
                    });
            }
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (!sender.Name.Contains("displacement_blink_indicator"))
                return;

            for (var i = 0; i < ExistingSlide.Count; i++)
            {
                if (ExistingSlide[i].NetworkId == sender.NetworkId)
                {
                    ExistingSlide.RemoveAt(i);
                    return;
                }
            }
        }

        private static void UserSummoners(Obj_AI_Base t)
        {
            if (Fqc.IsReady())
                Fqc.Cast(t.ServerPosition);
        }

        public static class Config
        {
            public static int ComboOption
            {
                get { return ComboMenu["ComboSetOption"].Cast<Slider>().CurrentValue; }
            }

            public static int ComboEHitchance
            {
                get { return ComboMenu["ComboSetEHitCh"].Cast<Slider>().CurrentValue; }
            }

            public static bool ComboDoubleStun
            {
                get { return ComboMenu["ComboDblStun"].Cast<KeyBind>().CurrentValue; }
            }

            public static bool HarassUseQ
            {
                get { return HarassMenu["useqharass"].Cast<CheckBox>().CurrentValue; }
            }

            public static int HarassMinManaQ
            {
                get { return HarassMenu["HarassManaQ"].Cast<Slider>().CurrentValue; }
            }

            public static bool HarassUseQToggle
            {
                get { return HarassMenu["HarassUseTQ"].Cast<KeyBind>().CurrentValue; }
            }

            public static bool HarassUseW
            {
                get { return HarassMenu["usewharass"].Cast<CheckBox>().CurrentValue; }
            }

            public static int HarassMinManaW
            {
                get { return HarassMenu["HarassManaW"].Cast<Slider>().CurrentValue; }
            }

            public static bool HarassUseWToggle
            {
                get { return HarassMenu["HarassUseTW"].Cast<KeyBind>().CurrentValue; }
            }

            public static bool HarassUseE
            {
                get { return HarassMenu["useeharass"].Cast<CheckBox>().CurrentValue; }
            }

            public static int HarassMinManaE
            {
                get { return HarassMenu["HarassManaE"].Cast<Slider>().CurrentValue; }
            }

            public static bool HarassUseEToggle
            {
                get { return HarassMenu["HarassUseTE"].Cast<KeyBind>().CurrentValue; }
            }

            public static bool RunUseW
            {
                get { return RunMenu["RunUseW"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool RunUseR
            {
                get { return RunMenu["RunUseR"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool ExtrasInterruptSpells
            {
                get { return ExtrasMenu["interruptspells"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool DrawQ
            {
                get { return DrawingsMenu["drawq"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool DrawW
            {
                get { return DrawingsMenu["draww"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool DrawE
            {
                get { return DrawingsMenu["drawe"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool ShowComboInfo
            {
                get { return ComboMenu["showcomboinfo"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool ShowHarassInfo
            {
                get { return HarassMenu["harassshowinfo"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool DrawWObjTick
            {
                get { return DrawingsMenu["ObjTimeTick"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool DrawActiveERange
            {
                get { return DrawingsMenu["activeerange"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool DrawWplusQRange
            {
                get { return DrawingsMenu["wqrange"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool DrawDamageBarIndicator
            {
                get { return DrawingsMenu["drawdamagebar"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool UseQKS
            {
                get { return KillStealMenu["KUse_q"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool UseWKS
            {
                get { return KillStealMenu["KUse_w"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool UseEKS
            {
                get { return KillStealMenu["KUse_e"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool UseRQKS
            {
                get { return KillStealMenu["KUse_q2"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool UseRWKS
            {
                get { return KillStealMenu["KUse_w2"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool UseWRKS
            {
                get { return KillStealMenu["KUse_wr"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool UseREKS
            {
                get { return KillStealMenu["KUse_e2"].Cast<CheckBox>().CurrentValue; }
            }
        }

        private enum ComboType
        {
            Auto,
            ComboQR,
            ComboWR,
            ComboER
        }

        private enum ComboKill
        {
            None,
            FullCombo,
            WithoutW
        }
    }
}