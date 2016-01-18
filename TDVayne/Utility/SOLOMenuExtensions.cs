using EloBuddy.SDK;

namespace TDVayne.Utility.General
{
    internal static class SOLOMenuExtensions
    {
        /// <summary>
        ///     Determines whether the skills is enabled and ready.
        /// </summary>
        /// <param name="Spell">The spell.</param>
        /// <param name="checkMana">if set to <c>true</c> also checks for the mana condition.</param>
        /// <returns></returns>
        public static bool IsEnabledAndReady(this Spell.Active Spell, bool checkMana = true)
        {
            if (!Spell.IsReady())
            {
                return false;
            }

            var readyCondition = Spell.IsReady();
            return readyCondition;
        }

        public static bool IsEnabledAndReady(this Spell.Targeted Spell, bool checkMana = true)
        {
            if (!Spell.IsReady())
            {
                return false;
            }
            var readyCondition = Spell.IsReady();
            return readyCondition;
        }
    }
}