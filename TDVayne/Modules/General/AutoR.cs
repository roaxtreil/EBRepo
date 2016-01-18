using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using TDVayne.Skills.Tumble;
using TDVayne.Utility;

namespace TDVayne.Modules.General
{
    internal class AutoR : ISOLOModule
    {
        private TumbleLogicProvider Provider;

        /// <summary>
        ///     Called when the module is loaded.
        /// </summary>
        public void OnLoad()
        {
            Provider = new TumbleLogicProvider();
            Obj_AI_Base.OnSpellCast += OnDoCast;
        }

        /// <summary>
        ///     Shoulds the module get executed.
        /// </summary>
        /// <returns></returns>
        public bool ShouldGetExecuted()
        {
            return
                ObjectManager.Player.GetEnemiesInRange(2300f).Count(en => en.IsValidTarget() && !(en.HealthPercent < 5)) >=
                2 && MenuGenerator.MiscMenu["usercombo"].Cast<CheckBox>().CurrentValue;
        }

        /// <summary>
        ///     Gets the type of the module.
        /// </summary>
        /// <returns></returns>
        public ModuleType GetModuleType()
        {
            return ModuleType.Other;
        }

        /// <summary>
        ///     Called when the module gets executed.
        /// </summary>
        public void OnExecute()
        {
        }

        /// <summary>
        ///     Called when the sender is done doing the windup time for a spell/AA
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="GameObjectProcessSpellCastEventArgs" /> instance containing the event data.</param>
        private void OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {
                if (sender.IsMe && args.Slot == SpellSlot.R)
                {
                    var QPosition = Provider.GetTDVayneQPosition();
                    if (QPosition != Vector3.Zero)
                    {
                        Player.CastSpell(SpellSlot.Q, QPosition);
                        return;
                    }

                    var secondaryQPosition = ObjectManager.Player.ServerPosition.Extend(Game.CursorPos, 300f).To3D();

                    if (!secondaryQPosition.IsUnderTurret())
                    {
                        Player.CastSpell(SpellSlot.Q, secondaryQPosition);
                    }
                }
            }
            catch (Exception e)
            {
            }
        }
    }
}