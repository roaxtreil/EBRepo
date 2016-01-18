using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using Color = System.Drawing.Color;

namespace Kappalista
{
    internal class Kappalista
    {
        public static Spell.Skillshot Q = new Spell.Skillshot(SpellSlot.Q, 1150, SkillShotType.Linear, 350,
            2400, 40);
        public static Spell.Targeted W = new Spell.Targeted(SpellSlot.W, 5000);
        public static Spell.Active E = new Spell.Active(SpellSlot.E, 950);
        public static Spell.Active R = new Spell.Active(SpellSlot.R, 1500);
        private static readonly Vector3 BaronLocation = new Vector3(5064f, 10568f, -71f);
        private static readonly Vector3 DragonLocation = new Vector3(9796f, 4432f, -71f);
        private static int ELastCastTime;
        private static int CanMoveTick;

        private static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoad;
        }

        private static void OnLoad(EventArgs args)
        {
            if (Player.Hero != Champion.Kalista) return;
            Chat.Print("Kappalista made by ThugDoge loaded.");
            Q.MinimumHitChance = HitChance.AveragePoint;
            KappalistaMenu.InitMenu();
            Activator.Initialize();
            DamageIndicator.Initialize();
            Orbwalker.OnUnkillableMinion += Orbwalking_OnNonKillableMinion;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.OnTick += OnTick;
        }

        private static void OnTick(EventArgs args)
        {
            if (Orbwalker.CanMove)
            {
                CanMoveTick = Environment.TickCount;
            }

            if (!Player.IsDead)
            {
                if (CanMoveTick <= Environment.TickCount + 100)
                {
                    //Combo
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                    {
                        if (KappalistaMenu.GetBoolValue(KappalistaMenu.SpellsMenu, "atk.minion"))
                        {
                            if (Orbwalker.GetTarget() == null)
                            {
                                if (
                                    !EntityManager.Heroes.Enemies.Any(
                                        x => x.IsValidTarget() && Player.IsInAutoAttackRange(x)))
                                {
                                    var minion =
                                        EntityManager.MinionsAndMonsters.Get(
                                            EntityManager.MinionsAndMonsters.EntityType.Both,
                                            EntityManager.UnitTeam.Enemy, Player.Position,
                                            Player.GetAutoAttackRange() + 65)
                                            .OrderBy(x => x.Distance(Player))
                                            .FirstOrDefault();
                                    if (minion != null)
                                    {
                                        Orbwalker.ForcedTarget = minion;
                                    }
                                }
                            }
                            else
                            {
                                Orbwalker.ForcedTarget = null;
                            }
                        }
                        if (KappalistaMenu.GetBoolValue(KappalistaMenu.SpellsMenu, "use.q.combo"))
                        {
                            if (Q.Ready())
                            {
                                if (!Player.IsDashing())
                                {
                                    if (!Orbwalker.IsAutoAttacking)
                                    {
                                        var target = Utility.GetTargetNoCollision(Q);
                                        if (target != null)
                                        {
                                            if (Player.Mana - EloBuddy.Player.GetSpell(SpellSlot.Q).SData.Mana >= 40)
                                                Q.Cast(target.ServerPosition);
                                        }
                                        else
                                        {
                                            var killableTarget =
                                                EntityManager.Heroes.Enemies.FirstOrDefault(
                                                    x =>
                                                        !Player.IsInAutoAttackRange(x) &&
                                                        x.IsKillableAndValidTarget(
                                                            Player.GetSpellDamage(x, SpellSlot.Q),
                                                            DamageType.Physical, Q.Range) &&
                                                        Q.GetPrediction(x).HitChance >= Q.MinimumHitChance);
                                            if (killableTarget != null)
                                                Q.Cast(killableTarget.ServerPosition);
                                        }
                                    }
                                }
                            }
                        }
                        if (KappalistaMenu.GetBoolValue(KappalistaMenu.SpellsMenu, "use.e"))
                            if (E.Ready())
                                if (
                                    EntityManager.Heroes.Enemies.Any(
                                        x =>
                                            HealthPrediction.GetHealthPrediction(x, 250) > 0 &&
                                            x.IsKillableAndValidTarget(Utility.GetRendDamage(x),
                                                DamageType.Physical, E.Range)))
                                {
                                    E.Cast();
                                }
                    }
                    //Harass
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                    {
                        if (KappalistaMenu.GetBoolValue(KappalistaMenu.SpellsMenu, "use.q.harass") &&
                            Player.ManaPercent < KappalistaMenu.GetSliderValue(KappalistaMenu.SpellsMenu, "manapercent") &&
                            !Player.IsDashing() && !Orbwalker.IsAutoAttacking && Q.Ready())
                        {
                            var target = Utility.GetTargetNoCollision(Q);
                            if (target != null)
                            {
                                Q.Cast(target.ServerPosition);
                            }
                        }
                    }
                    //Lane clear
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                    {
                        //Q Laneclear Logic
                        if (Q.Ready() && !Player.IsDashing() &&
                            Player.ManaPercent > KappalistaMenu.GetSliderValue(KappalistaMenu.SpellsMenu, "manapercent"))
                        {
                            foreach (
                                var killableMinion in
                                    EntityManager.MinionsAndMonsters.Get(
                                        EntityManager.MinionsAndMonsters.EntityType.Minion, EntityManager.UnitTeam.Enemy,
                                        Player.Position, Q.Range)
                                        .Where(
                                            x =>
                                                Q.GetPrediction(x).HitChance >= Q.MinimumHitChance &&
                                                x.IsKillableAndValidTarget(Player.GetSpellDamage(x, SpellSlot.Q),
                                                    DamageType.Physical, Q.Range)))
                            {
                                var killableNumber = 0;
                                var collisionMinions = Collision.GetCollision(
                                    new List<Vector3>
                                    {
                                        ObjectManager.Player.ServerPosition.Extend(
                                            killableMinion.ServerPosition, Q.Range)
                                    },
                                    new PredictionInput
                                    {
                                        Unit = ObjectManager.Player,
                                        Delay = Q.CastDelay,
                                        Speed = Q.Speed,
                                        Radius = Q.Width,
                                        Range = Q.Range,
                                        CollisionObjectsEx = new[] {CollisionableObjectsEx.Minions},
                                        UseBoundingRadius = false
                                    }
                                    ).OrderBy(x => x.Distance(ObjectManager.Player));

                                foreach (var collisionMinion in collisionMinions)
                                {
                                    var hue = collisionMinion as Obj_AI_Minion;
                                    if (
                                        hue.IsKillableAndValidTarget(
                                            ObjectManager.Player.GetSpellDamage(collisionMinion,
                                                SpellSlot.Q), DamageType.Physical,
                                            Q.Range))
                                        killableNumber++;
                                    else
                                        break;
                                }
                                if (killableNumber >=
                                    KappalistaMenu.GetSliderValue(KappalistaMenu.LaneClear, "q.min.kill"))
                                {
                                    if (!Orbwalker.IsAutoAttacking)
                                    {
                                        Q.Cast(killableMinion.ServerPosition);
                                        break;
                                    }
                                }
                            }
                        }
                        //E Laneclear Logic
                        if (E.Ready() &&
                            KappalistaMenu.GetSliderValue(KappalistaMenu.LaneClear, "manapercent") < Player.ManaPercent)
                        {
                            if (
                                EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                                    Player.Position, E.Range).Count(x =>
                                        HealthPrediction.GetHealthPrediction(x, 250) > 0 &&
                                        x.IsKillableAndValidTarget(Utility.GetRendDamage(x),
                                            DamageType.Physical)) >=
                                KappalistaMenu.GetSliderValue(KappalistaMenu.LaneClear, "e.minkill"))
                            {
                                E.Cast();
                            }
                        }
                    }
                    //Jungle Clear
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) &&
                        KappalistaMenu.GetSliderValue(KappalistaMenu.JungleClear, "manapercent") < Player.ManaPercent)
                    {
                        //Q logic
                        if (KappalistaMenu.GetBoolValue(KappalistaMenu.JungleClear, "use.q") && Q.Ready())
                        {
                            var target = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Position, Q.Range)
                                .OrderByDescending(i => i.MaxHealth)
                                .FirstOrDefault(
                                    x =>
                                        x.IsValidTarget(Q.Range) &&
                                        Q.GetPrediction(x).HitChance >= HitChance.AveragePoint);

                            if (target != null)
                                Q.Cast(target.ServerPosition);
                        }
                        //E logic
                        if (KappalistaMenu.GetBoolValue(KappalistaMenu.JungleClear, "use.e") && E.Ready())
                        {
                            if (
                                EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Position, E.Range)
                                    .OrderByDescending(i => i.MaxHealth)
                                    .Any(x => HealthPrediction.GetHealthPrediction(x, 250) > 0 &&
                                              x.IsKillableAndValidTarget(Utility.GetRendDamage(x),
                                                  DamageType.Physical)))
                            {
                                E.Cast();
                            }
                        }
                    }
                }
                //Perma Active
                if (KappalistaMenu.GetBoolValue(KappalistaMenu.Misc, "e.killsteal") && E.Ready())
                {
                    if (EntityManager.Heroes.Enemies.Any(x =>
                        HealthPrediction.GetHealthPrediction(x, 250) > 0 &&
                        x.IsKillableAndValidTarget(Utility.GetRendDamage(x), DamageType.Physical,
                            E.Range)))
                    {
                        E.Cast();
                    }
                }

                if (KappalistaMenu.GetBoolValue(KappalistaMenu.Misc, "e.mobsteal") && E.Ready())
                {
                    if (
                        EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Position, E.Range)
                            .OrderByDescending(x => x.MaxHealth)
                            .Any(y => HealthPrediction.GetHealthPrediction(y, 500) > 0 &&
                                      y.IsKillableAndValidTarget(Utility.GetRendDamage(y), DamageType.Physical)))
                    {
                        E.Cast();
                    }
                }

                if (KappalistaMenu.GetBoolValue(KappalistaMenu.Misc, "e.siegeandsuper") && E.Ready())
                {
                    if (
                        EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Position,
                            E.Range).Any(x => HealthPrediction.GetHealthPrediction(x, 250) > 0 &&
                                              x.IsKillableAndValidTarget(Utility.GetRendDamage(x), DamageType.Physical) &&
                                              (x.CharData.BaseSkinName.ToLower().Contains("siege") ||
                                               x.CharData.BaseSkinName.ToLower().Contains("super"))))
                    {
                        E.Cast();
                    }
                }

                if (KappalistaMenu.GetBoolValue(KappalistaMenu.Misc, "r.balista") && R.Ready())
                {
                    var blitz =
                        EntityManager.Heroes.Allies.FirstOrDefault(
                            x => !x.IsDead && x.HasBuff("kalistacoopstrikeally") && x.ChampionName == "Blitzcrank");
                    if (blitz != null)
                    {
                        var grabTarget =
                            EntityManager.Heroes.Enemies.FirstOrDefault(x => !x.IsDead && x.HasBuff("rocketgrab2"));
                        if (grabTarget != null)
                            if (ObjectManager.Player.Distance(grabTarget) > blitz.Distance(grabTarget))
                                R.Cast();
                    }
                }

                if (KappalistaMenu.GetBoolValue(KappalistaMenu.Misc, "e.harass") &&
                    !KappalistaMenu.GetBoolValue(KappalistaMenu.Misc, "e.dontharasscombo") && E.Ready() &&
                    Player.Mana - EloBuddy.Player.GetSpell(SpellSlot.E).SData.Mana >=
                    EloBuddy.Player.GetSpell(SpellSlot.E).SData.Mana)
                {
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                        if (
                            EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Both,
                                EntityManager.UnitTeam.Enemy, Player.Position).Any(x =>
                                    HealthPrediction.GetHealthPrediction(x, 250) > 0 &&
                                    x.IsKillableAndValidTarget(Utility.GetRendDamage(x),
                                        DamageType.Physical, E.Range)))
                        {
                            E.Cast();
                        }
                }
                if (KappalistaMenu.Misc["w.dragonorbaron"].Cast<CheckBox>().CurrentValue && !Orbwalker.IsAutoAttacking &&
                    !Player.IsRecalling() && W.Ready() && Player.Position.CountEnemiesInRange(1500) <= 0 &&
                    Orbwalker.GetTarget() == null && Player.ManaPercent > 50)
                {
                    if (Player.Distance(DragonLocation) <= W.Range)
                    {
                        W.Cast(DragonLocation);
                    }
                    if (Player.Distance(BaronLocation) <= W.Range)
                    {
                        W.Cast(BaronLocation);
                    }
                }

                if (KappalistaMenu.Misc["w.castdragon"].Cast<KeyBind>().CurrentValue && W.Ready())
                {
                    if (Player.Distance(DragonLocation) <= W.Range)
                    {
                        W.Cast(DragonLocation);
                    }
                }
                if (KappalistaMenu.Misc["w.castbaron"].Cast<KeyBind>().CurrentValue && W.Ready())
                {
                    if (Player.Distance(BaronLocation) <= W.Range)
                    {
                        W.Cast(BaronLocation);
                    }
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!Player.IsDead)
            {
                if (KappalistaMenu.GetBoolValue(KappalistaMenu.Drawings, "draw.q") && Q.Ready())
                    Drawing.DrawCircle(Player.Position, Q.Range,
                        Color.DarkBlue);

                if (KappalistaMenu.GetBoolValue(KappalistaMenu.Drawings, "draw.w") && W.Ready())
                    Drawing.DrawCircle(Player.Position, W.Range,
                        Color.DarkBlue);

                if (KappalistaMenu.GetBoolValue(KappalistaMenu.Drawings, "draw.e") && E.Ready())
                    Drawing.DrawCircle(Player.Position, E.Range,
                        Color.DarkBlue);

                if (KappalistaMenu.GetBoolValue(KappalistaMenu.Drawings, "draw.r") && R.Ready())
                    Drawing.DrawCircle(Player.Position, R.Range,
                        Color.DarkBlue);


                if (KappalistaMenu.GetBoolValue(KappalistaMenu.Drawings, "draw.e.dmgpercent"))
                {
                    foreach (var target in EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsVisible))
                    {
                        if (Utility.GetRendDamage(target) > 2)
                        {
                            var targetPos = Drawing.WorldToScreen(target.Position);
                            var damagePercent = (Utility.GetRendDamage(target)/target.Health + target.AttackShield)*100;

                            if (damagePercent > 0)
                                Drawing.DrawText(targetPos.X + 80, targetPos.Y - 130,
                                    damagePercent >= 100 ? Color.Red : Color.GreenYellow,
                                    damagePercent.ToString("0.0") + "%");
                        }
                    }

                    foreach (
                        var target in
                            EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Position, int.MaxValue)
                                .Where(x => !x.IsDead && x.IsVisible))
                    {
                        if (Utility.GetRendDamage(target) > 2)
                        {
                            var targetPos = Drawing.WorldToScreen(target.Position);
                            var damagePercent = (Utility.GetRendDamage(target)/target.Health + target.AttackShield)*100;

                            if (damagePercent > 0)
                                Drawing.DrawText(targetPos.X, targetPos.Y - 100,
                                    damagePercent >= 100 ? Color.Red : Color.GreenYellow, damagePercent.ToString("0.0"));
                        }
                    }
                }
            }
        }

        private static void Orbwalking_OnNonKillableMinion(Obj_AI_Base minion, Orbwalker.UnkillableMinionArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                if (KappalistaMenu.GetBoolValue(KappalistaMenu.Misc, "e.lasthit.assist") && E.Ready() && !EntityManager.Heroes.Enemies.Any(x => Player.IsInAutoAttackRange(x)))
                    foreach (
                        var marioteachedmekappa in
                            EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Minion,
                                EntityManager.UnitTeam.Enemy, Player.Position, E.Range)
                                .Where(x => x.IsKillableAndValidTarget(Utility.GetRendDamage(x), DamageType.Physical) && x.HasRendBuff() && HealthPrediction.GetHealthPrediction(x, 250) > 0))
                    {
                        E.Cast();
                    }
                  
            }
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
                if (sender.Owner.IsMe)
                    if (args.Slot == SpellSlot.E)
                        if (ELastCastTime > Utility.TickCount - 700)
                            args.Process = false;
                        else
                            ELastCastTime = Utility.TickCount;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender != null && args.Target != null)
                if (sender.IsEnemy)
                    if (sender.Type == GameObjectType.AIHeroClient)
                    {
                        if (KappalistaMenu.GetBoolValue(KappalistaMenu.Misc, "r.savebuddy"))
                            if (R.Ready())
                            {
                                var soulbound =
                                    EntityManager.Heroes.Allies.FirstOrDefault(
                                        x => !x.IsDead && x.HasBuff("kalistacoopstrikeally"));
                                if (soulbound != null)
                                    if (args.Target.NetworkId == soulbound.NetworkId ||
                                        args.End.Distance(soulbound.Position) <= 200)
                                        if (soulbound.HealthPercent < 20)
                                            R.Cast();
                            }

                        if (KappalistaMenu.GetBoolValue(KappalistaMenu.Misc, "e.beforedie"))
                            if (args.Target.IsMe)
                                if (Player.HealthPercent <= 10)
                                    if (E.Ready())
                                        if (
                                            EntityManager.Heroes.Enemies.Any(
                                                x => x.IsValidTarget(E.Range) && Utility.GetRendDamage(x) > 0))
                                            E.Cast();
                    }
        }
    }
}