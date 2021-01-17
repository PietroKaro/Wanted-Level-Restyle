﻿using System;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.UI;
using GTA.Native;

namespace Wanted_Level_Restyle_2
{
    [ScriptAttributes(Author = "PietroKaro99")] // Main gameplay control script.
    public class MainScript : Script
    {
        readonly Model[] _NpcModels = new Model[] { PedHash.Cop01SFY, PedHash.Cop01SMY, PedHash.Sheriff01SFY, PedHash.Sheriff01SMY, PedHash.Hwaycop01SMY, PedHash.Ranger01SFY,
        PedHash.Ranger01SMY, PedHash.Uscg01SMY, PedHash.Swat01SMY, PedHash.FibSec01SMM, PedHash.FibOffice02SMM, PedHash.Marine01SMM, PedHash.Marine03SMY, PedHash.Pilot01SMY};

        readonly Keys _ToggleKey;
        bool _ModToggle, _IsSpawnerOnPause, _RequestAbort, _IsSpecialSpawnBlocked, _IsActive, _CanGiveSixthStar, _IsFirstPedMoneyCheck = true, _IsPoliceDispatchBlocked;
        readonly bool _AutoStart, _RoadBlocks, _HeliMission, _WaterAPC;
        byte _DeadNpcs;
        int _MaxDeadNpcs;
        readonly DispatchServiceScript _Spawner;
        readonly Random _Generator;
        DateTime _NextTime, _HeliTime, _RoadblockTime, _ApcTime;
        SpecialSpawn _SpecialSpawn;

        public MainScript()
        {
            Tick += MainScript_OnTick;
            KeyDown += MainScript_OnKeyDown;
            Aborted += OnMainScriptAborted;
            string _Path = BaseDirectory + @"\CustomDispatch.xml";
            try // Try to read all settings of this mod (WlrMod.cs).
            {
                (_ToggleKey, _AutoStart, _RoadBlocks, _HeliMission, _WaterAPC) = WlrMod.ReadSettings(_Path);
                _Spawner = InstantiateScript<DispatchServiceScript>();
                (_Spawner.MinTime, _Spawner.MaxTime) = WlrMod.ReadIntervals(_Path);
                for (byte levelCounter = 1; levelCounter < 7; levelCounter++)
                {
                    _Spawner.KeysWantedLevelList.Add(levelCounter, WlrMod.GetDispatchVehicleList(_Path, levelCounter)); // Create dictionary to contains wanted levels vehicles.
                }
                DispatchVehicle.CommonVehicles.AddRange(new DispatchVehicle[] { new DispatchVehicle(VehicleHash.Annihilator, 500), new DispatchVehicle(VehicleHash.Annihilator2, 500), new DispatchVehicle(VehicleHash.Maverick, 500), new DispatchVehicle(VehicleHash.Insurgent2, 300), new DispatchVehicle(VehicleHash.Riot2, 300), new DispatchVehicle(VehicleHash.Apc, 300) });
                DispatchVehicle.CommonVehicles = DispatchVehicle.CommonVehicles.Distinct(new DispatchVehicle.DispatchVehicleSelector()).ToList();
                _Generator = new Random();
                if (_AutoStart)
                {
                    _ModToggle = true;
                    StartMod();
                }
                _Spawner.Aborted += OnSpawnerAborted;
            }
            catch (WlrException)
            {
                _RequestAbort = true;
            }
        }

        void MainScript_OnTick(object sender, EventArgs args)
        {
            if (_ModToggle)
            {
                if (CanRunning(out int level))
                {
                    bool toggleSpawner = ToggleSpawner(level);
                    if (_Spawner.IsActive && !toggleSpawner)
                    {
                        _Spawner.IsActive = false;
                        _IsSpecialSpawnBlocked = true;
                    }
                    else if (!_Spawner.IsActive && toggleSpawner)
                    {
                        _Spawner.IsActive = true;
                        _IsSpecialSpawnBlocked = false;
                        Game.Player.Character.AttachedBlip?.Delete();
                    }
                    ToggleSixthStar();
                    DispatchVehicle[] vehicleModels = DispatchVehicle.CommonVehicles.ToArray();
                    bool cleanEmptyVehicles = false;
                    if (DateTime.UtcNow >= _NextTime)
                    {
                        cleanEmptyVehicles = true;
                        _NextTime = DateTime.UtcNow.AddMinutes(_Generator.Next(2, 11));
                    }
                    foreach (DispatchVehicle dispatchVehicle in vehicleModels)
                    {
                        dispatchVehicle.ManageModel(false, false, cleanEmptyVehicles);
                    }
                    Ped[] npcs = World.GetAllPeds(_NpcModels);
                    if (level == 5 && !_CanGiveSixthStar && _IsFirstPedMoneyCheck)
                    {
                        SetOneDollarToAllPeds(npcs);
                        _IsFirstPedMoneyCheck = false;
                    }
                    foreach (Ped npc in npcs)
                    {
                        if (npc.Exists())
                        {
                            if (KeepPedInGame(npc))
                            {
                                if (level == 1 && npc.Weapons.Current != WeaponHash.StunGun)
                                {
                                    npc.Weapons.RemoveAll();
                                    npc.Weapons.Give(WeaponHash.StunGun, 9999, false, true);
                                }
                                else if (level != 1 && npc.CurrentVehicle?.Model == VehicleHash.Polmav && npc.Weapons.Current != WeaponHash.MG)
                                {
                                    npc.Weapons.RemoveAll();
                                    npc.Weapons.Give(WeaponHash.MG, 9999, false, true).InfiniteAmmo = true;
                                }
                            }
                            else
                            {
                                npc.MarkAsNoLongerNeeded();
                            }
                        }
                    }
                    if (!_IsSpecialSpawnBlocked)
                    {
                        DateTime currentTime = DateTime.UtcNow;
                        if (currentTime >= _HeliTime && _HeliMission)
                        {
                            VehicleHash heliTask;
                            PedHash pedTask;
                            if (WlrMod.IsPlayerInCoastArea() && level <= 4)
                            {
                                heliTask = VehicleHash.Maverick;
                                pedTask = PedHash.Uscg01SMY;
                            }
                            else if (level == 6)
                            {
                                heliTask = VehicleHash.Annihilator2;
                                pedTask = PedHash.Marine01SMM;
                            }
                            else
                            {
                                heliTask = VehicleHash.Annihilator;
                                pedTask = PedHash.Swat01SMY;
                            }
                            if (SpecialSpawn.TryCreateHeliMission(heliTask, pedTask, out SpecialSpawn specialSpawn))
                            {
                                _SpecialSpawn = specialSpawn;
                            }
                            _HeliTime = DateTime.UtcNow.AddMinutes(_Generator.Next(1, 3));
                        }
                        else if (currentTime >= _RoadblockTime && _RoadBlocks)
                        {
                            SpecialSpawn.TrySpawnRoadblock(level);
                            _RoadblockTime = DateTime.UtcNow.AddMinutes(0.25d);
                        }
                        else if (currentTime >= _ApcTime && _WaterAPC)
                        {
                            SpecialSpawn.TrySpawnWaterApc(level);
                            _ApcTime = DateTime.UtcNow.AddMinutes(_Generator.Next(1, 3));
                        }
                    }
                }
                else if (_IsActive) // Prevent the Reset() method from being repeated.
                {
                    Reset(false);
                }
                if (Game.IsMissionActive && !_IsSpawnerOnPause) // Spawner script is disabled when RAGE reports a mission running (story mission, event mission, etc..). Main script continue running.
                {
                    PauseSpawner(true);
                }
                else if (!Game.IsMissionActive && _IsSpawnerOnPause)
                {
                    Wait(500);
                    PauseSpawner(false);
                }
                if (SpecialSpawn.MissionRunning)
                {
                    _SpecialSpawn.CheckHeliMission();
                }
            }
            if (_RequestAbort) // Abort all two scripts in case of exception.
            {
                Abort();
            }
        }

        void MainScript_OnKeyDown(object sender, KeyEventArgs args)
        {
            if (args.KeyCode == _ToggleKey)
            {
                Wait(500);
                _ModToggle = !_ModToggle;
                if (_ModToggle)
                {
                    StartMod();
                }
                else
                {
                    if (_IsSpawnerOnPause)
                    {
                        _Spawner.Resume();
                        _IsSpawnerOnPause = false;
                    }
                    ToggleBlockSwatDispatch(false);
                    Reset(false);
                    Notification.Show("New wanted level: ~r~OFF~w~.");
                }
            }
        }

        void OnMainScriptAborted(object sender, EventArgs args)
        {
            if (_Spawner.IsRunning)
            {
                _Spawner.Abort();
            }
            Reset(true);
        }

        void OnSpawnerAborted(object sender, EventArgs args)
        {
            _ModToggle = false;
            _RequestAbort = true;
        }

        void ToggleBlockSwatDispatch(bool toggle) => Function.Call(Hash.BLOCK_DISPATCH_SERVICE_RESOURCE_CREATION, 4, toggle); // Block SUV noose riders.

        void ToggleBlockPoliceDispatch(bool toggle) => Function.Call(Hash.BLOCK_DISPATCH_SERVICE_RESOURCE_CREATION, 1, toggle); // Block police cars.

        bool ToggleSpawner(int wantedLevel) // Spawner "toggle" (bool IsActive).
        {
            if (!ArePlayerStarsWhite(wantedLevel) || World.PedCount > 100)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        bool ArePlayerStarsWhite(int level) 
        {
            if (Function.Call<bool>(Hash.ARE_PLAYER_STARS_GREYED_OUT, Game.Player))
            {
                if (level == 6 && Game.Player.Character.AttachedBlip == null) // Blip for sixth star when cops lose player position.
                {
                    Blip lostPositionBlip = Game.Player.Character.AddBlip();
                    lostPositionBlip.Scale = 1.5f;
                    lostPositionBlip.Color = BlipColor.GreenDark;
                    lostPositionBlip.Name = "Wanted player";
                }
                if (_IsPoliceDispatchBlocked)
                {
                    ToggleBlockPoliceDispatch(false);
                    _IsPoliceDispatchBlocked = false;
                }
                return false;
            }
            else // When cops lose player position, police cars dispatch needs to be enabled.
            {
                if (level >= 5)
                {
                    if (!_IsPoliceDispatchBlocked)
                    {
                        ToggleBlockPoliceDispatch(true);
                        _IsPoliceDispatchBlocked = true;
                    }
                }
                else if (_IsPoliceDispatchBlocked)
                {
                    ToggleBlockPoliceDispatch(false);
                    _IsPoliceDispatchBlocked = false;
                }
                return true;
            }
        }

        void ToggleSixthStar() // Sixth star "toggle".
        {
            int realLevel = Game.Player.WantedLevel;
            if (realLevel == 5 && _CanGiveSixthStar && Function.Call<int>(Hash.GET_FAKE_WANTED_LEVEL) == 0)
            {
                Function.Call(Hash.SET_FAKE_WANTED_LEVEL, 6);
                _IsFirstPedMoneyCheck = true;
            }
            else if (realLevel < 5 && (_CanGiveSixthStar || !_IsFirstPedMoneyCheck))
            {
                Function.Call(Hash.SET_FAKE_WANTED_LEVEL, 0);
                _IsFirstPedMoneyCheck = true;
                _DeadNpcs = 0;
                _CanGiveSixthStar = false;
            }
        }
        
        void StartMod()
        {
            ToggleBlockSwatDispatch(true);
            _MaxDeadNpcs = _Generator.Next(40, 61);
            Notification.Show("New wanted level: ~g~ON~w~.");
        }

        bool CanRunning(out int modWantedLevel) // Check if mod gameplay control can running in Tick() method.
        {
            modWantedLevel = WlrMod.GetModWantedLevel();
            if (modWantedLevel > 0 && !Game.Player.IsDead && !Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player, 0))
            {
                if (!_IsActive) // Set various datetimes.
                {
                    _IsActive = true;
                    _NextTime = DateTime.UtcNow.AddMinutes(_Generator.Next(2, 11));
                    _HeliTime = DateTime.UtcNow.AddMinutes(_Generator.Next(1, 3));
                    _ApcTime = DateTime.UtcNow.AddMinutes(_Generator.Next(1, 3));
                    _RoadblockTime = DateTime.UtcNow.AddMinutes(0.25d);
                }
                return true;
            }
            return false;
        }

        void PauseSpawner(bool toggle) // Spawner script "toggle" (pause/running).
        {
            if (toggle)
            {
                _Spawner.Pause();
            }
            else
            {
                _Spawner.Resume();
            }
            _IsSpawnerOnPause = toggle;
            _IsSpecialSpawnBlocked = toggle;
            ToggleBlockSwatDispatch(!toggle); // For safety, noose riders dispatch is reactivated.
        }

        void Reset(bool abort)
        {
            _IsActive = false;
            Function.Call(Hash.SET_FAKE_WANTED_LEVEL, 0);
            ToggleBlockPoliceDispatch(false);
            _IsPoliceDispatchBlocked = false;
            _DeadNpcs = 0;
            _CanGiveSixthStar = false;
            _IsFirstPedMoneyCheck = true;
            _Spawner.IsActive = false;
            _IsSpecialSpawnBlocked = false;
            Game.Player.Character.AttachedBlip?.Delete();
            SpecialSpawn.MissionRunning = false;
            if (abort) // Reset due to scripts aborted (exceptions, reloading from SHVDN console, etc..).
            {
                ToggleBlockSwatDispatch(false);
                foreach (DispatchVehicle dispatchVehicle in DispatchVehicle.CommonVehicles)
                {
                    dispatchVehicle.ManageModel(false, true, false);
                }
                foreach (Ped npc in World.GetAllPeds(_NpcModels))
                {
                    npc.Delete();
                }
            }
            else // Normal reset (cops lost player, player is dead, etc..).
            {
                foreach (DispatchVehicle dispatchVehicle in DispatchVehicle.CommonVehicles)
                {
                    dispatchVehicle.ManageModel(true, false, false);
                }
                foreach (Ped npc in World.GetAllPeds(_NpcModels))
                {
                    if (npc.Exists() && npc.IsPersistent)
                    {
                        if (npc.CurrentVehicle != null && (npc.CurrentVehicle.Model.IsPlane || npc.CurrentVehicle.Model.IsHelicopter || npc.CurrentVehicle.Model.IsBoat))
                        {
                            npc.Task.ClearAll();
                            npc.Task.FleeFrom(Game.Player.Character);
                        }
                        npc.MarkAsNoLongerNeeded();
                    }
                }
            }
        }

        void SetOneDollarToAllPeds(Ped[] pedList) // This method prevents dead peds already counted from being counted again (for sixth star).
        {
            foreach (Ped ped in pedList)
            {
                if (ped.Exists() && ped.IsDead)
                {
                    ped.Money = 1;
                }
            }
        }

        bool KeepPedInGame(Ped ped) // Ped in memory management / Check ped if is to be counted for sixth star.
        {
            if (ped.IsDead)
            {
                if (!_IsFirstPedMoneyCheck && ped.Money != 1 && ped.Killer != null && (ped.Killer == Game.Player.Character || (Game.Player.Character.CurrentVehicle != null && ped.Killer == Game.Player.Character.CurrentVehicle)))
                {
                    ped.Money = 1;
                    _DeadNpcs++;
                    if (_DeadNpcs >= _MaxDeadNpcs)
                    {
                        _DeadNpcs = 0;
                        _CanGiveSixthStar = true;
                    }
                }
                return !ped.IsPersistent;
            }
            else
            {
                if (ped.IsPersistent)
                {
                    float distanceFromPlayer = 300.0f;
                    if (ped.CurrentVehicle != null)
                    {
                        if (ped.CurrentVehicle.Model.IsPlane)
                        {
                            distanceFromPlayer = 2000.0f;
                        }
                        else if (ped.CurrentVehicle.Model.IsHelicopter)
                        {
                            distanceFromPlayer = 500.0f;
                        }
                        else if (ped.CurrentVehicle.Model.IsBoat)
                        {
                            distanceFromPlayer = 400.0f;
                        }
                    }
                    if (ped.IsInRange(Game.Player.Character.Position, distanceFromPlayer))
                    {
                        return true;
                    }
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }
}
