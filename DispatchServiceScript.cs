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

        readonly Random _Generator;
        public bool IsActive;
        public int MinTime, MaxTime; // Interval between spawns.

        public DispatchServiceScript()
        {
            Tick += DispatchServiceScript_OnTick;
            KeysWantedLevelList = new Dictionary<int, List<DispatchVehicle>>();
            _Generator = new Random();
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
                            Wait(_Generator.Next(MinTime, MaxTime + 1));
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

        void SetUnitColor(Vehicle vehicle, VehicleColor primary, VehicleColor secondary, VehicleColor pearlescent)
        {
            vehicle.Mods.PrimaryColor = primary;
            vehicle.Mods.SecondaryColor = secondary;
            vehicle.Mods.PearlescentColor = pearlescent;
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

        PedHash CasualGender(string pedHash) // It chooses a casual gender for ped (ex: used for peds in Sheriff SUV).
        {
            int result = _Generator.Next(2);
            PedHash pedResult;
            if (result == 0)
            {
                Enum.TryParse(pedHash + "SMY", out pedResult);
                return pedResult;
            }
            else
            {
                Enum.TryParse(pedHash + "SFY", out pedResult);
                return pedResult;
            }
        }

        void SetNpcsInUnit(Vehicle vehicle, Model npcModel, WeaponHash[] weaponsForUnit, string womenMenNpcs, bool setAsCops, bool armyRelationship, bool taskCombatPlayer, VehicleWeaponHash vehicleWeapon, params int[] seats) // To spawn peds in spawned vehicle.
        {
            try
            {
                foreach (int seat in seats)
                {
                    PedHash pedModel;
                    if (womenMenNpcs != null)
                    {
                        pedModel = CasualGender(womenMenNpcs);
                    }
                    else
                    {
                        pedModel = npcModel;
                    }
                    Ped spawnedNpc = vehicle.CreatePedOnSeat((VehicleSeat)seat, pedModel);
                    SetRandomPedBehaviours(spawnedNpc, vehicle);
                    foreach (WeaponHash weapon in weaponsForUnit)
                    {
                        spawnedNpc.Weapons.Give(weapon, 9999, false, true).InfiniteAmmo = true;
                    }
                    if (setAsCops)
                    {
                        Function.Call(Hash.SET_PED_AS_COP, spawnedNpc, true);
                    }
                    if (armyRelationship)
                    {
                        Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, spawnedNpc, Function.Call<int>(Hash.GET_HASH_KEY, "ARMY"));
                    }
                    if (vehicleWeapon != VehicleWeaponHash.Invalid)
                    {
                        spawnedNpc.VehicleWeapon = vehicleWeapon;
                    }
                    if (taskCombatPlayer)
                    {
                        spawnedNpc.AlwaysKeepTask = true;
                        spawnedNpc.BlockPermanentEvents = true;
                        spawnedNpc.Task.FightAgainst(Game.Player.Character);
                    }
                }
            }
            catch (NullReferenceException)
            {
                foreach (Ped occupant in vehicle.Occupants) // Delete vehicle and its occupants in case of exception.
                {
                    occupant.Delete();
                }
                vehicle.Delete();
                throw new WlrException(null, false);
            }
        }

        bool SpawnDispatchUnit(List<DispatchVehicle> unitList) // Dispatch vehicle chosen from dictionary.
        {
            if (unitList.Count == 0)
            {
                return false;
            }
            DispatchVehicle vehicle = unitList[_Generator.Next(unitList.Count)];
            (bool spawnSuccess, Vehicle spawnedVehicle) = vehicle.SpawnModel(true);
            if (spawnSuccess)
            {
                switch (vehicle.Hash)
                {
                    case VehicleHash.Police4:
                        SetUnitColor(spawnedVehicle, VehicleColor.MetallicBlack, VehicleColor.MetallicBlack, VehicleColor.MetallicBlack);
                        spawnedVehicle.IsSirenSilent = true;
                        SetNpcsInUnit(spawnedVehicle, PedHash.FibOffice02SMM, new WeaponHash[] { WeaponHash.Pistol50 }, null, true, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        break;
                    case VehicleHash.Frogger:
                        SetUnitColor(spawnedVehicle, VehicleColor.MetallicYellowBird, VehicleColor.MetallicBlazeRed, VehicleColor.MetallicWhite);
                        SetNpcsInUnit(spawnedVehicle, PedHash.Uscg01SMY, new WeaponHash[] { WeaponHash.MG }, null, false, false, true, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        break;
                    case VehicleHash.Apc:
                        SetUnitColor(spawnedVehicle, VehicleColor.UtilDarkBlue, VehicleColor.UtilDarkBlue, VehicleColor.MatteBlack);
                        SetNpcsInUnit(spawnedVehicle, PedHash.Swat01SMY, new WeaponHash[] { WeaponHash.MG }, null, false, false, false, VehicleWeaponHash.Invalid, -1, 1, 2);
                        break;
                    case VehicleHash.Riot2:
                        SetUnitColor(spawnedVehicle, VehicleColor.UtilDarkBlue, VehicleColor.UtilDarkBlue, VehicleColor.MatteBlack);
                        SetNpcsInUnit(spawnedVehicle, PedHash.Swat01SMY, new WeaponHash[] { WeaponHash.SpecialCarbine }, null, false, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2, 3, 4);
                        break;
                    case VehicleHash.Polmav:
                        spawnedVehicle.Mods.Livery = 0;
                        SetNpcsInUnit(spawnedVehicle, PedHash.Swat01SMY, new WeaponHash[] { WeaponHash.MG }, null, false, false, true, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        break;
                    case VehicleHash.Barrage:
                        SetUnitColor(spawnedVehicle, VehicleColor.MatteDesertTan, VehicleColor.MatteDesertTan, VehicleColor.MetallicDarkSilver);
                        SetNpcsInUnit(spawnedVehicle, PedHash.Marine03SMY, new WeaponHash[] { WeaponHash.Pistol50, WeaponHash.CombatMG }, null, false, false, false, VehicleWeaponHash.PlayerBullet, -1, 0, 1, 2);
                        break;
                    case VehicleHash.HalfTrack:
                        SetUnitColor(spawnedVehicle, VehicleColor.MatteDesertTan, VehicleColor.MatteDesertTan, VehicleColor.MetallicDarkSilver);
                        SetNpcsInUnit(spawnedVehicle, PedHash.Marine03SMY, new WeaponHash[] { WeaponHash.CombatMG }, null, false, false, false, VehicleWeaponHash.PlayerBullet, -1, 0, 1);
                        break;
                    case VehicleHash.Hunter:
                        SetUnitColor(spawnedVehicle, VehicleColor.MatteDesertTan, VehicleColor.MatteDesertTan, VehicleColor.MetallicDarkSilver);
                        SetNpcsInUnit(spawnedVehicle, PedHash.Pilot01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, true, true, VehicleWeaponHash.PlayerBullet, -1, 0);
                        SetUnitBlip(spawnedVehicle, "Hunter", 0.9f, BlipSprite.EnemyHelicopter, BlipColor.Red);
                        break;
                    case VehicleHash.Akula:
                        SetUnitColor(spawnedVehicle, VehicleColor.MetallicBlack, VehicleColor.MetallicBlack, VehicleColor.MetallicBlack);
                        SetNpcsInUnit(spawnedVehicle, PedHash.Pilot01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, true, true, VehicleWeaponHash.Invalid, -1, 0);
                        SetUnitBlip(spawnedVehicle, "Akula", 0.9f, BlipSprite.EnemyHelicopter, BlipColor.Red);
                        break;
                    case VehicleHash.Khanjari:
                        SetUnitColor(spawnedVehicle, VehicleColor.MetallicBlack, VehicleColor.MetallicBlack, VehicleColor.MetallicBlack);
                        SetNpcsInUnit(spawnedVehicle, PedHash.Marine03SMY, new WeaponHash[] { WeaponHash.Railgun }, null, false, false, false, VehicleWeaponHash.Tank, -1);
                        break;
                    case VehicleHash.Police:
                        SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.Pistol }, "Cop01", false, false, false, VehicleWeaponHash.Invalid, -1);
                        SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.PumpShotgun }, "Cop01", false, false, false, VehicleWeaponHash.Invalid, 0);
                        break;
                    case VehicleHash.Police3:
                        SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.Pistol }, "Cop01", false, false, false, VehicleWeaponHash.Invalid, -1);
                        SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.PumpShotgun }, "Cop01", false, false, false, VehicleWeaponHash.Invalid, 0);
                        break;
                    case VehicleHash.Sheriff:
                        SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.Pistol }, "Sheriff01", false, false, false, VehicleWeaponHash.Invalid, -1);
                        SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.PumpShotgun }, "Sheriff01", false, false, false, VehicleWeaponHash.Invalid, 0);
                        break;
                    case VehicleHash.Policeb:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Hwaycop01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, false, false, VehicleWeaponHash.Invalid, -1);
                        break;
                    case VehicleHash.Pranger:
                        SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.Pistol }, "Ranger01", true, false, false, VehicleWeaponHash.Invalid, -1);
                        SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.DoubleBarrelShotgun }, "Ranger01", true, false, false, VehicleWeaponHash.Invalid, 0);
                        break;
                    case VehicleHash.Police2:
                        SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.Pistol50 }, "Cop01", false, false, false, VehicleWeaponHash.Invalid, -1, 0);
                        break;
                    case VehicleHash.PoliceT:
                        SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.Pistol50 }, "Cop01", false, false, false, VehicleWeaponHash.Invalid, -1, 0);
                        SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.MicroSMG }, "Cop01", false, false, false, VehicleWeaponHash.Invalid, 1, 2);
                        break;
                    case VehicleHash.Sheriff2:
                        if (WlrMod.IsPlayerInBlaineCounty())
                        {
                            SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.Pistol50 }, "Sheriff01", false, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        }
                        else
                        {
                            SetNpcsInUnit(spawnedVehicle, null, new WeaponHash[] { WeaponHash.Pistol50 }, "Cop01", false, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        }
                        break;
                    case VehicleHash.Lguard:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Uscg01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, false, false, VehicleWeaponHash.Invalid, -1, 0);
                        break;
                    case VehicleHash.Seashark2:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Uscg01SMY, new WeaponHash[] { WeaponHash.MicroSMG }, null, false, false, true, VehicleWeaponHash.Invalid, -1);
                        break;
                    case VehicleHash.Riot:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Swat01SMY, new WeaponHash[] { WeaponHash.CarbineRifle }, null, false, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2, 3, 4, 5, 6);
                        break;
                    case VehicleHash.Dinghy3:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Swat01SMY, new WeaponHash[] { WeaponHash.MicroSMG }, null, false, false, true, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        break;
                    case VehicleHash.FBI:
                        SetNpcsInUnit(spawnedVehicle, PedHash.FibSec01SMM, new WeaponHash[] {WeaponHash.Pistol50, WeaponHash.CarbineRifle }, null, false, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        break;
                    case VehicleHash.FBI2:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Swat01SMY, new WeaponHash[] {WeaponHash.Pistol50, WeaponHash.CarbineRifle }, null, false, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        break;
                    case VehicleHash.Annihilator:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Swat01SMY, new WeaponHash[] { WeaponHash.MG }, null, false, false, true, VehicleWeaponHash.Invalid, -1, 0, 1, 2, 3, 4);
                        break;
                    case VehicleHash.Barracks:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Marine01SMM, new WeaponHash[] { WeaponHash.SpecialCarbine, WeaponHash.Pistol50 }, null, false, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8);
                        break;
                    case VehicleHash.Crusader:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Marine01SMM, new WeaponHash[] { WeaponHash.SpecialCarbine, WeaponHash.Pistol50 }, null, false, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        break;
                    case VehicleHash.Rhino:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Marine03SMY, new WeaponHash[] { WeaponHash.GrenadeLauncher }, null, false, false, false, VehicleWeaponHash.Invalid, -1);
                        break;
                    case VehicleHash.Insurgent3:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Marine03SMY, new WeaponHash[] { WeaponHash.CombatMG }, null, false, false, false, VehicleWeaponHash.Invalid, -1, 0, 1, 2, 3, 4, 5, 6, 7);
                        break;
                    case VehicleHash.Buzzard:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Pilot01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, true, true, VehicleWeaponHash.PlayerBuzzard, -1);
                        SetNpcsInUnit(spawnedVehicle, PedHash.Marine03SMY, new WeaponHash[] { WeaponHash.CombatMG }, null, false, false, true, VehicleWeaponHash.Invalid, 0, 1, 2);
                        SetUnitBlip(spawnedVehicle, "Buzzard", 0.9f, BlipSprite.EnemyHelicopter, BlipColor.Red);
                        break;
                    case VehicleHash.Strikeforce:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Pilot01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, true, true, VehicleWeaponHash.Invalid, -1);
                        SetUnitBlip(spawnedVehicle, "Strikeforce", 1.0f, BlipSprite.Plane, BlipColor.Red);
                        break;
                    case VehicleHash.Hydra:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Pilot01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, true, true, VehicleWeaponHash.Invalid, -1);
                        Function.Call(Hash._SET_VEHICLE_VERTICAL_FLIGHT_PHASE, spawnedVehicle, 0.0f);
                        SetUnitBlip(spawnedVehicle, "Hydra", 1.0f, BlipSprite.Plane, BlipColor.Red);
                        break;
                    case VehicleHash.Lazer:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Pilot01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, true, true, VehicleWeaponHash.Invalid, -1);
                        SetUnitBlip(spawnedVehicle, "Lazer", 1.0f, BlipSprite.Plane, BlipColor.Red);
                        break;
                    case VehicleHash.Savage:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Pilot01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, true, true, VehicleWeaponHash.PlayerBullet, -1, 0);
                        SetNpcsInUnit(spawnedVehicle, PedHash.Marine03SMY, new WeaponHash[] { WeaponHash.CombatMG }, null, false, true, true, VehicleWeaponHash.Invalid, 1, 2);
                        SetUnitBlip(spawnedVehicle, "Savage", 0.9f, BlipSprite.EnemyHelicopter, BlipColor.Red);
                        break;
                    case VehicleHash.Annihilator2:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Pilot01SMY, new WeaponHash[] { WeaponHash.Pistol50 }, null, false, true, true, VehicleWeaponHash.PlayerBuzzard, -1);
                        SetNpcsInUnit(spawnedVehicle, PedHash.Marine01SMM, new WeaponHash[] { WeaponHash.CombatMG }, null, false, false, true, VehicleWeaponHash.Invalid, 0, 1, 2, 3, 4);
                        SetUnitBlip(spawnedVehicle, "Stealth Annihilator", 0.9f, BlipSprite.EnemyHelicopter, BlipColor.Red);
                        break;
                    case VehicleHash.PatrolBoat:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Marine01SMM, new WeaponHash[] {WeaponHash.MicroSMG, WeaponHash.SpecialCarbine }, null, false, false, true, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        break;
                    case VehicleHash.Dinghy5:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Swat01SMY, new WeaponHash[] { WeaponHash.MicroSMG }, null, false, false, true, VehicleWeaponHash.Invalid, -1, 3);
                        SetUnitColor(spawnedVehicle, VehicleColor.MetallicBlack, VehicleColor.MetallicBlack, VehicleColor.MetallicBlack);
                        break;
                    case VehicleHash.Predator:
                        SetNpcsInUnit(spawnedVehicle, PedHash.Uscg01SMY, new WeaponHash[] { WeaponHash.MicroSMG }, null, false, false, true, VehicleWeaponHash.Invalid, -1, 0, 1, 2);
                        break;
                }
                return true;
            }
            return false;
        }

        void SetRandomPedBehaviours(Ped npc, Vehicle vehicleModel) // Set some ped behaviours...
        {
            float view, angle;
            if (vehicleModel.Model.IsHelicopter || vehicleModel.Model.IsPlane)
            {
                view = _Generator.Next(100, 301);
            }
            else
            {
                view = _Generator.Next(50, 101);
            }
            angle = _Generator.Next(20, 91);
            Function.Call(Hash.SET_PED_VISUAL_FIELD_CENTER_ANGLE, npc, angle);
            Function.Call(Hash.SET_PED_SEEING_RANGE, npc, view);
            Function.Call(Hash.SET_PED_HEARING_RANGE, npc, 200.0f);
        }
    }
}
