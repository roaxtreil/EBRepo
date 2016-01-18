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

namespace Sebby_Jinx
{
    internal class Program
    {
        public static Spell.Active Q;
        public static Spell.Skillshot W, E, R;
        public static float QMANA, WMANA, EMANA, RMANA;
        public static bool Combo, Farm;
        public static double lag, WCastTime, QCastTime = 0, DragonTime, grabTime;
        public static float DragonDmg;
        public static List<AIHeroClient> Enemies = new List<AIHeroClient>();

        public static Menu JinxMenu,
            SpellsMenu,
            PredictionMenu,
            JungleStealMenu,
            DrawingsMenu;

        private static readonly List<UnitIncomingDamage> IncomingDamageList = new List<UnitIncomingDamage>();

        private static bool FishBoneActive
        {
            get { return Player.HasBuff("JinxQ"); }
        }

        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        public static bool LaneClear
        {
            get { return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear); }
        }

        private static void Main(string[] args)
        {
            foreach (var hero in ObjectManager.Get<AIHeroClient>())
            {
                if (hero.IsEnemy)
                {
                    Enemies.Add(hero);
                }
            }
            Loading.OnLoadingComplete += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.Hero != Champion.Jinx) return;

            LoadMenu();

            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Skillshot(SpellSlot.W, 1500, SkillShotType.Linear, (int)0.6f, (int?)3300f, (int?)60f);
            W.AllowedCollisionCount = 0;
            E = new Spell.Skillshot(SpellSlot.E, 920, SkillShotType.Circular, (int)1.2f, (int?)1750f, (int?)100f);
            R = new Spell.Skillshot(SpellSlot.R, 3000, SkillShotType.Linear, (int)0.7f, (int?)1500f, (int?)140f);


            Game.OnUpdate += Game_OnGameUpdate;
            Orbwalker.OnPreAttack += BeforeAttack;
            Gapcloser.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Drawing.OnDraw += Drawing_OnDraw;
            Chat.Print("Sebby Jinx by Sebby, ported by ThugDoge");
        }

        private static void LoadMenu()
        {
            JinxMenu = MainMenu.AddMenu("Sebby Jinx", "jinx");
            SpellsMenu = JinxMenu.AddSubMenu("Spells Menu", "spells");
            SpellsMenu.AddLabel("Q Settings");
            SpellsMenu.Add("autoQ", new CheckBox("Auto Q"));
            SpellsMenu.Add("Qharass", new CheckBox("Harass Q"));
            SpellsMenu.Add("farmQout", new CheckBox("Q farm out range AA"));
            SpellsMenu.Add("farmQ", new CheckBox("Farm Q"));
            SpellsMenu.Add("Mana", new Slider("LaneClear Q Mana", 80, 30));
            SpellsMenu.AddLabel("W Settings");
            SpellsMenu.Add("autoW", new CheckBox("Combo W"));
            SpellsMenu.AddLabel("Harass W");
            foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.Team != Player.Team))
                SpellsMenu.Add("wharass" + enemy.ChampionName, new CheckBox(enemy.BaseSkinName));

            SpellsMenu.AddLabel("E Settings");
            SpellsMenu.Add("comboE", new CheckBox("Auto E in Combo BETA"));
            SpellsMenu.Add("autoE", new CheckBox("E on CC"));
            SpellsMenu.Add("AGC", new CheckBox("AntiGapcloserE"));
            SpellsMenu.Add("opsE", new CheckBox("OnProcessSpellCastE"));
            SpellsMenu.Add("tel", new CheckBox("Auto E teleport"));
            SpellsMenu.AddLabel("R Settings");
            SpellsMenu.Add("autoR", new CheckBox("Auto R"));
            SpellsMenu.Add("useR",
                new KeyBind("Semi-manual cast R key", false, KeyBind.BindTypes.HoldActive, "T".ToCharArray()[0]));
            SpellsMenu.Add("hitchanceR", new Slider("Hit Chance R", 0, 0, 2));
            SpellsMenu.Add("Rturret", new CheckBox("Don't R under turret"));
            SpellsMenu.AddLabel("Misc Settings");
            SpellsMenu.Add("manaDisable", new CheckBox("Disable mana manager in combo"));

            JungleStealMenu = JinxMenu.AddSubMenu("Jungle Steal", "JungleSteal");
            JungleStealMenu.AddLabel("Jungle Steal Settings");
            JungleStealMenu.Add("Rjungle", new CheckBox("R Jungle stealer"));
            JungleStealMenu.Add("Rdragon", new CheckBox("Dragon"));
            JungleStealMenu.Add("Rbaron", new CheckBox("Baron"));

            DrawingsMenu = JinxMenu.AddSubMenu("Drawings", "drawingsmenu");
            DrawingsMenu.AddGroupLabel("Drawings Settings");
            DrawingsMenu.AddSeparator();
            DrawingsMenu.Add("qRange", new CheckBox("Q range", false));
            DrawingsMenu.Add("wRange", new CheckBox("W range", false));
            DrawingsMenu.Add("eRange", new CheckBox("E range", false));
            DrawingsMenu.Add("rRange", new CheckBox("R range", false));
            DrawingsMenu.Add("noti", new CheckBox("Show notification", false));
            DrawingsMenu.Add("semi", new CheckBox("Semi-manual R target", false));
            DrawingsMenu.Add("onlyRdy", new CheckBox("Draw only ready spells"));

            PredictionMenu = JinxMenu.AddSubMenu("Prediction", "prediction");
            PredictionMenu.AddSeparator();
            PredictionMenu.Add("Wpred", new Slider("W Hitchance", 50));
            PredictionMenu.Add("Epred", new Slider("E Hitchance", 50));
            PredictionMenu.Add("Rpred", new Slider("R Hitchance", 50));
        }

        public static void StringList(Menu menu, string uniqueId, string displayName, string[] values, int defaultValue)
        {
            var mode = menu.Add(uniqueId, new Slider(displayName, defaultValue, 0, values.Length - 1));
            mode.DisplayName = displayName + ": " + values[mode.CurrentValue];
            mode.OnValueChange +=
                delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
                {
                    sender.DisplayName = displayName + ": " + values[args.NewValue];
                };
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (MenuConfig.qRange)
            {
                if (FishBoneActive)
                    Drawing.DrawCircle(Player.Position, bonusRange() - 40, Color.DeepPink);
                else
                    Drawing.DrawCircle(Player.Position, 590f + Player.BoundingRadius, Color.DeepPink);
            }
            if (MenuConfig.wRange)
            {
                if (MenuConfig.onlyRdy)
                {
                    if (W.IsReady())
                        Drawing.DrawCircle(Player.Position, W.Range, Color.Cyan);
                }
                else
                    Drawing.DrawCircle(Player.Position, W.Range, Color.Cyan);
            }
            if (MenuConfig.eRange)
            {
                if (MenuConfig.onlyRdy)
                {
                    if (E.IsReady())
                        Drawing.DrawCircle(Player.Position, E.Range, Color.Gray);
                }
                else
                    Drawing.DrawCircle(Player.Position, E.Range, Color.Gray);
            }
            if (MenuConfig.noti)
            {
                var t = TargetSelector.GetTarget(R.Range, DamageType.Physical);

                if (R.IsReady() && t.IsValidTarget() &&
                    RDamage(t) > t.Health)
                {
                    /*
                                        Drawing.DrawText(Drawing.Width*0.1f, Drawing.Height*0.5f, Color.Red,
                                            "Ult can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                                */
                }
                else if (t.IsValidTarget(2000) && ObjectManager.Player.GetSpellDamage(t, SpellSlot.W) > t.Health)
                {
                    /*
                                        Drawing.DrawText(Drawing.Width*0.1f, Drawing.Height*0.5f, Color.Red,
                                            "W can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                                     */
                }
            }
        }

        public static void drawLine(Vector3 pos1, Vector3 pos2, int bold, Color color)
        {
            var wts1 = Drawing.WorldToScreen(pos1);
            var wts2 = Drawing.WorldToScreen(pos2);

            Drawing.DrawLine(wts1[0], wts1[1], wts2[0], wts2[1], bold, color);
        }

        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            if (unit.IsMinion)
                return;

            if (unit.IsMe)
            {
                if (args.SData.Name == "JinxWMissile")
                    WCastTime = Game.Time;
            }
            if (E.IsReady())
            {
                if (unit.IsEnemy && MenuConfig.opsE && unit.IsValidTarget(E.Range) && ShouldUseE(args.SData.Name))
                {
                    E.Cast(unit.ServerPosition);
                }
                if (unit.IsAlly && args.SData.Name == "RocketGrab" && Player.Distance(unit.Position) < E.Range)
                {
                    grabTime = Game.Time;
                }
            }
        }

        public static bool ShouldUseE(string SpellName)
        {
            switch (SpellName)
            {
                case "ThreshQ":
                    return true;
                case "KatarinaR":
                    return true;
                case "AlZaharNetherGrasp":
                    return true;
                case "GalioIdolOfDurand":
                    return true;
                case "LuxMaliceCannon":
                    return true;
                case "MissFortuneBulletTime":
                    return true;
                case "RocketGrabMissile":
                    return true;
                case "CaitlynPiltoverPeacemaker":
                    return true;
                case "EzrealTrueshotBarrage":
                    return true;
                case "InfiniteDuress":
                    return true;
                case "VelkozR":
                    return true;
            }
            return false;
        }

        private static void LogicQ()
        {
            if (Farm && (Game.Time - lag > 0.1) && !FishBoneActive && !ObjectManager.Player.Spellbook.IsAutoAttacking &&
                Orbwalker.CanAutoAttack && MenuConfig.farmQout && Player.Mana > RMANA + WMANA + EMANA + 10)
            {
                foreach (
                    var minion in
                        EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Position,
                            bonusRange() + 30).Where(
                                minion =>
                                    !ObjectManager.Player.IsInAutoAttackRange(minion) &&
                                    minion.Health < Player.GetAutoAttackDamage(minion) * 1.2 &&
                                    GetRealPowPowRange(minion) < GetRealDistance(minion) &&
                                    bonusRange() < GetRealDistance(minion)))
                {
                    Orbwalker.ForcedTarget = minion;
                    Q.Cast();
                    return;
                }
                lag = Game.Time;
            }
            var t = TargetSelector.GetTarget(bonusRange() + 60, DamageType.Physical);
            if (t.IsValidTarget())
            {
                if (!FishBoneActive && (!ObjectManager.Player.IsInAutoAttackRange(t) || t.CountEnemiesInRange(250) > 2) &&
                    Orbwalker.GetTarget() == null)
                {
                    var distance = GetRealDistance(t);
                    if (Combo && (Player.Mana > RMANA + WMANA + 10 || Player.GetAutoAttackDamage(t) * 3 > t.Health))
                        Q.Cast();
                    else if (Farm && !Player.Spellbook.IsAutoAttacking && Orbwalker.CanAutoAttack && MenuConfig.Qharass &&
                             !Player.IsUnderEnemyturret() && Player.Mana > RMANA + WMANA + EMANA + 20 &&
                             distance < bonusRange() + t.BoundingRadius + Player.BoundingRadius)
                        Q.Cast();
                }
            }
            else if (!FishBoneActive && Combo && Player.Mana > RMANA + WMANA + 20 && Player.CountEnemiesInRange(2000) > 0)
                Q.Cast();
            else if (FishBoneActive && Combo && Player.Mana < RMANA + WMANA + 20)
                Q.Cast();
            else if (FishBoneActive && Combo && Player.CountEnemiesInRange(2000) == 0)
                Q.Cast();
            else if (FishBoneActive && (Farm || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit)))
            {
                Q.Cast();
            }
        }

        private static void LogicW()
        {
            var t = TargetSelector.GetTarget(W.Range, DamageType.Physical);
            if (t.IsValidTarget() && Player.CountEnemiesInRange(bonusRange() + 50) == 0)
            {
                if (Game.Time - QCastTime > 0.6)
                {
                    var comboDmg = GetKsDamage(t, SpellSlot.W);
                    if (R.IsReady() && Player.Mana > RMANA + WMANA + 20)
                    {
                        comboDmg += RDamage(t);
                    }
                    if (comboDmg > t.Health)
                    {
                        CastSpell(W, t);
                        return;
                    }
                }
                if (Combo && Player.Mana > RMANA + WMANA + 10 && GetRealDistance(t) > bonusRange() - 50)
                {
                    CastSpell(W, t);
                }
                else if (Farm && Player.Mana > RMANA + EMANA + WMANA + WMANA + 40 &&
                         CanHarras())
                {
                    foreach (
                        var enemy in
                            Enemies.Where(
                                enemy =>
                                    enemy.IsValidTarget(W.Range) &&
                                    SpellsMenu["wharass" + enemy.ChampionName].Cast<CheckBox>().CurrentValue))
                        CastSpell(W, enemy);
                }
                else if ((Combo || Farm) && Player.Mana > RMANA + WMANA &&
                         Player.CountEnemiesInRange(GetRealPowPowRange(t)) == 0)
                {
                    foreach (
                        var enemy in Enemies.Where(enemy => enemy.IsValidTarget(W.Range) && !CanMove(enemy)))
                        W.Cast(enemy);
                }
            }
        }

        private static void LogicE()
        {
            if (Player.Mana > RMANA + EMANA && MenuConfig.autoE)
            {
                foreach (
                    var enemy in
                        Enemies.Where(
                            enemy => enemy.IsValidTarget(E.Range) && !CanMove(enemy) && Game.Time - grabTime > 1))
                {
                    E.Cast(enemy.Position);
                    return;
                }

                if (MenuConfig.telE)
                {
                    foreach (
                        var Object in
                            ObjectManager.Get<Obj_AI_Base>()
                                .Where(
                                    Obj =>
                                        Obj.IsEnemy && Obj.Distance(Player.ServerPosition) < E.Range &&
                                        (Obj.HasBuff("teleport_target") || Obj.HasBuff("Pantheon_GrandSkyfall_Jump"))))
                    {
                        E.Cast(Object.Position);
                    }
                }
                if (Combo && Player.IsMoving && MenuConfig.comboE && Player.Mana > RMANA + EMANA + WMANA)
                {
                    var t = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                    if (t.IsValidTarget(E.Range) && E.GetPrediction(t).CastPosition.Distance(t.Position) > 200 &&
                        E.GetPrediction(t).HitChance == HitChance.Medium)
                    {
                        if (t.HasBuffOfType(BuffType.Slow) ||
                            CountEnemiesInRangeDeley(E.GetPrediction(t).CastPosition, 250, E.CastDelay) > 1)
                        {
                            CastSpell(E, t);
                        }
                        else
                        {
                            if (E.GetPrediction(t).CastPosition.Distance(t.Position) > 200)
                            {
                                var epred = E.GetPrediction(t);
                                if (Player.Position.Distance(t.ServerPosition) > Player.Position.Distance(t.Position))
                                {
                                    if (t.Position.Distance(Player.ServerPosition) <
                                        t.Position.Distance(Player.Position))
                                        E.Cast(epred.CastPosition);
                                }
                                else
                                {
                                    if (t.Position.Distance(Player.ServerPosition) > t.Position.Distance(Player.Position))
                                        E.Cast(epred.CastPosition);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static bool IsFacing(Obj_AI_Base source, Obj_AI_Base target)
        {
            if (source == null || target == null)
            {
                return false;
            }

            const float angle = 90;
            return source.Direction.To2D().Perpendicular().AngleBetween((target.Position - source.Position).To2D()) < angle;
        }

        public static void CastSpell(Spell.Skillshot QWER, Obj_AI_Base target)
        {
            if (QWER.Slot == SpellSlot.W)
            {
                var pred = W.GetPrediction(target);
                if (pred.HitChancePercent >= MenuConfig.Wpred)
                {
                    QWER.Cast(pred.CastPosition);
                }
            }
            if (QWER.Slot == SpellSlot.E)
            {
                var pred = E.GetPrediction(target);
                QWER.Cast(pred.CastPosition);
                
            }

            if (QWER.Slot == SpellSlot.R)
            {
                var pred = R.GetPrediction(target);
                if (pred.HitChancePercent >= MenuConfig.Rpred)
                {
                    QWER.Cast(pred.CastPosition);
                }
            }
        }

        public static int CountEnemiesInRangeDeley(Vector3 position, float range, int delay)
        {
            var count = 0;
            foreach (var t in Enemies.Where(t => t.IsValidTarget()))
            {
                var prepos = Prediction.Position.PredictUnitPosition(t, delay);
                if (position.Distance(prepos) < range)
                    count++;
            }
            return count;
        }

        public static bool CanMove(Obj_AI_Base target)
        {
            if (target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) ||
                target.HasBuffOfType(BuffType.Knockup) ||
                target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Fear) ||
                target.HasBuffOfType(BuffType.Knockback) ||
                target.HasBuffOfType(BuffType.Taunt) || target.HasBuffOfType(BuffType.Suppression) ||
                target.IsStunned || (target.IsChannelingImportantSpell() && !target.IsMoving))
            {
                return false;
            }
            return true;
        }

        private static float RDamage(Obj_AI_Base target)
        {
            if (!R.IsLearned) return 0;
            var level = R.Level - 1;

            #region Less than Range

            if (target.Distance(Player) < 1350 && !target.IsMinion && !target.IsMonster)
            {
                return Player.CalculateDamageOnUnit(target, DamageType.Physical,
                    (float)
                        (new double[] { 25, 35, 45 }[level] +
                         new double[] { 25, 30, 35 }[level] / 100 * (target.MaxHealth - target.Health) +
                         0.1 * Player.FlatPhysicalDamageMod));
            }

            if ((target.IsMonster || target.IsMinion) && target.Distance(Player) < 1350)
            {
                var damage = Player.CalculateDamageOnUnit(target, DamageType.Physical,
                    (float)
                        (new double[] { 25, 35, 45 }[level] +
                         new double[] { 25, 30, 35 }[level] / 100 * (target.MaxHealth - target.Health) +
                         0.1 * Player.FlatPhysicalDamageMod));

                return (damage * 0.8) > 300f ? 300f : damage;
            }

            #endregion

            #region More Than Range

            var damage2 = Player.CalculateDamageOnUnit(target, DamageType.Physical,
                (float)
                    (new double[] { 250, 350, 450 }[level] +
                     new double[] { 25, 30, 35 }[level] / 100 * (target.MaxHealth - target.Health) +
                     Player.FlatPhysicalDamageMod));

            if ((target.IsMonster || target.IsMinion) && (damage2 * 0.8) > 300f)
            {
                damage2 = 300f;
            }

            return damage2;

            #endregion
        }

        public static bool ValidUlt(Obj_AI_Base target)
        {
            if (target.HasBuffOfType(BuffType.PhysicalImmunity)
                || target.HasBuffOfType(BuffType.SpellImmunity)
                || target.IsZombie
                || target.IsInvulnerable
                || target.HasBuffOfType(BuffType.Invulnerability)
                || target.HasBuffOfType(BuffType.SpellShield)
                )
                return false;
            return true;
        }

        private static void LogicR()
        {
            if (Player.IsUnderEnemyturret() && SpellsMenu["Rturret"].Cast<CheckBox>().CurrentValue)
                return;
            if (Game.Time - WCastTime > 0.9 && MenuConfig.autoR)
            {
                var cast = false;
                foreach (var target in Enemies.Where(target => target.IsValidTarget(R.Range) && ValidUlt(target)))
                {
                    var predictedHealth = target.Health + target.HPRegenRate * 2;
                    var Rdmg = RDamage(target);

                    if (Rdmg > predictedHealth)
                    {
                        cast = true;
                        var output = R.GetPrediction(target);
                        var direction = output.CastPosition.To2D() - Player.Position.To2D();
                        direction.Normalize();
                        var enemies = Enemies.Where(x => x.IsEnemy && x.IsValidTarget()).ToList();
                        foreach (var enemy in enemies)
                        {
                            if (enemy.BaseSkinName == target.BaseSkinName || !cast)
                                continue;
                            var prediction = R.GetPrediction(enemy);
                            var predictedPosition = prediction.CastPosition;
                            var v = output.CastPosition - Player.ServerPosition;
                            var w = predictedPosition - Player.ServerPosition;
                            double c1 = Vector3.Dot(w, v);
                            double c2 = Vector3.Dot(v, v);
                            var b = c1 / c2;
                            var pb = Player.ServerPosition + ((float)b * v);
                            var length = Vector3.Distance(predictedPosition, pb);
                            if (length < (R.Width + 150 + enemy.BoundingRadius / 2) &&
                                Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                                cast = false;
                        }

                        if (cast && GetRealDistance(target) > bonusRange() + 300 + target.BoundingRadius &&
                            target.CountAlliesInRange(600) == 0 && Player.CountEnemiesInRange(400) == 0)
                        {
                            castR(target);
                        }
                        else if (cast && target.CountEnemiesInRange(200) > 2 &&
                                 GetRealDistance(target) > bonusRange() + 200 + target.BoundingRadius)
                        {
                            R.Cast(target);
                        }
                    }
                }
            }
        }

        public static bool CanHarras()
        {
            if (!Player.Spellbook.IsAutoAttacking && Orbwalker.CanAutoAttack)
                return true;
            return false;
        }

        public static float GetKsDamage(Obj_AI_Base t, SpellSlot QWER)
        {
            var totalDmg = ObjectManager.Player.GetSpellDamage(t, QWER);
            totalDmg -= t.HPRegenRate;

            if (totalDmg > t.Health)
            {
                if (Player.HasBuff("summonerexhaust"))
                    totalDmg = totalDmg * 0.6f;

                if (t.HasBuff("ferocioushowl"))
                    totalDmg = totalDmg * 0.7f;

                if (t.BaseSkinName == "Blitzcrank" && !t.HasBuff("BlitzcrankManaBarrierCD") && !t.HasBuff("ManaBarrier"))
                {
                    totalDmg -= t.Mana / 2f;
                }
            }

            totalDmg += (float)GetIncomingDamage(t);
            return totalDmg;
        }

        public static double GetIncomingDamage(Obj_AI_Base target, float time = 0.5f, bool skillshots = true)
        {
            double totalDamage = 0;

            foreach (
                var damage in
                    IncomingDamageList.Where(
                        damage => damage.TargetNetworkId == target.NetworkId && Game.Time - time < damage.Time))
            {
                if (skillshots)
                {
                    totalDamage += damage.Damage;
                }
                else
                {
                    if (!damage.Skillshot)
                        totalDamage += damage.Damage;
                }
            }

            return totalDamage;
        }

        private static void castR(AIHeroClient target)
        {
            var inx = MenuConfig.hitchanceR;
            if (inx == 0)
            {
                var pred = R.GetPrediction(target);
                if (pred.HitChance >= HitChance.Medium)
                {
                    R.Cast(pred.UnitPosition);
                }
            }
            else if (inx == 1)
            {
                var pred = R.GetPrediction(target);
                if (pred.HitChance >= HitChance.High)
                {
                    R.Cast(pred.UnitPosition);
                }
            }
            else if (inx == 2)
            {
                var pred = R.GetPrediction(target);
                if (pred.HitChance >= HitChance.Dashing)
                {
                    R.Cast(pred.UnitPosition);
                }
            }
        }

        private static float GetUltTravelTime(AIHeroClient source, float speed, float delay, Vector3 targetpos)
        {
            var distance = Vector3.Distance(source.ServerPosition, targetpos);
            var missilespeed = speed;
            if (source.ChampionName == "Jinx" && distance > 1350)
            {
                const float accelerationrate = 0.3f; //= (1500f - 1350f) / (2200 - speed), 1 unit = 0.3units/second
                var acceldifference = distance - 1350f;
                if (acceldifference > 150f) //it only accelerates 150 units
                    acceldifference = 150f;
                var difference = distance - 1500f;
                missilespeed = (1350f * speed + acceldifference * (speed + accelerationrate * acceldifference) +
                                difference * 2200f) / distance;
            }
            return (distance / missilespeed + delay);
        }

        private static void AntiGapcloser_OnEnemyGapcloser(AIHeroClient unit, Gapcloser.GapcloserEventArgs gapcloser)
        {
            if (MenuConfig.AGC && E.IsReady() && Player.Mana > RMANA + EMANA)
            {
                var Target = gapcloser.Sender;
                if (Target.IsValidTarget(E.Range))
                {
                    E.Cast(gapcloser.End);
                }
            }
        }

        public static void SetMana()
        {
            if ((MenuConfig.manaDisable && Combo) || Player.HealthPercent < 20)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = 20;
            WMANA = EloBuddy.Player.GetSpell(SpellSlot.W).SData.ManaCostArray[ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level];
            EMANA = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).SData.ManaCostArray[ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level];

            if (!R.IsReady())
                RMANA = WMANA -
                        Player.PARRegenRate * ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).SData.CooldownTime;
            else
                RMANA = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).SData.ManaCostArray[ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level];
        }



        private static void BeforeAttack(AttackableUnit unit, Orbwalker.PreAttackArgs args)
        {
            if (!Q.IsReady() || !MenuConfig.autoQ || !(args.Target is Obj_AI_Base))
                return;
            var t = (Obj_AI_Base)args.Target;

            if (FishBoneActive && t.IsValidTarget() && t is AIHeroClient)
            {
                var realDistance = GetRealDistance(t);
                if (Combo && realDistance < GetRealPowPowRange(t) &&
                    (Player.Mana < RMANA + 20 || Player.GetAutoAttackDamage(t) * 3 < t.Health))
                    Q.Cast();
                else if (Farm && MenuConfig.Qharass &&
                         (realDistance > bonusRange() || realDistance < GetRealPowPowRange(t) ||
                          Player.Mana < RMANA + EMANA + WMANA + WMANA))
                    Q.Cast();
            }

            if (LaneClear && !FishBoneActive &&
                MenuConfig.farmQ && Player.ManaPercent > MenuConfig.Mana && Player.Mana > RMANA + EMANA + WMANA + 30 &&
                t is Obj_AI_Minion)
            {
                var allMinionsQ = EntityManager.MinionsAndMonsters.Get(
                    EntityManager.MinionsAndMonsters.EntityType.Both, EntityManager.UnitTeam.Enemy,
                    ObjectManager.Player.ServerPosition, bonusRange());
                foreach (var minion in allMinionsQ.Where(
                    minion =>
                        args.Target.NetworkId != minion.NetworkId && minion.Distance(args.Target.Position) < 200 &&
                        (5 - Q.Level) * Player.GetAutoAttackDamage(minion) < args.Target.Health &&
                        (5 - Q.Level) * Player.GetAutoAttackDamage(minion) < minion.Health))
                {
                    Q.Cast();
                }
            }
        }

        public static float bonusRange()
        {
            return 670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level;
        }

        private static void SetValues()
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                Combo = true;
            else
                Combo = false;

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) ||
                Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit) ||
                Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))

                Farm = true;
            else
                Farm = false;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            SetValues();

            if (R.IsReady())
            {
                if (MenuConfig.useR)
                {
                    var t = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                    if (t.IsValidTarget())
                        R.Cast(t);
                }

                if (MenuConfig.Rjungle)
                {
                    KsJungle();
                }
            }
            SetMana();


            if (E.IsReady())
                LogicE();

            if (Q.IsReady() && MenuConfig.autoQ)
                LogicQ();

            if (W.IsReady() && !ObjectManager.Player.Spellbook.IsAutoAttacking && MenuConfig.autoW)
                LogicW();

            if (R.IsReady())
                LogicR();
        }
         public static int CountAlliesInRange(Vector3 position, float range)
         {
             return EntityManager.Heroes.Allies.Count(h => h.Distance(position) <= range);
         }

        private static void KsJungle()
        {
            var mobs =
                EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.ServerPosition)
                    .OrderByDescending(mob => mob.MaxHealth);

            foreach (var mob in mobs)
            {
                if (mob.Health < mob.MaxHealth && ((mob.BaseSkinName == "SRU_Dragon" && MenuConfig.Rdragon)
                                                   || (mob.BaseSkinName == "SRU_Baron" && MenuConfig.Rbaron)
                     &&  mob.Position.CountAlliesInRange(1000) == 0 &&  mob.Distance(Player.ServerPosition) > 1000)
                    )
                {
                    if (DragonDmg == 0)
                        DragonDmg = mob.Health;

                    if (Game.Time - DragonTime > 4)
                    {
                        if (DragonDmg - mob.Health > 0)
                        {
                            DragonDmg = mob.Health;
                        }
                        DragonTime = Game.Time;
                    }

                    else
                    {
                        var DmgSec = (DragonDmg - mob.Health) * (Math.Abs(DragonTime - Game.Time) / 4);
                        if (DragonDmg - mob.Health > 0)
                        {
                            var timeTravel = GetUltTravelTime(Player, R.Speed, R.CastDelay, mob.Position);
                            var timeR = (mob.Health -
                                         Player.CalculateDamageOnUnit(mob, DamageType.Physical,
                                             (250 + (100 * R.Level)) + Player.FlatPhysicalDamageMod + 300)) / (DmgSec / 4);
                            if (timeTravel > timeR)
                                R.Cast(mob.Position);
                        }
                        else
                        {
                            DragonDmg = mob.Health;
                        }
                    }
                }
            }
        }

        private static float GetRealDistance(Obj_AI_Base target)
        {
            return
                Player.ServerPosition.Distance(Prediction.Position.PredictUnitPosition(target, 500).To3D() +
                                               Player.BoundingRadius + target.BoundingRadius);
        }

        private static float GetRealPowPowRange(GameObject target)
        {
            return 650f + Player.BoundingRadius + target.BoundingRadius;
        }

        public static class MenuConfig
        {
            public static bool autoQ
            {
                get { return SpellsMenu["autoQ"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Qharass
            {
                get { return SpellsMenu["Qharass"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool farmQout
            {
                get { return SpellsMenu["farmQout"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool farmQ
            {
                get { return SpellsMenu["farmQ"].Cast<CheckBox>().CurrentValue; }
            }

            public static int Mana
            {
                get { return SpellsMenu["Mana"].Cast<Slider>().CurrentValue; }
            }

            public static bool autoW
            {
                get { return SpellsMenu["autoW"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool wharass
            {
                get { return SpellsMenu["wharass"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool autoE
            {
                get { return SpellsMenu["autoE"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool comboE
            {
                get { return SpellsMenu["comboE"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool AGC
            {
                get { return SpellsMenu["AGC"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool telE
            {
                get { return SpellsMenu["tel"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool opsE
            {
                get { return SpellsMenu["opsE"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool autoR
            {
                get { return SpellsMenu["autoR"].Cast<CheckBox>().CurrentValue; }
            }

            public static int Wpred
            {
                get { return PredictionMenu["Wpred"].Cast<Slider>().CurrentValue; }
            }

            public static int Epred
            {
                get { return PredictionMenu["Epred"].Cast<Slider>().CurrentValue; }
            }

            public static int Rpred
            {
                get { return PredictionMenu["Rpred"].Cast<Slider>().CurrentValue; }
            }

            public static bool Rjungle
            {
                get { return JungleStealMenu["Rjungle"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Rdragon
            {
                get { return JungleStealMenu["Rdragon"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Rbaron
            {
                get { return JungleStealMenu["Rbaron"].Cast<CheckBox>().CurrentValue; }
            }

            public static int hitchanceR
            {
                get { return SpellsMenu["hitchanceR"].Cast<Slider>().CurrentValue; }
            }

            public static bool useR
            {
                get { return SpellsMenu["useR"].Cast<KeyBind>().CurrentValue; }
            }

            public static bool qRange
            {
                get { return DrawingsMenu["qRange"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool wRange
            {
                get { return DrawingsMenu["wRange"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool eRange
            {
                get { return DrawingsMenu["eRange"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool rRange
            {
                get { return DrawingsMenu["rRange"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool onlyRdy
            {
                get { return DrawingsMenu["onlyRdy"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool noti
            {
                get { return DrawingsMenu["noti"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool manaDisable
            {
                get { return SpellsMenu["manaDisable"].Cast<CheckBox>().CurrentValue; }
            }
        }

        private class UnitIncomingDamage
        {
            public int TargetNetworkId { get; set; }
            public float Time { get; set; }
            public double Damage { get; set; }
            public bool Skillshot { get; set; }
        }
    }
}