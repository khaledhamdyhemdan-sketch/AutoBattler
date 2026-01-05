using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoBattler.Core.VoidGame.AI
{
    /// <summary>
    /// Sensory System - Provides perception and targeting utilities for AI units
    /// Converted from TypeScript SensorySystem.ts
    /// </summary>
    public static class SensorySystem
    {
        #region Distance Calculations

        /// <summary>
        /// Get distance between two positions
        /// </summary>
        public static float GetDistance(Position p1, Position p2)
        {
            float dx = p1.x - p2.x;
            float dz = p1.z - p2.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// Get distance between two Vector3 positions (XZ plane)
        /// </summary>
        public static float GetDistance(Vector3 p1, Vector3 p2)
        {
            float dx = p1.x - p2.x;
            float dz = p1.z - p2.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// Get distance between two game units
        /// </summary>
        public static float GetDistance(GameUnit unit1, GameUnit unit2)
        {
            return GetDistance(unit1.position, unit2.position);
        }

        /// <summary>
        /// Get squared distance (faster, no sqrt)
        /// </summary>
        public static float GetDistanceSqr(Position p1, Position p2)
        {
            float dx = p1.x - p2.x;
            float dz = p1.z - p2.z;
            return dx * dx + dz * dz;
        }

        /// <summary>
        /// Check if target is within range
        /// </summary>
        public static bool IsWithinRange(Position source, Position target, float range)
        {
            return GetDistanceSqr(source, target) <= range * range;
        }

        /// <summary>
        /// Check if unit is within range of target
        /// </summary>
        public static bool IsWithinRange(GameUnit source, GameUnit target, float range)
        {
            return IsWithinRange(source.position, target.position, range);
        }

        #endregion

        #region Target Selection

        /// <summary>
        /// Get the closest enemy to a unit
        /// </summary>
        public static GameUnit GetClosestEnemy(GameUnit unit, List<GameUnit> enemies)
        {
            if (enemies == null || enemies.Count == 0)
                return null;

            GameUnit closest = null;
            float closestDist = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy.IsDead)
                    continue;

                float dist = GetDistance(unit.position, enemy.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = enemy;
                }
            }

            return closest;
        }

        /// <summary>
        /// Get the closest ally to a unit (excluding self)
        /// </summary>
        public static GameUnit GetClosestAlly(GameUnit unit, List<GameUnit> allies)
        {
            if (allies == null || allies.Count == 0)
                return null;

            GameUnit closest = null;
            float closestDist = float.MaxValue;

            foreach (var ally in allies)
            {
                if (ally.id == unit.id || ally.IsDead)
                    continue;

                float dist = GetDistance(unit.position, ally.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = ally;
                }
            }

            return closest;
        }

        /// <summary>
        /// Get the lowest health ally (for healing targeting)
        /// </summary>
        public static GameUnit GetLowestHealthAlly(GameUnit unit, List<GameUnit> allies)
        {
            if (allies == null || allies.Count == 0)
                return null;

            GameUnit lowestHealthAlly = null;
            float lowestHealthPercent = float.MaxValue;

            foreach (var ally in allies)
            {
                if (ally.IsDead)
                    continue;

                float healthPercent = (float)ally.stats.hp / ally.stats.maxHp;
                if (healthPercent < lowestHealthPercent)
                {
                    lowestHealthPercent = healthPercent;
                    lowestHealthAlly = ally;
                }
            }

            return lowestHealthAlly;
        }

        /// <summary>
        /// Get the lowest health ally that needs healing (below threshold)
        /// </summary>
        public static GameUnit GetLowestHealthAllyNeedingHealing(GameUnit unit, List<GameUnit> allies, float healthThreshold = 0.8f)
        {
            if (allies == null || allies.Count == 0)
                return null;

            GameUnit lowestHealthAlly = null;
            float lowestHealthPercent = healthThreshold;

            foreach (var ally in allies)
            {
                if (ally.IsDead)
                    continue;

                float healthPercent = (float)ally.stats.hp / ally.stats.maxHp;
                if (healthPercent < lowestHealthPercent)
                {
                    lowestHealthPercent = healthPercent;
                    lowestHealthAlly = ally;
                }
            }

            return lowestHealthAlly;
        }

        /// <summary>
        /// Get the highest threat enemy (based on damage potential)
        /// </summary>
        public static GameUnit GetHighestThreatEnemy(GameUnit unit, List<GameUnit> enemies)
        {
            if (enemies == null || enemies.Count == 0)
                return null;

            GameUnit highestThreat = null;
            float highestThreatLevel = float.MinValue;

            foreach (var enemy in enemies)
            {
                if (enemy.IsDead)
                    continue;

                float threat = GetThreatLevel(enemy, unit);
                if (threat > highestThreatLevel)
                {
                    highestThreatLevel = threat;
                    highestThreat = enemy;
                }
            }

            return highestThreat;
        }

        /// <summary>
        /// Get enemies within a specific range
        /// </summary>
        public static List<GameUnit> GetEnemiesInRange(GameUnit unit, List<GameUnit> enemies, float range)
        {
            if (enemies == null || enemies.Count == 0)
                return new List<GameUnit>();

            var result = new List<GameUnit>();
            float rangeSqr = range * range;

            foreach (var enemy in enemies)
            {
                if (enemy.IsDead)
                    continue;

                if (GetDistanceSqr(unit.position, enemy.position) <= rangeSqr)
                {
                    result.Add(enemy);
                }
            }

            return result;
        }

        /// <summary>
        /// Get allies within a specific range
        /// </summary>
        public static List<GameUnit> GetAlliesInRange(GameUnit unit, List<GameUnit> allies, float range)
        {
            if (allies == null || allies.Count == 0)
                return new List<GameUnit>();

            var result = new List<GameUnit>();
            float rangeSqr = range * range;

            foreach (var ally in allies)
            {
                if (ally.id == unit.id || ally.IsDead)
                    continue;

                if (GetDistanceSqr(unit.position, ally.position) <= rangeSqr)
                {
                    result.Add(ally);
                }
            }

            return result;
        }

        /// <summary>
        /// Get all units (enemies and allies) within range
        /// </summary>
        public static List<GameUnit> GetUnitsInRange(Position center, List<GameUnit> allUnits, float range, string excludeId = null)
        {
            if (allUnits == null || allUnits.Count == 0)
                return new List<GameUnit>();

            var result = new List<GameUnit>();
            float rangeSqr = range * range;

            foreach (var unit in allUnits)
            {
                if (unit.id == excludeId || unit.IsDead)
                    continue;

                if (GetDistanceSqr(center, unit.position) <= rangeSqr)
                {
                    result.Add(unit);
                }
            }

            return result;
        }

        #endregion

        #region Threat Assessment

        /// <summary>
        /// Calculate threat level of an enemy relative to the unit
        /// </summary>
        public static float GetThreatLevel(GameUnit enemy, GameUnit unit)
        {
            if (enemy == null || enemy.IsDead)
                return 0f;

            // Base threat from damage potential
            float baseThreat = enemy.stats.damage;

            // Factor in attack speed (higher = more threat)
            baseThreat *= enemy.stats.attackSpeed;

            // Factor in distance (closer = more threatening)
            float distance = GetDistance(enemy.position, unit.position);
            float distanceFactor = Mathf.Max(0.1f, 1f - (distance / 20f)); // Threat decreases with distance
            baseThreat *= distanceFactor;

            // Factor in crit chance
            baseThreat *= (1f + enemy.stats.critChance * 0.5f);

            // Factor in remaining health (wounded enemies are less threatening)
            float healthFactor = (float)enemy.stats.hp / enemy.stats.maxHp;
            baseThreat *= healthFactor;

            // Type-specific threat modifiers
            switch (enemy.type)
            {
                case UnitType.EnemyRanged:
                    baseThreat *= 1.3f; // Ranged enemies are more threatening
                    break;
                case UnitType.EnemyBoss:
                    baseThreat *= 2.0f; // Bosses are much more threatening
                    break;
                case UnitType.Dps:
                    baseThreat *= 1.5f; // DPS units deal high damage
                    break;
            }

            return baseThreat;
        }

        /// <summary>
        /// Get the overall threat level of all enemies
        /// </summary>
        public static float GetTotalThreatLevel(GameUnit unit, List<GameUnit> enemies)
        {
            if (enemies == null || enemies.Count == 0)
                return 0f;

            float totalThreat = 0f;
            foreach (var enemy in enemies)
            {
                totalThreat += GetThreatLevel(enemy, unit);
            }

            return totalThreat;
        }

        /// <summary>
        /// Check if unit is in danger (low health or high threat nearby)
        /// </summary>
        public static bool IsInDanger(GameUnit unit, List<GameUnit> enemies, float healthThreshold = 0.3f, float dangerRange = 3f)
        {
            // Low health = danger
            float healthPercent = (float)unit.stats.hp / unit.stats.maxHp;
            if (healthPercent <= healthThreshold)
                return true;

            // Enemies very close = danger
            var nearbyEnemies = GetEnemiesInRange(unit, enemies, dangerRange);
            if (nearbyEnemies.Count >= 2) // Multiple enemies close
                return true;

            return false;
        }

        #endregion

        #region Positioning

        /// <summary>
        /// Get direction vector from source to target
        /// </summary>
        public static Position GetDirection(Position source, Position target)
        {
            float dx = target.x - source.x;
            float dz = target.z - source.z;
            float length = Mathf.Sqrt(dx * dx + dz * dz);

            if (length < 0.0001f)
                return new Position(0, 0);

            return new Position(dx / length, dz / length);
        }

        /// <summary>
        /// Get position at a certain distance in a direction
        /// </summary>
        public static Position GetPositionInDirection(Position start, Position direction, float distance)
        {
            return new Position(
                start.x + direction.x * distance,
                start.z + direction.z * distance
            );
        }

        /// <summary>
        /// Get retreat position (away from threats)
        /// </summary>
        public static Position GetRetreatPosition(GameUnit unit, List<GameUnit> enemies, float retreatDistance = 5f)
        {
            if (enemies == null || enemies.Count == 0)
                return unit.position;

            // Calculate average enemy position
            float avgX = 0f, avgY = 0f;
            int count = 0;

            foreach (var enemy in enemies)
            {
                if (!enemy.IsDead)
                {
                    avgX += enemy.position.x;
                    avgY += enemy.position.z;
                    count++;
                }
            }

            if (count == 0)
                return unit.position;

            avgX /= count;
            avgY /= count;

            // Move away from average enemy position
            var threatCenter = new Position(avgX, avgY);
            var awayDirection = GetDirection(threatCenter, unit.position);

            return GetPositionInDirection(unit.position, awayDirection, retreatDistance);
        }

        /// <summary>
        /// Get flanking position (to the side of target)
        /// </summary>
        public static Position GetFlankingPosition(GameUnit unit, GameUnit target, float flankDistance = 3f)
        {
            if (target == null)
                return unit.position;

            var toTarget = GetDirection(unit.position, target.position);
            
            // Get perpendicular direction (rotate 90 degrees)
            var flankDir = new Position(-toTarget.z, toTarget.x);
            
            // Randomly choose left or right flank
            if (UnityEngine.Random.value > 0.5f)
            {
                flankDir = new Position(-flankDir.x, -flankDir.z);
            }

            // Calculate position to the side of target
            return GetPositionInDirection(target.position, flankDir, flankDistance);
        }

        /// <summary>
        /// Get optimal ranged attack position (maintain distance)
        /// </summary>
        public static Position GetOptimalRangedPosition(GameUnit unit, GameUnit target, float optimalRange, float tolerance = 1f)
        {
            if (target == null)
                return unit.position;

            float currentDist = GetDistance(unit.position, target.position);
            
            if (Mathf.Abs(currentDist - optimalRange) <= tolerance)
            {
                // Already at optimal range
                return unit.position;
            }

            var direction = GetDirection(target.position, unit.position);
            return GetPositionInDirection(target.position, direction, optimalRange);
        }

        /// <summary>
        /// Check if there's line of sight between two positions
        /// </summary>
        public static bool HasLineOfSight(Position source, Position target, List<Obstacle> obstacles)
        {
            if (obstacles == null || obstacles.Count == 0)
                return true;

            foreach (var obstacle in obstacles)
            {
                if (obstacle.destroyed)
                    continue;

                // Simple circle-line intersection test
                if (LineIntersectsCircle(source, target, obstacle.position, obstacle.radius))
                    return false;
            }

            return true;
        }

        private static bool LineIntersectsCircle(Position lineStart, Position lineEnd, Position circleCenter, float radius)
        {
            float dx = lineEnd.x - lineStart.x;
            float dz = lineEnd.z - lineStart.z;

            float fx = lineStart.x - circleCenter.x;
            float fz = lineStart.z - circleCenter.z;

            float a = dx * dx + dz * dz;
            float b = 2f * (fx * dx + fz * dz);
            float c = fx * fx + fz * fz - radius * radius;

            float discriminant = b * b - 4f * a * c;

            if (discriminant < 0)
                return false;

            discriminant = Mathf.Sqrt(discriminant);

            float t1 = (-b - discriminant) / (2f * a);
            float t2 = (-b + discriminant) / (2f * a);

            return (t1 >= 0f && t1 <= 1f) || (t2 >= 0f && t2 <= 1f);
        }

        #endregion

        #region Group Queries

        /// <summary>
        /// Get the centroid of a group of units
        /// </summary>
        public static Position GetGroupCenter(List<GameUnit> units)
        {
            if (units == null || units.Count == 0)
                return new Position(0, 0);

            float totalX = 0f, totalY = 0f;
            int count = 0;

            foreach (var unit in units)
            {
                if (!unit.IsDead)
                {
                    totalX += unit.position.x;
                    totalY += unit.position.z;
                    count++;
                }
            }

            if (count == 0)
                return new Position(0, 0);

            return new Position(totalX / count, totalY / count);
        }

        /// <summary>
        /// Get the spread (average distance from center) of a group
        /// </summary>
        public static float GetGroupSpread(List<GameUnit> units)
        {
            if (units == null || units.Count < 2)
                return 0f;

            var center = GetGroupCenter(units);
            float totalDist = 0f;
            int count = 0;

            foreach (var unit in units)
            {
                if (!unit.IsDead)
                {
                    totalDist += GetDistance(unit.position, center);
                    count++;
                }
            }

            return count > 0 ? totalDist / count : 0f;
        }

        /// <summary>
        /// Count units by type
        /// </summary>
        public static Dictionary<UnitType, int> CountUnitsByType(List<GameUnit> units)
        {
            var counts = new Dictionary<UnitType, int>();

            if (units == null)
                return counts;

            foreach (var unit in units)
            {
                if (unit.IsDead)
                    continue;

                if (counts.ContainsKey(unit.type))
                    counts[unit.type]++;
                else
                    counts[unit.type] = 1;
            }

            return counts;
        }

        #endregion
    }
}
