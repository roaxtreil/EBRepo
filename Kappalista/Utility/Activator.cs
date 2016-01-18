using System;
using EloBuddy;
using EloBuddy.SDK;

namespace Kappalista
{
    internal class Activator
    {
        public static Item Cutlass = new Item((int) ItemId.Bilgewater_Cutlass, 550);
        public static Item Botrk = new Item((int) ItemId.Blade_of_the_Ruined_King, 550);
        public static Item Youmuu = new Item((int) ItemId.Youmuus_Ghostblade);

        public static void Initialize()
        {
            Game.OnUpdate += RunActivator;
        }

        private static void BotRKandCutlass(AIHeroClient target)
        {
            if (Botrk.IsOwned() && Botrk.IsReady())
            {
                Botrk.Cast(target);
            }
            if (Cutlass.IsOwned() && Cutlass.IsReady())
            {
                Cutlass.Cast(target);
            }
        }

        private static void RunActivator(EventArgs args)
        {
            if (KappalistaMenu.GetBoolValue(KappalistaMenu.ActivatorSettings, "use.botrk.cutlass") &&
                Player.Instance.HealthPercent <=
                KappalistaMenu.GetSliderValue(KappalistaMenu.ActivatorSettings, "botrk.cutlass.health"))
            {
                if (Orbwalker.LastTarget != null && Orbwalker.LastTarget is AIHeroClient &&
                    Orbwalker.LastTarget.IsValidTarget(Player.Instance.GetAutoAttackRange()))
                    BotRKandCutlass(Orbwalker.LastTarget as AIHeroClient);
            }

            if (KappalistaMenu.GetBoolValue(KappalistaMenu.ActivatorSettings, "use.youmuu") && Youmuu.IsOwned() &&
                Youmuu.IsReady() && Orbwalker.LastTarget != null && Orbwalker.LastTarget is AIHeroClient)
            {
                if (Orbwalker.LastTarget.IsValidTarget(Player.Instance.GetAutoAttackRange(Orbwalker.LastTarget) + 100))
                {
                    Youmuu.Cast();
                }
            }
        }
    }
}