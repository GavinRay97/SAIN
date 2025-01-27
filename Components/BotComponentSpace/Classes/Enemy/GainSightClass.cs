﻿using EFT;
using EFT.InventoryLogic;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.Info;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Enemy
{
    public class GainSightClass : EnemyBase
    {
        public GainSightClass(SAINEnemy enemy) : base(enemy)
        {
        }

        public float GainSightCoef
        {
            get
            {
                if (_nextCheckVisTime < Time.time)
                {
                    _nextCheckVisTime = Time.time + 0.1f;
                    _gainSightModifier = GetGainSightModifier() * calcRepeatSeenCoef();
                }
                return _gainSightModifier;
            }
        }

        private float calcRepeatSeenCoef()
        {
            float result = 1f;
            if (Enemy.Seen)
            {
                Vector3? lastSeenPos = Enemy.LastSeenPosition;
                if (lastSeenPos != null)
                {
                    result = calcVisionSpeedPositional(
                        lastSeenPos.Value,
                        _minSeenSpeedCoef,
                        _minDistRepeatSeen,
                        _maxDistRepeatSeen,
                        SeenSpeedCheck.Vision);
                }
            }

            if (Enemy.Heard)
            {
                Vector3? lastHeardPosition = Enemy.LastHeardPosition;
                if (lastHeardPosition != null)
                {
                    result *= calcVisionSpeedPositional(
                        lastHeardPosition.Value,
                        _minHeardSpeedCoef,
                        _minDistRepeatHeard,
                        _maxDistRepeatHeard,
                        SeenSpeedCheck.Audio);
                }
            }
            return result;
        }

        private enum SeenSpeedCheck
        {
            None = 0,
            Vision = 1,
            Audio = 2,
        }

        private float calcVisionSpeedPositional(Vector3 position, float minSpeedCoef, float minDist, float maxDist, SeenSpeedCheck check)
        {
            float distance = (position - EnemyPosition).magnitude;
            if (distance <= minDist)
            {
                return minSpeedCoef;
            }
            if (distance >= maxDist)
            {
                return 1f;
            }

            float num = maxDist - minDist;
            float num2 = distance - minDist;
            float ratio = num2 / num;
            float result = Mathf.Lerp(minSpeedCoef, 1f, ratio);
            Logger.LogInfo($"{check} Distance from Position: {distance} Result: {result}");
            return result;
        }

        private float _minSeenSpeedCoef = 0.1f;
        private float _minDistRepeatSeen = 3f;
        private float _maxDistRepeatSeen = 15f;

        private float _minHeardSpeedCoef = 0.25f;
        private float _minDistRepeatHeard = 5f;
        private float _maxDistRepeatHeard = 25f;

        private float _gainSightModifier;
        private float _nextCheckVisTime;

        private float calcGearMod()
        {
            return Enemy.EnemyPlayerComponent.AIData.AIGearModifier.StealthModifier(Enemy.RealDistance);
        }

        private float calcFlareMod()
        {
            var aiData = EnemyPlayer.AIData;
            bool flare = aiData?.GetFlare == true;
            bool usingSuppressor = Enemy.EnemyPlayerComponent?.Equipment.CurrentWeapon?.HasSuppressor == true;

            // Only apply vision speed debuff from weather if their enemy has not shot an unsuppressed weapon
            if (!flare || usingSuppressor)
            {
                return SAINPlugin.BotController.WeatherVision.InverseWeatherModifier;
            }
            return 1f;
        }

        private float GetGainSightModifier()
        {
            float partMod = calcPartsMod();
            float gearMod = calcGearMod();
            float flareMod = calcFlareMod();
            float moveMod = calcMoveModifier();
            float elevMod = calcElevationModifier();
            float posFlareMod = calcPosFlareMod();
            float thirdPartyMod = calcThirdPartyMod();
            float angleMod = calcAngleMod();

            float notLookMod = 1f;
            if (!Enemy.IsAI)
                notLookMod = SAINNotLooking.GetVisionSpeedDecrease(Enemy.EnemyInfo);

            float result = 1f * partMod * gearMod * flareMod * moveMod * elevMod * posFlareMod * thirdPartyMod * angleMod * notLookMod;

            //if (EnemyPlayer.IsYourPlayer && result != 1f)
            //{
            //    Logger.LogWarning($"GainSight Time Result: [{result}] : partMod {partMod} : gearMod {gearMod} : flareMod {flareMod} : moveMod {moveMod} : elevMod {elevMod} : posFlareMod {posFlareMod} : thirdPartyMod {thirdPartyMod} : angleMod {angleMod} : notLookMod {notLookMod} ");
            //}

            return result;
        }

        // private static float _nextLogTime;

        private float calcPartsMod()
        {
            if (!Enemy.IsAI)
            {
                float max = 2f;
                float partRatio = GetRatioPartsVisible(EnemyInfo, out int visibleCount);
                if (visibleCount < 1)
                {
                    //if (Enemy.EnemyPlayer.IsYourPlayer)
                    //{
                    //    Logger.LogInfo($"part mod: result: {max} : part ratio {partRatio} : vis count: {visibleCount}");
                    //}
                    return max;
                }
                float min = 0.9f;
                if (partRatio >= 1f)
                {
                    //if (Enemy.EnemyPlayer.IsYourPlayer)
                    //{
                    //    Logger.LogInfo($"part mod: result: {min} : part ratio {partRatio} : vis count: {visibleCount}");
                    //}
                    return min;
                }
                float result = Mathf.Lerp(max, min, partRatio);
                //if (Enemy.EnemyPlayer.IsYourPlayer)
                //{
                //    Logger.LogInfo($"part mod: result: {result} : part ratio {partRatio} : vis count: {visibleCount}");
                //}
                return result;
            }
            return 1f;
        }

        private static float GetRatioPartsVisible(EnemyInfo enemyInfo, out int visibleCount)
        {
            var enemyParts = enemyInfo.AllActiveParts;
            int partCount = 0;
            visibleCount = 0;

            var bodyPartData = enemyInfo.BodyData().Value;
            if (bodyPartData.IsVisible || bodyPartData.LastVisibilityCastSucceed)
            {
                visibleCount++;
            }
            partCount++;

            foreach (var part in enemyParts)
            {
                if (part.Value.LastVisibilityCastSucceed || part.Value.IsVisible)
                {
                    visibleCount++;
                }
                partCount++;
            }

            return (float)visibleCount / (float)partCount;
        }

        private float calcMoveModifier()
        {
            if (EnemyPlayer.IsSprintEnabled)
            {
                LookSettings globalLookSettings = SAINPlugin.LoadedPreset.GlobalSettings.Look;
                return Mathf.Lerp(1, globalLookSettings.SprintingVisionModifier, Enemy.Vision.EnemyVelocity);
            }
            return 1f;
        }

        private float calcElevationModifier()
        {
            LookSettings globalLookSettings = SAINPlugin.LoadedPreset.GlobalSettings.Look;

            Vector3 botEyeToPlayerBody = EnemyPlayer.MainParts[BodyPartType.body].Position - BotOwner.MainParts[BodyPartType.head].Position;
            var visionAngleDeviation = Vector3.Angle(new Vector3(botEyeToPlayerBody.x, 0f, botEyeToPlayerBody.z), botEyeToPlayerBody);

            if (botEyeToPlayerBody.y >= 0)
            {
                float angleFactor = Mathf.InverseLerp(0, globalLookSettings.HighElevationMaxAngle, visionAngleDeviation);
                return Mathf.Lerp(1f, globalLookSettings.HighElevationVisionModifier, angleFactor);
            }
            else
            {
                float angleFactor = Mathf.InverseLerp(0, globalLookSettings.LowElevationMaxAngle, visionAngleDeviation);
                return Mathf.Lerp(1f, globalLookSettings.LowElevationVisionModifier, angleFactor);
            }
        }

        private float calcPosFlareMod()
        {
            if (Enemy.EnemyStatus.PositionalFlareEnabled
                && Enemy.Heard
                && Enemy.TimeSinceHeard < 300f)
            {
                return 0.8f;
            }
            return 1f;
        }

        private float calcThirdPartyMod()
        {
            if (!Enemy.IsCurrentEnemy)
            {
                SAINEnemy activeEnemy = Enemy.Bot.Enemy;
                if (activeEnemy != null)
                {
                    Vector3? activeEnemyLastKnown = activeEnemy.LastKnownPosition;
                    if (activeEnemyLastKnown != null)
                    {
                        Vector3 currentEnemyDir = (activeEnemyLastKnown.Value - Enemy.Bot.Position).normalized;
                        Vector3 myDir = Enemy.EnemyDirection.normalized;

                        float angle = Vector3.Angle(currentEnemyDir, myDir);

                        float minAngle = 10f;
                        float maxAngle = Enemy.Vision.MaxVisionAngle;
                        if (angle > minAngle && 
                            angle < maxAngle)
                        {
                            float num = maxAngle - minAngle;
                            float num2 = angle - minAngle;

                            float maxRatio = 1.5f;
                            float ratio = num2 / num;
                            float reductionMod = Mathf.Lerp(1f, maxRatio, ratio);
                            return reductionMod;
                        }
                    }
                }
            }
            return 1f;
        }

        private static bool _reduceVisionSpeedOnPeriphVis = true;
        private static float _periphVisionStart = 30f;
        private static float _maxPeriphVisionSpeedReduction = 3f;

        private float calcAngleMod()
        {
            if (!_reduceVisionSpeedOnPeriphVis)
            {
                return 1f;
            }

            float angle = Enemy.Vision.AngleToEnemy;

            float minAngle = _periphVisionStart;
            if (angle < minAngle)
            {
                return 1f;
            }
            float maxAngle = Enemy.Vision.MaxVisionAngle;
            float maxRatio = _maxPeriphVisionSpeedReduction;
            if (angle > maxAngle)
            {
                return maxRatio;
            }

            float angleDiff = maxAngle - minAngle;
            float enemyAngleDiff = angle - minAngle;
            float ratio = enemyAngleDiff / angleDiff;
            return Mathf.Lerp(1f, maxRatio, ratio);
        }
    }
}