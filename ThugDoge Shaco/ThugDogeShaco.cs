using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Drawing;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using Shaco.Utility;
using SharpDX;

namespace Shaco
{
    internal class ThugDogeShaco
    {
        public static bool CloneDelay;
        public static int CloneRange = 2300;
        public static int LastAATick;
        public static Item Hydra, Tiamat;
        public static Spell.Targeted Q, E, R;
        public static Spell.Skillshot W;
        public static Spell.Targeted Ignite;
        public static Spell.Active R2;
        public static Menu ShacoMenu, ComboMenu, HarassMenu, LaneClearMenu, MiscMenu, DrawingsMenu, SmiteMenu;
        private static CheckBox _useq;
        private static Slider _useqmin;
        private static CheckBox _usew;
        private static CheckBox _useecombo;
        private static CheckBox _useeharass;
        private static CheckBox _user;
        private static CheckBox _usercc;
        private static CheckBox _moveclone;
        private static CheckBox _jukefleewithclone;
        private static Slider _jukefleepercentage;
        private static CheckBox _waitforstealth;
        private static CheckBox _useignite;
        private static CheckBox _ksq;
        private static KeyBind _automoveclone;
        private static CheckBox _ks;
        private static CheckBox _useitems;
        private static KeyBind _stackbox;
        private static CheckBox _drawq;
        private static CheckBox _drawqtimer;
        private static CheckBox _draww;
        private static CheckBox _drawe;
        private static CheckBox _drawdamagebar;
        public static Text StealthTimer;
        public static float DeceiveTime;

        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        public static Obj_AI_Minion Clone
        {
            get
            {
                Obj_AI_Minion Clone = null;
                if (Player.Spellbook.GetSpell(SpellSlot.R).Name != "hallucinateguide") return null;
                return ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(m => m.Name == Player.Name && !m.IsMe);
            }
        }

        public static bool ShacoStealth
        {
            get { return Player.HasBuff("Deceive"); }
        }

        public static int GameTimeTickCount
        {
            get { return (int)(Game.Time * 1000); }
        }



        public static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.Hero != Champion.Shaco) return;
            Chat.Print("ThugDoge Shaco loaded, good luck :)");
            Q = new Spell.Targeted(SpellSlot.Q, 400);
            W = new Spell.Skillshot(SpellSlot.W, 425, SkillShotType.Circular);
            E = new Spell.Targeted(SpellSlot.E, 625);
            R = new Spell.Targeted(SpellSlot.R, 2300);
            R2 = new Spell.Active(SpellSlot.R, 200);
            StealthTimer = new Text("", new Font("Calisto MT", 25F, FontStyle.Bold));
            var slot = Player.GetSpellSlotFromName("summonerdot");
            if (slot != SpellSlot.Unknown)
            {
                Ignite = new Spell.Targeted(slot, 600);
            }
            Tiamat = new Item((int)ItemId.Tiamat_Melee_Only, 420);
            Hydra = new Item((int)ItemId.Ravenous_Hydra_Melee_Only, 420);

            ShacoMenu = MainMenu.AddMenu("ThugDoge Shaco", "shaco");
            ComboMenu = ShacoMenu.AddSubMenu("Combo", "Combo");
            ComboMenu.AddSeparator();
            //Combo Menu
            _useq = ComboMenu.Add("useqcombo", new CheckBox("Use Q"));
            _useqmin = ComboMenu.Add("useminrange", new Slider("Q min range", 200, 0, 400));
            _usew = ComboMenu.Add("usewcombo", new CheckBox("Use W"));
            _useecombo = ComboMenu.Add("useecombo", new CheckBox("Use E"));
            _user = ComboMenu.Add("usercombo", new CheckBox("Use R"));
            _moveclone = ComboMenu.Add("useclone", new CheckBox("Move clone on combo"));
            _usercc = ComboMenu.Add("usercc", new CheckBox("Dodge targeted cc"));
            _waitforstealth = ComboMenu.Add("waitforstealth", new CheckBox("Block spells in stealth"));
            _useignite = ComboMenu.Add("useignite", new CheckBox("Use ignite"));
            _useitems = ComboMenu.Add("useitems", new CheckBox("Use Items"));
            //Harass Menu
            HarassMenu = ShacoMenu.AddSubMenu("Harass", "harassmenu");
            HarassMenu.AddGroupLabel("Harass Settings");
            HarassMenu.AddSeparator();
            _useeharass = HarassMenu.Add("useeharass", new CheckBox("Use E"));
            //Misc Menu
            MiscMenu = ShacoMenu.AddSubMenu("Misc", "miscmenu");
            MiscMenu.AddGroupLabel("Misc Settings");
            StringList(MiscMenu, "Clonetarget", "Clone Priority",
                new[] { "Target Selector", "Lowest health", "Closest to you" }, 0);
            StringList(MiscMenu, "clonemode", "Clone mode",
                new[] { "Attack priority target", "Follow mouse" }, 0);
            MiscMenu.Add("followmouse", new CheckBox("Follow mouse when there's no target"));
            MiscMenu.Add("cloneorb",
               new KeyBind("Switch clone mode", false, KeyBind.BindTypes.PressToggle, "U".ToCharArray()[0]));
            MiscMenu.AddSeparator();         
            _jukefleewithclone = MiscMenu.Add("jukefleewithclone", new CheckBox("Juke flee with clone"));
            _jukefleepercentage = MiscMenu.Add("jukefleeminpercentage", new Slider("Juke flee health percent", 15));
            MiscMenu.Add("ultwall", new CheckBox("Use ult to jump wall when fleeing"));
            _ksq = MiscMenu.Add("ksq", new CheckBox("KS E"));
            _ks = MiscMenu.Add("ks", new CheckBox("KS Q+E"));
            _stackbox = MiscMenu.Add("stackbox",
                new KeyBind("Stack Boxes", false, KeyBind.BindTypes.HoldActive, "T".ToCharArray()[0]));
            //Drawings Menu
            DrawingsMenu = ShacoMenu.AddSubMenu("Drawings", "drawingsmenu");
            DrawingsMenu.AddGroupLabel("Drawings Settings");
            DrawingsMenu.AddSeparator();
            _drawq = DrawingsMenu.Add("drawq", new CheckBox("Draw Q"));
            _drawqtimer = DrawingsMenu.Add("drawqtimer", new CheckBox("Draw deceive timer"));
            _draww = DrawingsMenu.Add("draww", new CheckBox("Draw W"));
            _drawe = DrawingsMenu.Add("drawe", new CheckBox("Draw E"));
            _drawdamagebar = DrawingsMenu.Add("drawdamagebar", new CheckBox("Draw HP bar damage indicator"));

            //Events
            if (HasSpell("summonersmite"))
            {
                Smite.Smitemethod();
            }
            CloneFleeLogic.FleeLogic();
            DamageIndicator.Initialize();
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Orbwalker.OnPostAttack += OrbwalkingOnAfterAttack;
        }


        private static void Combo(AIHeroClient target)
        {
            if (target == null)
            {
                return;
            }


            var cmbDmg = ComboDamage(target);

            if (ComboConfig.UseItems)
            {
                ItemHandler.UseItems(target, cmbDmg);
            }

            var dist = (float)(Q.Range + Player.MoveSpeed * 2.5);
            if (Clone != null && !CloneDelay && MiscConfig.Moveclone && MiscConfig.JukeFleePercentage != Player.HealthPercent && MiscConfig.JukeFleeWithClone | !MiscConfig.JukeFleeWithClone)
            {
                moveClone();
            }
            if ((ComboConfig.Waitforstealth && ShacoStealth && cmbDmg < target.Health) ||
                !Orbwalker.CanMove)
            {
                return;
            }
            if (ComboConfig.UseQ && Q.IsReady() &&
                Game.CursorPos.Distance(target.Position) < 250 && target.Distance(Player) < dist &&
                (target.Distance(Player) >= ComboConfig.UseQMin ||
                 (cmbDmg > target.Health && Player.CountEnemiesInRange(2000) == 1)))
            {
                if (target.Distance(Player) < Q.Range)
                {
                    var pos = Predict(target, true, 0.5f);
                    Q.Cast(pos);
                }
                else
                {
                    if (!CheckWalls(target) || GetPath(Player, target.Position) < dist)
                    {
                        Q.Cast(
                            Player.Position.ExtendVector3(target.Position, Q.Range));
                    }
                }
            }
            if (ComboConfig.UseW && W.IsReady() &&
                target.Health > cmbDmg && Player.Distance(target) < W.Range)
            {
                HandleW(target);
            }
            if (ComboConfig.UseE && E.IsInRange(target) && E.IsReady())
            {
                E.Cast(target);
            }

            if (ComboConfig.UseR && R.IsReady() && Clone == null && target.HealthPercent < 75 &&
                cmbDmg < target.Health && target.HealthPercent > cmbDmg && target.HealthPercent > 25 && target.IsValidTarget(E.Range))
            {
                R2.Cast();
            }
        }




        private static void Harass()
        {
            Obj_AI_Base target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
            if (target == null)
            {
                return;
            }
            if (HarassConfig.UseEHarass && E.IsInRange(target) && target.IsValidTarget(E.Range))
            {
                E.Cast(target);
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            var buff = Player.GetBuff("Deceive");
            if (buff != null && DrawingConfig.DrawDeceiveTimer)
            {
                StealthTimer.Color = System.Drawing.Color.White;
                StealthTimer.TextValue = Convert.ToString(Convert.ToInt32(buff.EndTime - Game.Time));
                StealthTimer.Position = Player.Position.WorldToScreen();
                StealthTimer.Draw();
            }
            var color = new ColorBGRA(255, 255, 255, 100);
            if (DrawingConfig.DrawQ && Q.IsReady())
            {
                Circle.Draw(color, Q.Range, Player.Position);
            }

            if (DrawingConfig.DrawW && W.IsReady())
            {
                Circle.Draw(color, W.Range, Player.Position);
            }
            if (DrawingConfig.DrawE && E.IsReady())
            {
                Circle.Draw(color, E.Range, Player.Position);
            }

            if (MiscMenu["cloneorb"].Cast<KeyBind>().CurrentValue && ObjectManager.Player.Level >= 6 && R.IsLearned)
            {
                Drawing.DrawText(Drawing.Width * 0.45f, Drawing.Height * 0.78f, System.Drawing.Color.Red, "Clone mode: Attack priority target");
            }
            if (!MiscMenu["cloneorb"].Cast<KeyBind>().CurrentValue && ObjectManager.Player.Level >= 6 && R.IsLearned)
            {
                Drawing.DrawText(Drawing.Width * 0.45f, Drawing.Height * 0.78f, System.Drawing.Color.Red, "Clone mode: Follow Mouse");
            }


        }

        private static void HandleW(Obj_AI_Base target)
        {
            var turret =
                ObjectManager.Get<Obj_AI_Turret>()
                    .OrderByDescending(t => t.Distance(target))
                    .FirstOrDefault(t => t.IsEnemy && t.Distance(target) < 3000 && !t.IsDead);
            if (turret != null)
            {
                CastW(target, target.Position, turret.Position);
            }
            else
            {
                if (target.IsMoving)
                {
                    var pred = W.GetPrediction(target);
                    if (pred.HitChance >= HitChance.Dashing)
                    {
                        CastW(target, target.Position, pred.UnitPosition);
                    }
                }
                else
                {
                    W.Cast(Player.Position.ExtendVector3(target.Position, W.Range - Player.Distance(target)));
                }
            }
        }

        private static void CastW(Obj_AI_Base target, Vector3 from, Vector3 to)
        {
            var positions = new List<Vector3>();

            for (var i = 1; i < 11; i++)
            {
                positions.Add(from.ExtendVector3(to, 42 * i));
            }
            var best =
                positions.OrderByDescending(p => p.Distance(target.Position))
                    .FirstOrDefault(
                        p => !p.IsWall() && p.Distance(Player.Position) < W.Range && p.Distance(target.Position) > 350);
            if (best != null && best.IsValid())
            {
                W.Cast(best);
            }
        }

        public static int TickCount
        {
            get
            {
                return Environment.TickCount & int.MaxValue;
            }
        }
        
        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (MiscMenu["cloneorb"].Cast<KeyBind>().CurrentValue)
            {
                MiscMenu["clonemode"].Cast<Slider>().CurrentValue = 0;
            }

            else if (!MiscMenu["cloneorb"].Cast<KeyBind>().CurrentValue)
            {
                MiscMenu["clonemode"].Cast<Slider>().CurrentValue = 1;
            }
            if (!MiscConfig.JukeFleeWithClone)
            {
                _jukefleepercentage.CurrentValue = 0;
            }
            AIHeroClient target = TargetSelector.GetTarget(
                Q.Range + Player.MoveSpeed * 3, DamageType.Physical);
            if (ShacoStealth && target != null && target.Health > ComboDamage(target) &&
                CombatHelper.IsFacing(target, Player.Position) &&
                Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
            {
                Orbwalker.DisableAttacking = true;
            }
            else
            {
                Orbwalker.DisableAttacking = false;
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo(target);
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                /*Clear(); */
            }

            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass)) Harass();
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                if (Ignite != null && ComboConfig.Useignite)
                {
                    if (Ignite.IsReady() &&
                        Player.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite) >=
                        enemy.TotalShieldHealth() && enemy.Health < 50 + 20 * Player.Level - (enemy.HPRegenRate / 5 * 3))
                    {
                        Ignite.Cast(enemy);
                    }
                }
            }

            if (E.IsReady())
            {
                var ksTarget =
                    EntityManager.Heroes.Enemies.FirstOrDefault(
                        h =>
                            h.IsValidTarget() && !h.Buffs.Any(b => CombatHelper.invulnerable.Contains(b.Name)) &&
                            h.Health < GetDamage(SpellSlot.E, h));
                if (ksTarget != null)
                {
                    if ((MiscConfig.ks || MiscConfig.ksq) &&
                        E.IsInRange(ksTarget) && E.IsReady() &&
                        Player.Mana > Player.Spellbook.GetSpell(SpellSlot.E).SData.Mana)
                    {
                        E.Cast(ksTarget);
                    }
                    if (Q.IsReady() && MiscConfig.ks &&
                        ksTarget.Distance(Player) < Q.Range + E.Range && ksTarget.Distance(Player) > E.Range &&
                        !Player.Position.Extend(ksTarget.Position, Q.Range).IsWall() &&
                        Player.Mana >
                        Player.Spellbook.GetSpell(SpellSlot.Q).SData.Mana +
                        Player.Spellbook.GetSpell(SpellSlot.E).SData.Mana)
                    {
                        Q.Cast(Player.Position.ExtendVector3(ksTarget.Position, Q.Range));
                    }
                }

                if (MiscConfig.Stackbox && W.IsReady())
                {
                    var box =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(m => m.Distance(Player) < W.Range && m.Name == "Jack In The Box" && !m.IsDead)
                            .OrderBy(m => m.Distance(Game.CursorPos))
                            .FirstOrDefault();

                    if (box != null)
                    {
                        W.Cast(box.Position);
                    }
                    else
                    {
                        if (Player.Distance(Game.CursorPos) < W.Range)
                        {
                            W.Cast(Game.CursorPos);
                        }
                        else
                        {
                            W.Cast(Player.Position.ExtendVector3(Game.CursorPos, W.Range));
                        }
                    }
                }
            }
            if (Clone != null && !CloneDelay && MiscConfig.JukeFleePercentage != Player.HealthPercent && MiscConfig.JukeFleeWithClone | !MiscConfig.JukeFleeWithClone)
            {
                moveClone();
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

        private static void moveClone()
        {
            AIHeroClient Gtarget = TargetSelector.GetTarget(CloneRange, DamageType.Physical);
            if (MiscMenu["cloneorb"].Cast<KeyBind>().CurrentValue)
            {
                if (MiscMenu["followmouse"].Cast<CheckBox>().CurrentValue && Clone != null && Gtarget == null &&!Clone.Spellbook.IsAutoAttacking &&
                             (MiscConfig.JukeFleePercentage >= Player.HealthPercent && !MiscConfig.JukeFleeWithClone || MiscConfig.JukeFleePercentage <= Player.HealthPercent && !MiscConfig.JukeFleeWithClone || MiscConfig.JukeFleePercentage < Player.HealthPercent && MiscConfig.JukeFleeWithClone))
                {
                    R.Cast(Game.CursorPos);
                }
                if (Gtarget != null && CanCloneAttack(Clone) &&
                              (MiscConfig.JukeFleePercentage >= Player.HealthPercent && !MiscConfig.JukeFleeWithClone || MiscConfig.JukeFleePercentage <= Player.HealthPercent && !MiscConfig.JukeFleeWithClone || MiscConfig.JukeFleePercentage < Player.HealthPercent && MiscConfig.JukeFleeWithClone))
                {
                switch (MiscMenu["Clonetarget"].Cast<Slider>().CurrentValue)
                {
                    case 0:
                        Gtarget = TargetSelector.GetTarget(2300, DamageType.Physical);
                        break;
                    case 1:
                        Gtarget =
                            ObjectManager.Get<AIHeroClient>()
                                .Where(i => i.IsEnemy && !i.IsDead && Player.Distance(i) <= CloneRange)
                                .OrderBy(i => i.Health).FirstOrDefault();

                        break;
                    case 2:
                        Gtarget =
                            ObjectManager.Get<AIHeroClient>()
                                .Where(i => i.IsEnemy && !i.IsDead && Player.Distance(i) <= CloneRange)
                                .OrderBy(i => Player.Distance(i))
                                .FirstOrDefault();                     
                        break;
                }
                    if (Clone != null && Orbwalker.LastTarget != Gtarget && Orbwalker.LastTarget is Obj_AI_Base && Orbwalker.LastTarget.IsValid && !Clone.Spellbook.IsAutoAttacking && CanCloneAttack(Clone) && (MiscConfig.JukeFleePercentage >= Player.HealthPercent && !MiscConfig.JukeFleeWithClone || MiscConfig.JukeFleePercentage <= Player.HealthPercent && !MiscConfig.JukeFleeWithClone || MiscConfig.JukeFleePercentage < Player.HealthPercent && MiscConfig.JukeFleeWithClone))
                    {
                        Obj_AI_Base target = (Obj_AI_Base) Orbwalker.LastTarget;
                        R.Cast(target);
                        CloneDelay = true;
                        Core.DelayAction(() => CloneDelay = false, 200);
                    }
                if (Clone != null && Gtarget != null && Gtarget.IsValid && !Clone.Spellbook.IsAutoAttacking && CanCloneAttack(Clone) && (MiscConfig.JukeFleePercentage >= Player.HealthPercent && !MiscConfig.JukeFleeWithClone || MiscConfig.JukeFleePercentage <= Player.HealthPercent && !MiscConfig.JukeFleeWithClone || MiscConfig.JukeFleePercentage < Player.HealthPercent && MiscConfig.JukeFleeWithClone))
                {
                    if (CanCloneAttack(Clone))
                    {                      
                     R.Cast(Gtarget);
                                                            
                    }
                    else if (Player.HealthPercent > 25)
                    {
                        var prediction = Prediction.Position.PredictUnitPosition(Gtarget, 2);
                        R.Cast(
                            Gtarget.Position.ExtendVector3(prediction.To3DPlayer(), Gtarget.GetAutoAttackRange()));
                    }
                    CloneDelay = true;
                    Core.DelayAction(() => CloneDelay = false, 200);
                }
            }
           }
            if (!MiscMenu["cloneorb"].Cast<KeyBind>().CurrentValue)
            {
                if (Clone != null && !Clone.Spellbook.IsAutoAttacking && (MiscConfig.JukeFleePercentage >= Player.HealthPercent && !MiscConfig.JukeFleeWithClone || MiscConfig.JukeFleePercentage <= Player.HealthPercent && !MiscConfig.JukeFleeWithClone || MiscConfig.JukeFleePercentage < Player.HealthPercent && MiscConfig.JukeFleeWithClone))
                {
                    if (Gtarget != null && Clone.IsInRange(Gtarget, ObjectManager.Player.GetAutoAttackRange()) && CanCloneAttack(Clone))
                    { }
                    else
                    {
                        R.Cast(Game.CursorPos);
                    }
                }
            }
        }

        public static bool CanCloneAttack(Obj_AI_Minion clone)
        {
            if (clone != null)
            {
                return GameTimeTickCount >=
                       LastAATick + Game.Ping + 100 + (clone.AttackDelay - clone.AttackCastDelay) * 1000;
            }
            return false;
        }

        private static bool CheckWalls(Obj_AI_Base target)
        {
            var step = Player.Distance(target) / 15;
            for (var i = 1; i < 16; i++)
            {
                if (Player.Position.Extend(target.Position, step * i).IsWall())
                {
                    return true;
                }
            }
            return false;
        }

        public static Vector3 Predict(Obj_AI_Base target, bool serverPos, float distance)
        {
            var enemyPos = serverPos ? target.ServerPosition : target.Position;
            var myPos = serverPos ? Player.ServerPosition : Player.Position;

            return enemyPos + Vector3.Normalize(enemyPos - myPos) * distance;
        }

        public static float GetDamage(SpellSlot spell, Obj_AI_Base target)
        {
            var ap = Player.FlatMagicDamageMod + Player.BaseAbilityDamage;
            if (spell == SpellSlot.Q)
            {
                if (!Q.IsReady())
                    return 0;
                return Player.CalculateDamageOnUnit(target, DamageType.Magical, 75f + 40f * (Q.Level - 1) + 45 / 100 * ap);
            }
            if (spell == SpellSlot.W)
            {
                if (!W.IsReady())
                    return 0;
                return Player.CalculateDamageOnUnit(target, DamageType.Magical, 90f + 45f * (W.Level - 1) + 90 / 100 * ap);
            }
            if (spell == SpellSlot.E)
            {
                if (!E.IsReady())
                    return 0;
                return Player.CalculateDamageOnUnit(target, DamageType.Magical, 55f + 25f * (E.Level - 1) + 55 / 100 * ap);
            }
            if (spell == SpellSlot.R)
            {
                if (!R.IsReady())
                    return 0;
                return Player.CalculateDamageOnUnit(target, DamageType.Magical, 150f + 100f * (R.Level - 1) + 50 / 100 * ap);
            }

            return 0;
        }

        private static float ComboDamage(Obj_AI_Base hero)
        {
            double damage = 0;

            if (Q.IsReady())
            {
                damage += Player.GetSpellDamage(hero, SpellSlot.Q);
            }
            if (E.IsReady())
            {
                damage += Player.GetSpellDamage(hero, SpellSlot.E);
            }

            damage += ItemHandler.GetItemsDamage(hero);

            var ignitedmg = Player.GetSummonerSpellDamage(hero, DamageLibrary.SummonerSpells.Ignite);
            if (Player.Spellbook.CanUseSpell(Player.GetSpellSlotFromName("summonerdot")) == SpellState.Ready &&
                hero.Health < damage + ignitedmg)
            {
                damage += ignitedmg;
            }

            return (float)damage;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base hero, GameObjectProcessSpellCastEventArgs args)
        {
            if (Clone != null)
            {
                var clone = ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(m => m.Name == Player.Name && !m.IsMe);

                if (args == null || clone == null)
                {
                    return;
                }
                if (hero.NetworkId != clone.NetworkId)
                {
                    return;
                }
                LastAATick = GameTimeTickCount;
            }

            if (args == null || hero == null)
            {
                return;
            }
            if (ComboConfig.UseRcc && hero is AIHeroClient && hero.IsEnemy &&
                Player.Distance(hero) < Q.Range &&
                CombatHelper.isDangerousSpell(
                    args.SData.Name, args.Target as AIHeroClient, hero, args.End, float.MaxValue))
            {
                R2.Cast();
            }
        }

        public static bool HasSpell(string s)
        {
            return EloBuddy.Player.Spells.FirstOrDefault(o => o.SData.Name.Contains(s)) != null;
        }

        private static void OrbwalkingOnAfterAttack(AttackableUnit target, EventArgs args)
        {

            if (target is AIHeroClient)
            {
                var t = target as AIHeroClient;
              if (!t.IsValidTarget())
            {
                return;
            }

            if (Hydra.IsReady() && Hydra.IsOwned() && Player.Distance(target) < Hydra.Range)
            {
                Hydra.Cast();
            }
            else if (Tiamat.IsReady() && Tiamat.IsOwned() && Player.Distance(target) < Tiamat.Range)
            {
                Tiamat.Cast();
            }
            }
        }


        public static float GetPath(AIHeroClient hero, Vector3 b)
        {
            var path = hero.GetPath(b);
            var lastPoint = path[0];
            var distance = 0f;
            foreach (var point in path.Where(point => !point.Equals(lastPoint)))
            {
                distance += lastPoint.Distance(point);
                lastPoint = point;
            }
            return distance;
        }

        public static class DrawingConfig
        {
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

            public static bool DrawDamageBar
            {
                get { return _drawdamagebar.CurrentValue; }
            }

            public static bool DrawDeceiveTimer
            {
                get { return _drawqtimer.CurrentValue; }
            }
        }

        public static class MiscConfig
        {
            public static bool Stackbox
            {
                get { return _stackbox.CurrentValue; }
            }

            public static bool clonemode
            {
                get { return MiscMenu["clonemode"].Cast<KeyBind>().CurrentValue; }
            }

            public static bool JukeFleeWithClone
            {
                get { return _jukefleewithclone.CurrentValue; }
            }

            public static int JukeFleePercentage
            {
                get { return _jukefleepercentage.CurrentValue; }
            }

            public static bool Moveclone
            {
                get { return _moveclone.CurrentValue; }
            }

            public static bool ksq
            {
                get { return _ksq.CurrentValue; }
            }

            public static bool ks
            {
                get { return _ks.CurrentValue; }
            }
        }

        public static class HarassConfig
        {
            public static bool UseEHarass
            {
                get { return _useeharass.CurrentValue; }
            }
        }

        public static class ComboConfig
        {
            public static bool UseQ
            {
                get { return _useq.CurrentValue; }
            }

            public static int UseQMin
            {
                get { return _useqmin.CurrentValue; }
            }

            public static bool UseW
            {
                get { return _usew.CurrentValue; }
            }

            public static bool UseE
            {
                get { return _useecombo.CurrentValue; }
            }

            public static bool UseR
            {
                get { return _user.CurrentValue; }
            }

            public static bool Waitforstealth
            {
                get { return _waitforstealth.CurrentValue; }
            }

            public static bool Useignite
            {
                get { return _useignite.CurrentValue; }
            }

            public static bool UseRcc
            {
                get { return _usercc.CurrentValue; }
            }

            public static bool UseItems
            {
                get { return _useitems.CurrentValue; }
            }
        }
    }
}