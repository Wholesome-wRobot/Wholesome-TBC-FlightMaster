using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System;
using System.Collections.Generic;
using wManager;
using wManager.Wow.Enums;
using wManager.Wow.ObjectManager;
using static wManager.Wow.Helpers.PathFinder;

public class WFMSetup
{
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
            ToolBox.SoftRestart();
        }
    }

    public static void RestoreWRobotSettings()
    {
        wManagerSetting.CurrentSetting.FlightMasterDiscoverRange = Main.saveFlightMasterDiscoverRange;
        wManagerSetting.CurrentSetting.FlightMasterTaxiUse = Main.saveFlightMasterTaxiUse;
        wManagerSetting.CurrentSetting.FlightMasterTaxiUseOnlyIfNear = Main.saveFlightMasterTaxiUseOnlyIfNear;
        wManagerSetting.CurrentSetting.Save();
    }

    public static void AddTransportOffMesh(
        Vector3 waitForTransport,
        Vector3 stepIn,
        Vector3 objectDeparture,
        Vector3 objectArrival,
        Vector3 stepOut,
        int objectId,
        ContinentId continentId,
        string name = "",
        float precision = 0.5f)
    {
        OffMeshConnection offMeshConnection = new OffMeshConnection(new List<Vector3>
            {
                waitForTransport,
                new Vector3(stepIn.X, stepIn.Y, stepIn.Z, "None")
                {
                    Action = "c#: Logging.WriteNavigator(\"Waiting for transport\"); " +
                    "while (Conditions.InGameAndConnectedAndProductStartedNotInPause) " +
                    "{ " +
                        $"var elevator = ObjectManager.GetWoWGameObjectByEntry({objectId}).OrderBy(o => o.GetDistance).FirstOrDefault(); " +
                        $"if (elevator != null && elevator.IsValid && elevator.Position.DistanceTo(new Vector3({objectDeparture.X.ToString().Replace(",", ".")}, {objectDeparture.Y.ToString().Replace(",", ".")}, {objectDeparture.Z.ToString().Replace(",", ".")})) < {precision.ToString().Replace(",", ".")}) " +
                            "break; " +
                        "Thread.Sleep(100); " +
                    "}"
                },
                new Vector3(stepOut.X, stepOut.Y, stepOut.Z, "None")
                {
                    Action = "c#: Logging.WriteNavigator(\"Wait to leave Elevator\"); " +
                    "while (Conditions.InGameAndConnectedAndProductStartedNotInPause) " +
                    "{ " +
                        $"var elevator = ObjectManager.GetWoWGameObjectByEntry({objectId}).OrderBy(o => o.GetDistance).FirstOrDefault(); " +
                        $"if (elevator != null && elevator.IsValid && elevator.Position.DistanceTo(new Vector3({objectArrival.X.ToString().Replace(",", ".")}, {objectArrival.Y.ToString().Replace(",", ".")}, {objectArrival.Z.ToString().Replace(",", ".")})) < {precision.ToString().Replace(",", ".")}) " +
                            "break; " +
                        "Thread.Sleep(100); " +
                    "}"
                },
            }, (int)continentId, OffMeshConnectionType.Unidirectional, true);
        offMeshConnection.Name = name;
        OffMeshConnections.Add(offMeshConnection, true);
    }

    public static void SetBlacklistedZonesAndOffMeshConnections()
    {
        // Avoid Orgrimmar Braseros
        wManagerSetting.AddBlackListZone(new Vector3(1731.702, -4423.403, 36.86293), 5, ContinentId.Kalimdor);
        wManagerSetting.AddBlackListZone(new Vector3(1669.99, -4359.609, 29.23425), 5, ContinentId.Kalimdor);

        // Warsong hold top elevator
        wManagerSetting.AddBlackListZone(new Vector3(2892.18, 6236.34, 208.908), 15, ContinentId.Northrend);

        OffMeshConnections.MeshConnection.Clear();

        AddTransportOffMesh(new Vector3(2865.628, 6211.75, 104.262), // wait for transport
            new Vector3(2878.712, 6224.032, 105.3798), // Step in
            new Vector3(2878.315, 6223.635, 105.3792), // Object departure
            new Vector3(2892.18, 6236.34, 208.908), // Object arrival
            new Vector3(2880.497, 6226.416, 208.7462, "None"), // Step out
            188521,
            ContinentId.Northrend,
            "Warsong Hold Elevator UP");

        AddTransportOffMesh(new Vector3(2880.497, 6226.416, 208.7462, "None"), // wait for transport
            new Vector3(2891.717, 6236.516, 208.9086, "None"), // Step in
            new Vector3(2892.18, 6236.34, 208.908), // Object departure
            new Vector3(2878.315, 6223.635, 105.3792), // Object arrival
            new Vector3(2865.628, 6211.75, 104.262), // Step out
            188521,
            ContinentId.Northrend,
            "Warsong Hold Elevator DOWN");

        AddTransportOffMesh(new Vector3(4219.52, 3126.461, 184.3423, "None"), // wait for transport
            new Vector3(4208.915, 3111.077, 184.3453, "None"), // Step in
            new Vector3(4208.69, 3111.24, 183.8219), // Object departure
            new Vector3(4208.69, 3111.24, 335.2971), // Object arrival
            new Vector3(4196.539, 3095.831, 335.8202, "None"), // Step out
            184330,
            ContinentId.Expansion01,
            "Stormspire elevator UP");

        AddTransportOffMesh(new Vector3(4197.577, 3095.454, 335.8203, "None"), // wait for transport
            new Vector3(4209.05, 3111.383, 335.8167, "None"), // Step in
            new Vector3(4208.69, 3111.24, 335.2971), // Object departure
            new Vector3(4208.69, 3111.24, 183.8219), // Object arrival
            new Vector3(4219.52, 3126.461, 184.3423, "None"), // Step out
            184330,
            ContinentId.Expansion01,
            "Stormspire elevator DOWN");
    }

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
}
