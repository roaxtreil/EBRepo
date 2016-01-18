using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK;

namespace TDVayne.Skills.Tumble.CCTracker.Tracker
{
    internal class TrackerBootstrap
    {
        public Dictionary<AIHeroClient, TrackerModule> modules = new Dictionary<AIHeroClient, TrackerModule>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="TrackerBootstrap" /> class.
        /// </summary>
        public TrackerBootstrap()
        {
            Init();
        }

        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        private void Init()
        {
            foreach (var champion in EntityManager.Heroes.Enemies)
            {
                var module = new TrackerModule(champion);

                modules.Add(champion, module);
            }
        }

        /// <summary>
        ///     Gets the module of the module by.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public TrackerModule GetModuleByName(string name)
        {
            foreach (var kvp in modules)
            {
                if (kvp.Key.ChampionName.ToLower() == name.ToLower())
                {
                    return kvp.Value;
                }
            }

            return new TrackerModule();
        }
    }
}