using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
namespace TD_Draven
{
    public static class AxesManager
    {
        public static List<Axe> Axes = new List<Axe>();
        public static List<MissileClient> AaMissiles = new List<MissileClient>();
        public static int AxesCount
        {
            get
            {
                if (ObjectManager.Player.HasBuff("dravenspinningattack"))
                {
                    return ObjectManager.Player.GetBuff("dravenspinningattack").Count + Axes.Count;
                }
                return Axes.Count;
            }
        }
        public static int CatchMode { get { return Program.AxeSettingsMenu["CatchMode"].Cast<Slider>().CurrentValue; } }
        public static bool CanCatch { get { return ((CatchMode == 0 && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None)) || CatchMode == 1); } }
        public static float CatchDelay { get { return Program.AxeSettingsMenu["Delay"].Cast<Slider>().CurrentValue / 100.0f; } }
        public static float CatchRadius
        {
            get
            {
                return Program.AxeSettingsMenu["axeCatchRange"].Cast<Slider>().CurrentValue;
            }
        }
        public static int OrbwalkMode { get { return Program.AxeSettingsMenu["OrbwalkMode"].Cast<Slider>().CurrentValue; } }
        public static Vector3 CatchSource { get { return OrbwalkMode == 1 ? Game.CursorPos : ObjectManager.Player.Position; } }

        public static void Init(EventArgs args)
        {
            Game.OnTick += Game_OnUpdate;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Game.OnWndProc += Game_OnWndProc;
        }


        private static void AddReticleToAxe(GameObject obj)
        {
            var a = Axes.Where(m => m.MissileIsValid && m.Reticle == null).OrderBy(m => obj.Distance(m.Missile.EndPosition, true)).FirstOrDefault();
            if (a != null)
            {
                a.AddReticle(obj);
            }
        }
        public static Axe FirstAxe
        {
            get
            {
                return Axes.Where(m => m.InTime && !m.InTurret).OrderBy(m => m.TimeLeft).FirstOrDefault();
            }
        }
        public static Axe FirstAxeInRadius
        {
            get
            {
                return Axes.Where(m => m.InTime && !m.InTurret && m.SourceInRadius).OrderBy(m => m.TimeLeft).FirstOrDefault();
            }
        }
        public static bool IsFirst(this Axe a)
        {
            return FirstAxe == a;
        }
        public static Axe AxeAfter(this Axe a)
        {
            return Axes.Where(m => m.InTime && !m.InTurret && m.TimeLeft > a.TimeLeft).OrderBy(m => m.TimeLeft).FirstOrDefault();
        }
        public static Axe AxeBefore(this Axe a)
        {
            return Axes.Where(m => m.InTime && !m.InTurret && m.TimeLeft < a.TimeLeft).OrderBy(m => m.TimeLeft).LastOrDefault();
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            Axes.RemoveAll(a => a.Reticle != null && (!a.Reticle.IsValid || !a.InTime));
            var canMove = true;
            var canAttack = true;
            if (CanCatch)
            {
                foreach (var a in Axes)
                {
                    if (!a.CanOrbwalkWithUserDelay)
                    {
                        canMove = false;
                    }
                    if (!a.CanAttack)
                    {
                        canAttack = false;
                    }
                }
            }
            Orbwalker.DisableAttacking = !canAttack;
            Orbwalker.DisableMovement = !canMove;
            if (!canMove)
            {
                var bestAxe = FirstAxeInRadius;
                if (bestAxe != null && !bestAxe.HeroInReticle)
                {
                    if (Orbwalker.CanMove)
                    {
                        if (!MoveWillChangeReticleEndPosition)
                        {
                            var t = TargetSelector.GetTarget(800, DamageType.Physical);
                            if (Program.MenuConfig.axeKill && t.IsValidTarget() &&
                                ObjectManager.Player.Distance(t.Position) > 400 &&
                                ObjectManager.Player.GetAutoAttackDamage(t)*2 > t.Health)
                            {
                                Orbwalker.DisableMovement = false;
                                Orbwalker.MoveTo(bestAxe.EndPosition);
                                Orbwalker.DisableMovement = true;
                                if (!bestAxe.MoveSent)
                                {
                                    Player.IssueOrder(GameObjectOrder.MoveTo, bestAxe.EndPosition);
                                    bestAxe.MoveSent = true;
                                }
                            }
                            if (Axes.Any())
                            {
                                if (ObjectManager.Player.Distance(Axes.First().EndPosition) < 100)
                                {
                                    Orbwalker.DisableMovement = false;
                                    Orbwalker.MoveTo(bestAxe.EndPosition);
                                    Orbwalker.DisableMovement = true;
                                    if (!bestAxe.MoveSent)
                                    {
                                        Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                                        bestAxe.MoveSent = true;
                                    }

                                }

                                if (Program.MenuConfig.axeTower && Program.Combo && Axes.First().EndPosition.UnderTurret(true))
                                {
                                    Orbwalker.DisableMovement = false;
                                    Orbwalker.MoveTo(bestAxe.EndPosition);
                                    Orbwalker.DisableMovement = true;
                                    if (!bestAxe.MoveSent)
                                    {
                                        Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                                        bestAxe.MoveSent = true;
                                    }
                                }

                                if (Program.MenuConfig.axeTower2 && Program.Farm && Axes.First().EndPosition.UnderTurret(true))
                                {
                                    Orbwalker.DisableMovement = false;
                                    Orbwalker.MoveTo(bestAxe.EndPosition);
                                    Orbwalker.DisableMovement = true;
                                    if (!bestAxe.MoveSent)
                                    {
                                        Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                                        bestAxe.MoveSent = true;
                                    }
                                }

                                if (Program.MenuConfig.axeEnemy && Axes.First().EndPosition.CountEnemiesInRange(500) > 2)
                                {
                                    Orbwalker.DisableMovement = false;
                                    Orbwalker.MoveTo(bestAxe.EndPosition);
                                    Orbwalker.DisableMovement = true;
                                    if (!bestAxe.MoveSent)
                                    {
                                        Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                                        bestAxe.MoveSent = true;
                                    }
                                }

                                if (Game.CursorPos.Distance(Axes.First().EndPosition) < Program.MenuConfig.axeCatchRange)
                                {
                                    Orbwalker.DisableMovement = false;
                                    Orbwalker.MoveTo(bestAxe.EndPosition);
                                    Orbwalker.DisableMovement = true;
                                    if (!bestAxe.MoveSent)
                                    {
                                        Player.IssueOrder(GameObjectOrder.MoveTo, Axes.First().EndPosition);
                                        bestAxe.MoveSent = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        

        public static
            bool MoveWillChangeReticleEndPosition
        {
            get
            {
                return AaMissiles.Any() && AaMissiles.Any(missile => missile.Target != null && missile.IsValid && missile.Position.Distance(missile.Target) / missile.StartPosition.Distance(missile.Target) <= 0.2f);
            }
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != (uint)WindowMessages.LeftButtonDoubleClick) return;
            if (FirstAxeInRadius != null)
            {
                Axes.Remove(FirstAxeInRadius);
            }
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
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
            else if (sender is Obj_GeneralParticleEmitter)
            {
                var name = sender.Name.ToLower();
                if (name.Contains(ObjectManager.Player.ChampionName.ToLower()) && name.Contains("reticle"))
                {
                    if (name.Contains("q_reticle_self.troy"))
                    {
                        AddReticleToAxe(sender);
                    }
                }
            }
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
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
                if (name.Contains(ObjectManager.Player.ChampionName.ToLower()) && name.Contains("reticle"))
                {
                    if (name.Contains("q_reticle_self.troy"))
                    {
                        Axes.RemoveAll(m => m.Reticle != null && m.Reticle.NetworkId == sender.NetworkId);
                    }
                }
            }
        }

    }
}