﻿using EFT;
using SAIN.SAINComponent;
using System;
using System.Collections.Generic;
using System.Text;

namespace SAIN.Components.BotController
{
    public class BotSpawnController : SAINControl
    {
        public BotSpawnController(SAINBotController botController) : base(botController)
        {
            Instance = this;
        }

        public static BotSpawnController Instance;

        public Dictionary<string, BotComponent> SAINBotDictionary = new Dictionary<string, BotComponent>();

        public static readonly List<WildSpawnType> StrictExclusionList = new List<WildSpawnType>
        {
            WildSpawnType.bossZryachiy,
            WildSpawnType.followerZryachiy,
            WildSpawnType.peacefullZryachiyEvent,
            WildSpawnType.ravangeZryachiyEvent,
            WildSpawnType.shooterBTR,
            WildSpawnType.marksman
        };

        public void Update()
        {
            if (Subscribed &&
                GameEnding)
            {
                UnSubscribe();
            }
        }

        public bool GameEnding
        {
            get
            {
                var status = GameStatus;
                return status == GameStatus.Stopping || status == GameStatus.Stopped || status == GameStatus.SoftStopping;
            }
        }

        private GameStatus GameStatus
        {
            get
            {
                var botGame = BotController?.BotGame;
                if (botGame != null)
                {
                    return botGame.Status;
                }
                return GameStatus.Starting;
            }
        }

        public void Subscribe(BotSpawner botSpawner)
        {
            if (!Subscribed)
            {
                botSpawner.OnBotRemoved += RemoveBot;
                Subscribed = true;
            }
        }

        public void UnSubscribe()
        {
            if (Subscribed &&
                BotController?.BotSpawner != null)
            {
                BotController.BotSpawner.OnBotRemoved -= RemoveBot;
                Subscribed = false;
            }
        }

        private bool Subscribed = false;

        public BotComponent GetSAIN(BotOwner botOwner, StringBuilder debugString)
        {
            if (botOwner == null)
            {
                Logger.LogAndNotifyError("Botowner is null, cannot get SAIN!");
                debugString = null;
                return null;
            }

            if (debugString == null && SAINPlugin.DebugMode)
            {
                debugString = new StringBuilder();
            }

            string name = botOwner.name;
            BotComponent result = GetSAIN(name, debugString);

            if (result == null)
            {
                //debugString?.AppendLine( $"[{name}] not found in SAIN Bot Dictionary. Getting Component Manually..." );

                result = botOwner.gameObject.GetComponent<BotComponent>();

                if (result != null)
                {
                    //debugString?.AppendLine($"[{name}] found after using GetComponent.");
                }
            }

            if (result == null)
            {
                //debugString?.AppendLine( $"[{name}] could not be retrieved from SAIN Bots. WildSpawnType: [{botOwner.Profile.Info.Settings.Role}] Returning Null" );
            }

            if (result == null && debugString != null)
            {
                //Logger.LogAndNotifyError( debugString, EFT.Communications.ENotificationDurationType.Long );
            }
            return result;
        }

        public BotComponent GetSAIN(Player player, StringBuilder debugString)
        {
            if (debugString == null && SAINPlugin.DebugMode)
            {
                debugString = new StringBuilder();
            }

            if (player == null)
            {
                if (debugString != null)
                {
                    //debugString.AppendLine("Player is Null, cannot get SAIN!");
                    //Logger.LogAndNotifyError(debugString, EFT.Communications.ENotificationDurationType.Long);
                }
                return null;
            }

            if (player.AIData?.BotOwner != null)
            {
                return GetSAIN(player.AIData.BotOwner, debugString);
            }
            return null;
        }

        public BotComponent GetSAIN(string botName, StringBuilder debugString)
        {
            if (debugString == null && SAINPlugin.DebugMode)
            {
                debugString = new StringBuilder();
            }

            BotComponent result = null;
            if (SAINBotDictionary.ContainsKey(botName))
            {
                result = SAINBotDictionary[botName];
            }
            if (result == null)
            {
                //debugString?.AppendLine( $"[{botName}] not found in SAIN Bot Dictionary. Comparing names manually to find the bot..." );

                foreach (var bot in SAINBotDictionary)
                {
                    if (bot.Value != null && bot.Value.name == botName)
                    {
                        result = bot.Value;
                        debugString?.AppendLine($"[{botName}] found after comparing names");
                        break;
                    }
                }
            }
            if (result == null)
            {
                //debugString?.AppendLine( $"[{botName}] Still not found in SAIN Bot Dictionary. Comparing Profile Id instead..." );

                foreach (var bot in SAINBotDictionary)
                {
                    if (bot.Value != null && bot.Value.ProfileId == botName)
                    {
                        result = bot.Value;
                        //debugString?.AppendLine($"[{botName}] found after comparing profileID. Bot Name was [{bot.Value.name}]");
                        break;
                    }
                }
            }
            return result;
        }

        public void AddBot(BotOwner botOwner)
        {
            try
            {
                if (botOwner != null)
                {
                    botOwner.LeaveData.OnLeave += RemoveBot;

                    if (SAINPlugin.IsBotExluded(botOwner))
                    {
                        botOwner.GetOrAddComponent<SAINNoBushESP>().Init(botOwner);
                        return;
                    }

                    if (BotComponent.TryAddBotComponent(botOwner, out BotComponent component))
                    {
                        string name = botOwner.name;
                        if (SAINBotDictionary.ContainsKey(name))
                        {
                            SAINBotDictionary.Remove(name);
                        }
                        SAINBotDictionary.Add(name, component);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"AddBot: Add Component Error: {ex}");
            }
        }

        public void RemoveBot(BotOwner botOwner)
        {
            try
            {
                if (botOwner != null)
                {
                    SAINBotDictionary.Remove(botOwner.name);
                    if (botOwner.TryGetComponent(out BotComponent component))
                    {
                        component.Dispose();
                    }
                    if (botOwner.TryGetComponent(out SAINNoBushESP noBush))
                    {
                        UnityEngine.Object.Destroy(noBush);
                    }
                }
                else
                {
                    Logger.LogError("Bot is null, cannot dispose!");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Dispose Component Error: {ex}");
            }
        }
    }
}