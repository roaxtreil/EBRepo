using System;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Constants;
using EloBuddy.SDK.Events;
using TDVayne.Modules;
using TDVayne.Skills.Tumble.WardTracker;
using TDVayne.Utility;
using TDVayne.Utility.Enums;
using TDVayne.Utility.General;

namespace TDVayne
{
    internal class TDVayne
    {
        /**
         * TODO List
         * Safe enemies around check for Q into Wall
         * Don't aa while stealthed should be on I guess with 3 enemies, but if you have an ally near it shouldn't aa with 2. Maybe it should just always stealth.
         * Add Condemn To Trundle / J4 / Anivia Walls
         * Q Away if targetted from turret and no killable low health enemy is near.
         */
        private float lastTick;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TDVayne" /> class.
        /// </summary>
        public TDVayne()
        {
            Game.OnTick += OnUpdate;
            Drawing.OnDraw += OnDraw;

            Obj_AI_Base.OnSpellCast += OnDoCast;
            Obj_AI_Base.OnProcessSpellCast += YasuoWall.OnProcessSpellCast;

            Obj_AI_Base.OnProcessSpellCast += WardDetector.OnProcessSpellCast;
            GameObject.OnCreate += WardDetector.OnCreate;
            GameObject.OnDelete += WardDetector.OnDelete;

            foreach (var module in Variables.ModuleList)
            {
                module.OnLoad();
            }
        }

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoad;
        }

        /// <summary>
        ///     Raises the <see cref="E:Load" /> event.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private static void OnLoad(EventArgs args)
        {
            Variables.Instance = new SOLOBootstrap();
        }

        /// <summary>
        ///     Raises the <see cref="E:Draw" /> event.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void OnDraw(EventArgs args)
        {
        }

        /// <summary>
        ///     Called when an unit has executed the windup time for a skill.
        /// </summary>
        /// <param name="sender">The unit.</param>
        /// <param name="args">The <see cref="GameObjectProcessSpellCastEventArgs" /> instance containing the event data.</param>
        private void OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && AutoAttacks.IsAutoAttack(args.SData.Name)
                && args.Target is Obj_AI_Base )
            {
                foreach (var skill in Variables.skills)
                {
                    if (skill.GetSkillMode() == SkillMode.OnAfterAA)
                    {
                        if ((Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) ||
                             Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass)) && args.Target != null)
                        {
                            skill.Execute(args.Target as Obj_AI_Base);
                        }
                        if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) && args.Target != null)
                        {
                            skill.ExecuteFarm(args.Target as Obj_AI_Base);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Raises the <see cref="E:Update" /> event.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void OnUpdate(EventArgs args)
        {
            try
            {
                if (Environment.TickCount - lastTick < 80)
                {
                    return;
                }
                lastTick = Environment.TickCount;

                WardDetector.OnTick();

                if (ObjectManager.Player.IsDead)
                {
                    return;
                }

                foreach (var skill in Variables.skills)
                {
                    if (skill.GetSkillMode() == SkillMode.OnUpdate)
                    {
                        skill.Execute(Orbwalker.GetTarget() is Obj_AI_Base
                            ? Orbwalker.GetTarget() as Obj_AI_Base
                            : null);
                    }
                }

                foreach (var module in Variables.ModuleList)
                {
                    if (module.ShouldGetExecuted() && module.GetModuleType() == ModuleType.OnUpdate)
                    {
                        module.OnExecute();
                    }
                }
            }
            catch (Exception e)
            {
            }
        }
    }
}