using EloBuddy;
using TDVayne.Skills.General;
using TDVayne.Utility;

namespace TDVayne
{
    internal class SOLOBootstrap
    {
        /// <summary>
        ///     The antigapcloser module
        /// </summary>
        public SOLOAntigapcloser Antigapcloser;

        /// <summary>
        ///     The menu generator module
        /// </summary>
        public MenuGenerator MenuGenerator;

        /// <summary>
        ///     The assembly module
        /// </summary>
        public TDVayne TDVayne;

        /// <summary>
        ///     The translator module
        /// </summary>
        /// <summary>
        ///     Initializes a new instance of the <see cref="SOLOBootstrap" /> class.
        /// </summary>
        public SOLOBootstrap()
        {
            if (ObjectManager.Player.ChampionName != "Vayne")
            {
                return;
            }

            if (Variables.Instance != null)
            {
                return;
            }

            TDVayne = new TDVayne();
            MenuGenerator = new MenuGenerator();
            Antigapcloser = new SOLOAntigapcloser();
            MenuGenerator.GenerateMenu();

            PrintLoaded();
        }

        /// <summary>
        ///     Prints the "SOLO Vayne Loaded" string in chat
        /// </summary>
        public void PrintLoaded()
        {
            Chat.Print("<b>[<font color='#009aff'>ThugDoge</font>] Vayne loaded!");
        }

        /// <summary>
        ///     Gets the instance.
        /// </summary>
        /// <returns></returns>
        public SOLOBootstrap GetInstance()
        {
            return Variables.Instance ?? (Variables.Instance = new SOLOBootstrap());
        }
    }
}