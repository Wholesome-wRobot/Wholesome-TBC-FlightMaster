using robotManager.FiniteStateMachine;
using robotManager.Products;
using System;
using System.Collections.Generic;
using System.Threading;
using wManager;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class ToolBox
{
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
                //Logger.Log($"Adding state {state.DisplayName}");
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
        if (wManagerSetting.CurrentSetting.FlightMasterTaxiUse
            || wManagerSetting.CurrentSetting.FlightMasterTaxiUseOnlyIfNear
            || wManagerSetting.CurrentSetting.FlightMasterDiscoverRange > 1)
        {
            Logger.Log("Disabling WRobot's Taxi");
            wManagerSetting.CurrentSetting.FlightMasterTaxiUse = false;
            wManagerSetting.CurrentSetting.FlightMasterTaxiUseOnlyIfNear = false;
            wManagerSetting.CurrentSetting.FlightMasterDiscoverRange = 1;
            wManagerSetting.CurrentSetting.Save();
            SoftRestart();
        }
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
        Thread.Sleep(1000);
        Products.InPause = false;
    }

    public static void MessageHandler(LuaEventsId id, List<string> args)
    {
        if (Main.isLaunched)
        {
            if (id == LuaEventsId.UI_INFO_MESSAGE)
            {
                if (args[0] == "There is no direct path to that destination!")
                    PausePlugin("Unconnected flight");
                if (args[0] == "You don't have enough money!")
                    PausePlugin("Not enough money");
            }

            if (id == LuaEventsId.TAXIMAP_OPENED)
                Main.isTaxiMapOpened = true;
            if (id == LuaEventsId.TAXIMAP_CLOSED)
                Main.isTaxiMapOpened = false;
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
        bool FMDetected = false;
        List<WoWUnit> surroundingEnemies = ObjectManager.GetObjectWoWUnit();
        foreach (WoWUnit unit in surroundingEnemies)
        {
            if (unit.Entry == fm.NPCId && unit.IsAlive)
                FMDetected = true;
        }
        return FMDetected;
    }

    public static bool ShatterPointFailSafe(FlightMaster fm)
    {
        return fm.NPCId != 20234 || fm.Position.DistanceTo(ObjectManager.Me.Position) < 100;
    }
}
