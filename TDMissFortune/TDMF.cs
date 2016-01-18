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

namespace MissFortune
{
    class MissFortune
    {
        public static Spell.Targeted Q;
        public static Spell.Skillshot Q1;
        public static Spell.Active W;
        public static Spell.Skillshot E;
        public static Spell.Skillshot R;
        private static float QMANA, WMANA, EMANA, RMANA;
        public static List<AIHeroClient> Enemies = new List<AIHeroClient>();
        private static int LastAttackId;
        private static float RCastTime;
        public static bool blockSpells, blockMove, blockAttack;
        public static Menu MissFortunMainMenuSettings, MainMenuSettings, FarmMenu, DrawingMenu;

        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        public static bool Combo
        {
            get { return (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)); }
        }

        public static bool LaneClear
        {
            get { return (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear)); }
        }

        public static bool Farm
        {
            get
            {
                return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) ||
                       Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass);
            }
        }
        public static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Load;
        }

        public static void Load(EventArgs args)
        {
            if (ObjectManager.Player.Hero != Champion.MissFortune) return;
            Q = new Spell.Targeted(SpellSlot.Q, 655);
            Q1 = new Spell.Skillshot(SpellSlot.Q, 1300, SkillShotType.Linear, (int) 0.25f, 1500, (int) 70f);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Skillshot(SpellSlot.E, 800, SkillShotType.Circular, (int) 0.5f, int.MaxValue, 200);
            R = new Spell.Skillshot(SpellSlot.R, 1400, SkillShotType.Cone, (int) 0.25f, int.MaxValue, (int) 100f);

            MissFortunMainMenuSettings = MainMenu.AddMenu("TDMF", "mf");
            MainMenuSettings = MissFortunMainMenuSettings.AddSubMenu("Spell Settings", "settings");
            MainMenuSettings.AddLabel("Q Settings");
            MainMenuSettings.Add("autoQ", new CheckBox("Q on Combo"));
            MainMenuSettings.Add("harassQ", new CheckBox("Use Q on minion"));
            MainMenuSettings.Add("killQ", new CheckBox("Use Q only if can kill minion", false));
            MainMenuSettings.Add("qMinionMove", new CheckBox("Don't use if minions moving"));
            MainMenuSettings.Add("qMinionWidth", new Slider("Collision width calculation", 70, 200, 0));
            MainMenuSettings.AddLabel("W Settings");
            MainMenuSettings.Add("autoW", new CheckBox("W on Combo"));
            MainMenuSettings.Add("harassW", new CheckBox("Harass W"));
            MainMenuSettings.AddLabel("E Settings");
            MainMenuSettings.Add("autoE", new CheckBox("E on Combo"));
            MainMenuSettings.Add("AGC", new CheckBox("AntiGapcloser E"));
            StringList(MainMenuSettings, "EHitchance", "E Hitchance", new[] {"Low", "Medium", "Average Point", "High"},
                2);
            MainMenuSettings.AddLabel("R Settings");
            MainMenuSettings.Add("autoR", new CheckBox("R on Combo"));
            MainMenuSettings.Add("forceBlockMove", new CheckBox("Force player block"));
            MainMenuSettings.Add("useR",
                new KeyBind("Manual R Cast", false, KeyBind.BindTypes.HoldActive, "T".ToCharArray()[0]));
            MainMenuSettings.Add("disableBlock",
                new KeyBind("Disable the movement block", false, KeyBind.BindTypes.HoldActive, "R".ToCharArray()[0]));
            MainMenuSettings.Add("Rturret", new CheckBox("Don't R under turret"));
            MainMenuSettings.AddLabel("Misc");
            MainMenuSettings.Add("newTarget", new CheckBox("Try to change focus after attack"));
            MainMenuSettings.Add("manaDisable", new CheckBox("Disable mana manager in combo"));

            FarmMenu = MissFortunMainMenuSettings.AddSubMenu("Farm Settings", "farm");
            FarmMenu.Add("jungleQ", new CheckBox("Jungle Q KS"));
            FarmMenu.Add("jungleW", new CheckBox("Jungle clear W"));
            FarmMenu.Add("jungleE", new CheckBox("Jungle clear E"));

            DrawingMenu = MissFortunMainMenuSettings.AddSubMenu("Drawing Settings", "drawing");
            DrawingMenu.Add("QRange", new CheckBox("Q range"));
            DrawingMenu.Add("ERange", new CheckBox("E range"));
            DrawingMenu.Add("RRange", new CheckBox("R range"));
            DrawingMenu.Add("onlyRdy", new CheckBox("Draw only ready spells"));
            DrawingMenu.Add("noti", new CheckBox("Show notification & line"));

            foreach (var hero in ObjectManager.Get<AIHeroClient>())
            {
                if (hero.IsEnemy)
                {
                    Enemies.Add(hero);
                }
            }

            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalker.OnPostAttack += afterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            EloBuddy.Player.OnIssueOrder += Obj_AI_Base_OnIssueOrder;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Gapcloser.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;
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

        private static void AntiGapcloser_OnEnemyGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs gapcloser)
        {
            if (E.IsReady() && Config.AGC && Player.Mana > RMANA + EMANA)
            {
                var Target = sender;
                if (Target.IsValidTarget(E.Range) && sender.IsEnemy)
                {
                    E.Cast(gapcloser.End);
                }
            }
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (blockSpells)
            {
                args.Process = false;
            }
        }

        private static void Obj_AI_Base_OnIssueOrder(Obj_AI_Base sender, PlayerIssueOrderEventArgs args)
        {
            if (!sender.IsMe)
                return;

            if (blockMove && !args.IsAttackMove)
            {
                args.Process = false;
            }
            if (blockAttack && args.IsAttackMove)
            {
                args.Process = false;
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "MissFortuneBulletTime")
            {
                RCastTime = Game.Time;
                Orbwalker.DisableAttacking = true;
                Orbwalker.DisableMovement = true;
                if (Config.forceBlockMove)
                {
                    blockMove = true;
                    blockAttack = true;
                    blockSpells = true;
                }
            }
        }

        private static void afterAttack(AttackableUnit target, EventArgs args)
        {
            LastAttackId = target.NetworkId;

            if (!(target is AIHeroClient))
                return;
            var t = (AIHeroClient) target;

            if (Q.IsReady() && t.IsValidTarget(Q.Range))
            {
                if (Player.GetSpellDamage(t, SpellSlot.Q) + Player.GetAutoAttackDamage(t)*3 > t.Health)
                    Q.Cast(t);
                else if (Combo && Player.Mana > RMANA + QMANA + WMANA)
                    Q.Cast(t);
                else if (Farm && Player.Mana > RMANA + QMANA + EMANA + WMANA)
                    Q.Cast(t);
            }
            if (W.IsReady())
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && Player.Mana > RMANA + WMANA &&
                    Config.autoW)
                    W.Cast();
                else if (Player.Mana > RMANA + WMANA + QMANA && Config.harassW)
                    W.Cast();
            }
        }

        private static void Jungle()
        {
            if (LaneClear && Player.Mana > RMANA + WMANA + QMANA)
            {
                var mobs =
                    EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Monster, EntityManager.UnitTeam.Both, Player.Position, 600)
                        .OrderByDescending(i => i.MaxHealth)
                        .ToArray();
                if (mobs.Any())
                {
                    var mob = mobs[0];
                    if (Q.IsReady() && Config.jungleQ &&
                        Player.GetSpellDamage(mob, SpellSlot.Q) > mob.Health)
                    {
                        Q.Cast(mob);
                        return;
                    }
                    if (W.IsReady() && Config.jungleW)
                    {
                        W.Cast();
                        return;
                    }
                    if (E.IsReady() && Config.jungleE)
                    {
                        E.Cast(mob.ServerPosition);
                    }
                }
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Config.disableBlock)
            {
                Orbwalker.DisableAttacking = false;
                Orbwalker.DisableMovement = false;
                blockSpells = false;
                blockAttack = false;
                blockMove = false;
                return;
            }
            if (Player.IsChannelingImportantSpell() || Game.Time - RCastTime < 0.3)
            {
                if (Config.forceBlockMove)
                {
                    blockMove = true;
                    blockAttack = true;
                    blockSpells = true;
                }

                Orbwalker.DisableAttacking = true;
                Orbwalker.DisableMovement = true;
                return;
            }
            Orbwalker.DisableAttacking = false;
            Orbwalker.DisableMovement = false;
            if (Config.forceBlockMove)
            {
                blockAttack = false;
                blockMove = false;
                blockSpells = false;
            }
            if (R.IsReady() && Config.useR)
            {
                var t = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                if (t.IsValidTarget(R.Range))
                {
                    R.Cast(t);
                    RCastTime = Game.Time;
                    return;
                }
            }

            if (Config.newTarget)
            {
                var orbT = Orbwalker.GetTarget();

                AIHeroClient t2 = null;

                if (orbT != null && orbT is AIHeroClient)
                    t2 = (AIHeroClient) orbT;

                if (t2.IsValidTarget() && t2.NetworkId == LastAttackId)
                {
                    var ta = ObjectManager.Get<AIHeroClient>()
                        .FirstOrDefault(enemy => enemy.IsValidTarget() && enemy.IsEnemy && Utils.InAutoAttackRange(enemy)
                        && (enemy.NetworkId != LastAttackId || enemy.Health < Player.GetAutoAttackDamage(enemy)*2));

                    if (ta != null)
                        Orbwalker.ForcedTarget = ta;
                    Orbwalker.ForcedTarget = null;
                }
            }
            SetMana();
            Jungle();


            if (!Player.Spellbook.IsAutoAttacking && Q.IsReady() && Config.autoQ)
                LogicQ();

            if (!Player.Spellbook.IsAutoAttacking && E.IsReady() && Config.autoE)
                LogicE();

            if (!Player.Spellbook.IsAutoAttacking && R.IsReady() && Config.autoR)
                LogicR();
        }

        private static void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
            var t1 = TargetSelector.GetTarget(Q1.Range, DamageType.Physical);
            if (t.IsValidTarget(Q.Range) && Player.Distance(t.ServerPosition) > 500)
            {
                var qDmg = Utils.GetKsDamage(t, Q.Slot);
                if (qDmg + Player.GetAutoAttackDamage(t) > t.Health)
                    Q.Cast(t);
                else if (qDmg + Player.GetAutoAttackDamage(t)*3 > t.Health)
                    Q.Cast(t);
                else if (Combo && Player.Mana > RMANA + QMANA + WMANA)
                    Q.Cast(t);
                else if (Farm && Player.Mana > RMANA + QMANA + EMANA + WMANA)
                    Q.Cast(t);
            }
            else if (t1.IsValidTarget(Q1.Range) && Config.harassQ && Player.Distance(t1.ServerPosition) > Q.Range + 50)
            {
                if (Config.qMinionMove)
                {
                    var minions = EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Both,
                        EntityManager.UnitTeam.Enemy, Player.Position, Q1.Range).ToArray();
                    if (Array.Exists(minions, x => x.IsMoving))
                        return;
                }

                Q1.Width = Config.qMinionWidth;

                var poutput = Q1.GetPrediction(t1);
                var col = poutput.CollisionObjects;
                if (!col.Any())
                    return;

                var minionQ = col.Last();
                if (minionQ.IsValidTarget(Q.Range))
                {
                    if (Config.killQ && Player.GetSpellDamage(minionQ, SpellSlot.Q) < minionQ.Health)
                        return;
                    var minionToT = minionQ.Distance(t1.Position);
                    var minionToP = minionQ.Distance(poutput.CastPosition);
                    if (minionToP < 400 && minionToT < 420 && minionToT > 150 && minionToP > 200)
                    {
                        if (Player.GetSpellDamage(t1, SpellSlot.Q) + Player.GetAutoAttackDamage(t1) >
                            t1.Health)
                            Q.Cast(col.Last());
                        else if (Combo && Player.Mana > RMANA + QMANA + WMANA)
                            Q.Cast(col.Last());
                        else if (Farm && Player.Mana > RMANA + QMANA + EMANA + WMANA + QMANA)
                            Q.Cast(col.Last());
                    }
                }
            }
        }

        private static void LogicE()
        {
            var t = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            if (t.IsValidTarget())
            {
                var eDmg = Utils.GetKsDamage(t, E.Slot);
                if (eDmg > t.Health)
                    CastE(E, t);
                else if (eDmg + Player.GetSpellDamage(t, SpellSlot.Q) > t.Health && Player.Mana > QMANA + EMANA + RMANA)
                    CastE(E, t);
                else if (Combo && Player.Mana > RMANA + WMANA + QMANA + EMANA)
                {
                    if (!Player.IsInAutoAttackRange(t) || Player.CountEnemiesInRange(300) > 0 ||
                        t.CountEnemiesInRange(250) > 1)
                        CastE(E, t);
                    else
                    {
                        foreach (
                            var enemy in Enemies.Where(enemy => enemy.IsValidTarget(E.Range) && !Utils.CanMove(enemy)))
                            E.Cast(enemy);
                    }
                }
            }
        }

        private static void LogicR()
        {
            if (Utils.UnderTurret(Player, true) && Config.Rturret)
                return;

            var t = TargetSelector.GetTarget(R.Range, DamageType.Physical);

            if (t.IsValidTarget(R.Range) && Utils.ValidUlt(t))
            {
                var rDmg = Player.GetSpellDamage(t, SpellSlot.R)*new[] {0.5, 0.75, 1}[R.Level];

                if (Player.CountEnemiesInRange(700) == 0 && t.CountAlliesInRange(400) == 0)
                {
                    var tDis = Player.Distance(t.ServerPosition);
                    if (rDmg*7 > t.Health && tDis < 800)
                    {
                        R.Cast(t);
                        RCastTime = Game.Time;
                    }
                    else if (rDmg*6 > t.Health && tDis < 900)
                    {
                        R.Cast(t);
                        RCastTime = Game.Time;
                    }
                    else if (rDmg*5 > t.Health && tDis < 1000)
                    {
                        R.Cast(t);
                        RCastTime = Game.Time;
                    }
                    else if (rDmg*4 > t.Health && tDis < 1100)
                    {
                        R.Cast(t);
                        RCastTime = Game.Time;
                    }
                    else if (rDmg*3 > t.Health && tDis < 1200)
                    {
                        R.Cast(t);
                        RCastTime = Game.Time;
                    }
                    else if (rDmg > t.Health && tDis < 1300)
                    {
                        R.Cast(t);
                        RCastTime = Game.Time;
                    }
                    return;
                }
                if (rDmg*8 > t.Health - Utils.GetIncomingDamage(t) && rDmg*2 < t.Health &&
                    Player.CountEnemiesInRange(300) == 0 && !Utils.CanMove(t))
                {
                    R.Cast(t);
                    RCastTime = Game.Time;
                }
            }
        }

        public static void CastE(Spell.Skillshot E, Obj_AI_Base target)
        {
            var pred = E.GetPrediction(target);
            if (Config.EHitChance == 3)
            {
                if (pred.HitChance >= HitChance.High)
                {
                    E.Cast(target);
                }
                return;
            }
            if (Config.EHitChance == 2)
            {
                if (pred.HitChance >= HitChance.AveragePoint)
                {
                    E.Cast(target);
                }
                return;
            }
            if (Config.EHitChance == 1)
            {
                if (pred.HitChance >= HitChance.Medium)
                {
                    E.Cast(target);
                    return;
                }
            }
            if (Config.EHitChance == 0)
            {
                if (pred.HitChance >= HitChance.Low)
                {
                    E.Cast(target);
                }
            }
        }

        private static void SetMana()
        {
            if ((Config.manaDisable && Combo) || Player.HealthPercent < 20)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = Player.Spellbook.GetSpell(SpellSlot.Q).SData.Mana;
            WMANA = Player.Spellbook.GetSpell(SpellSlot.W).SData.Mana;
            EMANA = Player.Spellbook.GetSpell(SpellSlot.E).SData.Mana;

            if (!R.IsReady())
                RMANA = QMANA - Player.PARRegenRate*Player.Spellbook.GetSpell(SpellSlot.Q).SData.CooldownTime;
            else
                RMANA = Player.Spellbook.GetSpell(SpellSlot.R).SData.Mana;
        }

        public static void drawLine(Vector3 pos1, Vector3 pos2, int bold, Color color)
        {
            var wts1 = Drawing.WorldToScreen(pos1);
            var wts2 = Drawing.WorldToScreen(pos2);

            Drawing.DrawLine(wts1[0], wts1[1], wts2[0], wts2[1], bold, color);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Config.noti && R.IsReady())
            {
                var t = TargetSelector.GetTarget(R.Range, DamageType.Physical);

                if (t.IsValidTarget())
                {
                    var rDamage = Player.GetSpellDamage(t, SpellSlot.R) +
                                  (Player.GetSpellDamage(t, SpellSlot.W)*10);
                    if (rDamage*8 > t.Health)
                    {
                        Drawing.DrawText(Drawing.Width*0.1f, Drawing.Height*0.5f, Color.GreenYellow,
                            "8 x R wave can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                        drawLine(t.Position, Player.Position, 10, Color.GreenYellow);
                    }
                    else if (rDamage*5 > t.Health)
                    {
                        Drawing.DrawText(Drawing.Width*0.1f, Drawing.Height*0.5f, Color.Orange,
                            "5 x R wave can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                        drawLine(t.Position, Player.Position, 10, Color.Orange);
                    }
                    else if (rDamage*3 > t.Health)
                    {
                        Drawing.DrawText(Drawing.Width*0.1f, Drawing.Height*0.5f, Color.Yellow,
                            "3 x R wave can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                        drawLine(t.Position, Player.Position, 10, Color.Yellow);
                    }
                    else if (rDamage > t.Health)
                    {
                        Drawing.DrawText(Drawing.Width*0.1f, Drawing.Height*0.5f, Color.Red,
                            "1 x R wave can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                        drawLine(t.Position, Player.Position, 10, Color.Red);
                    }
                }
            }

            if (Config.QRange)
            {
                if (Config.onlyRdy)
                {
                    if (W.IsReady())
                        Drawing.DrawCircle(Player.Position, Q.Range, Color.Cyan);
                }
                else
                    Drawing.DrawCircle(Player.Position, Q.Range, Color.Cyan);
            }
            if (Config.ERange)
            {
                if (Config.onlyRdy)
                {
                    if (E.IsReady())
                        Drawing.DrawCircle(Player.Position, E.Range, Color.Orange);
                }
                else
                    Drawing.DrawCircle(Player.Position, E.Range, Color.Orange);
            }
            if (Config.RRange)
            {
                if (Config.onlyRdy)
                {
                    if (R.IsReady())
                        Drawing.DrawCircle(Player.Position, R.Range, Color.Gray);
                }
                else
                    Drawing.DrawCircle(Player.Position, R.Range, Color.Gray);
            }
        }

        public static void drawText(string msg, Obj_AI_Base Hero, Color color)
        {
            var wts = Drawing.WorldToScreen(Hero.Position);
            Drawing.DrawText(wts[0] - (msg.Length)*5, wts[1], color, msg);
        }

        public static class Config
        {
            public static bool newTarget
            {
                get { return MainMenuSettings["newTarget"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool manaDisable
            {
                get { return MainMenuSettings["manaDisable"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool autoQ
            {
                get { return MainMenuSettings["autoQ"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool harassQ
            {
                get { return MainMenuSettings["harassQ"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool killQ
            {
                get { return MainMenuSettings["killQ"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool autoW
            {
                get { return MainMenuSettings["autoW"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool harassW
            {
                get { return MainMenuSettings["harassW"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool autoE
            {
                get { return MainMenuSettings["autoE"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool AGC
            {
                get { return MainMenuSettings["AGC"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool autoR
            {
                get { return MainMenuSettings["autoR"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool forceBlockMove
            {
                get { return MainMenuSettings["forceBlockMove"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool useR
            {
                get { return MainMenuSettings["useR"].Cast<KeyBind>().CurrentValue; }
            }

            public static bool disableBlock
            {
                get { return MainMenuSettings["disableBlock"].Cast<KeyBind>().CurrentValue; }
            }

            public static bool jungleQ
            {
                get { return FarmMenu["jungleQ"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool jungleW
            {
                get { return FarmMenu["jungleW"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool jungleE
            {
                get { return FarmMenu["jungleE"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool QRange
            {
                get { return DrawingMenu["QRange"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool ERange
            {
                get { return DrawingMenu["ERange"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool RRange
            {
                get { return DrawingMenu["RRange"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool onlyRdy
            {
                get { return DrawingMenu["onlyRdy"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool noti
            {
                get { return DrawingMenu["noti"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Rturret
            {
                get { return MainMenuSettings["Rturret"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool qMinionMove
            {
                get { return MainMenuSettings["qMinionMove"].Cast<CheckBox>().CurrentValue; }
            }

            public static int qMinionWidth
            {
                get { return MainMenuSettings["qMinionWidth"].Cast<Slider>().CurrentValue; }
            }

            public static int EHitChance
            {
                get { return MainMenuSettings["EHitchance"].Cast<Slider>().CurrentValue; }
            }
        }
    }
}