﻿using EFT;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Enemy
{
    public class SAINEnemyStatus : EnemyBase
    {
        public EEnemyAction VulnerableAction
        {
            get
            {
                if (EnemyIsReloading)
                {
                    return EEnemyAction.Reloading;
                }
                else if (EnemyHasGrenadeOut)
                {
                    return EEnemyAction.HasGrenade;
                }
                else if (EnemyIsHealing)
                {
                    return EEnemyAction.Healing;
                }
                else if (EnemyUsingSurgery)
                {
                    return EEnemyAction.UsingSurgery;
                }
                else
                {
                    return EEnemyAction.None;
                }
            }
            set
            {
                switch (value)
                {
                    case EEnemyAction.None:
                        break;

                    case EEnemyAction.Reloading:
                        EnemyIsReloading = true;
                        break;

                    case EEnemyAction.HasGrenade:
                        EnemyHasGrenadeOut = true;
                        break;

                    case EEnemyAction.Healing:
                        EnemyIsHealing = true;
                        break;

                    case EEnemyAction.Looting:
                        EnemyIsLooting = true;
                        break;

                    case EEnemyAction.UsingSurgery:
                        EnemyUsingSurgery = true;
                        break;

                    default:
                        break;
                }
            }
        }

        public bool PositionalFlareEnabled
        {
            get
            {
                if (Enemy.LastKnownPosition != null
                    && (Enemy.LastKnownPosition.Value - EnemyPlayer.Position).sqrMagnitude < _maxDistFromPosFlareEnabled * _maxDistFromPosFlareEnabled)
                {
                    return true;
                }
                return false;
            }
        }

        public bool HeardRecently
        {
            get
            {
                return _heardRecently.Value;
            }
            set
            {
                _heardRecently.Value = value;
            }
        }

        public bool EnemyLookingAtMe
        {
            get
            {
                if (_nextCheckEnemyLookTime < Time.time)
                {
                    _nextCheckEnemyLookTime = Time.time + 0.2f;
                    Vector3 directionToBot = (Bot.Position - EnemyPosition).normalized;
                    Vector3 enemyLookDirection = EnemyPerson.Transform.LookDirection.normalized;
                    float dot = Vector3.Dot(directionToBot, enemyLookDirection);
                    _enemyLookAtMe = dot >= 0.9f;
                }
                return _enemyLookAtMe;
            }
        }

        public bool SearchStarted
        {
            get
            {
                return _searchStarted.Value;
            }
            set
            {
                if (value)
                {
                    TimeSearchLastStarted = Time.time;
                }
                _searchStarted.Value = value;
            }
        }

        public bool ShotByEnemyRecently
        {
            get
            {
                return _shotByEnemy.Value;
            }
            set
            {
                _shotByEnemy.Value = value;
            }
        }

        public bool EnemyUsingSurgery
        {
            get
            {
                return _enemySurgery.Value;
            }
            set
            {
                _enemySurgery.Value = value;
            }
        }

        public bool EnemyIsLooting
        {
            get
            {
                return _enemyLooting.Value;
            }
            set
            {
                _enemyLooting.Value = value;
            }
        }

        public bool EnemyIsSuppressed
        {
            get
            {
                return _enemyIsSuppressed.Value;
            }
            set
            {
                _enemyIsSuppressed.Value = value;
            }
        }

        public bool ShotAtMeRecently
        {
            get
            {
                return _enemyShotAtMe.Value;
            }
            set
            {
                _enemyShotAtMe.Value = value;
            }
        }

        public bool EnemyIsReloading
        {
            get
            {
                return _enemyIsReloading.Value;
            }
            set
            {
                _enemyIsReloading.Value = value;
            }
        }

        public bool EnemyHasGrenadeOut
        {
            get
            {
                return _enemyHasGrenade.Value;
            }
            set
            {
                _enemyHasGrenade.Value = value;
            }
        }

        public bool EnemyIsHealing
        {
            get
            {
                return _enemyIsHealing.Value;
            }
            set
            {
                _enemyIsHealing.Value = value;
            }
        }

        public int NumberOfSearchesStarted { get; set; }
        public float TimeSearchLastStarted { get; private set; }
        public float TimeSinceSearchLastStarted => Time.time - TimeSearchLastStarted;

        public void RegisterShotByEnemy(DamageInfo damageInfo)
        {
            IPlayer player = damageInfo.Player?.iPlayer;
            if (player != null && 
                player.ProfileId == Enemy.EnemyProfileId)
            {
                ShotByEnemyRecently = true;
            }
        }

        public SAINEnemyStatus(SAINEnemy enemy) : base(enemy)
        {
        }

        private readonly ExpirableBool _heardRecently = new ExpirableBool(2f, 0.85f, 1.15f);
        private readonly ExpirableBool _enemyIsReloading = new ExpirableBool(4f, 0.75f, 1.25f);
        private readonly ExpirableBool _enemyHasGrenade = new ExpirableBool(4f, 0.75f, 1.25f);
        private readonly ExpirableBool _enemyIsHealing = new ExpirableBool(4f, 0.75f, 1.25f);
        private readonly ExpirableBool _enemyShotAtMe = new ExpirableBool(30f, 0.75f, 1.25f);
        private readonly ExpirableBool _enemyIsSuppressed = new ExpirableBool(4f, 0.85f, 1.15f);
        private readonly ExpirableBool _enemyLooting = new ExpirableBool(30f, 0.85f, 1.15f);
        private readonly ExpirableBool _enemySurgery = new ExpirableBool(8f, 0.85f, 1.15f);
        private readonly ExpirableBool _searchStarted = new ExpirableBool(300f, 0.85f, 1.15f);
        private readonly ExpirableBool _shotByEnemy = new ExpirableBool(2f, 0.75f, 1.25f);
        private bool _enemyLookAtMe;
        private float _nextCheckEnemyLookTime;
        private const float _maxDistFromPosFlareEnabled = 10f;
    }
}