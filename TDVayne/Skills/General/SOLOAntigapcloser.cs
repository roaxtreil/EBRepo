using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu.Values;
using TDVayne.Utility;
using TDVayne.Utility.General;
using Gapcloser = EloBuddy.SDK.Events.Gapcloser;

namespace TDVayne.Skills.General
{
    internal class SOLOAntigapcloser
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SOLOAntigapcloser" /> class.
        /// </summary>
        public SOLOAntigapcloser()
        {
            Gapcloser.OnGapcloser += OnEnemyGapcloser;
            Interrupter.OnInterruptableSpell += OnInterruptable;
        }

        /// <summary>
        ///     Called when an interruptable skill is casted.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="Interrupter2.InterruptableTargetEventArgs" /> instance containing the event data.</param>
        private void OnInterruptable(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            var interrupterEnabled =
                MenuGenerator.MiscMenu["TDVaynemiscmiscellaneousinterrupter"].Cast<CheckBox>().CurrentValue;

            if (!interrupterEnabled
                || !Variables.E.IsReady()
                || !sender.IsValidTarget())
            {
                return;
            }

            if (args.DangerLevel == DangerLevel.High)
            {
                Core.DelayAction(() => { Variables.E.Cast(sender); },
                    MenuGenerator.MiscMenu["TDVaynemiscmiscellaneousdelay"].Cast<Slider>().CurrentValue);
            }
        }

        /// <summary>
        ///     Called when an enemy gapcloser is casted on the player.
        /// </summary>
        /// <param name="gapcloser">The gapcloser.</param>
        private void OnEnemyGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs gapcloser)
        {
            var antigapcloserEnabled =
                MenuGenerator.MiscMenu["TDVaynemiscmiscellaneousantigapcloser"].Cast<CheckBox>().CurrentValue;
            var endPosition = gapcloser.End;

            if (!antigapcloserEnabled || !Variables.E.IsReady() || !gapcloser.Sender.IsValidTarget() ||
                ObjectManager.Player.Distance(endPosition) > 400)
            {
                return;
            }

            //Smart
            var ShouldBeRepelled = CustomAntiGapcloser.SpellShouldBeRepelledOnSmartMode(gapcloser.SpellName);

            if (ShouldBeRepelled)
            {
                Core.DelayAction(() => { Variables.E.Cast(gapcloser.Sender); },
                    MenuGenerator.MiscMenu["TDVaynemiscmiscellaneousdelay"].Cast<Slider>().CurrentValue);
            }
            else
            {
                //Use Q
                var extendedPosition = ObjectManager.Player.ServerPosition.Extend(endPosition, -300f);
                if (!extendedPosition.IsUnderTurret() &&
                    !(extendedPosition.CountEnemiesInRange(400f) >= 2 && extendedPosition.CountAlliesInRange(400f) < 3))
                {
                    Core.DelayAction(() => { Player.CastSpell(SpellSlot.Q, extendedPosition.To3D()); },
                        MenuGenerator.MiscMenu["TDVaynemiscmiscellaneousdelay"].Cast<Slider>().CurrentValue);
                }
            }
        }
    }
}