﻿using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using robotManager.Products;
using System;
using System.Collections.Generic;
using System.Threading;
using wManager;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static wManager.Wow.Helpers.PathFinder;

public class ToolBox
{
    // BANNED points
    static Vector3 TBCenter = new Vector3(-1190.982f, 6.03807f, 165.4799f, "None");

    public static void AddState(Engine engine, State state, string replace)
    {
        bool statedAdded = engine.States.Exists(s => s.DisplayName == state.DisplayName);

        if (!statedAdded && engine != null)
        {
            try
            {
                State stateToReplace = engine.States.Find(s => s.DisplayName == replace);

                if (stateToReplace == null)
                {
                    Logger.LogError($"Couldn't find state {replace}");
                    return;
                }

                int priorityToSet = stateToReplace.Priority;

                // Move all superior states one slot up
                foreach (State s in engine.States)
                {
                    if (s.Priority >= priorityToSet)
                        s.Priority++;
                }

                state.Priority = priorityToSet;
                //Logger.Log($"Adding state {state.DisplayName} with prio {priorityToSet}");
                engine.AddState(state);
                engine.States.Sort();
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur : {0}" + ex.ToString());
            }
        }
    }

    public static void RemoveState(Engine engine, string stateToRemove)
    {
        bool stateExists = engine.States.Exists(s => s.DisplayName == stateToRemove);
        if (stateExists && engine != null && engine.States.Count > 5)
        {
            try
            {
                State state = engine.States.Find(s => s.DisplayName == stateToRemove);
                engine.States.Remove(state);
                engine.States.Sort();
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur : {0}" + ex.ToString());
            }
        }
    }

    // Check if Horde
    public static bool GetIsHorde()
    {
        if (ObjectManager.Me.Faction == 35)
        {
            Logger.LogWarning("Wholesome FlightMaster won't work in GM Mode. Please turn GM mode off. " +
                "Once WRobot is started in normal mode, you can turn back GM mode on during runtime.");
            Products.ProductStop();
        }
        bool isHorde = ObjectManager.Me.Faction == (uint)PlayerFactions.Orc 
            || ObjectManager.Me.Faction == (uint)PlayerFactions.Tauren
            || ObjectManager.Me.Faction == (uint)PlayerFactions.Undead 
            || ObjectManager.Me.Faction == (uint)PlayerFactions.BloodElf
            || ObjectManager.Me.Faction == (uint)PlayerFactions.Troll;
        return isHorde;
    }

    public static void DiscoverDefaultNodes()
    {
        if (Main.isHorde)
        {
            if (ObjectManager.Me.PlayerRace == PlayerFactions.Orc || ObjectManager.Me.PlayerRace == PlayerFactions.Troll)
                FlightMasterDB.SetFlightMasterToKnown(3310);
            if (ObjectManager.Me.PlayerRace == PlayerFactions.Tauren)
                FlightMasterDB.SetFlightMasterToKnown(2995);
            if (ObjectManager.Me.PlayerRace == PlayerFactions.Undead)
                FlightMasterDB.SetFlightMasterToKnown(4551);
            if (ObjectManager.Me.PlayerRace == PlayerFactions.BloodElf)
                FlightMasterDB.SetFlightMasterToKnown(16192);
        }
        else
        {
            if (ObjectManager.Me.PlayerRace == PlayerFactions.Gnome || ObjectManager.Me.PlayerRace == PlayerFactions.Dwarf)
                FlightMasterDB.SetFlightMasterToKnown(1573);
            if (ObjectManager.Me.PlayerRace == PlayerFactions.Human)
                FlightMasterDB.SetFlightMasterToKnown(352);
        }
    }

    public static void SetWRobotSettings()
    {
        bool settingchanged = false;
        if (wManagerSetting.CurrentSetting.FlightMasterTaxiUse)
        {
            Main.saveFlightMasterTaxiUse = wManagerSetting.CurrentSetting.FlightMasterTaxiUse;
            wManagerSetting.CurrentSetting.FlightMasterTaxiUse = false;
            settingchanged = true;
        }
        if (wManagerSetting.CurrentSetting.FlightMasterTaxiUseOnlyIfNear)
        {
            Main.saveFlightMasterTaxiUseOnlyIfNear = wManagerSetting.CurrentSetting.FlightMasterTaxiUseOnlyIfNear;
            wManagerSetting.CurrentSetting.FlightMasterTaxiUseOnlyIfNear = false;
            settingchanged = true;
        }
        if (wManagerSetting.CurrentSetting.FlightMasterDiscoverRange > 1)
        {
            Main.saveFlightMasterDiscoverRange = wManagerSetting.CurrentSetting.FlightMasterDiscoverRange;
            wManagerSetting.CurrentSetting.FlightMasterDiscoverRange = 1;
            settingchanged = true;
        }
        if (settingchanged)
        {
            Logger.Log("Disabling WRobot's Taxi");
            wManagerSetting.CurrentSetting.Save();
            SoftRestart();
        }
    }

    public static void SetBlacklistedZones()
    {
        // Avoid Orgrimmar Braseros
        wManagerSetting.AddBlackListZone(new Vector3(1731.702, -4423.403, 36.86293, "None"), 5.00f, true);
        wManagerSetting.AddBlackListZone(new Vector3(1669.99, -4359.609, 29.23425, "None"), 5.00f, true);
    }

    public static void RestoreWRobotSettings()
    {
        wManagerSetting.CurrentSetting.FlightMasterDiscoverRange = Main.saveFlightMasterDiscoverRange;
        wManagerSetting.CurrentSetting.FlightMasterTaxiUse = Main.saveFlightMasterTaxiUse;
        wManagerSetting.CurrentSetting.FlightMasterTaxiUseOnlyIfNear = Main.saveFlightMasterTaxiUseOnlyIfNear;
        wManagerSetting.CurrentSetting.Save();
    }

    public static void Restart()
    {
        new Thread(() =>
        {
            Products.ProductStop();
            Products.ProductStart();
        }).Start();
    }

    public static void SoftRestart()
    {
        Products.InPause = true;
        Thread.Sleep(100);
        Products.InPause = false;
    }

    public static void MessageHandler(LuaEventsId id, List<string> args)
    {
        if (Main.isLaunched)
        {
            string eventName = id.ToString();
            //Logger.Log(eventName);
            if (eventName == "UI_INFO_MESSAGE")
            {
                if (args[0] == "There is no direct path to that destination!")
                    PausePlugin("Unconnected flight");
                if (args[0] == "You don't have enough money!")
                    PausePlugin("Not enough money");
                if (args[0] == "You are too far away from the taxi stand!")
                    Main.clickNodeError = true;
            }
        }
    }

    public static void PausePlugin(string reason)
    {
        Logger.Log($"Pausing plugin for {WFMSettings.CurrentSettings.PauseLengthInSeconds} seconds ({reason})");
        Main.pauseTimer.Restart();
        Main.inPause = true;
    }

    public static void UnPausePlugin()
    {
        if (Main.inPause)
        {
            Logger.Log("Unpausing plugin");
            Main.pauseTimer.Reset();
            Main.inPause = false;
        }
    }

    public static bool PlayerInBloodElfStartingZone()
    {
        string zone = Lua.LuaDoString<string>("return GetRealZoneText();");
        return zone == "Eversong Woods" || zone == "Ghostlands" || zone == "Silvermoon City";
    }

    public static bool PlayerInDraneiStartingZone()
    {
        string zone = Lua.LuaDoString<string>("return GetRealZoneText();");
        return zone == "Azuremyst Isle" || zone == "Bloodmyst Isle" || zone == "The Exodar";
    }

    public static bool FMIsOnMyContinent(FlightMaster fm)
    {
        if (PlayerInBloodElfStartingZone())
            return FMIsInBloodElfStartingZone(fm) || fm.Continent == ContinentId.Azeroth;

        if (PlayerInDraneiStartingZone())
            return FMIsInDraneiStartingZone(fm);

        return !FMIsInDraneiStartingZone(fm) 
            && (fm.Continent == (ContinentId)Usefuls.ContinentId || (FMIsInBloodElfStartingZone(fm) && (ContinentId)Usefuls.ContinentId == ContinentId.Azeroth));
    }

    private static bool FMIsInBloodElfStartingZone(FlightMaster fm)
    {
        return fm.NPCId == 16189
            || fm.NPCId == 16192
            || fm.NPCId == 24851
            || fm.NPCId == 26560;
    }

    private static bool FMIsInDraneiStartingZone(FlightMaster fm)
    {
        return fm.NPCId == 17555
            || fm.NPCId == 17554;
    }

    public static bool FMIsNearbyAndAlive(FlightMaster fm)
    {
        // Only if we're nearby the taxi node
        if (ObjectManager.Me.Position.DistanceTo(fm.Position) > 20)
            return true;

        // 3 attempts to find NPC
        for (int i = 1; i <= 3; i++)
        {
            if (ObjectManager.GetObjectWoWUnit().Exists(unit => unit.Entry == fm.NPCId && unit.IsAlive))
                return true;
            else
                Thread.Sleep(1000);
        }
        return false;
    }

    public static bool ExceptionConditionsAreMet(FlightMaster fm)
    {
        return (fm != null
            && (fm.NPCId != 20234 || fm.Position.DistanceTo(ObjectManager.Me.Position) < 100)) // Shatter Point, Hellfire Peninsula
            && (fm.NPCId != 19581 || GetReputation("The Aldor") > 0) // Altar of Sha'tar, Shadowmoon Valley
            && (fm.NPCId != 21766 || GetReputation("The Scryers") > 0) // Sanctum of the Stars, Shadowmoon Valley
            && (fm.NPCId != 30314 || Quest.GetLogQuestIsComplete(12896) || Quest.GetLogQuestIsComplete(12897) // The Shadow Vault, Icecrown
            && (fm.NPCId != 28037 || Quest.GetLogQuestIsComplete(12523)) // Nesingwary Base Camp, Sholazar Basin
            && (fm.NPCId != 31069 || Quest.GetLogQuestIsComplete(13141)) // Crusaders' Pinnacle, Icecrown
            && (fm.NPCId != 32571 || Quest.GetLogQuestIsComplete(12956)) // Dun Nifflelem, The Storm Peaks
            && (fm.NPCId != 37915 || GetWoWVersion().Equals("3.3.5")) // The Bulwark, Tirisfal
            && (fm.NPCId != 37888 || GetWoWVersion().Equals("3.3.5")) // Thondoril River, Western Plaguelands
            && (fm.NPCId != 29480 || ObjectManager.Me.WowClass == WoWClass.DeathKnight) // Acherus: The Ebon Hold
            );
    }

    private static int GetReputation(string faction)
    {
        return Lua.LuaDoString<int>($@"for i=1, 25 do 
                                local name, _, _, _, _, earnedValue, _, _, _, _, _, _, _ = GetFactionInfo(i);
                                    if name == '{faction}' then
                                        return earnedValue
                                end
                            end");
    }

    public static string GetWoWVersion()
    {
        return Lua.LuaDoString<string>("v, b, d, t = GetBuildInfo(); return v");
    }

    // Count the amount of the specified item stacks in your bags
    public static int CountItemStacks(string itemName)
    {
        return Lua.LuaDoString<int>("local count = GetItemCount('" + itemName + "'); return count");
    }

    // Calculate real walking distance
    public static float CalculatePathTotalDistance(Vector3 from, Vector3 to)
    {
        float distance = 0.0f;
        List<Vector3> path = FindPath(from, to, false);

        for (int i = 0; i < path.Count - 1; ++i)
        {
            distance += path[i].DistanceTo2D(path[i + 1]);
            
            // FIX FOR TB JUMP OFF
            if (path[i].DistanceTo(TBCenter) < 400 && path[i + 1].DistanceTo(path[i]) > 200)
            {
                Logger.Log($"Jump off Thunder Bluff detected {path[i]} to {path[i + 1]}, Trying to find an alternative. Please wait.");
                return 100000;
            }
        }

        return distance;
    }
}
