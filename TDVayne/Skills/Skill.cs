using EloBuddy;
using TDVayne.Utility.Enums;

namespace TDVayne.Skills
{
    internal interface Skill
    {
        /// <summary>
        ///     Gets the skill mode.
        /// </summary>
        /// <returns></returns>
        SkillMode GetSkillMode();

        /// <summary>
        ///     Executes the logic give a specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        void Execute(Obj_AI_Base target);

        /// <summary>
        ///     Executes the farm logic given a specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        void ExecuteFarm(Obj_AI_Base target);
    }
}