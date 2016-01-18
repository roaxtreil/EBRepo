using EloBuddy;

namespace TDVayne.Skills.Tumble.CCTracker.Tracker
{
    internal class TrackerModule
    {
        private readonly AIHeroClient Champ;

        public TrackerModule()
        {
        }

        public TrackerModule(AIHeroClient champion)
        {
            Champ = champion;
        }

        public float GetSpellCooldown(SpellSlot slot)
        {
            if (Champ == null)
            {
                return -1;
            }

            var spellInstance = GetChampion().Spellbook.GetSpell(slot);
            var spellCooldown = spellInstance.CooldownExpires - Game.Time;
            if (spellInstance.Level == 0)
            {
                return -1;
            }

            return spellCooldown > 0 ? spellCooldown : 0f;
        }

        public AIHeroClient GetChampion()
        {
            return Champ;
        }

        public string GetChampionName()
        {
            return Champ.ChampionName;
        }
    }
}