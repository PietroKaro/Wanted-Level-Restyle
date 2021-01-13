using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Wanted_Level_Restyle_2
{
    class SpecialSpawn
    {
        static readonly Model[] RoadblockCones = { new Model("prop_roadcone01a"), new Model("prop_roadcone01b"), new Model("prop_roadcone01c") };

        const int PED_DRIVER = 0;
        const int PED_LEFT = 1;
        const int PED_RIGHT = 2;
        public static bool MissionRunning;
        byte _Checkpoint;
        readonly Vehicle _Heli;
        readonly List<Ped> _PedsInTask; 
        Vector3 _PlayerPosition;
        DateTime _MaxTimeTask;

        SpecialSpawn(Vehicle vehicle, List<Ped> pedsInTask, Vector3 playerPosition)
        {
            _Heli = vehicle;
            _PedsInTask = pedsInTask;
            _PlayerPosition = playerPosition;
        }

        public static bool TrySpawnWaterApc(int type) // WaterAPC with cannon.
        {
            if (type == 6 && Game.Player.Character.IsInVehicle() && Game.Player.Character.CurrentVehicle.Model.IsBoat)
            {
                DispatchVehicle waterApc = new DispatchVehicle(VehicleHash.Apc, 10, SpecialVehicleType.Boat, OperativeArea.SanAndreas);
                (bool success, Vehicle Apc) = waterApc.SpawnModel(false);
                if (success)
                {
                    try
                    {
                        Apc.Mods.PrimaryColor = VehicleColor.MatteDesertTan;
                        Apc.Mods.SecondaryColor = VehicleColor.MatteDesertTan;
                        Apc.Mods.PearlescentColor = VehicleColor.MetallicDarkSilver;
                        Apc.IsEngineRunning = true;
                        foreach (int seat in new int[] { -1, 0 })
                        {
                            Ped npc = Apc.CreatePedOnSeat((VehicleSeat)seat, PedHash.Marine03SMY);
                            npc.Weapons.Give(WeaponHash.GrenadeLauncher, 9999, false, true).InfiniteAmmo = true;
                            npc.Task.FightAgainst(Game.Player.Character);
                        }
                        return true;
                    }
                    catch (NullReferenceException)
                    {
                        foreach (Ped occupant in Apc.Occupants)
                        {
                            occupant.Delete();
                        }
                        return false;
                    }
                }
            }
            return false;
        }

        public static bool TrySpawnRoadblock(int type) // Roadblock (4/5/6 stars).
        {
            if (type > 3)
            {
                foreach (Vehicle reference in World.GetAllVehicles(VehicleHash.Police3, VehicleHash.Sheriff, VehicleHash.PoliceT, VehicleHash.Riot))
                {
                    if (!Game.Player.Character.IsInWater && Game.Player.Character.IsInVehicle() && !Game.Player.Character.IsInRange(reference.Position, 100.0f) && reference.IsDriveable && reference.Occupants.Length == 0 && reference.Speed == 0 && World.GetNearbyVehicles(reference.Position, 20f, VehicleHash.Riot2, VehicleHash.Insurgent2).Length == 0 && World.GetNearbyProps(reference.Position, 20.0f, RoadblockCones).Length > 0)
                    {
                        Vehicle block = null;
                        try
                        {
                            switch (type)
                            {
                                case 4:
                                    foreach (int position in new int[] {-2, 2, -3, 3 })
                                    {
                                        World.CreatePed(PedHash.Swat01SMY, reference.Position + reference.RightVector * position, reference.Heading).Weapons.Give(WeaponHash.HeavySniper, 9999, true, true).InfiniteAmmo = true;
                                    }
                                    break;
                                case 5:
                                    block = World.CreateVehicle(VehicleHash.Riot2, reference.Position + reference.RightVector * -4, reference.Heading);
                                    block.Mods.PrimaryColor = VehicleColor.MatteGray;
                                    block.Mods.PrimaryColor = VehicleColor.MetallicBlack;
                                    block.Mods.PrimaryColor = VehicleColor.MetallicBlack;
                                    block.IsSirenActive = true;
                                    foreach (Vector3 position in new Vector3[] { block.ForwardVector * 5, block.ForwardVector * -5, block.UpVector * 4, block.UpVector * 5 })
                                    {
                                        World.CreatePed(PedHash.Swat01SMY, block.Position + position, block.Heading).Weapons.Give(WeaponHash.Minigun, 9999, true, true).InfiniteAmmo = true;
                                    }
                                    break;
                                case 6:
                                    block = World.CreateVehicle(VehicleHash.Insurgent2, reference.Position + reference.RightVector * -4, reference.Heading);
                                    block.Mods.PrimaryColor = VehicleColor.MatteBlack;
                                    block.Mods.SecondaryColor = VehicleColor.MatteBlack;
                                    block.Mods.PearlescentColor = VehicleColor.MatteBlack;
                                    World.CreatePed(PedHash.Marine03SMY, block.Position + block.ForwardVector * 4, block.Heading).Weapons.Give(WeaponHash.Railgun, 9999, true, true).InfiniteAmmo = true;
                                    World.CreatePed(PedHash.Marine03SMY, block.Position + block.ForwardVector * -4, block.Heading).Weapons.Give(WeaponHash.Railgun, 9999, true, true).InfiniteAmmo = true;
                                    World.CreatePed(PedHash.Marine03SMY, block.Position + block.UpVector * 3, block.Heading).Weapons.Give(WeaponHash.RPG, 9999, true, true).InfiniteAmmo = true;
                                    World.CreatePed(PedHash.Marine03SMY, block.Position + block.UpVector * 4, block.Heading).Weapons.Give(WeaponHash.RPG, 9999, true, true).InfiniteAmmo = true;
                                    break;
                            }
                            return true;
                        }
                        catch (NullReferenceException)
                        {
                            block?.Delete();
                            return false;
                        }
                    }
                }
            }
            return false;
        }

        public static bool TryCreateHeliMission(VehicleHash heliModel, PedHash pedModel, out SpecialSpawn heliResult) // HeliMission (land helicopter / npc rappel from heli).
        {
            if (!MissionRunning && Function.Call<int>(Hash.GET_INTERIOR_FROM_ENTITY, Game.Player.Character) == 0 && Game.Player.Character.CurrentVehicle == null && !Game.Player.Character.IsInWater && !Game.Player.Character.IsInAir)
            {
                Vehicle[] reference = World.GetNearbyVehicles(Game.Player.Character, 400.0f);
                foreach (Vehicle vehicleRef in reference)
                {
                    Vehicle heli = null;
                    if (vehicleRef.Exists() && vehicleRef.IsPersistent && vehicleRef.Model.IsHelicopter && vehicleRef.IsInAir && vehicleRef.IsDriveable && !vehicleRef.IsOnScreen)
                    {
                        try
                        {
                            heli = World.CreateVehicle(heliModel, vehicleRef.Position + vehicleRef.ForwardVector * -new Random().Next(10, 31), vehicleRef.Heading);
                            if (heliModel == VehicleHash.Maverick)
                            {
                                heli.Mods.PrimaryColor = VehicleColor.MetallicYellowBird;
                                heli.Mods.SecondaryColor = VehicleColor.MetallicBlazeRed;
                                heli.Mods.PearlescentColor = VehicleColor.MetallicWhite;
                            }
                            else if (heliModel == VehicleHash.Annihilator2)
                            {
                                heli.LandingGearState = VehicleLandingGearState.Retracted;
                                Blip anniBlip = heli.AddBlip();
                                anniBlip.Scale = 0.9f;
                                anniBlip.Sprite = BlipSprite.EnemyHelicopter;
                                anniBlip.Color = BlipColor.Red;
                                anniBlip.Name = "Stealth Annihilator";
                            }
                            List<Ped> peds = new List<Ped>(3);
                            foreach (int seat in new int[] { -1, 1, 2 })
                            {
                                Ped npc = heli.CreatePedOnSeat((VehicleSeat)seat, pedModel);
                                npc.BlockPermanentEvents = true;
                                npc.Weapons.Give(WeaponHash.HeavyShotgun, 9999, false, true).InfiniteAmmo = true;
                                peds.Add(npc);
                            }
                            heli.IsEngineRunning = true;
                            heli.HeliBladesSpeed = 1.0f;
                            heliResult = new SpecialSpawn(heli, peds, Game.Player.Character.Position);
                            MissionRunning = true;
                            heliResult.CheckHeliMission();
                            return true;
                        }
                        catch (NullReferenceException)
                        {
                            if (heli != null)
                            {
                                foreach (Ped occ in heli.Occupants)
                                {
                                    occ.Delete();
                                }
                                heli.Delete();
                            }
                            heliResult = null;
                            return false;
                        }
                    }
                }
            }
            heliResult = null;
            return false;
        }

        public void CheckHeliMission() // Check Heli to continue HeliMission (each tick in MainScript.cs).
        {
            if (CheckEntities())
            {
                switch (_Checkpoint)
                {
                    case 0:
                        _MaxTimeTask = DateTime.UtcNow.AddMinutes(1d);
                        Function.Call(Hash.TASK_HELI_MISSION, _PedsInTask[PED_DRIVER], _Heli, 0, 0, _PlayerPosition.X, _PlayerPosition.Y, _PlayerPosition.Z, 4, 30.0f, 50.0f, (_PlayerPosition - _Heli.Position).ToHeading(), -1, -1, -1, 32);
                        _Checkpoint++;
                        break;
                    case 1:
                        if ((DateTime.UtcNow >= _MaxTimeTask && !_Heli.IsOnAllWheels) || _Heli.EngineHealth <= 925.0f)
                        {
                            _PedsInTask[PED_DRIVER].Task.ClearAll();
                            Function.Call(Hash.TASK_HELI_MISSION, _PedsInTask[PED_DRIVER], _Heli, 0, 0, _PlayerPosition.X, _PlayerPosition.Y, _PlayerPosition.Z + 15.0f, 4, 20.0f, 10.0f, (_PlayerPosition - _Heli.Position).ToHeading(), -1, -1, -1, 0);
                            _MaxTimeTask = DateTime.UtcNow.AddMinutes(2d);
                            _Checkpoint = 3;
                        }
                        else if (_Heli.IsInWater)
                        {
                            _Heli.IsPositionFrozen = true;
                            TerminateHeliMission();
                        }
                        else if (_Heli.IsOnAllWheels)
                        {
                            _PedsInTask[PED_LEFT]?.Task.LeaveVehicle();
                            _PedsInTask[PED_RIGHT]?.Task.LeaveVehicle();
                            _Checkpoint++;
                        }
                        break;
                    case 2:
                        if (_Heli.Occupants.Length == 1)
                        {
                            _PedsInTask[PED_LEFT]?.Task.FightAgainst(Game.Player.Character);
                            _PedsInTask[PED_RIGHT]?.Task.FightAgainst(Game.Player.Character);
                            TerminateHeliMission();
                        }
                        break;
                    case 3:
                        if (DateTime.UtcNow >= _MaxTimeTask)
                        {
                            TerminateHeliMission();
                        }
                        else if (_PlayerPosition.DistanceTo2D(_Heli.Position) <= 10.0f)
                        {
                            _PedsInTask[PED_DRIVER]?.Task.ClearAll();
                            _Heli.MaxSpeed = 10.0f;
                            _PedsInTask[PED_LEFT]?.Task.RappelFromHelicopter();
                            _PedsInTask[PED_RIGHT]?.Task.RappelFromHelicopter();
                            _Checkpoint++;
                        }
                        break;
                    case 4:
                        if (Function.Call<bool>(Hash._IS_ANY_PASSENGER_RAPPELING_FROM_VEHICLE, _Heli))
                        {
                            _Checkpoint++;
                        }
                        break;
                    case 5:
                        if (!Function.Call<bool>(Hash._IS_ANY_PASSENGER_RAPPELING_FROM_VEHICLE, _Heli))
                        {
                            _PedsInTask[PED_LEFT]?.Task.FightAgainst(Game.Player.Character);
                            _PedsInTask[PED_RIGHT]?.Task.FightAgainst(Game.Player.Character);
                            TerminateHeliMission();
                        }
                        break;
                }
            }
            else
            {
                TerminateHeliMission();
            }
        }

        void TerminateHeliMission()
        {
            if (_Heli.Exists())
            {
                _Heli.MaxSpeed = 100.0f;
                _Heli.IsPositionFrozen = false;
                _Heli.AttachedBlip?.Delete();
                foreach (Ped occ in _Heli.Occupants)
                {
                    if (!occ.IsPlayer)
                    {
                        occ.Task.ClearAll();
                        occ.Task.FleeFrom(Game.Player.Character);
                        occ.MarkAsNoLongerNeeded();
                    }
                }
                _Heli.MarkAsNoLongerNeeded();
            }
            MissionRunning = false;
        }

        private bool CheckEntities() // If false, HeliMission needs to be aborted immediately.
        {
            return _Heli.Exists() && _PedsInTask[PED_DRIVER].Exists() && _PedsInTask[PED_DRIVER].IsAlive && _PedsInTask[PED_DRIVER].IsInVehicle(_Heli) && _Heli.IsDriveable && ((_PedsInTask[PED_LEFT].Exists() && _PedsInTask[PED_LEFT].IsAlive) || (_PedsInTask[PED_RIGHT].Exists() && _PedsInTask[PED_RIGHT].IsAlive)) && Game.Player.Character.IsInRange(_PlayerPosition, 150.0f);
        }
    }
}
