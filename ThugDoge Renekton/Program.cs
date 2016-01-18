using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.Remoting.Messaging;
using Color = System.Drawing.Color;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace Renekton
{
    internal class Renekton
    {
        public static Menu RenektonMenu, ComboMenu, HarassMenu, LaneClearMenu, DrawingsMenu;
        public static readonly AIHeroClient player = ObjectManager.Player;
        public static Spell.Active Q, W, R;
        public static Spell.Skillshot E;
        public static Spell.Targeted Ignite;
        private static float lastE;

        private static Vector3 lastEpos;
        private static Bool wChancel = false;

        static void Main(string [] args)
        {
            Loading.OnLoadingComplete += RenektonLoad;
        }

        public static void RenektonLoad(EventArgs args)
        {
            InitRenekton();
            InitMenu();
            Chat.Print("ThugDoge Renekton loaded.");
            Game.OnUpdate += Game_OnGameUpdate;
            Orbwalker.OnPreAttack += beforeAttack;
            Orbwalker.OnPostAttack += afterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Drawing.OnDraw += Game_OnDraw;
        }

      
    
    private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                Console.WriteLine(args.SData.Name);
            }
        }

       

        public static int GameTimeTickCount
        {
            get { return (int)(Game.Time * 1000); }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                if (Ignite != null && Config.Useignite)
                {
                    if (Ignite.IsReady() &&
                        player.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite) >=
                        enemy.TotalShieldHealth() && enemy.Health < 50 + 20 * player.Level - (enemy.HPRegenRate / 5 * 3))
                    {
                        Ignite.Cast(enemy);
                    }
                }
            }

            if (System.Environment.TickCount - lastE > 4100)
            {
                lastE = 0;
            }
            switch (Orbwalker.ActiveModesFlags)
            {
                case Orbwalker.ActiveModes.Combo:
                    Combo();
                    break;
                case Orbwalker.ActiveModes.Harass:
                    Harass();
                    break;
                case Orbwalker.ActiveModes.LaneClear:
                    LaneClear();
                    break;
                case Orbwalker.ActiveModes.LastHit:
                    break;
                default:
                    break;
            }
          
        }

        private static void afterAttack(AttackableUnit target, EventArgs args)
        {
            if (target is Obj_AI_Base &&
                ((Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) &&
                  checkFuryMode(SpellSlot.W, (Obj_AI_Base)target)) ||
                 Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass)))
            {
                var time = Game.Time - player.Spellbook.GetSpell(SpellSlot.W).CooldownExpires;
                if (time < -9 || (!W.IsReady() && time < -1))
                {
                    ItemHandler.castHydra((Obj_AI_Base)target);
                }
            }
            if (target is Obj_AI_Base && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) &&
                Config.UseWCombo && checkFuryMode(SpellSlot.W, (Obj_AI_Base)target))
            {
                W.Cast();
            }
            if (target is Obj_AI_Base && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) &&
                HarassMenu["useCH"].Cast<Slider>().CurrentValue == 0)
            {
                if (W.IsReady())
                {
                    W.Cast();
                    Orbwalker.ResetAutoAttack();
                    return;
                }
                if (Q.IsReady())
                {
                    Q.Cast();
                    return;
                }
                if (target.IsValidTarget(E.Range) && E.IsReady())
                {
                    E.Cast(target.Position);
                    return;
                }
            }
        }

        

        private static void beforeAttack(AttackableUnit unit, Orbwalker.PreAttackArgs args)
        {
            if (unit.IsMe && W.IsReady() && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) &&
                args.Target is Obj_AI_Base && checkFuryMode(SpellSlot.W, (Obj_AI_Base)args.Target) &&
                Config.UseWCombo)
            {
                if ((player.Mana > 40 && !fury) || (Q.IsReady() && canBeOpWIthQ(player.Position)))
                {
                    return;
                }

                W.Cast();
                return;
            }
            if (unit.IsMe && W.IsReady() && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) &&
                Config.UseWHarass && args.Target is Obj_AI_Base &&
                HarassMenu["useCH"].Cast<Slider>().CurrentValue != 0)
            {
                W.Cast();
            }
        }


        static void Drawing_OnEndScene(EventArgs args)
        {
            if (!player.IsDead)
            {
                var Target = TargetSelector.GetTarget(900, DamageType.Physical);
                if (Target != null)
                {
                    if (Menu["ComboDamage on HPBar"].Cast<CheckBox>().CurrentValue)
                    {
                        float FutureDamage = ComboDamage(Target) > Target.Health ? -1 : GetComboDamage() / Target.MaxHealth;

                        if (FutureDamage == -1)
                        {
                            Drawing.DrawText(Target.Position.WorldToScreen().X - 30, Target.Position.WorldToScreen().Y - 150, Color.Yellow, "Killable");
                            return;
                        }

                        Line.DrawLine
                        (
                            Color.LightSkyBlue, 9f,
                            new Vector2(Target.HPBarPosition.X + 1, Target.HPBarPosition.Y + 9),
                            new Vector2(Target.HPBarPosition.X + 1 + FutureDamage * 104, Target.HPBarPosition.Y + 9)
                        );
                    }
                }

            }

            return;
        }


        static float GetComboDamage()
        {
            var Target = TargetSelector.GetTarget(900, DamageType.Physical);
            if (Target != null)
            {
                float ComboDamage = new float();

                ComboDamage = Q.IsReady() ? ComboDamage(Target, SpellSlot.Q) : 0;
                ComboDamage += E.IsReady() ? ComboDamage(Target, SpellSlot.E) : 0;
                ComboDamage += R.IsReady() ? ComboDamage(Target, SpellSlot.R) : 0;
                ComboDamage += player.GetAutoAttackDamage(Target) * 2;
                ComboDamage += Bilgewater.IsReady() ? DamageLibrary.GetItemDamage(Player, Target, ItemId.Bilgewater_Cutlass) : 0;
                ComboDamage += BOTRK.IsReady() ? DamageLibrary.GetItemDamage(Player, Target, ItemId.Blade_of_the_Ruined_King) : 0;
                ComboDamage += Hydra.IsReady() ? DamageLibrary.GetItemDamage(Player, Target, ItemId.Ravenous_Hydra_Melee_Only) : 0;

                if (Ignite != null) ComboDamage += Convert.ToSingle(Ignite.IsReady() ? DamageLibrary.GetSummonerSpellDamage(Player, Target, DamageLibrary.SummonerSpells.Ignite) : 0);
                if (Smite != null) ComboDamage += Convert.ToSingle(Smite.IsReady() && Smite.Name.Contains("gank") ? DamageLibrary.GetSummonerSpellDamage(Player, Target, DamageLibrary.SummonerSpells.Smite) : 0);

                return ComboDamage;
            }
            return 0;
        }

        private static bool rene
        {
            get { return player.Buffs.Any(buff => buff.Name == "renektonsliceanddicedelay"); }
        }

        private static bool fury
        {
            get { return player.Buffs.Any(buff => buff.Name == "renektonrageready"); }
        }

        private static bool renw
        {
            get { return player.Buffs.Any(buff => buff.Name == "renektonpreexecute"); }
        }

        private static void Combo()
        {
            Obj_AI_Base target = TargetSelector.GetTarget(E.Range * 2, DamageType.Physical);
            if (target == null)
            {
                return;
            }
            
            var FuryQ = DamageLibrary.GetSpellDamage(player, target, SpellSlot.Q) * 0.5;
            var FuryW = DamageLibrary.GetSpellDamage(player, target, SpellSlot.W) * 0.5;
            var eDmg = DamageLibrary.GetSpellDamage(player, target, SpellSlot.E);
            var combodamage = ComboDamage(target);
            
            if (player.Distance(target) > E.Range && E.IsReady() && (W.IsReady() || Q.IsReady()) && lastE.Equals(0) &&
                Config.UseECombo)
            {
                var closeGapTarget =
                    ObjectManager.Get<Obj_AI_Base>()
                        .FirstOrDefault(
                            i =>
                                i.IsEnemy && player.Distance(i) < E.Range && !i.IsDead &&
                                i.Distance(target.ServerPosition) < E.Range);
                if (closeGapTarget != null)
                {
                    if ((canBeOpWIthQ(closeGapTarget.Position) || fury) && !rene)
                    {
                        if (closeGapTarget.IsValidTarget(E.Range) && E.IsReady())
                        {
                            E.Cast(closeGapTarget.Position);
                            lastE = System.Environment.TickCount;
                            return;
                        }
                    }
                }
            }
            if (Config.UseQCombo && target.IsValidTarget(Q.Range) && Q.IsReady() && !renw && !player.IsDashing() &&
                checkFuryMode(SpellSlot.Q, target) &&
                (!W.IsReady() ||
                 ((W.IsReady() && !fury) || (player.Health < target.Health) ||
                  (EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, player.Position, Q.Range).Count() +
                   player.CountEnemiesInRange(Q.Range) > 3 && fury))))
            {
                Q.Cast();
            }
            var distance = player.Distance(target.Position);
            if (Config.UseECombo && lastE.Equals(0) && target.IsValidTarget(E.Range) && E.IsReady() &&
                (eDmg > target.Health ||
                 (((W.IsReady() && canBeOpWIthQ(target.Position) && !rene) ||
                   (distance > target.Distance(player.Position.Extend(target.Position, E.Range)) - distance)))))
            {
                E.Cast(target.Position);
                lastE = System.Environment.TickCount;
                return;
            }
            if (Config.UseECombo && checkFuryMode(SpellSlot.E, target) && !lastE.Equals(0) &&
                (eDmg + player.GetAutoAttackDamage(target) > target.Health ||
                 (((W.IsReady() && canBeOpWIthQ(target.Position) && !rene) ||
                   (distance < target.Distance(player.Position.Extend(target.Position, E.Range)) - distance) ||
                   player.Distance(target) > E.Range - 100))))
            {
                var time = System.Environment.TickCount - lastE;
                if (time > 3600f || combodamage > target.Health || (player.Distance(target) > E.Range - 100))
                {
                    E.Cast(target.Position);
                    lastE = 0;
                }
            }
            if ((player.Health * 100 / player.MaxHealth) <= Config.UseRUnder ||
                Config.UseRInDanger < player.CountEnemiesInRange(R.Range))
            {
                R.Cast();
            }
        }

        private static bool canBeOpWIthQ(Vector3 vector3)
        {
            if (fury)
            {
                return false;
            }
            if ((player.Mana > 45 && !fury) ||
                (Q.IsReady() &&
                 player.Mana + EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, vector3, Q.Range).Count() * 2.5 +
                 player.CountEnemiesInRange(Q.Range) * 10 > 50))
            {
                return true;
            }
            return false;
        }

        private bool canBeOpwithW()
        {
            if (player.Mana + 20 > 50)
            {
                return true;
            }
            return false;
        }

        private static void Harass()
        {
            Obj_AI_Base target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
            if (target == null)
            {
                return;
            }
            switch (HarassMenu["useCH"].Cast<Slider>().CurrentValue)
            {
                case 1:
                    if (Q.IsReady() && E.IsReady() && lastE.Equals(0) && fury && !rene)
                    {
                        if (Config.DontEQUnderTurret &&
                            player.Position.ExtendVector3(target.Position, E.Range).UnderTurret(true))
                        {
                            return;
                        }
                        var closeGapTarget =
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(
                                    i =>
                                        i.IsEnemy && player.Distance(i) < E.Range && !i.IsDead &&
                                        i.Distance(target.ServerPosition) < Q.Range - 40)
                                .OrderByDescending(i => EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, i.Position, Q.Range))
                                .FirstOrDefault();
                        if (closeGapTarget != null)
                        {
                            lastEpos = player.ServerPosition;
                            Core.DelayAction(() => lastEpos = new Vector3(),4100);
                            E.Cast(closeGapTarget.Position);
                            lastE = System.Environment.TickCount;
                            return;
                        }
                        else
                        {
                            lastEpos = player.ServerPosition;
                            Core.DelayAction(() => lastEpos = new Vector3(), 4100);
                            E.Cast(target.Position);
                            lastE = System.Environment.TickCount;
                            return;
                        }
                    }
                    if (player.Distance(target) < target.GetAutoAttackRange() && Q.IsReady() &&
                        E.IsReady() && E.IsReady())
                    {
                        Orbwalker.ForcedTarget = target;
                    }
                    return;
                    break;
                case 0:
                    if (Q.IsReady() && W.IsReady() && !rene && target.IsValidTarget(E.Range) && E.IsReady())
                    {
                        if (Config.DontEQUnderTurret &&
                            player.Position.ExtendVector3(target.Position, E.Range).UnderTurret(true))
                        {
                            return;
                        }
                        var prediction = E.GetPrediction(target);
                        if (prediction.HitChance == HitChance.High)
                        {
                            lastE = System.Environment.TickCount;
                        }
                    }
                    if (rene && target.IsValidTarget(E.Range) && E.IsReady() && !lastE.Equals(0) && System.Environment.TickCount - lastE > 3600)
                    {
                        var prediction = E.GetPrediction(target);
                        if (prediction.HitChance == HitChance.High)
                        {
                            E.Cast(target);
                        }
                    }
                    if (player.Distance(target) < target.GetAutoAttackRange() && Q.IsReady() &&
                        E.IsReady() && E.IsReady())
                    {
                        Orbwalker.ForcedTarget = target;
                    }
                    return;
                    break;
                default:
                    break;
            }

            if (Config.UseQHarass && target.IsValidTarget(Q.Range) && Q.IsReady())
            {
                Q.Cast();
            }

            if (HarassMenu["useCH"].Cast<Slider>().CurrentValue == 0 && !lastE.Equals(0) && rene &&
                !Q.IsReady() && !renw)
            {
                if (lastEpos.IsValid())
                {
                    E.Cast(player.Position.ExtendVector3(lastEpos, 350f));
                }
            }
        }
           
       


        private static List<List<Vector2>> GetCombinations(List<Vector2> allValues)
        {
            var collection = new List<List<Vector2>>();
            for (var counter = 0; counter < (1 << allValues.Count); ++counter)
            {
                var combination = allValues.Where((t, i) => (counter & (1 << i)) == 0).ToList();

                collection.Add(combination);
            }
            return collection;
        }

        private static void Game_OnDraw(EventArgs args)
        {           
            if (player.IsDead) return;
            var color = new ColorBGRA(255, 255, 255, 100);
            if (Config.DrawQ && Q.IsReady())
            {
                Circle.Draw(color, Q.Range, player.Position);
            }

            if (Config.DrawW && W.IsReady())
            {
                Circle.Draw(color, W.Range, player.Position);
            }
            if (Config.DrawE && E.IsReady())
            {
                Circle.Draw(color, E.Range, player.Position);
            }         
        }

        private static float ComboDamage(Obj_AI_Base hero)
        {
            double damage = 0;
            if (Q.IsReady())
            {
                damage += player.GetSpellDamage(hero, SpellSlot.Q);
            }
            if (W.IsReady())
            {
                damage += player.GetSpellDamage(hero, SpellSlot.W);
            }
            if (E.IsReady())
            {
                damage += player.GetSpellDamage(hero, SpellSlot.E);
            }          

            damage += ItemHandler.GetItemsDamage(hero);

            if ((Item.HasItem(ItemHandler.Bft.Id) && Item.CanUseItem(ItemHandler.Bft.Id)) ||
                (Item.HasItem(ItemHandler.Dfg.Id) && Item.CanUseItem(ItemHandler.Dfg.Id)))
            {
                damage = (float)(damage * 1.2);
            }

            var ignitedmg = player.GetSummonerSpellDamage(hero, DamageLibrary.SummonerSpells.Ignite);
            if (player.Spellbook.CanUseSpell(player.GetSpellSlotFromName("summonerdot")) == SpellState.Ready &&
                hero.Health < damage + ignitedmg)
            {
                damage += ignitedmg;
            }
            return (float)damage;
        }

        public static bool checkFuryMode(SpellSlot spellSlot, Obj_AI_Base target)
        {
            if (player.GetSpellDamage(target, spellSlot) > target.Health)
            {
                return true;
            }
            if (canBeOpWIthQ(player.Position))
            {
                return false;
            }
            if (!fury)
            {
                return true;
            }
            if (player.Spellbook.IsCastingSpell || player.Spellbook.IsCastingSpell)
            {
                return false;
            }
            switch (ComboMenu["furyMode"].Cast<Slider>().CurrentValue)
            {
                case 0:
                    return true;
                    break;
                case 1:
                    if (spellSlot != SpellSlot.Q && Q.IsReady())
                    {
                        return false;
                    }
                    break;
                case 2:
                    if (spellSlot != SpellSlot.W && (W.IsReady() || renw))
                    {
                        return false;
                    }
                    break;
                case 3:
                    if (spellSlot != SpellSlot.E && rene)
                    {
                        return false;
                    }
                    break;
                default:
                    return true;
                    break;
            }
            return false;
        }

        public static void LaneClear()
        {
            if (Config.UseELaneclear && E.IsReady())
            {
                var aenemy = (Obj_AI_Minion)GetEnemy(E.Range, GameObjectType.obj_AI_Minion);

                if (aenemy != null)
                    E.Cast(aenemy.ServerPosition);
            }
            if (Config.UseQLaneclear && Q.IsReady())
            {
                var qenemy = (Obj_AI_Minion)GetEnemy(Q.Range, GameObjectType.obj_AI_Minion);

                if (qenemy != null)
                    Q.Cast();
            }
            if (Config.UseWLaneclear && !W.IsReady())
            {
                var wenemy =
                    (Obj_AI_Minion)GetEnemy(player.GetAutoAttackRange(), GameObjectType.obj_AI_Minion);

                if (wenemy != null && player.GetSpellDamage(wenemy, SpellSlot.Q) >= wenemy.Health)
                    W.Cast();
            }
            if (!Orbwalker.CanAutoAttack) return;
            var cenemy = (Obj_AI_Minion)GetEnemy(player.GetAutoAttackRange(), GameObjectType.obj_AI_Minion);

            if (cenemy != null)
                Orbwalker.ForcedTarget = cenemy;
        }

        private static Obj_AI_Base GetEnemy(float range, GameObjectType t)
        {
            switch (t)
            {
                case GameObjectType.AIHeroClient:
                    return EntityManager.Heroes.Enemies.OrderBy(a => a.Health).FirstOrDefault(
                        a => a.Distance(Player.Instance) < range && !a.IsDead && !a.IsInvulnerable);
                default:
                    return EntityManager.MinionsAndMonsters.EnemyMinions.OrderBy(a => a.Health).FirstOrDefault(
                        a => a.Distance(Player.Instance) < range && !a.IsDead && !a.IsInvulnerable);
            }
        }

        private static void InitRenekton()
        {
            Q = new Spell.Active(SpellSlot.Q, 300);
            W = new Spell.Active(SpellSlot.W, Convert.ToUInt32(player.AttackRange + 55));
            E = new Spell.Skillshot(SpellSlot.E, 450, SkillShotType.Linear);
            R = new Spell.Active(SpellSlot.R);
            var slot = player.GetSpellSlotFromName("summonerdot");
            if (slot != SpellSlot.Unknown)
            {
                Ignite = new Spell.Targeted(slot, 600);
            }

        }


        private static CheckBox _useqcombo;
        private static CheckBox _usewcombo;
        private static CheckBox _useecombo;
        private static Slider _userunder;
        private static Slider _userindanger;
        private static CheckBox _useignite;
        private static CheckBox _useqharass;
        private static CheckBox _usewharass;
        private static CheckBox _useqlaneclear;
        private static CheckBox _usewlaneclear;
        private static CheckBox _useelaneclear;
        private static CheckBox _dontequnderturret;
        private static CheckBox _drawq;
        private static CheckBox _draww;
        private static CheckBox _drawe;

        public static class Config
        {
            public static bool UseQCombo
            {
                get { return _useqcombo.CurrentValue; }
            }

            public static bool UseWCombo
            {
                get { return _usewcombo.CurrentValue; }
            }

            public static bool UseECombo
            {
                get { return _useecombo.CurrentValue; }
            }

            public static int UseRUnder
            {
                get { return _userunder.CurrentValue; }
            }

            public static bool DontEQUnderTurret
            {
                get { return _dontequnderturret.CurrentValue; }
            }

            public static int UseRInDanger
            {
                get { return _userindanger.CurrentValue; }
            }

            public static bool Useignite
            {
                get { return _useignite.CurrentValue; }
            }

            public static bool UseQHarass
            {
                get { return _useqharass.CurrentValue; }
            }

            public static bool UseWHarass
            {
                get { return _usewharass.CurrentValue; }
            }

            public static bool UseQLaneclear
            {
                get { return _useqlaneclear.CurrentValue; }
            }

            public static bool UseWLaneclear
            {
                get { return _usewlaneclear.CurrentValue; }
            }

            public static bool UseELaneclear
            {
                get { return _useelaneclear.CurrentValue; }
            }

            public static bool DrawQ
            {
                get { return _drawq.CurrentValue; }
            }

            public static bool DrawW
            {
                get { return _draww.CurrentValue; }
            }

            public static bool DrawE
            {
                get { return _drawe.CurrentValue; }
            }
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

        private static void InitMenu()
        {

            // Combo Settings
            RenektonMenu = MainMenu.AddMenu("ThugDoge Renekton", "Renekton");
            RenektonMenu.AddLabel("I hope you enjoy this addon, and also  recommend you to test my other addons :)");
            ComboMenu = RenektonMenu.AddSubMenu("Combo", "Combo");
            ComboMenu.AddSeparator();
            //Combo Menu
            _useqcombo = ComboMenu.Add("useqcombo", new CheckBox("Use Q"));
            _usewcombo = ComboMenu.Add("usewcombo", new CheckBox("Use W"));
            _useecombo = ComboMenu.Add("useecombo", new CheckBox("Use E"));
            _userunder = ComboMenu.Add("user", new Slider("Use R under", 20, 0, 100));
            _userindanger = ComboMenu.Add("userindanger", new Slider("Use R above X enemy", 2, 1, 5));
            StringList(ComboMenu, "furyMode", "Fury priority",
                new[] {"No priority", "Q", "W", "E"}, 0);
            _useignite = ComboMenu.Add("useIgnite", new CheckBox("Use Ignite"));

            // Harass Settings
            HarassMenu = RenektonMenu.AddSubMenu("Harass", "Harass");
            HarassMenu.AddSeparator();
            _useqharass = HarassMenu.Add("useqH", new CheckBox("Use Q"));
            _usewharass = HarassMenu.Add("usewH", new CheckBox("Use W"));
            StringList(HarassMenu, "useCH", "Harass mode",
                new[] {"Use harass combo", "E-furyQ-Eback if possible", "Basic"}, 0);
            _dontequnderturret = HarassMenu.Add("donteqwebtower", new CheckBox("Don't dash under tower"));
            LaneClearMenu = RenektonMenu.AddSubMenu("Lane Clear", "Lane Clear");
            LaneClearMenu.AddSeparator();
            _useqlaneclear = LaneClearMenu.Add("laneclearq", new CheckBox("Use Q"));
            _usewlaneclear = LaneClearMenu.Add("laneclearw", new CheckBox("Use W"));
            _useelaneclear = LaneClearMenu.Add("lanecleare", new CheckBox("Use E"));
            DrawingsMenu = RenektonMenu.AddSubMenu("Drawings", "Drawings");
            DrawingsMenu.AddSeparator();
            _drawq = DrawingsMenu.Add("drawq", new CheckBox("Draw Q"));
            _draww = DrawingsMenu.Add("draww", new CheckBox("Draw W"));
            _drawe = DrawingsMenu.Add("drawe", new CheckBox("Draw E"));


        }
    }
}