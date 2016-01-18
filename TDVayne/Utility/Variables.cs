using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using TDVayne.Modules;
using TDVayne.Modules.Condemn;
using TDVayne.Modules.General;
using TDVayne.Skills;
using TDVayne.Skills.Tumble;
using TDVayne.Skills.Tumble.CCTracker;
using TDVayne.Skills.Tumble.CCTracker.Tracker;

namespace TDVayne.Utility
{
    internal class Variables
    {
        /// <summary>
        ///     The spells dictionary
        /// </summary>
        public static Spell.Active Q = new Spell.Active(SpellSlot.Q);
        public static Spell.Targeted E = new Spell.Targeted(SpellSlot.E, 590);
        public static Spell.Active R = new Spell.Active(SpellSlot.R);

        /// <summary>
        ///     The tracker
        /// </summary>
        public static TrackerBootstrap tracker = new TrackerBootstrap();

        /// <summary>
        ///     The cc list
        /// </summary>
        public static CCList CCList = new CCList();

        /// <summary>
        ///     The skills dictionary
        /// </summary>
        public static List<Skill> skills = new List<Skill>
        {
            new Tumble(),
            new Condemn()
        };

        /// <summary>
        ///     The module list
        /// </summary>
        public static List<ISOLOModule> ModuleList = new List<ISOLOModule>
        {
            //Condemn Modules
            new AutoE(),
            new JungleE(),
            new SaveE(),

            //Tumble Modules
            new KSQ(),

            //General Modules
            new AutoR(),
            new ActivatorModule(),
            new NoAAStealth()
        };

        /// <summary>
        ///     Gets or sets the menu.
        /// </summary>
        /// <value>
        ///     The menu.
        /// </value>
        public static Menu MenuPrincipal { get; set; }

        /// <summary>
        ///     Gets or sets the instance of the assembly.
        /// </summary>
        /// <value>
        ///     The instance.
        /// </value>
        public static SOLOBootstrap Instance { get; set; }
    }
}