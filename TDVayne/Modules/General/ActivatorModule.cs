using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using TDVayne.Utility;

namespace TDVayne.Modules.General
{
    internal class ActivatorModule : ISOLOModule
    {
        /// <summary>
        ///     Called when the module is loaded
        /// </summary>
        public void OnLoad()
        {
        }

        /// <summary>
        ///     Shoulds the module get executed.
        /// </summary>
        /// <returns></returns>
        public bool ShouldGetExecuted()
        {
            return (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) ||
                    ObjectManager.Player.HealthPercent < 10);
        }

        /// <summary>
        ///     Gets the type of the module.
        /// </summary>
        /// <returns></returns>
        public ModuleType GetModuleType()
        {
            return ModuleType.OnUpdate;
        }

        /// <summary>
        ///     Called when the module is executed.
        /// </summary>
        public void OnExecute()
        {
            var target = Orbwalker.GetTarget();

            if (target is AIHeroClient && target.IsValidTarget(ObjectManager.Player.GetAutoAttackRange(target) + 125f))
            {
                if (target.IsValidTarget(450f))
                {
                    var targetHealth = target.HealthPercent;
                    var myHealth = ObjectManager.Player.HealthPercent;

                    if (myHealth < MenuGenerator.MiscMenu["botrkcutlasshpercent"].Cast<Slider>().CurrentValue)
                    {
                        var spellSlot =
                            Player.Instance.InventoryItems.FirstOrDefault(a => a.Id == ItemId.Blade_of_the_Ruined_King);
                        if (spellSlot != null || Player.GetSpell(spellSlot.SpellSlot).IsReady)
                            Player.CastSpell(spellSlot.SpellSlot, (AIHeroClient) target);
                    }

                    if (targetHealth < MenuGenerator.MiscMenu["botrkcutlasshpercent"].Cast<Slider>().CurrentValue)
                    {
                        var spellSlot =
                            Player.Instance.InventoryItems.FirstOrDefault(a => a.Id == ItemId.Bilgewater_Cutlass);
                        if (spellSlot != null || Player.GetSpell(spellSlot.SpellSlot).IsReady)
                            Player.CastSpell(spellSlot.SpellSlot, (AIHeroClient) target);
                    }
                }
                var spellSlott = Player.Instance.InventoryItems.FirstOrDefault(a => a.Id == ItemId.Youmuus_Ghostblade);
                if (spellSlott != null || Player.GetSpell(spellSlott.SpellSlot).IsReady)
                    Player.CastSpell(spellSlott.SpellSlot);
            }
        }
    }

    internal enum ItemType
    {
        OnAfterAA,
        OnUpdate
    }
}