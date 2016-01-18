using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using TDVayne.Skills.Condemn;
using TDVayne.Skills.Tumble;
using TDVayne.Utility;
using TDVayne.Skills.Tumble.WardTracker;
using TDVayne.Utility.General;
using WardType = TDVayne.Skills.Tumble.WardTracker.WardType;

namespace TDVayne.Skills.Tumble
{
    class TumbleLogicProvider
    {
        public CondemnLogicProvider Provider = new CondemnLogicProvider();

        /// <summary>
        /// Gets the SOLO Vayne Q position using a patented logic!
        /// </summary>
        /// <returns></returns>
        public Vector3 GetTDVayneQPosition()
        {
            #region The Required Variables
            var positions = TumbleHelper.GetRotatedQPositions();
            var enemyPositions = TumbleHelper.GetEnemyPoints();
            var safePositions = positions.Where(pos => !enemyPositions.Contains(pos.To2D()));
            var BestPosition = ObjectManager.Player.ServerPosition.Extend(Game.CursorPos, 300f);
            var AverageDistanceWeight = .60f;
            var ClosestDistanceWeight = .40f;

            var bestWeightedAvg = 0f;

            var highHealthEnemiesNear =
                    EntityManager.Heroes.Enemies.Where(m => !m.IsMelee && m.IsValidTarget(1300f) && m.HealthPercent > 7);

            var alliesNear = EntityManager.Heroes.Allies.Count(ally => !ally.IsMe && ally.IsValidTarget(1500f));

            var enemiesNear =
                EntityManager.Heroes.Enemies.Where(m => m.IsValidTarget(m.GetAutoAttackRange() + 300f + 65f));
            #endregion


            #region 1 Enemy around only
            if (ObjectManager.Player.CountEnemiesInRange(1500f) <= 1)
            {
                //Logic for 1 enemy near
                var position = ObjectManager.Player.ServerPosition.Extend(Game.CursorPos, 300f);
                return position.IsSafeEx() ? position.To3D() : Vector3.Zero;
            }
            #endregion

            #region Alone, 2 Enemies, 1 Killable
            if (enemiesNear.Count() <= 2)
            {
                if (
                    enemiesNear.Any(
                        t =>
                            t.Health + 15 <
                            ObjectManager.Player.GetAutoAttackDamage(t) * 2 + ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q)
                            && t.Distance(ObjectManager.Player) < t.GetAutoAttackRange() + 80f))
                {
                    var QPosition =
                        ObjectManager.Player.ServerPosition.Extend(
                            highHealthEnemiesNear.OrderBy(t => t.Health).First().ServerPosition, 300f);

                    if (!QPosition.IsUnderTurret())
                    {
                        return QPosition.To3D();
                    }
                }
            }
            #endregion

            #region Alone, 2 Enemies, None Killable
            if (alliesNear == 0 && highHealthEnemiesNear.Count() <= 2)
            {
                //Logic for 2 enemies Near and alone

                //If there is a killable enemy among those. 
                var backwardsPosition = (ObjectManager.Player.ServerPosition.To2D() + 300f * ObjectManager.Player.Direction.To2D()).To3D();

                if (!backwardsPosition.UnderTurret(true))
                {
                    return backwardsPosition;
                }
            }
            #endregion

            #region Already in an enemy's attack range. 
            var closeNonMeleeEnemy = TumbleHelper.GetClosestEnemy(ObjectManager.Player.ServerPosition.Extend(Game.CursorPos, 300f).To3D());

            if (closeNonMeleeEnemy != null
                && ObjectManager.Player.Distance(closeNonMeleeEnemy) <= closeNonMeleeEnemy.AttackRange - 85
                && !closeNonMeleeEnemy.IsMelee)
            {
                return ObjectManager.Player.ServerPosition.Extend(Game.CursorPos, 300f).IsSafeEx()
                    ? ObjectManager.Player.ServerPosition.Extend(Game.CursorPos, 300f).To3D()
                    : Vector3.Zero;
            }
            #endregion

            #region Logic for multiple enemies / allies around.
            foreach (var position in safePositions)
            {
                var enemy = TumbleHelper.GetClosestEnemy(position);
                if (!enemy.IsValidTarget())
                {
                    continue;
                }

                var avgDist = TumbleHelper.GetAvgDistance(position);

                if (avgDist > -1)
                {
                    var closestDist = ObjectManager.Player.ServerPosition.Distance(enemy.ServerPosition);
                    var weightedAvg = closestDist * ClosestDistanceWeight + avgDist * AverageDistanceWeight;
                    if (weightedAvg > bestWeightedAvg && position.To2D().IsSafeEx())
                    {
                        bestWeightedAvg = weightedAvg;
                        BestPosition = position.To2D();
                    }
                }
            }
            #endregion

            var endPosition = (BestPosition.To3D().IsSafe()) ? BestPosition.To3D() : Vector3.Zero;

            #region Couldn't find a suitable position, tumble to nearest ally logic
            if (endPosition == Vector3.Zero)
            {
                //Try to find another suitable position. This usually means we are already near too much enemies turrets so just gtfo and tumble
                //to the closest ally ordered by most health.
                var alliesClose = EntityManager.Heroes.Allies.Where(ally => !ally.IsMe && ally.IsValidTarget(1500f));
                if (alliesClose.Any() && enemiesNear.Any())
                {
                    var closestMostHealth =
                    alliesClose.OrderBy(m => m.Distance(ObjectManager.Player)).ThenByDescending(m => m.Health).FirstOrDefault();

                    if (closestMostHealth != null
                        && closestMostHealth.Distance(enemiesNear.OrderBy(m => m.Distance(ObjectManager.Player)).FirstOrDefault())
                        > ObjectManager.Player.Distance(enemiesNear.OrderBy(m => m.Distance(ObjectManager.Player)).FirstOrDefault()))
                    {
                        var tempPosition = ObjectManager.Player.ServerPosition.Extend(closestMostHealth.ServerPosition,
                            300f);
                        if (tempPosition.IsSafeEx())
                        {
                            endPosition = tempPosition.To3D();
                        }
                    }

                }

            }
            #endregion

            #region Couldn't find an ally, tumble inside bush
            var AmInBush = NavMesh.IsWallOfGrass(ObjectManager.Player.ServerPosition, 33);
            var closeEnemies = TumbleVariables.EnemiesClose;
            //I'm not in bush, all the enemies close are outside a bush
            if (!AmInBush && endPosition == Vector3.Zero)
            {
                var PositionsComplete = TumbleHelper.GetCompleteRotatedQPositions();
                foreach (var position in PositionsComplete)
                {
                    //The end position is a wall of grass
                    //All enemies are outside of the bush and at least 340 units away
                    //There are no detected wards in that bush
                    if (NavMesh.IsWallOfGrass(position, 33)
                        && closeEnemies.All(m => m.Distance(position) > 340f && !NavMesh.IsWallOfGrass(m.ServerPosition, 40))
                        && !WardTrackerVariables.detectedWards.Any(m => NavMesh.IsWallOfGrass(m.Position, 33)
                            && m.Position.Distance(position) < m.WardTypeW.WardVisionRange
                            && !(m.WardTypeW.WardType == WardType.ShacoBox || m.WardTypeW.WardType == WardType.TeemoShroom)))
                    {
                        if (position.IsSafe())
                        {
                            endPosition = position;
                            break;
                        }
                    }
                }
            }

            #endregion


            #region Couldn't even tumble to ally, just go to mouse
            if (endPosition == Vector3.Zero)
            {
                var mousePosition = ObjectManager.Player.ServerPosition.Extend(Game.CursorPos, 300f);
                if (mousePosition.To3D().IsSafe())
                {
                    endPosition = mousePosition.To3D();
                }
            }
            #endregion

            if (ObjectManager.Player.HealthPercent < 10 && ObjectManager.Player.CountEnemiesInRange(1500) > 1)
            {
                var position = ObjectManager.Player.ServerPosition.Extend(Game.CursorPos, 300f);
                return position.IsSafeEx() ? position.To3D() : endPosition;
            }

            return endPosition;
        }

        /// <summary>
        /// Gets the QE position for the Tumble-Condemn combo.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetQEPosition()
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) || !Variables.E.IsReady() && MenuGenerator.EMenu["useecombo"].Cast<CheckBox>().CurrentValue)
            {
                return Vector3.Zero;
            }

            const int currentStep = 45;
            var direction = ObjectManager.Player.Direction.To2D().Perpendicular();
            for (var i = 0f; i < 360f; i += 45)
            {
                var angleRad = Geometry.DegreeToRadian(i);
                var rotatedPosition = ObjectManager.Player.Position.To2D() + (300f * direction.Rotated(angleRad));

                if (Provider.GetTarget(rotatedPosition.To3D()) != null && Provider.GetTarget(rotatedPosition.To3D()).IsValidTarget() && rotatedPosition.To3D().IsSafe())
                {
                    return rotatedPosition.To3D();
                }
            }

            return Vector3.Zero;
        }
    }
}