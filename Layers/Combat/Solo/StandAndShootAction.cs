﻿using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.Components;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Layers.Combat.Solo
{
    public class StandAndShootAction : SAINAction
    {
        public StandAndShootAction(BotOwner bot) : base(bot, nameof(StandAndShootAction))
        {
        }

        public override void Update()
        {
            Bot.Steering.SteerByPriority();

            /*
            if (SAIN.Cover.DuckInCover())
            {
                if (_stopMoveTime < Time.time)
                {
                    SAIN.Mover.StopMove();
                }
            }
            */

            Shoot.Update();

            return;

            if (Bot.Cover.BotIsAtCoverInUse())
            {
                return;
            }
            else
            {
                bool prone = Bot.Mover.Prone.ShallProne(true);
                Bot.Mover.Prone.SetProne(prone);
            }
        }

        public override void Start()
        {
            Bot.Mover.StopMove();
            BotOwner.Mover.SprintPause(0.5f);
            _stopMoveTime = Time.time + 0.5f;
        }

        private float _stopMoveTime;

        public override void Stop()
        {
            BotOwner.Mover.MovementResume();
        }
    }
}