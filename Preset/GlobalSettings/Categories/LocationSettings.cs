﻿using EFT;
using EFT.InventoryLogic;
using Interpolation;
using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Preset.Personalities;
using SAIN.SAINComponent.Classes.Info;
using System.Collections.Generic;
using System.Data;
using static EFT.Player;
using static SAIN.Preset.Personalities.PersonalitySettingsClass;

namespace SAIN.Preset.GlobalSettings
{
    public class LocationSettings
    {
        public bool Enabled = true;
        public float VisionSpeed = 1f;
        public float VisionDistance = 1f;
        public float AimScatter = 1f;
        public float AimAccuracy = 1f;
        public float Aggression = 1f;
        public bool EnablePersonalityOverrides = true;
        public Dictionary<EPersonality, PersonalityVariablesClass> LocationPersonalitySettings = new Dictionary<EPersonality, PersonalityVariablesClass>();
    }

    public class PowerCalcSettings
    {
        public float PMC_POWER = 20f;
        public float SCAV_POWER = -20f;

        public float SHOTGUN_POWER = 40f;
        public float SMG_POWER = 75f;
        public float ASSAULT_CARBINE_POWER = 60f;
        public float ASSAULT_RIFLE_POWER = 45f;
        public float MG_POWER = 55f;
        public float SNIPE_POWER = -20f;
        public float MARKSMAN_RIFLE_POWER = 10f;
        public float PISTOL_POWER = -10f;

        public float RED_DOT_POWER = 30f;
        public float OPTIC_POWER = -10f;
        //public float OPTIC_ZOOM_COEF = 2f;
        public float SUPPRESSOR_POWER = 20f;

        public float ARMOR_CLASS_COEF = 30f;
        public float ARMOR_CLASS_COEF_REALISM = 20f;

        public float HELMET_POWER = 30f;
        public float HELMET_HEAVY_POWER = 60f;
        public float FACESHIELD_POWER = 30f;
        //public float ATTACHMENT_POWER = 5f;
        public float EARPRO_POWER = 30f;

        public bool CalcPower(Player player, out float power)
        {
            power = 0f;
            if (player == null)
            {
                return false;
            }

            power += WeaponPower(player);
            if (power == 0f)
            {
                return false;
            }

            power += RolePower(player.Profile.Info.Settings.Role);
            power += ArmorPower(player);

            //Logger.LogAndNotifyInfo($"Calculated Power: [{power}] for [{player.Profile.Nickname}]");

            player.AIData.PowerOfEquipment = power;

            return true;
        }

        private float RolePower(WildSpawnType type)
        {
            if (_PMCS.Contains(type))
            {
                return PMC_POWER;
            }
            else if (_SCAVS.Contains(type))
            {
                return SCAV_POWER;
            }
            return 0f;
        }

        private float WeaponPower(Player player)
        {
            float result = 0f;

            PlayerComponent component = GameWorldComponent.Instance?.PlayerTracker.GetPlayerComponent(player?.ProfileId);
            if (component == null)
            {
                Logger.LogError("Player Component Null");
                return result;
            }

            WeaponInfo weaponInfo = component.Equipment.CurrentWeapon;
            if (weaponInfo == null)
            {
                Logger.LogError("weaponInfo Null");
                return result;
            }

            if (weaponInfo.HasSuppressor)
            {
                result += SUPPRESSOR_POWER;
            }
            if (weaponInfo.HasRedDot)
            {
                result += RED_DOT_POWER;
            }
            if (weaponInfo.HasOptic)
            {
                result += OPTIC_POWER;
            }

            switch (weaponInfo.WeaponClass)
            {
                case IWeaponClass.pistol:
                    result += PISTOL_POWER;
                    break;
                case IWeaponClass.smg:
                    result += SMG_POWER;
                    break;
                case IWeaponClass.assaultCarbine:
                    result += ASSAULT_CARBINE_POWER;
                    break;
                case IWeaponClass.assaultRifle:
                    result += ASSAULT_RIFLE_POWER;
                    break;
                case IWeaponClass.machinegun:
                    result += MG_POWER;
                    break;
                case IWeaponClass.marksmanRifle:
                    result += MARKSMAN_RIFLE_POWER;
                    break;
                case IWeaponClass.sniperRifle:
                    result += SNIPE_POWER;
                    break;
                case IWeaponClass.shotgun:
                    result += SHOTGUN_POWER;
                    break;
                default:
                    break;
            }
            return result;
        }

        private float ArmorPower(Player player)
        {
            armorComponents.Clear();
            float result = 0;
            var equipment = player.Inventory?.Equipment;
            if (equipment != null)
            {
                var armorVest = equipment.GetSlot(EFT.InventoryLogic.EquipmentSlot.ArmorVest)?.ContainedItem;
                if (armorVest != null)
                {
                    armorVest.GetItemComponentsInChildrenNonAlloc(armorComponents, true);
                    float highestArmorClass = FindHighestArmorClass(armorComponents);
                    //Logger.LogInfo($"Armor Components in Vest: [{armorComponents.Count}] Highest Armor Class: [{highestArmorClass}] Class Coef: [{ArmorClassCoef}] Combined: [{highestArmorClass * ArmorClassCoef}]");
                    result += highestArmorClass * ArmorClassCoef;
                    armorComponents.Clear();
                }
                else
                {
                    var rig = equipment.GetSlot(EFT.InventoryLogic.EquipmentSlot.TacticalVest)?.ContainedItem;
                    if (rig != null)
                    {
                        rig.GetItemComponentsInChildrenNonAlloc(armorComponents, true);
                        if (armorComponents.Count > 0)
                        {
                            float highestArmorClass = FindHighestArmorClass(armorComponents);
                            //Logger.LogInfo($"Armor Components in Rig: [{armorComponents.Count}] Highest Armor Class: [{highestArmorClass}] Class Coef: [{ArmorClassCoef}] Combined: [{highestArmorClass * ArmorClassCoef}]");
                            result += highestArmorClass * ArmorClassCoef;
                            armorComponents.Clear();
                        }
                    }
                }

                var helmet = equipment.GetSlot(EFT.InventoryLogic.EquipmentSlot.Headwear)?.ContainedItem;
                if (helmet != null)
                {
                    helmet.GetItemComponentsInChildrenNonAlloc(armorComponents, true);
                    if (armorComponents.Count > 0)
                    {
                        float highestArmorClass = FindHighestArmorClass(armorComponents);
                        if (highestArmorClass > 4)
                        {
                            result += HELMET_HEAVY_POWER;
                        }
                        else if (highestArmorClass > 1)
                        {
                            result += HELMET_POWER;
                        }
                        //Logger.LogInfo($"Armor Components in Helmet: [{armorComponents.Count}] Highest Armor Class: [{highestArmorClass}]");
                        armorComponents.Clear();
                    }
                }

                var faceProtection = equipment.GetSlot(EFT.InventoryLogic.EquipmentSlot.FaceCover)?.ContainedItem;
                if (faceProtection != null)
                {
                    faceProtection.GetItemComponentsInChildrenNonAlloc(armorComponents, true);
                    if (armorComponents.Count > 0)
                    {
                        result += FACESHIELD_POWER;
                    }
                }
                var earPro = equipment.GetSlot(EFT.InventoryLogic.EquipmentSlot.Earpiece)?.ContainedItem;
                if (earPro != null)
                {
                    result += EARPRO_POWER;
                }
            }
            armorComponents.Clear();
            //Logger.LogInfo($"Armor Power Result: [{result}]");
            return result;
        }


        private float FindHighestArmorClass(List<ArmorComponent> armorComponents)
        {
            float result = 0f;
            foreach (var armorComponent in armorComponents)
            {
                float armorClass = armorComponent.ArmorClass;
                if (armorClass > result)
                {
                    result = armorClass;
                }
            }
            return result;
        }

        [JsonIgnore]
        private float ArmorClassCoef
        {
            get
            {
                if (ModDetection.RealismLoaded)
                {
                    return ARMOR_CLASS_COEF_REALISM;
                }
                return ARMOR_CLASS_COEF;
            }
        }

        [JsonIgnore]
        private static readonly List<ArmorComponent> armorComponents = new List<ArmorComponent>();

        [JsonIgnore]
        private static readonly List<WildSpawnType> _PMCS = new List<WildSpawnType> 
        { 
            EnumValues.WildSpawn.Usec, 
            EnumValues.WildSpawn.Bear 
        };

        [JsonIgnore]
        private static readonly List<WildSpawnType> _SCAVS = new List<WildSpawnType> 
        { 
            WildSpawnType.assault, 
            WildSpawnType.cursedAssault, 
            WildSpawnType.assaultGroup, 
            WildSpawnType.crazyAssaultEvent, 
            WildSpawnType.marksman 
        };
    }
}