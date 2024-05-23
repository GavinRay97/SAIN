﻿using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using SAIN.Components;
using UnityEngine;
using SAIN.Helpers;

namespace SAIN.Layers.Combat.Solo
{
    internal class RushEnemyAction : SAINAction
    {
        public RushEnemyAction(BotOwner bot) : base(bot, nameof(RushEnemyAction))
        {
        }

        private float TryJumpTimer;

        public override void Update()
        {
            Bot.Mover.SetTargetPose(1f);
            Bot.Mover.SetTargetMoveSpeed(1f);
            Shoot.Update();

            if (Bot.Enemy == null)
            {
                Bot.Steering.SteerByPriority(true);
                return;
            }

            if (Bot.Enemy.InLineOfSight)
            {
                if (Bot.Info.PersonalitySettings.CanJumpCorners)
                {
                    if (_shallBunnyHop)
                    {
                        Bot.Mover.TryJump();
                    }
                    else if (TryJumpTimer < Time.time)
                    {
                        TryJumpTimer = Time.time + 5f;
                        if (EFTMath.RandomBool(Bot.Info.PersonalitySettings.JumpCornerChance))
                        {
                            if (!_shallBunnyHop && EFTMath.RandomBool(5))
                            {
                                _shallBunnyHop = true;
                            }
                            Bot.Mover.TryJump();
                        }
                    }
                }

                Bot.Mover.Sprint(false);
                Bot.Mover.SprintController.CancelRun();

                Bot.Mover.DogFight.DogFightMove();
                if (Bot.Enemy.IsVisible && Bot.Enemy.CanShoot)
                {
                    Bot.Steering.SteerByPriority();
                }
                else
                {
                    Bot.Steering.LookToEnemy(Bot.Enemy);
                }
                return;
            }

            Vector3[] EnemyPath = Bot.Enemy.PathToEnemy.corners;
            Vector3 EnemyPos = Bot.Enemy.EnemyPosition;
            if (NewDestTimer < Time.time)
            {
                Vector3 Destination = EnemyPos;
                if (Bot.Enemy.Path.PathDistance > 5f 
                    && Bot.Mover.SprintController.RunToPoint(Destination))
                {
                    NewDestTimer = Time.time + 2f;
                }
                else if (Bot.Mover.GoToPoint(Destination, out _))
                {
                    NewDestTimer = Time.time + 1f;
                }
                else
                {
                    NewDestTimer = Time.time + 0.25f;
                }
            }

            if (_shallTryJump && TryJumpTimer < Time.time && Bot.Enemy.Path.PathDistance > 5f)
            {
                var corner = Bot.Enemy?.LastCornerToEnemy;
                if (corner != null)
                {
                    float distance = (corner.Value - BotOwner.Position).magnitude;
                    if (distance < 0.5f)
                    {
                        TryJumpTimer = Time.time + 3f;
                        if (EFTMath.RandomBool(Bot.Info.PersonalitySettings.JumpCornerChance))
                        {
                            Bot.Mover.TryJump();
                        }
                    }
                }
            }

        }

        private bool _shallBunnyHop = false;
        private float NewDestTimer = 0f;
        private Vector3? PushDestination;

        public override void Start()
        {
            _shallTryJump = Bot.Info.PersonalitySettings.CanJumpCorners 
                && Bot.Decision.CurrentSquadDecision != SquadDecision.PushSuppressedEnemy
                && EFTMath.RandomBool(Bot.Info.PersonalitySettings.JumpCornerChance);

            _shallBunnyHop = false;
        }

        bool _shallTryJump = false;

        public override void Stop()
        {
            Bot.Mover.SprintController.CancelRun();
        }
    }
}