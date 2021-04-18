using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;

namespace Wanted_Level_Restyle_2
{
    [ScriptAttributes(Author = "PietroKaro99", NoDefaultInstance = true)]
    class DispatchServiceScript : Script // Spawner script
    {
        public Dictionary<int, List<DispatchVehicle>> KeysWantedLevelList;

        public bool IsActive;
        public int MinTime, MaxTime; // Interval between spawns.

        public DispatchServiceScript()
        {
            Tick += DispatchServiceScript_OnTick;
            KeysWantedLevelList = new Dictionary<int, List<DispatchVehicle>>();
        }

        void DispatchServiceScript_OnTick(object sender, EventArgs args)
        {
            if (IsActive)
            {
                try
                {
                    int level = WlrMod.GetModWantedLevel();
                    if (level == 0) // Otherwise, the script can request a vehicle from key "0" of dictionary, throwing an exception. Key "0" does not exist.
                    {
                        return;
                    }
                    else
                    {
                        if (SpawnDispatchUnit(KeysWantedLevelList[level].FindAll(vehicle => vehicle.IsSpecialVehicle)) | SpawnDispatchUnit(KeysWantedLevelList[level].FindAll(vehicle => !vehicle.IsSpecialVehicle)))
                        {
                            Wait(WlrMod.Generator.Next(MinTime, MaxTime + 1));
                        }
                    }
                }
                catch (WlrException wex)
                {
                    if (!wex.IsFatal) // In case of vehicle spawn exception (spawned vehicle = null).
                    {
                        Wait(1000);
                    }
                    else
                    {
                        Abort();
                    }
                }
            }
        }

        void SetUnitBlip(Vehicle vehicle, string blipName, float blipScale, BlipSprite sprite, BlipColor blipColor)
        {
            Blip unitBlip = vehicle.AddBlip();
            if (sprite != BlipSprite.Plane)
            {
                unitBlip.Sprite = sprite;
            }
            unitBlip.Color = blipColor;
            unitBlip.Scale = blipScale;
            unitBlip.Name = blipName;
        }

        bool SpawnDispatchUnit(List<DispatchVehicle> unitList) // Dispatch vehicle chosen from dictionary.
        {
            if (unitList.Count == 0)
            {
                return false;
            }
            DispatchVehicle vehicle = unitList[WlrMod.Generator.Next(unitList.Count)];
            (bool spawnSuccess, Vehicle spawnedVehicle) = vehicle.SpawnModel(true);
            if (spawnSuccess)
            {
                switch (vehicle.Hash)
                {
                    case VehicleHash.Police4:
                        WlrMod.SetUnitColor(spawnedVehicle, VehicleColor.MetallicBlack, VehicleColor.MetallicBlack, VehicleColor.MetallicBlack);
                        spawnedVehicle.IsSirenSilent = true;
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.FibOffice02SMM, new WeaponHash[] { WeaponHash.Pistol50 }, null, true, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        break;
                    case VehicleHash.Frogger:
                        WlrMod.SetUnitColor(spawnedVehicle, VehicleColor.MetallicYellowBird, VehicleColor.MetallicBlazeRed, VehicleColor.MetallicWhite);
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Uscg01SMY, new WeaponHash[] { WeaponHash.MG }, null, false, false, true, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        break;
                    case VehicleHash.Apc:
                        WlrMod.SetUnitColor(spawnedVehicle, VehicleColor.UtilDarkBlue, VehicleColor.UtilDarkBlue, VehicleColor.MatteBlack);
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Swat01SMY, new WeaponHash[] { WeaponHash.MG }, null, false, false, false, VehicleWeaponHash.Invalid, -1, 1, 2);
                        break;
                    case VehicleHash.Riot2:
                        WlrMod.SetUnitColor(spawnedVehicle, VehicleColor.UtilDarkBlue, VehicleColor.UtilDarkBlue, VehicleColor.MatteBlack);
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Swat01SMY, new WeaponHash[] { WeaponHash.SpecialCarbine }, null, false, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2, 3, 4);
                        break;
                    case VehicleHash.Polmav:
                        spawnedVehicle.Mods.Livery = 0;
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Swat01SMY, new WeaponHash[] { WeaponHash.MG }, null, false, false, true, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        break;
                    case VehicleHash.Barrage:
                        WlrMod.SetUnitColor(spawnedVehicle, VehicleColor.MatteDesertTan, VehicleColor.MatteDesertTan, VehicleColor.MetallicDarkSilver);
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Marine03SMY, new WeaponHash[] { WeaponHash.Pistol50, WeaponHash.CombatMG }, null, false, false, false, VehicleWeaponHash.PlayerBullet, -1, 0, 1, 2);
                        break;
                    case VehicleHash.HalfTrack:
                        WlrMod.SetUnitColor(spawnedVehicle, VehicleColor.MatteDesertTan, VehicleColor.MatteDesertTan, VehicleColor.MetallicDarkSilver);
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Marine03SMY, new WeaponHash[] { WeaponHash.CombatMG }, null, false, false, false, VehicleWeaponHash.PlayerBullet, -1, 0, 1);
                        break;
                    case VehicleHash.Hunter:
                        WlrMod.SetUnitColor(spawnedVehicle, VehicleColor.MatteDesertTan, VehicleColor.MatteDesertTan, VehicleColor.MetallicDarkSilver);
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Pilot01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, true, true, VehicleWeaponHash.PlayerBullet, -1, 0);
                        SetUnitBlip(spawnedVehicle, "Hunter", 0.9f, BlipSprite.EnemyHelicopter, BlipColor.Red);
                        break;
                    case VehicleHash.Akula:
                        WlrMod.SetUnitColor(spawnedVehicle, VehicleColor.MetallicBlack, VehicleColor.MetallicBlack, VehicleColor.MetallicBlack);
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Pilot01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, true, true, VehicleWeaponHash.Invalid, -1, 0);
                        SetUnitBlip(spawnedVehicle, "Akula", 0.9f, BlipSprite.EnemyHelicopter, BlipColor.Red);
                        break;
                    case VehicleHash.Khanjari:
                        WlrMod.SetUnitColor(spawnedVehicle, VehicleColor.MetallicBlack, VehicleColor.MetallicBlack, VehicleColor.MetallicBlack);
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Marine03SMY, new WeaponHash[] { WeaponHash.Railgun }, null, false, false, false, VehicleWeaponHash.Tank, -1);
                        break;
                    case VehicleHash.Police:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.Pistol }, "Cop01", false, false, false, VehicleWeaponHash.Invalid, -1);
                        WlrMod.SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.PumpShotgun }, "Cop01", false, false, false, VehicleWeaponHash.Invalid, 0);
                        break;
                    case VehicleHash.Police3:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.Pistol }, "Cop01", false, false, false, VehicleWeaponHash.Invalid, -1);
                        WlrMod.SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.PumpShotgun }, "Cop01", false, false, false, VehicleWeaponHash.Invalid, 0);
                        break;
                    case VehicleHash.Sheriff:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.Pistol }, "Sheriff01", false, false, false, VehicleWeaponHash.Invalid, -1);
                        WlrMod.SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.PumpShotgun }, "Sheriff01", false, false, false, VehicleWeaponHash.Invalid, 0);
                        break;
                    case VehicleHash.Policeb:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Hwaycop01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, false, false, VehicleWeaponHash.Invalid, -1);
                        break;
                    case VehicleHash.Pranger:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.Pistol }, "Ranger01", true, false, false, VehicleWeaponHash.Invalid, -1);
                        WlrMod.SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.DoubleBarrelShotgun }, "Ranger01", true, false, false, VehicleWeaponHash.Invalid, 0);
                        break;
                    case VehicleHash.Police2:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.Pistol50 }, "Cop01", false, false, false, VehicleWeaponHash.Invalid, -1, 0);
                        break;
                    case VehicleHash.PoliceT:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.Pistol50 }, "Cop01", false, false, false, VehicleWeaponHash.Invalid, -1, 0);
                        WlrMod.SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.MicroSMG }, "Cop01", false, false, false, VehicleWeaponHash.Invalid, 1, 2);
                        break;
                    case VehicleHash.Sheriff2:
                        if (WlrMod.IsPlayerInBlaineCounty())
                        {
                            WlrMod.SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.Pistol50 }, "Sheriff01", false, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        }
                        else
                        {
                            WlrMod.SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.Pistol50 }, "Cop01", false, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        }
                        break;
                    case VehicleHash.Lguard:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Uscg01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, false, false, VehicleWeaponHash.Invalid, -1, 0);
                        break;
                    case VehicleHash.Seashark2:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Uscg01SMY, new WeaponHash[] { WeaponHash.MicroSMG }, null, false, false, true, VehicleWeaponHash.Invalid, -1);
                        break;
                    case VehicleHash.Riot:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Swat01SMY, new WeaponHash[] { WeaponHash.CarbineRifle }, null, false, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2, 3, 4, 5, 6);
                        break;
                    case VehicleHash.Dinghy3:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Swat01SMY, new WeaponHash[] { WeaponHash.MicroSMG }, null, false, false, true, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        break;
                    case VehicleHash.FBI:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.FibSec01SMM, new WeaponHash[] {WeaponHash.Pistol50, WeaponHash.CarbineRifle }, null, false, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        break;
                    case VehicleHash.FBI2:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Swat01SMY, new WeaponHash[] {WeaponHash.Pistol50, WeaponHash.CarbineRifle }, null, false, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        break;
                    case VehicleHash.Annihilator:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Swat01SMY, new WeaponHash[] { WeaponHash.MG }, null, false, false, true, VehicleWeaponHash.Invalid, -1, 0, 1, 2, 3, 4);
                        break;
                    case VehicleHash.Barracks:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Marine01SMM, new WeaponHash[] { WeaponHash.SpecialCarbine, WeaponHash.Pistol50 }, null, false, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8);
                        break;
                    case VehicleHash.Crusader:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Marine01SMM, new WeaponHash[] { WeaponHash.SpecialCarbine, WeaponHash.Pistol50 }, null, false, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        break;
                    case VehicleHash.Rhino:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Marine03SMY, new WeaponHash[] { WeaponHash.GrenadeLauncher }, null, false, false, false, VehicleWeaponHash.Invalid, -1);
                        break;
                    case VehicleHash.Insurgent3:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Marine03SMY, new WeaponHash[] { WeaponHash.CombatMG }, null, false, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2, 3, 4, 5, 6, 7);
                        break;
                    case VehicleHash.Buzzard:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Pilot01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, true, true, VehicleWeaponHash.PlayerBuzzard, -1);
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Marine03SMY, new WeaponHash[] { WeaponHash.CombatMG }, null, false, false, true, VehicleWeaponHash.Invalid, 0, 1, 2);
                        SetUnitBlip(spawnedVehicle, "Buzzard", 0.9f, BlipSprite.EnemyHelicopter, BlipColor.Red);
                        break;
                    case VehicleHash.Strikeforce:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Pilot01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, true, true, VehicleWeaponHash.Invalid, -1);
                        SetUnitBlip(spawnedVehicle, "Strikeforce", 1.0f, BlipSprite.Plane, BlipColor.Red);
                        break;
                    case VehicleHash.Hydra:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Pilot01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, true, true, VehicleWeaponHash.Invalid, -1);
                        Function.Call(Hash._SET_VEHICLE_VERTICAL_FLIGHT_PHASE, spawnedVehicle, 0.0f);
                        SetUnitBlip(spawnedVehicle, "Hydra", 1.0f, BlipSprite.Plane, BlipColor.Red);
                        break;
                    case VehicleHash.Lazer:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Pilot01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, true, true, VehicleWeaponHash.Invalid, -1);
                        SetUnitBlip(spawnedVehicle, "Lazer", 1.0f, BlipSprite.Plane, BlipColor.Red);
                        break;
                    case VehicleHash.Savage:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Pilot01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, true, true, VehicleWeaponHash.PlayerBullet, -1, 0);
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Marine03SMY, new WeaponHash[] { WeaponHash.CombatMG }, null, false, true, true, VehicleWeaponHash.Invalid, 1, 2);
                        SetUnitBlip(spawnedVehicle, "Savage", 0.9f, BlipSprite.EnemyHelicopter, BlipColor.Red);
                        break;
                    case VehicleHash.Annihilator2:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Pilot01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, true, true, VehicleWeaponHash.PlayerBuzzard, -1);
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Marine01SMM, new WeaponHash[] { WeaponHash.CombatMG }, null, false, false, true, VehicleWeaponHash.Invalid, 0, 1, 2, 3, 4);
                        SetUnitBlip(spawnedVehicle, "Stealth Annihilator", 0.9f, BlipSprite.EnemyHelicopter, BlipColor.Red);
                        break;
                    case VehicleHash.PatrolBoat:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Marine01SMM, new WeaponHash[] {WeaponHash.MicroSMG, WeaponHash.SpecialCarbine }, null, false, false, true, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        break;
                    case VehicleHash.Dinghy5:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Swat01SMY, new WeaponHash[] { WeaponHash.MicroSMG }, null, false, false, true, VehicleWeaponHash.Invalid, -1, 3);
                        WlrMod.SetUnitColor(spawnedVehicle, VehicleColor.MetallicBlack, VehicleColor.MetallicBlack, VehicleColor.MetallicBlack);
                        break;
                    case VehicleHash.Predator:
                        WlrMod.SetNpcsInUnit(spawnedVehicle, PedHash.Uscg01SMY, new WeaponHash[] { WeaponHash.MicroSMG }, null, false, false, true, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        break;
                }
                return true;
            }
            return false;
        }
    }
}
