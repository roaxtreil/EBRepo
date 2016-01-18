using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using EloBuddy;
using EloBuddy.SDK;
using System.Threading.Tasks;

namespace Shaco
{
    class CombatHelper
    {
        public static Obj_AI_Base player = ObjectManager.Player;
        public static bool isDangerousSpell(string spellName,
            Obj_AI_Base target,
            Obj_AI_Base hero,
            Vector3 end,
            float spellRange)
        {
            if (spellName == "CurseofTheSadMummy")
            {
                if (player.Distance(hero.Position) <= 600f)
                {
                    return true;
                }
            }
            if (CombatHelper.IsFacing(target, player.Position) &&
                (spellName == "EnchantedCrystalArrow" || spellName == "rivenizunablade" ||
                 spellName == "EzrealTrueshotBarrage" || spellName == "JinxR" || spellName == "sejuaniglacialprison"))
            {
                if (player.Distance(hero.Position) <= spellRange - 60)
                {
                    return true;
                }
            }
            if (spellName == "InfernalGuardian" || spellName == "UFSlash" ||
                (spellName == "RivenW" && player.HealthPercent < 25))
            {
                if (player.Distance(end) <= 270f)
                {
                    return true;
                }
            }
            if (spellName == "BlindMonkRKick" || spellName == "SyndraR" || spellName == "VeigarPrimordialBurst" ||
                spellName == "AlZaharNetherGrasp" || spellName == "LissandraR")
            {
                if (target.IsMe)
                {
                    return true;
                }
            }
            if (spellName == "TristanaR" || spellName == "ViR")
            {
                if (target.IsMe || player.Distance(target.Position) <= 100f)
                {
                    return true;
                }
            }
            if (spellName == "GalioIdolOfDurand")
            {
                if (player.Distance(hero.Position) <= 600f)
                {
                    return true;
                }
            }
            if (target != null && target.IsMe)
            {
                if (CombatHelper.isTargetedCC(spellName) && spellName != "NasusW" && spellName != "ZedUlt")
                {
                    return true;
                }
            }
            return false;
        }

        public static bool CheckCriticalBuffs(Obj_AI_Base i)
        {
            foreach (BuffInstance buff in i.Buffs)
            {
                if (i.Health <= 6 * ObjectManager.Player.Level && dotsSmallDmg.Contains(buff.Name))
                {
                    return true;
                }
                if (i.Health <= 12 * ObjectManager.Player.Level && dotsMedDmg.Contains(buff.Name))
                {
                    return true;
                }
                if (i.Health <= 25 * ObjectManager.Player.Level && dotsHighDmg.Contains(buff.Name))
                {
                    return true;
                }
            }
            return false;
        }

        private static List<string> dotsHighDmg =
            new List<string>(
                new string[]
                {
                    "karthusfallenonecastsound", "CaitlynAceintheHole", "zedulttargetmark", "timebombenemybuff",
                    "VladimirHemoplague"
                });

        private static List<string> dotsMedDmg =
            new List<string>(
                new string[]
                {
                    "summonerdot", "cassiopeiamiasmapoison", "cassiopeianoxiousblastpoison", "bantamtraptarget",
                    "explosiveshotdebuff", "swainbeamdamage", "SwainTorment", "AlZaharMaleficVisions",
                    "fizzmarinerdoombomb"
                });

        private static List<string> dotsSmallDmg =
            new List<string>(
                new string[]
                { "deadlyvenom", "toxicshotparticle", "MordekaiserChildrenOfTheGrave", "dariushemo", "brandablaze" });

        public static bool IsFacing(Obj_AI_Base source, Vector3 target, float angle = 90)
        {
            if (source == null || !target.IsValid())
            {
                return false;
            }
            return
                (double)
                    Geometry.AngleBetween(
                        Geometry.Perpendicular(Extensions.To2D(source.Direction)), Extensions.To2D(target - source.Position)) <
                angle;
        }

        public static List<string> TargetedCC =
            new List<string>(
                new string[]
                {
                    "TristanaR", "BlindMonkRKick", "AlZaharNetherGrasp", "VayneCondemn", "JayceThunderingBlow", "Headbutt",
                    "Drain", "BlindingDart", "RunePrison", "IceBlast", "Dazzle", "Fling", "MaokaiUnstableGrowth",
                    "MordekaiserChildrenOfTheGrave", "ZedUlt", "LuluW", "PantheonW", "ViR", "JudicatorReckoning",
                    "IreliaEquilibriumStrike", "InfiniteDuress", "SkarnerImpale", "SowTheWind", "PuncturingTaunt",
                    "UrgotSwap2", "NasusW", "VolibearW", "Feast", "NocturneUnspeakableHorror", "Terrify", "VeigarPrimordialBurst"
                });

        public static List<string> invulnerable =
            new List<string>(
                new string[]
                {
                    "sionpassivezombie", "willrevive", "BraumShieldRaise", "UndyingRage", "PoppyDiplomaticImmunity",
                    "LissandraRSelf", "JudicatorIntervention", "ZacRebirthReady", "AatroxPassiveReady", "Rebirth",
                    "alistartrample", "NocturneShroudofDarknessShield", "SpellShield"
                });

        public static bool isTargetedCC(string Spellname)
        {
            return TargetedCC.Contains(Spellname);
        }
    }
}