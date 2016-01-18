using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using TDVayne.Utility;
using TDVayne.Utility.Entities;

namespace TDVayne.Skills.Tumble
{
    internal static class TumbleExtensions
    {
        public static int GameTickCount
        {
            get { return Environment.TickCount & int.MaxValue; }
        }

        public static bool UnderTurret(this Obj_AI_Base unit)
        {
            return UnderTurret(unit.Position, true);
        }

        /// <summary>
        ///     Returns true if the unit is under turret range.
        /// </summary>
        public static bool UnderTurret(this Obj_AI_Base unit, bool enemyTurretsOnly)
        {
            return UnderTurret(unit.Position, enemyTurretsOnly);
        }

        public static bool UnderTurretVector3(this Vector3 position, bool enemyTurretsOnly)
        {
            return
                ObjectManager.Get<Obj_AI_Turret>().Any(turret => turret.IsValidTarget(950, enemyTurretsOnly, position));
        }

        public static double GetComboDamage(
            this AIHeroClient source,
            Obj_AI_Base target,
            IEnumerable<SpellSlot> spellCombo)
        {
            return source.GetComboDamage(target, spellCombo.Select(spell => Tuple.Create(spell, 0)).ToArray());
        }

        public static double GetComboDamage(
            this AIHeroClient source,
            Obj_AI_Base target,
            IEnumerable<Tuple<SpellSlot, int>> spellCombo)
        {
            return spellCombo.Sum(spell => source.GetSpellDamage(target, spell.Item1));
        }

        public static List<AIHeroClient> GetEnemiesInRange(this Obj_AI_Base unit, float range)
        {
            return GetEnemiesInRange(unit.ServerPosition, range);
        }

        public static List<AIHeroClient> GetEnemiesInRange(this Vector3 point, float range)
        {
            return
                EntityManager.Heroes.Enemies
                    .FindAll(x => point.Distance(x.ServerPosition, true) <= range * range);
        }
        public static List<Vector2> ToVector2(this List<Vector3> path)
        {
            return path.Select(point => point.ToVector2()).ToList();
        }

        public static Vector2 ToVector2(this Vector3 vector3)
        {
            return new Vector2(vector3.X, vector3.Y);
        }
        public static Vector3 ExtendVector3(this Vector3 vector3, Vector3 toVector3, float distance)
        {
            return vector3 + (distance * (toVector3 - vector3).Normalized());
        }
        public static Vector3 ExtendVector(this Vector3 vector3, Vector2 toVector2, float distance)
        {
            return vector3 + (distance * (toVector2.ToVector3(vector3.Z) - vector3).Normalized());
        }
        public static Vector2 Extend(this Vector2 vector2, Vector3 toVector3, float distance)
        {
            return vector2 + (distance * (toVector3.ToVector2() - vector2).Normalized());
        }
        
        /// <summary>
        ///     Extends a Vector3 to a Vector2.
        /// </summary>
        /// <param name="vector3">Extended SharpDX Vector3 (From)</param>
        /// <param name="toVector2">SharpDX Vector2 (To)</param>
        /// <param name="distance">Distance (float units)</param>
        /// <returns>Extended Vector3</returns>
        public static Vector3 Extend(this Vector3 vector3, Vector2 toVector2, float distance)
        {
            return vector3 + (distance * (toVector2.ToVector3(vector3.Z) - vector3).Normalized());
        }

        public static Vector3 ToVector3(this Vector2 vector2, float z = 0f)
        {
            return new Vector3(vector2, z.Equals(0f) ? GameObjects.Player.ServerPosition.Z : z);
        }

        public static bool IsWall(this Vector3 vector3)
        {
            return NavMesh.GetCollisionFlags(vector3).HasFlag(CollisionFlags.Wall);
        }

        public static bool IsWall(this Vector2 vector2)
        {
            return vector2.ToVector3().IsWall();
        }

        public static bool UnderTurret(this Vector3 position, bool enemyTurretsOnly)
        {
            return
                ObjectManager.Get<Obj_AI_Turret>().Any(turret => turret.IsValidTarget(950, enemyTurretsOnly, position));
        }

        /// <summary>
        ///     Determines whether the position is safe.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        public static bool IsSafe(this Vector3 position)
        {
            return position.To2D().IsSafeEx()
                   && position.IsNotIntoEnemies()
                   && EntityManager.Heroes.Enemies.All(m => m.Distance(position) > 350f)
                   &&
                   (!position.UnderTurret(true) ||
                    (ObjectManager.Player.UnderTurret(true) && position.UnderTurret(true) &&
                     ObjectManager.Player.HealthPercent > 10));
            //Either it is not under turret or both the player and the position are under turret already and the health percent is greater than 10.
        }

        /// <summary>
        ///     Determines whether the position is Safe using the allies/enemies logic
        /// </summary>
        /// <param name="Position">The position.</param>
        /// <returns></returns>
        public static bool IsSafeEx(this Vector2 Position)
        {
            if (Position.IsUnderTurret() && !ObjectManager.Player.UnderTurret())
            {
                return false;
            }
            var range = 1000f;
            var lowHealthAllies =
                EntityManager.Heroes.Allies.Where(a => a.IsValidTarget(range) && a.HealthPercent < 10 && !a.IsMe);
            var lowHealthEnemies =
                EntityManager.Heroes.Allies.Where(a => a.IsValidTarget(range) && a.HealthPercent < 10);
            var enemies = ObjectManager.Player.CountEnemiesInRange(range);
            var allies = ObjectManager.Player.CountAlliesInRange(range);
            var enemyTurrets = GameObjects.EnemyTurrets.Where(m => m.IsValidTarget(975f));
            var allyTurrets = GameObjects.AllyTurrets.Where(m => m.IsValidTarget(975f));

            return (allies - lowHealthAllies.Count() + allyTurrets.Count() * 2 + 1 >=
                    enemies - lowHealthEnemies.Count() +
                    (!ObjectManager.Player.UnderTurret(true) ? enemyTurrets.Count() * 2 : 0));
        }

        /// <summary>
        ///     Determines whether the position is not into enemies.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        public static bool IsNotIntoEnemies(this Vector3 position)
        {
            if (!MenuGenerator.QMenu["TDVaynemisctumblesmartQ"].Cast<CheckBox>().CurrentValue &&
                !MenuGenerator.QMenu["TDVaynemisctumblenoqintoenemies"].Cast<CheckBox>().CurrentValue)
            {
                return true;
            }

            var enemyPoints = TumbleHelper.GetEnemyPoints();
            if (enemyPoints.ToArray().Contains(position.To2D()) &&
                !enemyPoints.Contains(ObjectManager.Player.ServerPosition.To2D()))
            {
                return false;
            }

            var closeEnemies =
                EntityManager.Heroes.Enemies.FindAll(
                    en =>
                        en.IsValidTarget(1500f) &&
                        !(en.Distance(ObjectManager.Player.ServerPosition) < en.AttackRange + 65f));
            if (
                closeEnemies.All(
                    enemy => position.CountEnemiesInRange(enemy.AttackRange > 350 ? enemy.AttackRange : 400) == 0))
            {
                return true;
            }

            return false;
        }
    }
}