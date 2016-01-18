using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using Color = System.Drawing.Color;

namespace TD_Draven
{
    internal class Program
    {
        public static List<AIHeroClient> Enemies = new List<AIHeroClient>();
        private static float QMANA, WMANA, EMANA, RMANA;
        private static int axeCatchRange;
        private static GameObject RMissile;
        public static List<Axe> Axes = new List<Axe>();
        public static List<MissileClient> AaMissiles = new List<MissileClient>();
        public static Spell.Skillshot E, R;
        public static Spell.Active Q, W;

        public static Menu DravenMenu,
            AxeSettingsMenu,
            SpellsMenu,
            PredictionMenu,
            DrawingsMenu;

        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        public static bool Farm
        {
            get
            {
                return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) ||
                       Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass);
            }
        }


        public static bool None
        {
            get { return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None); }
        }

        public static bool Combo
        {
            get { return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo); }
        }

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Load;
        }

        public static void Load(EventArgs args)
        {
            if (ObjectManager.Player.Hero != Champion.Draven) return;

            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Skillshot(SpellSlot.E, 950, SkillShotType.Linear, 250, 1600, 130);
            E.AllowedCollisionCount = int.MaxValue;
            R = new Spell.Skillshot(SpellSlot.R, 20000, SkillShotType.Linear, 500, 2000, 155);
            R.AllowedCollisionCount = int.MaxValue;

            DravenMenu = MainMenu.AddMenu("TDDraven", "draven");
            
            AxeSettingsMenu = DravenMenu.AddSubMenu("Axe Settings", "axe");
            AxeSettingsMenu.AddGroupLabel("Axe Settings");
            AxeSettingsMenu.AddSeparator();
            AxeSettingsMenu.Add("axeCatchRange", new Slider("Axe catch range", 500, 200, 2000));
            AxeSettingsMenu.Add("Delay", new Slider("% of delay to catch the axe", 100));
            AxeSettingsMenu.Add("axeTower", new CheckBox("Don't catch axe under enemy turret combo"));
            AxeSettingsMenu.Add("axeTower2", new CheckBox("Don't catch axe under enemy turret farm"));
            AxeSettingsMenu.Add("axeEnemy", new CheckBox("Don't catch axe in enemy group"));
            AxeSettingsMenu.Add("axeKill", new CheckBox("Don't catch axe if can kill 2 AA"));
            AxeSettingsMenu.Add("axePro", new CheckBox("if axe timeout: force laneclea"));
            StringList(AxeSettingsMenu, "CatchMode", "Catch Condition", new[] { "When Orbwalking", "AutoCatch" }, 0);
            StringList(AxeSettingsMenu, "OrbwalkMode", "Catch Mode", new[] { "My Hero in radius", "Mouse in radius" }, 0);
            

            SpellsMenu = DravenMenu.AddSubMenu("Spell Settings", "spell");
            SpellsMenu.AddLabel("Q Settings");
            SpellsMenu.Add("autoQ", new CheckBox("Auto Q"));
            SpellsMenu.Add("farmQ", new CheckBox("Farm Q"));

            SpellsMenu.AddLabel("W Settings");
            SpellsMenu.Add("autoW", new CheckBox("Auto W"));
            SpellsMenu.Add("slowW", new CheckBox("Auto W slow"));

            SpellsMenu.AddLabel("E Settings");
            SpellsMenu.Add("autoE", new CheckBox("Auto E"));
            SpellsMenu.Add("autoE2", new CheckBox("Harras E if can hit 2 targets"));

            SpellsMenu.AddLabel("R Settings");
            SpellsMenu.Add("autoR", new CheckBox("Auto R"));
            StringList(SpellsMenu, "Rdmg", "KS damage calculation", new[] {"X 1", "X 2"}, 1);
            SpellsMenu.Add("comboR", new CheckBox("Auto R in combo"));
            SpellsMenu.Add("Rcc", new CheckBox("R cc"));
            SpellsMenu.Add("Raoe", new CheckBox("R aoe combo"));
            SpellsMenu.Add("hitchanceR", new CheckBox("High HitChance R"));
            SpellsMenu.Add("useR",
                new KeyBind("Semi-manual cast R key", false, KeyBind.BindTypes.HoldActive, "T".ToCharArray()[0]));
            SpellsMenu.AddLabel("Misc");
            SpellsMenu.Add("manaDisable", new CheckBox("Disable mana manager in combo"));

            DrawingsMenu = DravenMenu.AddSubMenu("Drawings", "drawingsmenu");
            DrawingsMenu.AddGroupLabel("Drawings Settings");
            DrawingsMenu.AddSeparator();
            DrawingsMenu.Add("qCatchRange", new CheckBox("Q catch range"));
            DrawingsMenu.Add("qAxePos", new CheckBox("Q axe position"));
            DrawingsMenu.Add("eRange", new CheckBox("E range"));
            DrawingsMenu.Add("noti", new CheckBox("Draw R helper"));
            DrawingsMenu.Add("onlyRdy", new CheckBox("Draw only ready spells"));

            PredictionMenu = DravenMenu.AddSubMenu("Prediction", "prediction");
            PredictionMenu.AddSeparator();
            StringList(PredictionMenu, "Epred", "E Hit Chance", new[] {"Medium", "Average Point", "High"}, 1);
            StringList(PredictionMenu, "Rpred", "R Hit Chance", new[] {"Medium", "Average Point", "High"}, 1);
            foreach (var hero in ObjectManager.Get<AIHeroClient>())
            {
                if (hero.IsEnemy)
                {
                    Enemies.Add(hero);
                }
            }
            GameObject.OnCreate += SpellMissile_OnCreateOld;
            AxesManager.Init(args);
            GameObject.OnDelete += Obj_SpellMissile_OnDelete;
            Orbwalker.OnPreAttack += BeforeAttack;
            GameObject.OnCreate += GameObjectOnOnCreate;
            GameObject.OnDelete += GameObjectOnOnDelete;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += GameOnOnUpdate;
            Gapcloser.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;
            EloBuddy.SDK.Events.Interrupter.OnInterruptableSpell += Interrupter2_OnInterruptableTarget;
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

        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Base sender,
            EloBuddy.SDK.Events.Interrupter.InterruptableSpellEventArgs args)
        {
            if (E.IsReady() && sender.IsValidTarget(E.Range))
            {
                E.Cast(sender);
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs gapcloser)
        {
            if (E.IsReady() && gapcloser.Sender.IsValidTarget(E.Range))
            {
                E.Cast(gapcloser.Sender);
            }
        }

        private static void Obj_SpellMissile_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender is MissileClient == false)
                return;

            var missile = (MissileClient) sender;

            if (missile.IsValid && missile.IsAlly && missile.SData.Name != null && missile.SData.Name == "DravenR")
            {
                RMissile = null;
            }
        }

        private static void SpellMissile_OnCreateOld(GameObject sender, EventArgs args)
        {
            if (sender is MissileClient == false)
                return;

            var missile = (MissileClient) sender;

            if (missile.IsValid && missile.IsAlly && missile.SData.Name != null && missile.SData.Name == "DravenR")
            {
                RMissile = sender;
            }
        }

        private static void BeforeAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (Q.IsReady())
            {
                var buffCount = Utils.GetBuffCount(Player, "dravenspinningattack");
                if (args.Target is AIHeroClient && args.Target.IsValid)
                {
                    if (MenuConfig.autoQ && target.IsValid)
                    {
                        if (buffCount + AaMissiles.Count == 0)
                            Q.Cast();
                        else if (Player.Mana > RMANA + QMANA && buffCount == 0)
                            Q.Cast();
                    }
                    if (Farm && MenuConfig.farmQ)
                    {
                        if (buffCount + AaMissiles.Count == 0 && Player.Mana > RMANA + EMANA + WMANA)
                            Q.Cast();
                        else if (Player.ManaPercent > 70 && buffCount == 0)
                            Q.Cast();
                    }
                }
            }
        }

        private static void GameObjectOnOnCreate(GameObject sender, EventArgs args)
        {
            if (sender is MissileClient)
            {
                var missile = sender as MissileClient;
                if (missile.SpellCaster.IsMe)
                {
                    var name = missile.SData.Name.ToLower();
                    if (name.Contains("dravenspinningattack"))
                    {
                        AaMissiles.Add(missile);
                    }
                    else if (name.Equals("dravenspinningreturncatch") || name.Equals("dravenspinningreturnleftaxe"))
                    {
                        Axes.Add(new Axe(missile));
                    }
                }
            }
        }

        private static void GameObjectOnOnDelete(GameObject sender, EventArgs args)
        {
            if (sender is MissileClient)
            {
                var missile = sender as MissileClient;
                if (missile.SpellCaster.IsMe)
                {
                    var name = missile.SData.Name.ToLower();
                    if (name.Contains("dravenspinningattack"))
                    {
                        AaMissiles.RemoveAll(m => m.NetworkId == sender.NetworkId);
                    }
                }
            }
            else if (sender is Obj_GeneralParticleEmitter)
            {
                var name = sender.Name.ToLower();
                if (name.Contains(Player.ChampionName.ToLower()) && name.Contains("reticle"))
                {
                    if (name.Contains("q_reticle_self.troy"))
                    {
                        Axes.RemoveAll(m => m.Reticle != null && m.Reticle.NetworkId == sender.NetworkId);
                    }
                }
            }
        }

        private static void GameOnOnUpdate(EventArgs args)
        {
            Axes.RemoveAll(a => a.Reticle != null && (!a.Reticle.IsValid || !a.InTime));
            if (ObjectManager.Player.HasBuff("Recall"))
                return;
            axeCatchRange = MenuConfig.axeCatchRange;
            SetMana();

            if (E.IsReady() && MenuConfig.autoE)
                LogicE();

            if (W.IsReady())
                LogicW();

            if (R.IsReady() && !Player.Spellbook.IsAutoAttacking)
                LogicR();
        }

        private static void LogicW()
        {
            if (MenuConfig.autoW && Combo && Player.Mana > RMANA + EMANA + WMANA + QMANA &&
                Player.CountEnemiesInRange(1000) > 0 && !Player.HasBuff("dravenfurybuff"))
                W.Cast();
            else if (MenuConfig.slowW && Player.Mana > RMANA + EMANA + WMANA && Player.HasBuffOfType(BuffType.Slow))
                W.Cast();
        }

       

        public static bool MoveWillChangeReticleEndPosition
        {
            get
            {
                return AaMissiles.Any() && AaMissiles.Any(missile => missile.Target != null && missile.IsValid && missile.Position.Distance(missile.Target) / missile.StartPosition.Distance(missile.Target) <= 0.2f);
            }
        }

        public static Axe FirstAxeInRadius
        {
            get
            {
                return Axes.Where(m => m.InTime && !m.InTurret && m.SourceInRadius).OrderBy(m => m.TimeLeft).FirstOrDefault();
            }
        }

        private static void LogicE()
        {
            foreach (
                var enemy in
                    Enemies.Where(
                        enemy =>
                            enemy.IsValidTarget(E.Range) && !Player.IsInAutoAttackRange(enemy) &&
                            ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.E) > enemy.Health))
            {
                CastSpell(E, enemy);
                return;
            }

            var t = TargetSelector.GetTarget(E.Range, DamageType.Physical);

            if (t.IsValidTarget())
            {
                var EPrediction = E.GetPrediction(t);
                if (Combo)
                {
                    if (Player.Mana > RMANA + EMANA)
                    {
                        if (!Player.IsInAutoAttackRange(t))
                            CastSpell(E, t);
                        if (Player.Health < Player.MaxHealth*0.5)
                            CastSpell(E, t);
                    }

                    if (Player.Mana > RMANA + EMANA + QMANA &&
                        EntityManager.Heroes.Enemies.Count(tt => tt.Distance(EPrediction.CastPosition) <= 100) >= 2)
                        E.Cast(t);
                }
                if (Farm && MenuConfig.autoE2 && Player.Mana > RMANA + EMANA + WMANA + QMANA &&
                    EntityManager.Heroes.Enemies.Count(tt => tt.Distance(EPrediction.CastPosition) <= 100) >= 2)
                {
                    E.Cast(t);
                }
            }
            foreach (var target in Enemies.Where(target => target.IsValidTarget(E.Range)))
            {
                if (target.IsValidTarget(300) && target.IsMelee)
                {
                    CastSpell(E, t);
                }
            }
        }

        private static void LogicR()
        {
            if (MenuConfig.useR)
            {
                var t = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                var RPrediction = R.GetPrediction(t);
                if (t != null && t.IsValidTarget() &&
                    EntityManager.Heroes.Enemies.Count(tt => tt.Distance(RPrediction.CastPosition) <= 100) >= 2)
                {
                    R.Cast(RPrediction.CastPosition);
                }
                if (t.IsValidTarget())
                {
                    R.Cast(t);
                }
            }
            if (MenuConfig.autoR)
            {
                foreach (
                    var target in
                        Enemies.Where(
                            target =>
                                target.IsValidTarget(R.Range) && Utils.ValidUlt(target) &&
                                target.CountAlliesInRange(500) == 0))
                {
                    var predictedHealth = target.Health - (float) Utils.GetIncomingDamage(target);
                    double Rdmg = CalculateR(target);

                    if (Rdmg*2 > predictedHealth && MenuConfig.Rdmg == 1)
                        Rdmg = Rdmg + getRdmg(target);

                    var qDmg = ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q);
                    var eDmg = ObjectManager.Player.GetSpellDamage(target, SpellSlot.E);
                    if (Rdmg > predictedHealth && !ObjectManager.Player.IsInAutoAttackRange(target))
                    {
                        castR(target);
                    }
                    else if (Combo && MenuConfig.comboR && ObjectManager.Player.IsInAutoAttackRange(target) &&
                             Rdmg*2 + Player.GetAutoAttackDamage(target) > predictedHealth)
                    {
                        castR(target);
                    }
                    else if (MenuConfig.Rcc && Rdmg*2 > predictedHealth && !Utils.CanMove(target) &&
                             target.IsValidTarget(E.Range))
                    {
                        R.Cast(target);
                    }
                    else if (Combo && MenuConfig.Raoe)
                    {
                        var RPrediction = R.GetPrediction(target);
                        if (
                            EntityManager.Heroes.Enemies.Count(
                                tt => tt.Distance(RPrediction.CastPosition) <= 100) >= 3)
                        {
                            R.Cast(RPrediction.CastPosition);
                        }
                    }
                    else if (target.IsValidTarget(E.Range) && Rdmg*2 + qDmg + eDmg > predictedHealth &&
                             MenuConfig.Raoe)
                    {
                        var RPrediction = R.GetPrediction(target);
                        if (
                            EntityManager.Heroes.Enemies.Count(
                                tt => tt.Distance(RPrediction.CastPosition) <= 100) >= 2)
                        {
                            R.Cast(RPrediction.CastPosition);
                        }
                    }
                }
            }
        }

        public static void CastSpell(Spell.Skillshot QWER, AIHeroClient target)
        {
            if (QWER.Slot == SpellSlot.E && MenuConfig.Epred == 0)
            {
                var pred = E.GetPrediction(target);
                if (pred.HitChance >= HitChance.Medium)
                {
                    QWER.Cast(target);
                }
            }
            if (QWER.Slot == SpellSlot.E && MenuConfig.Epred == 1)
            {
                var pred = E.GetPrediction(target);
                if (pred.HitChance >= HitChance.AveragePoint)
                {
                    QWER.Cast(target);
                }
            }
            if (QWER.Slot == SpellSlot.E && MenuConfig.Epred == 2)
            {
                var pred = E.GetPrediction(target);
                if (pred.HitChance >= HitChance.High)
                {
                    QWER.Cast(target);
                }
            }
            if (QWER.Slot == SpellSlot.R && MenuConfig.Rpred == 0)
            {
                var pred = R.GetPrediction(target);
                if (pred.HitChance >= HitChance.Medium)
                {
                    QWER.Cast(target);
                }
            }
            if (QWER.Slot == SpellSlot.R && MenuConfig.Rpred == 1)
            {
                var pred = R.GetPrediction(target);
                if (pred.HitChance >= HitChance.AveragePoint)
                {
                    QWER.Cast(target);
                }
            }
            if (QWER.Slot == SpellSlot.R)
            {
                var pred = R.GetPrediction(target);
                if (pred.HitChance == HitChance.High)
                {
                    QWER.Cast(target);
                }
            }
            if (QWER.Slot == SpellSlot.R && MenuConfig.Rpred == 2)
            {
                var pred = R.GetPrediction(target);
                if (pred.HitChance >= HitChance.High)
                {
                    QWER.Cast(target);
                }
            }
        }

        private static void castR(AIHeroClient target)
        {
            if (MenuConfig.hitchanceR)
            {
                var waypoints = target.GetWaypoints();
                if (target.Path.Count() < 2 &&
                    (Player.Distance(waypoints.Last().To3D()) - Player.Distance(target.Position)) > 300)
                {
                    CastSpell(R, target);
                }
            }
            else
                CastSpell(R, target);
        }

        private static float CalculateR(AIHeroClient target)
        {
            return Player.CalculateDamageOnUnit(target, DamageType.Physical,
                (float) ((75 + (100*R.Level)) + Player.FlatPhysicalDamageMod*1.1));
        }

        private static double getRdmg(AIHeroClient target)
        {
            var rDmg = ObjectManager.Player.GetSpellDamage(target, SpellSlot.R);
            var dmg = 0;
            var output = R.GetPrediction(target);
            var direction = output.CastPosition.To2D() - Player.Position.To2D();
            direction.Normalize();
            var enemies = ObjectManager.Get<AIHeroClient>().Where(x => x.IsEnemy && x.IsValidTarget()).ToList();
            foreach (var enemy in enemies)
            {
                var prediction = R.GetPrediction(enemy);
                var predictedPosition = prediction.CastPosition;
                var v = output.CastPosition - Player.ServerPosition;
                var w = predictedPosition - Player.ServerPosition;
                double c1 = Vector3.Dot(w, v);
                double c2 = Vector3.Dot(v, v);
                var b = c1/c2;
                var pb = Player.ServerPosition + ((float) b*v);
                var length = Vector3.Distance(predictedPosition, pb);
                if (length < (R.Width + 100 + enemy.BoundingRadius/2) &&
                    Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                    dmg++;
            }
            var allMinionsR = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                ObjectManager.Player.ServerPosition, R.Range);
            foreach (var minion in allMinionsR)
            {
                var prediction = R.GetPrediction(minion);
                var predictedPosition = prediction.CastPosition;
                var v = output.CastPosition - Player.ServerPosition;
                var w = predictedPosition - Player.ServerPosition;
                double c1 = Vector3.Dot(w, v);
                double c2 = Vector3.Dot(v, v);
                var b = c1/c2;
                var pb = Player.ServerPosition + ((float) b*v);
                var length = Vector3.Distance(predictedPosition, pb);
                if (length < (R.Width + 100 + minion.BoundingRadius/2) &&
                    Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                    dmg++;
            }
            //if (Config.Item("debug", true).GetValue<bool>())
            //    Game.PrintChat("R collision" + dmg);

            if (dmg > 8)
                return rDmg*0.6;
            return rDmg - (rDmg*0.08*dmg);
        }

       

        private static void SetMana()
        {
            if ((MenuConfig.manaDisable && Combo) || Player.HealthPercent < 20)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).SData.Mana;
            WMANA = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).SData.Mana;
            EMANA = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).SData.Mana;

            if (!R.IsReady())
                RMANA = EMANA -
                        Player.PARRegenRate*ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).SData.CooldownTime;
            else
                RMANA = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).SData.Mana;
        }

        public static void drawText2(string msg, Vector3 Hero, Color color)
        {
            var wts = Drawing.WorldToScreen(Hero);
            Drawing.DrawText(wts[0] - (msg.Length)*5, wts[1] - 200, color, msg);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (MenuConfig.qAxePos)
            {
                if (Player.HasBuff("dravenspinningattack"))
                {
                    var BuffTime = Utils.GetPassiveTime(Player, "dravenspinningattack");
                    if (BuffTime < 2)
                    {
                        if ((int) (Game.Time*10)%2 == 0)
                        {
                            drawText2("Q:  " + string.Format("{0:0.0}", BuffTime), Player.Position, Color.Yellow);
                        }
                    }
                    else
                    {
                        drawText2("Q:  " + string.Format("{0:0.0}", BuffTime), Player.Position, Color.GreenYellow);
                    }
                }
                foreach (Axe a in AxesManager.Axes.Where(m => m.InTime && !m.InTurret))
                {
                    if (Game.CursorPos.Distance(a.EndPosition) > axeCatchRange || a.EndPosition.UnderTurret(true))
                    {
                        Drawing.DrawCircle(a.EndPosition, 150, Color.OrangeRed);
                    }
                    else if (Player.Distance(a.EndPosition) > 120)
                    {
                        Drawing.DrawCircle(a.EndPosition, 150, Color.Yellow);
                    }
                    else if (Player.Distance(a.EndPosition) < 150)
                    {
                        Drawing.DrawCircle(a.EndPosition, 150, Color.YellowGreen);
                    }
                }
            }

            if (MenuConfig.qCatchRange)
                Drawing.DrawCircle(Game.CursorPos, axeCatchRange, Color.LightSteelBlue);

            if (MenuConfig.noti && RMissile != null)
                Utils.DrawLineRectangle(RMissile.Position, Player.Position, R.Width, 1, Color.White);

            if (MenuConfig.eRange)
            {
                if (MenuConfig.onlyRdy)
                {
                    if (E.IsReady())
                        Drawing.DrawCircle(Player.Position, E.Range, Color.Yellow);
                }
                else
                    Drawing.DrawCircle(Player.Position, E.Range, Color.Yellow);
            }
        }

        public static class MenuConfig
        {
            public static int Epred
            {
                get { return PredictionMenu["Epred"].Cast<Slider>().CurrentValue; }
            }

            public static int Rpred
            {
                get { return PredictionMenu["Rpred"].Cast<Slider>().CurrentValue; }
            }

            public static bool qCatchRange
            {
                get { return DrawingsMenu["qCatchRange"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool qAxePos
            {
                get { return DrawingsMenu["qAxePos"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool eRange
            {
                get { return DrawingsMenu["eRange"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool noti
            {
                get { return DrawingsMenu["noti"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool onlyRdy
            {
                get { return DrawingsMenu["noti"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool manaDisable
            {
                get { return SpellsMenu["manaDisable"].Cast<CheckBox>().CurrentValue; }
            }

            public static int axeCatchRange
            {
                get { return AxeSettingsMenu["axeCatchRange"].Cast<Slider>().CurrentValue; }
            }

            public static bool axeTower
            {
                get { return AxeSettingsMenu["axeTower"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool axeTower2
            {
                get { return AxeSettingsMenu["axeTower2"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool axeEnemy
            {
                get { return AxeSettingsMenu["axeEnemy"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool axeKill
            {
                get { return AxeSettingsMenu["axeKill"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool axePro
            {
                get { return AxeSettingsMenu["axePro"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool autoQ
            {
                get { return SpellsMenu["autoQ"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool farmQ
            {
                get { return SpellsMenu["farmQ"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool autoW
            {
                get { return SpellsMenu["autoW"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool slowW
            {
                get { return SpellsMenu["slowW"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool autoE
            {
                get { return SpellsMenu["autoE"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool autoE2
            {
                get { return SpellsMenu["autoE2"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool autoR
            {
                get { return SpellsMenu["autoR"].Cast<CheckBox>().CurrentValue; }
            }

            public static int Rdmg
            {
                get { return SpellsMenu["Rdmg"].Cast<Slider>().CurrentValue; }
            }

            public static bool comboR
            {
                get { return SpellsMenu["comboR"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Rcc
            {
                get { return SpellsMenu["Rcc"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Raoe
            {
                get { return SpellsMenu["Raoe"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool hitchanceR
            {
                get { return SpellsMenu["hitchanceR"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool useR
            {
                get { return SpellsMenu["useR"].Cast<KeyBind>().CurrentValue; }
            }
        }
    }
}