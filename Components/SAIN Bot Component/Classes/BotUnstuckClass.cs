﻿using BepInEx.Logging;
using EFT;
using UnityEngine;

namespace SAIN.Classes
{
    public class BotUnstuckClass : SAINBot
    {
        public BotUnstuckClass(BotOwner bot) : base(bot)
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(GetType().Name);
        }

        private RaycastHit StuckHit = new RaycastHit();
        private float DebugStuckTimer = 0f;
        private float CheckStuckTimer = 0f;
        public float TimeSinceStuck => Time.time - TimeStuck;
        public float TimeStuck { get; private set; }

        private float CheckMoveTimer = 0f;

        private Vector3 LastPos = Vector3.zero;

        private ManualLogSource Logger;

        public void Update()
        {
            if (SAIN.BotActive && !SAIN.GameIsEnding)
            {
                if (CheckMoveTimer < Time.time)
                {
                    CheckMoveTimer = Time.time + 0.33f;
                    BotIsMoving = Vector3.Distance(LastPos, BotOwner.Position) > 0.01f;
                    LastPos = BotOwner.Position;
                }

                if (CheckStuckTimer < Time.time)
                {
                    CheckStuckTimer = Time.time + 0.1f;
                    bool stuck = BotStuckOnObject() || BotStuckOnPlayer();
                    if (!BotIsStuck && stuck)
                    {
                        TimeStuck = Time.time;
                    }
                    BotIsStuck = stuck;
                }

                if (BotIsStuck)
                {
                    if (DebugStuckTimer < Time.time)
                    {
                        DebugStuckTimer = Time.time + 3f;
                        Logger.LogWarning($"[{BotOwner.name}] has been stuck for [{TimeSinceStuck}] seconds on [{StuckHit.transform.name}] object");
                    }
                    if (JumpTimer < Time.time && TimeSinceStuck > 1f)
                    {
                        JumpTimer = Time.time + 1f;
                        BotOwner.GetPlayer.MovementContext.TryJump();
                    }
                }
            }
        }

        public bool BotIsStuck { get; private set; }

        private bool CanBeStuckDecisions(SAINLogicDecision decision)
        {
            return decision == SAINLogicDecision.Search || decision == SAINLogicDecision.MoveToCover || decision == SAINLogicDecision.GroupSearch || decision == SAINLogicDecision.DogFight || decision == SAINLogicDecision.RunForCover || decision == SAINLogicDecision.RunAway || decision == SAINLogicDecision.RegroupSquad || decision == SAINLogicDecision.UnstuckSearch || decision == SAINLogicDecision.UnstuckDogFight || decision == SAINLogicDecision.UnstuckMoveToCover;
        }

        public bool BotStuckOnPlayer()
        {
            var decision = SAIN.CurrentDecision;
            if (!BotIsMoving && CanBeStuckDecisions(decision))
            {
                Vector3 botPos = BotOwner.Position;
                botPos.y += 0.1f;
                Vector3 moveDir = BotOwner.Mover.DirCurPoint;
                moveDir.y = 0;
                Vector3 lookDir = BotOwner.LookDirection;
                lookDir.y = 0;

                var moveHits = Physics.RaycastAll(botPos, moveDir, 0.5f, LayerMaskClass.PlayerMask);
                if (moveHits.Length > 0)
                {
                    foreach (var move in moveHits)
                    {
                        if (move.transform.name != BotOwner.name)
                        {
                            StuckHit = move;
                            return true;
                        }
                    }
                }

                var lookHits = Physics.RaycastAll(botPos, lookDir, 0.5f, LayerMaskClass.PlayerMask);
                if (lookHits.Length > 0)
                {
                    foreach (var look in lookHits)
                    {
                        if (look.transform.name != BotOwner.name)
                        {
                            StuckHit = look;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool BotStuckOnObject()
        {
            if (CanBeStuckDecisions(SAIN.CurrentDecision) && !BotIsMoving && !BotOwner.DoorOpener.Interacting && SAIN.Decision.TimeSinceChangeDecision > 0.5f)
            {
                Vector3 botPos = BotOwner.Position;
                botPos.y += 0.1f;
                Vector3 moveDir = BotOwner.Mover.DirCurPoint;
                moveDir.y = 0;
                if (Physics.Raycast(botPos, moveDir, out var hit, 0.25f, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    StuckHit = hit;
                    return true;
                }
            }
            return false;
        }

        public bool BotIsMoving { get; private set; }

        private float JumpTimer = 0f;
    }
}