﻿using System.Collections.Generic;
using UnityEngine;
using SAIN.SAINComponent.Classes.Enemy;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class DogFightDecisionClass : SAINBase
    {
        public DogFightDecisionClass(BotComponent bot) : base(bot) { }

        public bool ShallDogFight()
        {
            if (checkDecisions() &&
                findDogFightTarget())
            {
                return true;
            }
            else
            {
                clearDogFightTarget();
                return false;
            }
        }

        private void clearDogFightTarget()
        {
            if (DogFightTarget != null)
            {
                DogFightTarget = null;
            }
        }

        private bool checkDecisions()
        {
            if (!BotOwner.WeaponManager.HaveBullets)
            {
                return false;
            }
            SoloDecision currentDecision = Bot.Decision.CurrentSoloDecision;
            if (currentDecision == SoloDecision.RushEnemy)
            {
                return false;
            }
            if (currentDecision == SoloDecision.Retreat || currentDecision == SoloDecision.RunToCover)
            {
                bool lowOnAmmo = Bot.Decision.SelfActionDecisions.LowOnAmmo(0.3f);
                if (lowOnAmmo)
                {
                    return false;
                }
            }
            return true;
        }

        private bool shallClearDogfightTarget(SAINEnemy enemy)
        {
            if (enemy == null || 
                !enemy.ShallUpdateEnemy || 
                enemy.Player?.HealthController.IsAlive == false)
            {
                return true;
            }
            float pathDist = enemy.Path.PathDistance;
            if (pathDist > _dogFightEndDist)
            {
                return true;
            }
            return !enemy.IsVisible && enemy.TimeSinceSeen > 2f;
        }

        private bool findDogFightTarget()
        {
            if (DogFightTarget != null)
            {
                if (shallDogFightEnemy(DogFightTarget))
                {
                    return true;
                }
                if (shallClearDogfightTarget(DogFightTarget))
                {
                    DogFightTarget = null;
                }
            }

            if (_changeDFTargetTime < Time.time)
            {
                _changeDFTargetTime = Time.time + 0.5f;

                clearDFTargets();
                SAINEnemy newTarget = selectDFTarget();
                if (newTarget != null)
                {
                    DogFightTarget = newTarget;
                    return true;
                }

                getNewDFTargets();
                DogFightTarget = selectDFTarget();

                return DogFightTarget != null;
            }

            return DogFightTarget != null;
        }

        private float _changeDFTargetTime;

        private void clearDFTargets()
        {
            int count = _dogFightTargets.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                if (shallClearDogfightTarget(_dogFightTargets[i]))
                {
                    _dogFightTargets.RemoveAt(i);
                }
            }
        }

        private void getNewDFTargets()
        {
            _dogFightTargets.Clear();

            var enemies = Bot.EnemyController.Enemies;
            foreach (var enemy in enemies.Values)
            {
                if (shallDogFightEnemy(enemy))
                {
                    _dogFightTargets.Add(enemy);
                }
            }
        }

        private SAINEnemy selectDFTarget()
        {
            int count = _dogFightTargets.Count;
            if (count > 0)
            {
                if (count > 1)
                {
                    _dogFightTargets.Sort((x, y) => x.RealDistance.CompareTo(y.RealDistance));
                }
                return _dogFightTargets[0];
            }
            return null;
        }

        private readonly List<SAINEnemy> _dogFightTargets = new List<SAINEnemy>();

        public SAINEnemy DogFightTarget { get; set; }

        private bool shallDogFightEnemy(SAINEnemy enemy)
        {
            return enemy?.IsValid == true && 
                enemy.IsVisible && 
                enemy.ShallUpdateEnemy && 
                enemy.Path.PathDistance < _dogFightStartDist;
        }

        private float _dogFightStartDist = 4f;
        private float _dogFightEndDist = 10f;
    }
}
