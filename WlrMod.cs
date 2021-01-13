using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.XPath;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Wanted_Level_Restyle_2
{
    static class WlrMod
    {
        static readonly int ListCapacity = Enum.GetNames(typeof(AvaiableDispatchVehicle)).Length; // Max list capacity for each wanted level list.

        public static (Keys toggleKey, bool autoStart, bool roadBlocks, bool heliMission, bool waterApc) ReadSettings(string xmlPath) // Read mod settings from XML.
        {
            Keys toggleKey = Keys.NumPad0;
            bool autoStart, roadBlocks, heliMission, waterApc;
            try
            {
                XPathDocument xmlSettings = new XPathDocument(xmlPath);
                XPathNavigator settingsNavigator = xmlSettings.CreateNavigator().SelectSingleNode("/WantedLevelRestyleDispatch/Settings/ToggleKey");
                if (Enum.TryParse(settingsNavigator.Value, out Keys resultAsKey))
                {
                    toggleKey = resultAsKey;
                }
                else
                {
                    WlrException.ShowMessage("Invalid key. NumPad0 will be the default key.", false);
                }
                autoStart = Convert.ToBoolean(settingsNavigator.GetAttribute("autostart", string.Empty));
                settingsNavigator = settingsNavigator.SelectSingleNode("//SpecialSpawns");
                roadBlocks = Convert.ToBoolean(settingsNavigator.GetAttribute("roadblocks", string.Empty));
                heliMission = Convert.ToBoolean(settingsNavigator.GetAttribute("heli_mission", string.Empty));
                waterApc = Convert.ToBoolean(settingsNavigator.GetAttribute("apc_cannon_in_water", string.Empty));
            }
            catch (FormatException)
            {
                throw new WlrException("Invalid attribute in mod settings.", true);
            }
            catch (Exception ex)
            {
                throw new WlrException(ex.Message, true);
            }
            return (toggleKey, autoStart, roadBlocks, heliMission, waterApc);
        }

        public static (int minTime, int maxTime) ReadIntervals(string xmlPath) // Read intervals from XML File (always settings node).
        {
            int minTime = 3000, maxTime = 10000;
            try
            {
                XPathDocument xmlInterval = new XPathDocument(xmlPath);
                minTime = xmlInterval.CreateNavigator().SelectSingleNode("/WantedLevelRestyleDispatch/Settings/MinTimeBetweenSpawn").ValueAsInt;
                maxTime = xmlInterval.CreateNavigator().SelectSingleNode("/WantedLevelRestyleDispatch/Settings/MaxTimeBetweenSpawn").ValueAsInt;
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
            {
                WlrException.ShowMessage("Invalid value. Default min time and max time will be 3000 and 10000.", false);
                return (minTime, maxTime);
            }
            catch (Exception ex)
            {
                throw new WlrException(ex.Message, true);
            }
            if (minTime < 2000 || minTime > 1999999999)
            {
                minTime = 2000;
            }
            if (maxTime > 2000000000 || maxTime < 2000)
            {
                maxTime = 10000;
            }
            if (minTime >= maxTime)
            {
                maxTime = minTime + 1;
                WlrException.ShowMessage("Min time is greater than or equal to max time. The interval between spawn will be equal to min time.", false);
            }
            return (minTime, maxTime);
        }

        public static List<DispatchVehicle> GetDispatchVehicleList(string xmlPath, byte wantedLevel) // Get a list of vehicle models for each wanted level.
        {
            List<DispatchVehicle> dispatchVehicles = new List<DispatchVehicle>(ListCapacity); // List that will be added to dictionary in DispatchServiceScript.cs
            string level;
            try
            {
                XPathDocument xmlDispatch = new XPathDocument(xmlPath);
                XPathNavigator dispatchNavigator = xmlDispatch.CreateNavigator();
                switch (wantedLevel) // To choose the right Xpath query.
                {
                    case 1:
                        level = "OneStar";
                        break;
                    case 2:
                        level = "TwoStars";
                        break;
                    case 3:
                        level = "ThreeStars";
                        break;
                    case 4:
                        level = "FourStars";
                        break;
                    case 5:
                        level = "FiveStars";
                        break;
                    case 6:
                        level = "SixStars";
                        break;
                    default:
                        throw new WlrException("Invalid wanted level parameter.", true);
                }
                foreach (XPathNavigator vehicleNode in dispatchNavigator.Select(".//" + level + "/Vehicle/*")) // Read all attributes of vehicle node.
                {
                    int amount, specialVehicleId;
                    try
                    {
                        amount = vehicleNode.ValueAsInt; // Max amount of vehicle.
                    }
                    catch (Exception)
                    {
                        throw new WlrException("Invalid int value in " + vehicleNode.Name + " vehicle node in " + level + " level.", true);
                    }
                    if (amount < 1)
                    {
                        continue;
                    }
                    else if (amount > 10)
                    {
                        amount = 10;
                    }
                    string attribute = vehicleNode.GetAttribute("special_vehicle", string.Empty); // Check if is special vehicle.
                    switch (attribute)
                    {
                        case "boat":
                            specialVehicleId = 11;
                            break;
                        case "heli":
                            specialVehicleId = 12;
                            break;
                        case "plane":
                            specialVehicleId = 13;
                            break;
                        default:
                            specialVehicleId = 0;
                            break;
                    }
                    (bool isValid, VehicleHash unitHash, OperativeArea dispatchArea) = IsModelValid(vehicleNode.GetAttribute("id", string.Empty), vehicleNode.GetAttribute("area", string.Empty), specialVehicleId);
                    if (!isValid)
                    {
                        throw new WlrException("Invalid attribute in " + vehicleNode.Name + " vehicle node in " + level + " level.", true);
                    }
                    dispatchVehicles.Add(new DispatchVehicle(unitHash, amount, (SpecialVehicleType)specialVehicleId, dispatchArea));
                }
                if (dispatchVehicles.Count == 0) // If true, there's not spawnable vehicles for this wanted level.
                {
                    WlrException.ShowMessage("Vehicle list for " + level + " level is empty.", false);
                }
                else if (dispatchVehicles.Count > ListCapacity) 
                {
                    throw new WlrException(level + " list capacity has been passed.", true);
                }
            }
            catch (WlrException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new WlrException(ex.Message, true);
            }
            return dispatchVehicles;
        }

        static (bool check, VehicleHash outHash, OperativeArea outArea) IsModelValid(string inHash, string inArea, int id) // Check if attributes are valid for a specific vehicle.
        {
            bool check;
            OperativeArea outArea;
            if (!Enum.TryParse(inHash, out AvaiableDispatchVehicle dispatchVehicle) || (int)dispatchVehicle != id)
            {
                check = false;
                return (check, VehicleHash.Police, OperativeArea.SanAndreas);
            }
            check = true;
            Enum.TryParse(inHash, out VehicleHash outHash);
            switch (inArea) // Set the expertise area.
            {
                case "1":
                    outArea = OperativeArea.LosSantos;
                    break;
                case "2":
                    outArea = OperativeArea.BlaineCounty;
                    break;
                case "3":
                    outArea = OperativeArea.MtChiliad;
                    break;
                case "4":
                    outArea = OperativeArea.CoastArea;
                    break;
                default:
                    outArea = OperativeArea.SanAndreas;
                    break;
            }
            return (check, outHash, outArea);
        }

        public static int GetModWantedLevel() // Return a wanted level related to mod instead of game wanted level (so can return even the sixth star).
        {
            if (Function.Call<int>(Hash.GET_FAKE_WANTED_LEVEL) == 6)
            {
                return 6;
            }
            return Game.Player.WantedLevel;
        }

        public static bool IsPlayerInBlaineCounty()
        {
            Vector3 losSantosPoint = new Vector3(-926.746f, -3133.886f, 13.94436f);
            if (Vector3.Distance2D(losSantosPoint, Game.Player.Character.Position) > 5000f)
            {
                return true;
            }
            return false;
        }

        public static bool IsPlayerAroundMtChiliad()
        {
            Vector3 mtChiliadPoint = new Vector3(441.6252f, 5580.923f, 793.5935f);
            if (Vector3.Distance2D(mtChiliadPoint, Game.Player.Character.Position) < 2000f)
            {
                return true;
            }
            return false;
        }

        public static bool IsPlayerInCoastArea()
        {
            string[] coastZones = { "OCEANA", "PROCOB", "PALETO", "PALCOV", "NCHU", "CHU", "DELBE", "BEACH" };
            Vector3 pPos = Game.Player.Character.Position;
            string playerZone = Function.Call<string>(Hash.GET_NAME_OF_ZONE, pPos.X, pPos.Y, pPos.Z);
            foreach (string zone in coastZones)
            {
                if (zone == playerZone)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
