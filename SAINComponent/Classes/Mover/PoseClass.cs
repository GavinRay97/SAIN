﻿using EFT;
using UnityEngine;
using SAIN.Helpers;
using System;
using SAIN.SAINComponent.Classes.Enemy;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class PoseClass : SAINBase, ISAINClass
    {
        public PoseClass(Bot sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            if (!Bot.PatrolDataPaused)
            {
                return;
            }

            FindObjectsInFront();
            if (Player.IsSprintEnabled)
            {
                _targetPoseLevel = 1f;
            }

            if (_targetPoseLevel == 1f && BotOwner.Mover.TargetPose != 1f)
            {
                _updatePoseTimer = Time.time + 0.25f;
                BotOwner.Mover?.SetPose(_targetPoseLevel);
                return;
            }
            if (_updatePoseTimer < Time.time)
            {
                _updatePoseTimer = Time.time + 0.25f;
                BotOwner.Mover?.SetPose(_targetPoseLevel);

                if (Bot.Mover.CurrentStamina > 0.1f)
                {
                    //BotOwner.Mover?.SetPose(_targetPoseLevel);
                }
            }
        }

        private float _updatePoseTimer;

        public void Dispose()
        {
        }

        public bool SetPoseToCover()
        {
            return SetTargetPose(ObjectTargetPoseCover);
        }

        public void SetTargetPose(float num)
        {
            _targetPoseLevel = num;
        }

        public bool SetTargetPose(float? num)
        {
            if (num != null)
            {
                _targetPoseLevel = num.Value;
            }
            return num != null;
        }

        private float _targetPoseLevel;

        public bool ObjectInFront => ObjectTargetPoseCover != null;
        public float? ObjectTargetPoseCover { get; private set; }

        private void FindObjectsInFront()
        {
            if (UpdateFindObjectTimer < Time.time)
            {
                UpdateFindObjectTimer = Time.time + 0.5f;

                if (FindCrouchFromCover(out float pose1))
                {
                    ObjectTargetPoseCover = pose1;
                }
                else
                {
                    ObjectTargetPoseCover = null;
                }
            }
        }

        private float UpdateFindObjectTimer { get; set; }
        private float UpdateFindObjectInCoverTimer {get; set;}

        private bool FindCrouchFromCover(out float targetPose, bool useCollider = false)
        {
            targetPose = 1f;
            if ((Bot.AILimit.CurrentAILimit == AILimitSetting.Close || Bot.Enemy?.IsAI == false))
            {
                SAINEnemy enemy = Bot.Enemy;
                if (enemy?.LastKnownPosition != null)
                {
                    Vector3 position = enemy.LastKnownPosition.Value + Vector3.up;
                    if (useCollider)
                    {
                        targetPose = FindCrouchHeightColliderSphereCast(position);
                    }
                    else
                    {
                        targetPose = FindCrouchHeightRaycast(position);
                    }
                }
            }
            return targetPose < 1f;
        }

        private float FindCrouchHeightRaycast(Vector3 target, float rayLength = 4f)
        {
            const float StartHeight = 1.6f;
            const int max = 6;
            const float heightStep = 1f / max;
            LayerMask Mask = LayerMaskClass.HighPolyWithTerrainMask;

            Vector3 offset = Vector3.up * heightStep;
            Vector3 start = Bot.Transform.Position + Vector3.up * StartHeight;
            Vector3 direction = target - start;
            float targetHeight = StartHeight;
            for (int i = 0; i <= max; i++)
            {
                DebugGizmos.Ray(start, direction, Color.red, rayLength, 0.05f, true, 0.5f, true);
                if (Physics.Raycast(start, direction, rayLength, Mask))
                {
                    break;
                }
                else
                {
                    start -= offset;
                    direction = target - start;
                    targetHeight -= heightStep;
                }
            }
            return FindCrouchHeight(targetHeight);
        }

        private float FindCrouchHeightColliderSphereCast(Vector3 target, float rayLength = 3f, bool flatDir = true)
        {
            LayerMask Mask = LayerMaskClass.HighPolyWithTerrainMask;
            Vector3 start = Bot.Transform.Position + Vector3.up * 0.75f;
            Vector3 direction = target - start;
            if (flatDir)
            {
                direction.y = 0f;
            }

            float targetHeight = 1f;
            if (Physics.SphereCast(start, 0.26f, direction, out var hitInfo, rayLength, Mask))
            {
                targetHeight = hitInfo.collider.bounds.size.y;
            }
            return FindCrouchHeight(targetHeight);
        }

        private float FindCrouchHeight(float height)
        {
            const float min = 0.5f;
            return height - min;
        }
    }
}
