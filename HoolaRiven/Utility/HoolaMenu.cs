using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
namespace HoolaRiven.Utility
{
    internal class HoolaMenu
    {
        public static Menu MenuPrincipal, ComboMenu, LaneClear, MiscMenu;

        public static bool BoolValue(Menu menu, string menuvalue)
        {
            return menu[menuvalue].Cast<CheckBox>().CurrentValue;
        }

        public static bool KeybindValue(Menu menu, string menuvalue)
        {
            return menu[menuvalue].Cast<KeyBind>().CurrentValue;
        }

        public static int SliderValue(Menu menu, string menuvalue)
        {
            return menu[menuvalue].Cast<Slider>().CurrentValue;
        }

        public static void InitializeMenu()
        {
            MenuPrincipal = MainMenu.AddMenu("Hoola Riven", "hoola");
            ComboMenu = MenuPrincipal.AddSubMenu("Combo Settings");
            ComboMenu.Add("Burst",
                new KeyBind("Burst", false, KeyBind.BindTypes.HoldActive, "T".ToCharArray()[0]));
            ComboMenu.Add("FastHarass",
                new KeyBind("Fast Harass", false, KeyBind.BindTypes.HoldActive, "Y".ToCharArray()[0]));
            ComboMenu.Add("AlwaysR",
                new KeyBind("Always Use R (Toggle)", false, KeyBind.BindTypes.PressToggle, "H".ToCharArray()[0]));
            ComboMenu.Add("UseHoola",
                new KeyBind("Use Hoola Combo Logic (Toggle)", true, KeyBind.BindTypes.PressToggle, "L".ToCharArray()[0]));
            ComboMenu.Add("ComboW", new CheckBox("Always use W"));
            ComboMenu.Add("RKillable", new CheckBox("Use R when the target is killable"));

            LaneClear = MenuPrincipal.AddSubMenu("Clear Settings");
            LaneClear.Add("LaneQ", new CheckBox("Use Q"));
            LaneClear.Add("LaneW", new Slider("Use W in X minions", 5, 0, 5));
            LaneClear.Add("LaneE", new CheckBox("Use E"));

            MiscMenu = MenuPrincipal.AddSubMenu("Misc Settings");
            MiscMenu.Add("youmuu", new CheckBox("Use Youmuus when E"));
            MiscMenu.Add("FirstHydra", new CheckBox("Flash burst Hydra before W"));
            MiscMenu.Add("Qstrange", new CheckBox("Strange Q"));
            MiscMenu.Add("Winterrupt", new CheckBox("W to interrupt spells"));
            MiscMenu.Add("AutoW", new Slider("Auto W when x enemies", 5, 0, 5));
            MiscMenu.Add("RMaxDam", new CheckBox("Use second R for max damage"));
            MiscMenu.Add("killstealw", new CheckBox("KillSteal with W"));
            MiscMenu.Add("killstealr", new CheckBox("KillSteal with second R"));
            MiscMenu.Add("AutoShield", new CheckBox("Auto E Shield"));
            MiscMenu.Add("Shield", new CheckBox("Auto E in lasthit mode"));
            MiscMenu.Add("KeepQ", new CheckBox("Keep Q alive"));
            MiscMenu.Add("QD", new Slider("First and second Q Delay", 29, 23, 43));
            MiscMenu.Add("QLD", new Slider("Third Q Delay", 39, 36, 53));
        }
    }
}