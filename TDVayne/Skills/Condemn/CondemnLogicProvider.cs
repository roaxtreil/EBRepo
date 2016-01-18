using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu.Values;
using iSeriesReborn.Utility.Positioning;
using SharpDX;
using TDVayne.Skills.Tumble;
using TDVayne.Utility;
using TDVayne.Utility.Entities;
using TDVayne.Utility.General;

namespace TDVayne.Skills.Condemn
{
    internal class CondemnLogicProvider
    {

        private static bool IsCollisionable(Vector3 pos)
        {

            return NavMesh.GetCollisionFlags(pos).HasFlag(CollisionFlags.Wall) ||
                (NavMesh.GetCollisionFlags(pos).HasFlag(CollisionFlags.Building));
        }
        public static AIHeroClient GetCondemnableTarget()
        {
            return ObjectManager.Get<AIHeroClient>().OrderByDescending(TargetSelector.GetPriority).FirstOrDefault(hero => IsCondemnable(hero));
        }

        internal AIHeroClient GetTarget(Vector3 position = default(Vector3))
        {
            var HeroList = EntityManager.Heroes.Enemies.Where(
                h =>
                    h.IsValidTarget(Variables.E.Range) &&
                    !h.HasBuffOfType(BuffType.SpellShield) &&
                    !h.HasBuffOfType(BuffType.SpellImmunity));

            var Accuracy = 38;
            var PushDistance = 425;

            if (ObjectManager.Player.ServerPosition.UnderTurret(true))
            {
                return null;
            }

            var currentTarget = Orbwalker.GetTarget();

            if (EntityManager.Heroes.Allies.Count(ally => !ally.IsMe && ally.IsValidTarget(1500f, false)) == 0
                && ObjectManager.Player.CountEnemiesInRange(1500f) == 1)
            {
                //It's a 1v1 situation. We push condemn to the limit and lower the accuracy by 5%.
                Accuracy = 33;
                PushDistance = 460;
            }

            var startPosition = position != default(Vector3) ? position : ObjectManager.Player.ServerPosition;

            foreach (var Hero in HeroList)
            {
                if (MenuGenerator.EMenu["TDVaynemisccondemncurrent"].Cast<CheckBox>().CurrentValue &&
                    !(MenuGenerator.EMenu["TDVaynemisccondemnautoe"].Cast<CheckBox>().CurrentValue))
                {
                    if (Hero.NetworkId != currentTarget.NetworkId)
                    {
                        continue;
                    }
                }

                if (Hero.Health + 10 <= ObjectManager.Player.GetAutoAttackDamage(Hero) * 2)
                {
                    continue;
                }

                Spell.Skillshot E = new Spell.Skillshot(SpellSlot.E, 550, SkillShotType.Linear, (int) 0.25f, (int?) 2200f);
                var prediction = Prediction.Position.PredictLinearMissile(Hero, E.Range, E.Width, E.CastDelay, E.Speed, int.MaxValue);
                var targetPosition = prediction.UnitPosition;
                var finalPosition = targetPosition.Extend(startPosition, -PushDistance);
                var finalPosition_ex = Hero.ServerPosition.Extend(startPosition, -PushDistance);
                var finalPosition_3 = prediction.CastPosition.Extend(startPosition, -PushDistance);

                //Yasuo Wall Logic
                if (YasuoWall.CollidesWithWall(startPosition, Hero.ServerPosition.Extend(startPosition, -450f).To3D()))
                {
                    continue;
                }

                //Condemn to turret logic
                if (
                    GameObjects.AllyTurrets.Any(
                        m => m.IsValidTarget(float.MaxValue, false) && m.Distance(finalPosition) <= 450f))
                {
                    var turret =
                        GameObjects.AllyTurrets.FirstOrDefault(
                            m => m.IsValidTarget(float.MaxValue, false) && m.Distance(finalPosition) <= 450f);
                    if (turret != null)
                    {
                        var enemies = GameObjects.Enemy.Where(m => m.Distance(turret) < 775f && m.IsValidTarget());

                        if (!enemies.Any())
                        {
                            return Hero;
                        }
                    }
                }

                //Condemn To Wall Logic
                var condemnRectangle =
                    new SOLOPolygon(SOLOPolygon.Rectangle(targetPosition.To2D(), finalPosition, Hero.BoundingRadius));
                var condemnRectangle_ex =
                    new SOLOPolygon(SOLOPolygon.Rectangle(Hero.ServerPosition.To2D(), finalPosition_ex,
                        Hero.BoundingRadius));
                var condemnRectangle_3 =
                    new SOLOPolygon(SOLOPolygon.Rectangle(prediction.CastPosition.To2D(), finalPosition_3,
                        Hero.BoundingRadius));

                if (IsBothNearWall(Hero))
                {
                    return null;
                }

                if (
                    condemnRectangle.Points.Count(
                        point => NavMesh.GetCollisionFlags(point.X, point.Y).HasFlag(CollisionFlags.Wall)) >=
                    condemnRectangle.Points.Count() * (Accuracy / 100f)
                    ||
                    condemnRectangle_ex.Points.Count(
                        point => NavMesh.GetCollisionFlags(point.X, point.Y).HasFlag(CollisionFlags.Wall)) >=
                    condemnRectangle_ex.Points.Count() * (Accuracy / 100f)
                    ||
                    condemnRectangle_3.Points.Count(
                        point => NavMesh.GetCollisionFlags(point.X, point.Y).HasFlag(CollisionFlags.Wall)) >=
                    condemnRectangle_ex.Points.Count() * (Accuracy / 100f))
                {
                    return Hero;
                }
            }
            return null;
        }



        public static bool IsCondemnable(AIHeroClient hero)
        {

            if (!hero.IsValidTarget(550f) || hero.HasBuffOfType(BuffType.SpellShield) ||
                hero.HasBuffOfType(BuffType.SpellImmunity) || hero.IsDashing())
                return false;

            //values for pred calc pP = player position; p = enemy position; pD = push distance
            var pP = ObjectManager.Player.ServerPosition;
            var p = hero.ServerPosition;
            var pD = MenuGenerator.EMenu["EPushDist"].Cast<Slider>().CurrentValue;
            var mode = MenuGenerator.EMenu["EMode"].Cast<Slider>().CurrentValue;


            if (mode == 0 && (IsCollisionable(p.ExtendVector3(pP, -pD)) || IsCollisionable(p.ExtendVector3(pP, -pD / 2f)) ||
                 IsCollisionable(p.ExtendVector3(pP, -pD / 3f))))
            {
                if (!hero.CanMove ||
                    (hero.Spellbook.IsAutoAttacking))
                    return true;

                var enemiesCount = ObjectManager.Player.CountEnemiesInRange(1200);
                if (enemiesCount > 1 && enemiesCount <= 3)
                {
                    Spell.Skillshot E = new Spell.Skillshot(SpellSlot.E, 550, SkillShotType.Linear, (int) 0.25f, (int?) 2200f);
                    var prediction = E.GetPrediction(hero);
                    for (var i = 15; i < pD; i += 75)
                    {
                        var posFlags = NavMesh.GetCollisionFlags(
                            prediction.UnitPosition.To2D()
                                .Extend(
                                    pP.To2D(),
                                    -i)
                                .To3D());
                        if (posFlags.HasFlag(CollisionFlags.Wall) || posFlags.HasFlag(CollisionFlags.Building))
                        {
                            return true;
                        }
                    }
                    return false;
                }
                else
                {
                    var hitchance = MenuGenerator.EMenu["EHitchance"].Cast<Slider>().CurrentValue;
                    var angle = 0.20 * hitchance;
                    const float travelDistance = 0.5f;
                    var alpha = new Vector2((float)(p.X + travelDistance * Math.Cos(Math.PI / 180 * angle)),
                        (float)(p.X + travelDistance * Math.Sin(Math.PI / 180 * angle)));
                    var beta = new Vector2((float)(p.X - travelDistance * Math.Cos(Math.PI / 180 * angle)),
                        (float)(p.X - travelDistance * Math.Sin(Math.PI / 180 * angle)));

                    for (var i = 15; i < pD; i += 100)
                    {
                        if (IsCollisionable(pP.To2D().Extend(alpha,
                                i)
                            .To3D()) && IsCollisionable(pP.To2D().Extend(beta, i).To3D()))
                            return true;
                    }
                    return false;
                }
            }

            if (mode == 1 &&
                (IsCollisionable(p.ExtendVector3(pP, -pD)) || IsCollisionable(p.ExtendVector3(pP, -pD / 2f)) ||
                 IsCollisionable(p.ExtendVector3(pP, -pD / 3f))))
            {
                if (!hero.CanMove ||
                    (hero.Spellbook.IsAutoAttacking))
                    return true;

                var hitchance = MenuGenerator.EMenu["EHitchance"].Cast<Slider>().CurrentValue;
                var angle = 0.20 * hitchance;
                const float travelDistance = 0.5f;
                var alpha = new Vector2((float)(p.X + travelDistance * Math.Cos(Math.PI / 180 * angle)),
                    (float)(p.X + travelDistance * Math.Sin(Math.PI / 180 * angle)));
                var beta = new Vector2((float)(p.X - travelDistance * Math.Cos(Math.PI / 180 * angle)),
                    (float)(p.X - travelDistance * Math.Sin(Math.PI / 180 * angle)));

                for (var i = 15; i < pD; i += 100)
                {
                    if (IsCollisionable(pP.To2D().Extend(alpha,
                            i)
                        .To3D()) && IsCollisionable(pP.To2D().Extend(beta, i).To3D()))
                        return true;
                }
                return false;
            }

            if (mode == 8)
            {
                if (!hero.CanMove ||
                    (hero.Spellbook.IsAutoAttacking))
                    return true;

                var hitchance = MenuGenerator.EMenu["EHitchance"].Cast<Slider>().CurrentValue;
                var angle = 0.20 * hitchance;
                const float travelDistance = 0.5f;
                var alpha = new Vector2((float)(p.X + travelDistance * Math.Cos(Math.PI / 180 * angle)),
                    (float)(p.X + travelDistance * Math.Sin(Math.PI / 180 * angle)));
                var beta = new Vector2((float)(p.X - travelDistance * Math.Cos(Math.PI / 180 * angle)),
                    (float)(p.X - travelDistance * Math.Sin(Math.PI / 180 * angle)));

                for (var i = 15; i < pD; i += 100)
                {
                    if (IsCollisionable(pP.To2D().Extend(alpha,
                            i)
                        .To3D()) || IsCollisionable(pP.To2D().Extend(beta, i).To3D()))
                        return true;
                }
                return false;
            }

            if (mode == 2)
            {
                Spell.Skillshot E = new Spell.Skillshot(SpellSlot.E, 550, SkillShotType.Linear, (int) 0.25f, (int?) 2200f);
                var prediction = E.GetPrediction(hero);
                return NavMesh.GetCollisionFlags(
                    prediction.UnitPosition.To2D()
                        .Extend(
                            pP.To2D(),
                            -pD)
                        .To3D()).HasFlag(CollisionFlags.Wall) ||
                       NavMesh.GetCollisionFlags(
                           prediction.UnitPosition.To2D()
                               .Extend(
                                   pP.To2D(),
                                   -pD / 2f)
                               .To3D()).HasFlag(CollisionFlags.Wall);
            }

            if (mode == 3)
            {
                Spell.Skillshot E = new Spell.Skillshot(SpellSlot.E, 550, SkillShotType.Linear, (int) 0.25f, (int?) 2200f);
                var prediction = E.GetPrediction(hero);
                for (var i = 15; i < pD; i += 100)
                {
                    var posCF = NavMesh.GetCollisionFlags(
                        prediction.UnitPosition.To2D()
                            .Extend(
                                pP.To2D(),
                                -i)
                            .To3D());
                    if (posCF.HasFlag(CollisionFlags.Wall) || posCF.HasFlag(CollisionFlags.Building))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (mode == 4)
            {
                Spell.Skillshot E = new Spell.Skillshot(SpellSlot.E, 550, SkillShotType.Linear, (int) 0.25f, (int?) 2200f);
                var prediction = E.GetPrediction(hero);
                for (var i = 15; i < pD; i += 75)
                {
                    var posCF = NavMesh.GetCollisionFlags(
                        prediction.UnitPosition.To2D()
                            .Extend(
                                pP.To2D(),
                                -i)
                            .To3D());
                    if (posCF.HasFlag(CollisionFlags.Wall) || posCF.HasFlag(CollisionFlags.Building))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (mode == 5)
            {
                Spell.Skillshot E = new Spell.Skillshot(SpellSlot.E, 550, SkillShotType.Linear, (int) 0.25f, (int?) 2200f);
                var prediction = E.GetPrediction(hero);
                for (var i = 15; i < pD; i += (int)hero.BoundingRadius) //:frosty:
                {
                    var posCF = NavMesh.GetCollisionFlags(
                        prediction.UnitPosition.To2D()
                            .Extend(
                                pP.To2D(),
                                -i)
                            .To3D());
                    if (posCF.HasFlag(CollisionFlags.Wall) || posCF.HasFlag(CollisionFlags.Building))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (mode == 6)
            {
                Spell.Skillshot E = new Spell.Skillshot(SpellSlot.E, 550, SkillShotType.Linear, (int) 0.25f, (int?) 2200f);
                var prediction = E.GetPrediction(hero);
                for (var i = 15; i < pD; i += 75)
                {
                    var posCF = NavMesh.GetCollisionFlags(
                        prediction.UnitPosition.To2D()
                            .Extend(
                                pP.To2D(),
                                -i)
                            .To3D());
                    if (posCF.HasFlag(CollisionFlags.Wall) || posCF.HasFlag(CollisionFlags.Building))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (mode == 7 && IsCollisionable(p.ExtendVector3(pP, -pD)) || IsCollisionable(p.ExtendVector3(pP, -pD / 2f)) ||
                                     IsCollisionable(p.ExtendVector3(pP, -pD / 3f)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Determines whether whether or not both the players and the target are near a wall.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        private static bool IsBothNearWall(Obj_AI_Base target)
        {
            var positions =
                GetWallQPositions(target, 110).ToList().OrderBy(pos => pos.Distance(target.ServerPosition, true));
            var positions_ex =
                GetWallQPositions(ObjectManager.Player, 110)
                    .ToList()
                    .OrderBy(pos => pos.Distance(ObjectManager.Player.ServerPosition, true));

            if (positions.Any(p => p.IsWall()) && positions_ex.Any(p => p.IsWall()))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Gets the wall q positions (Sideways positions to the players).
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="Range">The range.</param>
        /// <returns></returns>
        private static Vector3[] GetWallQPositions(Obj_AI_Base player, float Range)
        {
            Vector3[] vList =
            {
                (player.ServerPosition.To2D() + Range*player.Direction.To2D()).To3D(),
                (player.ServerPosition.To2D() - Range*player.Direction.To2D()).To3D()
            };

            return vList;
        }

        /// <summary>
        ///     Determines whether the specified target is condemnable.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="fromPosition">From position.</param>
        /// <returns></returns>
        public bool IsCondemnable(Obj_AI_Base target, Vector3 fromPosition)
        {
            var pushDistance = 420f;
            var targetPosition = target.ServerPosition;
            for (var i = 0; i < pushDistance; i += 40)
            {
                var tempPos = targetPosition.Extend(fromPosition, -i);
                if (tempPos.IsWall())
                {
                    return true;
                }
            }
            return false;
        }
    }
}