using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace Kappalista
{
    public class KappalistaMenu
    {
        public static Menu MenuPrincipal, SpellsMenu, LaneClear, JungleClear, Misc, Drawings, ActivatorSettings;

        public static int GetSliderValue(Menu menu, string uniqueid)
        {
            return menu[uniqueid].Cast<Slider>().CurrentValue;
        }

        public static bool GetBoolValue(Menu menu, string uniqueid)
        {
            return menu[uniqueid].Cast<CheckBox>().CurrentValue;
        }

        public static void InitMenu()
        {
            MenuPrincipal = MainMenu.AddMenu("Kappalista", "kappalista");
            //Spells Settings
            SpellsMenu = MenuPrincipal.AddSubMenu("Spells Settings");
            SpellsMenu.AddLabel("Combo Settings");
            SpellsMenu.Add("use.q.combo", new CheckBox("Use Q"));
            SpellsMenu.Add("use.e", new CheckBox("Use E"));
            SpellsMenu.Add("atk.minion", new CheckBox("Attack minions for chasing"));
            SpellsMenu.AddLabel("Harass Settings");
            SpellsMenu.Add("use.q.harass", new CheckBox("Use Q"));
            SpellsMenu.Add("manapercent", new Slider("Mana percent", 60));
            //LaneClear Settings
            LaneClear = MenuPrincipal.AddSubMenu("LaneClear Settings");
            LaneClear.Add("use.q", new CheckBox("Use Q", false));
            LaneClear.Add("q.min.kill", new Slider("Min minions killable", 3, 1, 5));
            LaneClear.Add("use.e", new CheckBox("Use E"));
            LaneClear.Add("e.minkill", new Slider("Min minions killable", 2, 1, 5));
            LaneClear.Add("manapercent", new Slider("Mana percent", 60));
            //JungleClear Settings
            JungleClear = MenuPrincipal.AddSubMenu("Jungle Clear Settings");
            JungleClear.Add("use.q", new CheckBox("Use Q"));
            JungleClear.Add("use.e", new CheckBox("Use E"));
            JungleClear.Add("manapercent", new Slider("Mana percent", 60));
            //Misc Settings
            Misc = MenuPrincipal.AddSubMenu("Misc Settings");
            Misc.Add("e.killsteal", new CheckBox("E Killsteal"));
            Misc.Add("e.mobsteal", new CheckBox("E Mob Steal"));
            Misc.Add("e.lasthit.assist", new CheckBox("E Lasthit Assist"));
            Misc.Add("r.savebuddy", new CheckBox("Save your sup with R"));
            Misc.Add("r.balista", new CheckBox("R Balista"));
            Misc.Add("e.siegeandsuper", new CheckBox("Auto E Siege and Super minions"));
            Misc.Add("e.harass", new CheckBox("E Harass"));
            Misc.Add("e.dontharasscombo", new CheckBox("Don't harass with E in combo"));
            Misc.Add("e.beforedie", new CheckBox("E before die"));
            Misc.Add("w.dragonorbaron", new CheckBox("Auto W on Dragon or Baron", false));
            Misc.Add("w.castdragon",
                new KeyBind("Cast W on Dragon", false, KeyBind.BindTypes.HoldActive, "J".ToCharArray()[0]));
            Misc.Add("w.castbaron",
                new KeyBind("Cast W on Baron", false, KeyBind.BindTypes.HoldActive, "K".ToCharArray()[0]));
            //Activator Settings
            ActivatorSettings = MenuPrincipal.AddSubMenu("Activator Settings");
            ActivatorSettings.Add("use.botrk.cutlass", new CheckBox("Use BotRK/Cutlass"));
            ActivatorSettings.Add("botrk.cutlass.health", new Slider("Health percent", 60));
            ActivatorSettings.Add("use.youmuu", new CheckBox("Use Youmuu"));
            //Drawing Settings
            Drawings = MenuPrincipal.AddSubMenu("Drawings Settings");
            Drawings.Add("draw.q", new CheckBox("Draw Q Range"));
            Drawings.Add("draw.w", new CheckBox("Draw W Range"));
            Drawings.Add("draw.e", new CheckBox("Draw E Range"));
            Drawings.Add("draw.r", new CheckBox("Draw R Range"));
            Drawings.Add("draw.e.dmgpercent", new CheckBox("Draw E Damage Percent"));
            Drawings.Add("draw.damageindicator", new CheckBox("Draw Damage Indicator"));
        }
    }
}