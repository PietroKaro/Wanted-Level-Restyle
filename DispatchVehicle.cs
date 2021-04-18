using GTA;
using System;
using System.Collections.Generic;
using GTA.Math;
using GTA.Native;

namespace Wanted_Level_Restyle_2
{
    public enum SpecialVehicleType : byte
    {
        None,
        Boat = 11,
        Heli = 12,
        Plane = 13,
    }

    public enum AvaiableDispatchVehicle : byte // Avaiable vehicles for this mod.
    {
        Police = 0,
        Police3 = 0,
        Sheriff = 0,
        Policeb = 0,
        Pranger = 0,
        Police2 = 0,
        PoliceT = 0,
        Sheriff2 = 0,
        Lguard = 0,
        Seashark2 = 11,
        Frogger = 12,
        Polmav = 12,
        Riot = 0,
        Riot2 = 0,
        Predator = 11,
        Dinghy3 = 11,
        Dinghy5 = 11,
        PatrolBoat = 11,
        FBI = 0,
        FBI2 = 0,
        Police4 = 0,
        Apc = 0,
        Annihilator = 12,
        Annihilator2 = 12,
        Barracks = 0,
        Barrage = 0,
        Crusader = 0,
        HalfTrack = 0,
        Rhino = 0,
        Insurgent3 = 0,
        Buzzard = 12,
        Akula = 12,
        Hunter = 12,
        Savage = 12,
        Strikeforce = 13,
        Hydra = 13,
        Lazer = 13,
        Khanjari = 0,
    }

    public enum OperativeArea : byte
    {
        SanAndreas,
        LosSantos,
        BlaineCounty,
        MtChiliad,
        CoastArea,
    }

    public class DispatchVehicle // Represents a specific model of vehicle that will be dispatched in game.
    {
        public static List<DispatchVehicle> CommonVehicles = new List<DispatchVehicle>();
        
        public readonly Model VehicleModel;
        public readonly int SpawnLimit;
        public readonly bool IsSpecialVehicle = true;
        public readonly SpecialVehicleType Type;
        public readonly OperativeArea Area;
        public readonly VehicleHash Hash;
        public readonly int MaxDistance;

        public DispatchVehicle(Model hash, int spawnLimit, SpecialVehicleType type, OperativeArea area)
        {
            Hash = hash;
            VehicleModel = hash;
            SpawnLimit = spawnLimit;
            switch (type)
            {
                case SpecialVehicleType.Boat:
                    MaxDistance = 400;
                    break;
                case SpecialVehicleType.Heli:
                    MaxDistance = 500;
                    break;
                case SpecialVehicleType.Plane:
                    MaxDistance = 2000;
                    break;
                case SpecialVehicleType.None:
                    IsSpecialVehicle = false;
                    MaxDistance = 300;
                    break;
            }
            Type = type;
            Area = area;
            CommonVehicles.Add(this);
        }

        public DispatchVehicle(VehicleHash hash, int maxDistance) // Used for create objects not related to XML File.
        {
            Hash = hash;
            VehicleModel = hash;
            Type = SpecialVehicleType.None;
            MaxDistance = maxDistance;
        }

        private bool IsInExpertiseArea()
        {
            switch (Area)
            {
                case OperativeArea.LosSantos:
                    return !WlrMod.IsPlayerInBlaineCounty();
                case OperativeArea.BlaineCounty:
                    return WlrMod.IsPlayerInBlaineCounty();
                case OperativeArea.MtChiliad:
                    return WlrMod.IsPlayerAroundMtChiliad();
                case OperativeArea.CoastArea:
                    return WlrMod.IsPlayerInCoastArea();
                default:
                    return true;
            }
        }

        private bool IsOverSpawnLimit()
        {
            int amount = World.GetAllVehicles(VehicleModel).Length;
            if (Game.Player.Character.CurrentVehicle?.Model == VehicleModel)
            {
                amount--;
            }
            if (amount >= SpawnLimit)
            {
                return true;
            }
            return false;
        }

        public void ManageModel(bool forceClean, bool delete, bool onlyEmptyVehicle) // Vehicle in memory management.
        {
            foreach (Vehicle vehicle in World.GetAllVehicles(VehicleModel))
            {
                if (delete)
                {
                    vehicle.Delete();
                }
                else
                {
                    if (vehicle.Exists() && vehicle.IsPersistent && (forceClean || (onlyEmptyVehicle && vehicle.Occupants.Length == 0) || !vehicle.IsDriveable || !vehicle.IsInRange(Game.Player.Character.Position, MaxDistance)))
                    {
                        if (vehicle.AttachedBlip != null)
                        {
                            vehicle.AttachedBlip.Scale = 0.0f; // Scale, not Delete(), otherwise RAGE will add its default blip.
                        }
                        vehicle.MarkAsNoLongerNeeded();
                    }
                }
            }
        }

        public (bool success, Vehicle spawn) SpawnModel(bool force) // Model spawn method (force is used for aircrafts).
        {
            Vehicle spawn;
            if (!IsOverSpawnLimit() && IsInExpertiseArea())
            {
                try
                {
                    if (Type == SpecialVehicleType.None) // Ground vehicle.
                    {
                        if (GetSpawnPointForGroundVehicles(out Vector3 spawnPosition, out float spawnHeading))
                        {
                            spawn = World.CreateVehicle(VehicleModel, spawnPosition, spawnHeading);
                            spawn.PlaceOnGround();
                            spawn.Repair();
                            spawn.IsEngineRunning = true;
                            if (spawn.HasSiren)
                            {
                                spawn.IsSirenActive = true;
                            }
                            return (true, spawn);
                        }
                        else
                        {
                            return (false, null);
                        }
                    }
                    else if (Type == SpecialVehicleType.Boat) // Boat (need to check if reference vehicle is empty because for a strange reason, even attached boats to pickup are counted as "InWater").
                    {
                        Vehicle[] reference = World.GetAllVehicles(VehicleHash.Predator, VehicleHash.Dinghy3, VehicleHash.Seashark2, VehicleHash.Suntrap, VehicleHash.Tropic, VehicleHash.Squalo);
                        foreach (Vehicle vRef in reference)
                        {
                            if (vRef.Exists() && vRef.Occupants.Length != 0 && vRef.IsInWater && vRef.IsDriveable && !vRef.IsOnScreen)
                            {
                                spawn = World.CreateVehicle(VehicleModel, vRef.Position + vRef.ForwardVector * -new Random().Next(10, 31), vRef.Heading);
                                if (!spawn.IsInWater)
                                {
                                    spawn.Delete();
                                    return (false, null);
                                }
                                spawn.IsEngineRunning = true;
                                return (true, spawn);
                            }
                        }
                    }
                    else // Aircraft.
                    {
                        Vehicle[] reference = World.GetNearbyVehicles(Game.Player.Character, 400.0f);
                        foreach (Vehicle vRef in reference)
                        {
                            if (vRef.Exists() && (vRef.Model.IsPlane || vRef.Model.IsHelicopter) && vRef.IsInAir && vRef.IsDriveable && !vRef.IsOnScreen)
                            {
                                if (Type == SpecialVehicleType.Heli)
                                {
                                    spawn = World.CreateVehicle(VehicleModel, vRef.Position + vRef.UpVector * new Random().Next(20, 41), vRef.Heading);
                                    spawn.HeliBladesSpeed = 1.0f;
                                }
                                else
                                {
                                    spawn = World.CreateVehicle(VehicleModel, vRef.Position + vRef.UpVector * 200, vRef.Heading);
                                }
                                spawn.IsEngineRunning = true;
                                spawn.LandingGearState = VehicleLandingGearState.Retracted;
                                return (true, spawn);
                            }
                        }
                        if (force)
                        {
                            return TryForceAircraftSpawn();
                        }
                    }
                }
                catch (NullReferenceException)
                {
                    throw new WlrException(null, false);
                }
            }
            return (false, null);
        }

        (bool success, Vehicle forcedSpawn) TryForceAircraftSpawn() // "Emergency" method to spawn aircrafts when there's not vehicle references. It's called when SpawnModel() fails.
        {
            Vehicle forcedSpawn;
            Vector3 spawnPosition;
            if (Game.Player.Character.HeightAboveGround > 800.0f)
            {
                spawnPosition = Game.Player.Character.Position.Around(300.0f);
            }
            else
            {
                spawnPosition = (Game.Player.Character.Position + Game.Player.Character.UpVector * 250.0f).Around(300.0f);
            }
            forcedSpawn = World.CreateVehicle(Hash, spawnPosition);
            if (forcedSpawn == null)
            {
                return (false, null);
            }
            if (Type == SpecialVehicleType.Heli)
            {
                forcedSpawn.HeliBladesSpeed = 1.0f;
            }
            forcedSpawn.IsEngineRunning = true;
            forcedSpawn.LandingGearState = VehicleLandingGearState.Retracted;
            return (true, forcedSpawn);
        }

        bool GetSpawnPointForGroundVehicles(out Vector3 posResult, out float headingResult) // Return (if exists) a free position around player to spawn ground vehicles.
        {
            OutputArgument outArgsPosition = new OutputArgument();
            OutputArgument outArgsHeading = new OutputArgument();
            for (float spawnRange = 150.0f; spawnRange <= 250; spawnRange += 50)
            {
                Vector3 playerPositionAround = Game.Player.Character.Position.Around(spawnRange);
                Function.Call(GTA.Native.Hash.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING, playerPositionAround.X, playerPositionAround.Y, playerPositionAround.Z, outArgsPosition, outArgsHeading, 1, 3.0f, 0);
                posResult = outArgsPosition.GetResult<Vector3>();
                headingResult = outArgsHeading.GetResult<float>();
                if (Vector3.Distance(Game.Player.Character.Position, posResult) >= 100 && !Function.Call<bool>(GTA.Native.Hash.IS_POINT_OBSCURED_BY_A_MISSION_ENTITY, posResult.X, posResult.Y, posResult.Z, 5.0f, 5.0f, 5.0f, 0))
                {
                    return true;
                }
            }
            posResult = Vector3.Zero;
            headingResult = 0.0f;
            return false;
        }

        public class DispatchVehicleSelector : IEqualityComparer<DispatchVehicle> // Use for Distinct() method in MainScript.cs
        {
            public bool Equals(DispatchVehicle left, DispatchVehicle right)
            {
                if (left.Hash == right.Hash)
                {
                    return true;
                }
                return false;
            }

            public int GetHashCode(DispatchVehicle dispatchVehicle)//
            {
                return dispatchVehicle.Hash.GetHashCode();
            }
        }
    }
}
