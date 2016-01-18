using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace TDVayne.Utility
{
    internal class MenuGenerator
    {
        public static Menu ComboMenu, HarassMenu, FarmMenu, QMenu, EMenu, MiscMenu;

        /// <summary>
        ///     The main menu
        /// </summary>
        /// <summary>
        ///     Initializes a new instance of the <see cref="MenuGenerator" /> class.
        /// </summary>
        public MenuGenerator()
        {
            if (Variables.MenuPrincipal == null)
            {
                Variables.MenuPrincipal = MainMenu.AddMenu("[TD]Vayne", "td.vayne");
            }
        }

        /// <summary>
        ///     Generates the menu.
        /// </summary>
        public void GenerateMenu()
        {
            QMenu = Variables.MenuPrincipal.AddSubMenu("Q Settings");
            QMenu.Add("TDVaynemisctumblenoqintoenemies", new CheckBox("Don't Q into enemies"));
            QMenu.Add("TDVaynemisctumbleqks", new CheckBox("Q for Killsteal"));
            QMenu.Add("TDVaynemisctumblesmartQ", new CheckBox("Use SOLO Vayne Q Logic"));

            EMenu = Variables.MenuPrincipal.AddSubMenu("E Settings");
            EMenu.Add("useecombo", new CheckBox("Use E"));
            EMenu.Add("TDVaynemisccondemnautoe", new CheckBox("Auto E"));
            EMenu.Add("TDVaynemisccondemncurrent", new CheckBox("Only E Current Target"));
            StringList(EMenu, "EMode", "E Mode", new[] { "PRADASMART", "PRADAPERFECT", "MARKSMAN", "SHARPSHOOTER", "GOSU", "VHR", "PRADALEGACY", "FASTEST", "OLDPRADA" }, 0);
            EMenu.Add("EPushDist", new Slider("E Push Distance", 450, 300, 475));
            EMenu.Add("EHitchance", new Slider("E Hitchance", 50));
            EMenu.Add("TDVaynemisccondemnsave", new CheckBox("Save yourself"));

            HarassMenu = Variables.MenuPrincipal.AddSubMenu("Harass Settings");
            StringList(HarassMenu, "TDVaynemixedmode", "Harass Mode", new[] {"Passive", "Aggresive"}, 1);

            FarmMenu = Variables.MenuPrincipal.AddSubMenu("Farm Settings");
            FarmMenu.Add("useqfarm", new CheckBox("Use Q"));
            FarmMenu.Add("TDVaynelaneclearcondemnjungle", new CheckBox("Condemn Jungle Mobs"));
            
            MiscMenu = Variables.MenuPrincipal.AddSubMenu("Misc Settings");
            MiscMenu.Add("usercombo", new CheckBox("Auto Q when use ult"));
            MiscMenu.Add("botrkcutlasshpercent", new Slider("BotRK/Cutlass Health %", 50));
            MiscMenu.Add("TDVaynemiscmiscellaneousantigapcloser", new CheckBox("Antigapcloser"));
            MiscMenu.Add("TDVaynemiscmiscellaneousinterrupter", new CheckBox("Interrupter"));
            MiscMenu.Add("TDVaynemiscmiscellaneousnoaastealth", new CheckBox("Don't AA while stealthed"));
            MiscMenu.Add("TDVaynemiscmiscellaneousdelay", new Slider("Antigapcloser / Interrupter Delay", 300, 0, 1000));
        }

        public static void StringList(Menu menu, string uniqueId, string displayName, string[] values, int defaultValue)
        {
            var mode = menu.Add(uniqueId, new Slider(displayName, defaultValue, 0, values.Length - 1));
            mode.DisplayName = displayName + ": " + values[mode.CurrentValue];
            mode.OnValueChange +=
                delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
                {
                    sender.DisplayName = displayName + ": " + values[args.NewValue];
                };
        }
    }
}