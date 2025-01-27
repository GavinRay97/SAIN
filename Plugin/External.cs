﻿using Comfort.Common;
using EFT;
using HarmonyLib;
using SAIN.Components;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.Enemy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using SAIN.Plugin;

namespace SAIN.Plugin
{
    public static class External
    {
        public static bool ExtractBot(BotOwner bot)
        {
            var component = bot.GetComponent<BotComponent>();
            if (component == null)
            {
                return false;
            }

            component.Info.ForceExtract = true;

            return true;
        }

        public static void GetExtractedBots(List<string> list)
        {
            var botController = SAINPlugin.BotController;
            if (botController == null)
            {
                Logger.LogWarning("SAIN Bot Controller is Null, cannot retrieve Extracted Bots List.");
                return;
            }
            var extractedBots = botController.BotExtractManager?.ExtractedBots;
            if (extractedBots == null)
            {
                Logger.LogWarning("List of extracted bots is null! Cannot copy list.");
                return;
            }
            list.Clear();
            list.AddRange(extractedBots);
        }

        public static void GetExtractionInfos(List<ExtractionInfo> list)
        {
            var botController = SAINPlugin.BotController;
            if (botController == null)
            {
                Logger.LogWarning("SAIN Bot Controller is Null, cannot retrieve Extracted Bots List.");
                return;
            }
            var extractedBots = botController.BotExtractManager?.BotExtractionInfos;
            if (extractedBots == null)
            {
                Logger.LogWarning("List of extracted bots is null! Cannot copy list.");
                return;
            }
            list.Clear();
            list.AddRange(extractedBots);
        }

        public static bool TrySetExfilForBot(BotOwner bot)
        {
            var component = bot.GetComponent<BotComponent>();
            if (component == null)
            {
                return false;
            }

            if (!Components.BotController.BotExtractManager.IsBotAllowedToExfil(component))
            {
                Logger.LogWarning($"{bot.name} is not allowed to use extracting logic.");
            }

            if (!SAINPlugin.BotController.BotExtractManager.TryFindExfilForBot(component))
            {
                return false;
            }

            return true;
        }

        private static bool DebugExternal => SAINPlugin.EditorDefaults.DebugExternal;

        public static bool ResetDecisionsForBot(BotOwner bot)
        {
            var component = bot.GetComponent<BotComponent>();
            if (component == null)
            {
                return false;
            }

            // Do not do anything if the bot is currently in combat
            if (isBotInCombat(component, out ECombatReason reason))
            {
                if (DebugExternal)
                    Logger.LogInfo($"{bot.name} is currently engaging an enemy; cannot reset its decisions. Reason: [{reason}]");

                return true;
            }

            if (IsBotSearching(component))
            {
                if (DebugExternal)
                    Logger.LogInfo($"{bot.name} is currently searching and hasn't cleared last known position, cannot reset its decisions.");

                return false;
            }

            if (DebugExternal)
                Logger.LogInfo($"Forcing {bot.name} to reset its decisions...");

            PropertyInfo enemyLastSeenTimeSenseProperty = AccessTools.Property(typeof(BotSettingsClass), "EnemyLastSeenTimeSense");
            if (enemyLastSeenTimeSenseProperty == null)
            {
                Logger.LogError($"Could not reset EnemyLastSeenTimeSense for {bot.name}'s enemies");
                return false;
            }

            // Force the bot to think it has not seen any enemies in a long time
            foreach (IPlayer player in bot.BotsGroup.Enemies.Keys)
            {
                bot.BotsGroup.Enemies[player].Clear();
                enemyLastSeenTimeSenseProperty.SetValue(bot.BotsGroup.Enemies[player], 1);
            }

            // Until the bot next identifies an enemy, do not search anywhere
            component.Decision.GoalTargetDecisions.IgnorePlaceTarget = true;

            // Force the bot to "forget" what it was doing
            bot.Memory.GoalTarget.Clear();
            bot.Memory.GoalEnemy = null;
            component.EnemyController.ClearEnemy();
            component.Decision.ResetDecisions(true);

            return true;
        }

        public static float TimeSinceSenseEnemy(BotOwner botOwner)
        {
            var component = botOwner.GetComponent<BotComponent>();
            if (component == null)
            {
                return float.MaxValue;
            }

            SAINEnemy enemy = component.Enemy;
            if (enemy == null)
            {
                return float.MaxValue;
            }

            return enemy.TimeSinceLastKnownUpdated;
        }

        public static bool IsPathTowardEnemy(NavMeshPath path, BotOwner botOwner, float ratioSameOverAll = 0.25f, float sqrDistCheck = 0.05f)
        {
            var component = botOwner.GetComponent<BotComponent>();
            if (component == null)
            {
                return false;
            }

            SAINEnemy enemy = component.Enemy;
            if (enemy == null)
            {
                return false;
            }

            // Compare the corners in both paths, and check if the nodes used in each are the same.
            if (SAINBotSpaceAwareness.ArePathsDifferent(path, enemy.Path.PathToEnemy, ratioSameOverAll, sqrDistCheck))
            {
                return false;
            }

            return true;
        }

        public static bool CanBotQuest(BotOwner botOwner, Vector3 questPosition, float dotProductThresh = 0.33f)
        {
            var component = botOwner.GetComponent<BotComponent>();
            if (component == null)
            {
                return false;
            }
            if (isBotInCombat(component, out var reason))
            {
                if (DebugExternal)
                    Logger.LogInfo($"{botOwner.name} is currently engaging an enemy, cannot quest. Reason: [{reason}]");

                return false;
            }
            if (IsBotSearching(component))
            {
                if (DebugExternal)
                    Logger.LogInfo($"{botOwner.name} is currently searching and hasn't cleared last known position, cannot quest.");

                return false;
            }
            return true;
        }

        public static bool IsQuestTowardTarget(BotComponent component, Vector3 questPosition, float dotProductThresh)
        {
            Vector3? currentTarget = component.CurrentTargetPosition;
            if (currentTarget == null)
            {
                return false;
            }

            Vector3 botPosition = component.Position;
            Vector3 targetDirection = currentTarget.Value - botPosition;
            Vector3 questDirection = questPosition - botPosition;

            return Vector3.Dot(targetDirection.normalized, questDirection.normalized) > dotProductThresh;
        }

        private static bool IsBotSearching(BotComponent component)
        {
            if (component.Decision.CurrentSoloDecision == SoloDecision.Search || component.Decision.CurrentSquadDecision == SquadDecision.Search)
            {
                return !component.Search.SearchedTargetPosition;
            }
            return false;
        }

        private static bool isBotInCombat(BotComponent component, out ECombatReason reason)
        {
            const float TimeSinceSeenThreshold = 10f;
            const float TimeSinceHeardThreshold = 5f;
            const float TimeSinceUnderFireThreshold = 10f;

            reason = ECombatReason.None;
            SAINEnemy enemy = component?.EnemyController?.ActiveEnemy;
            if (enemy == null)
            {
                return false;
            }
            if (enemy.IsVisible)
            {
                reason = ECombatReason.EnemyVisible;
                return true;
            }
            if (enemy.TimeSinceSeen < TimeSinceSeenThreshold)
            {
                reason = ECombatReason.EnemySeenRecently;
                return true;
            }
            if (enemy.TimeSinceHeard < TimeSinceHeardThreshold)
            {
                reason = ECombatReason.EnemyHeardRecently;
                return true;
            }
            BotMemoryClass memory = component.BotOwner.Memory;
            if (memory.IsUnderFire)
            {
                reason = ECombatReason.UnderFireNow;
                return true;
            }
            if (memory.UnderFireTime + TimeSinceUnderFireThreshold < Time.time)
            {
                reason = ECombatReason.UnderFireRecently;
                return true;
            }
            return false;
        }

        public enum ECombatReason
        {
            None = 0,
            EnemyVisible = 1,
            EnemyHeardRecently = 2,
            EnemySeenRecently = 3,
            UnderFireNow = 4,
            UnderFireRecently = 5,
        }
    }
}
