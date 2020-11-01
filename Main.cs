using robotManager.Events;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using robotManager.Products;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using wManager;
using wManager.Events;
using wManager.Plugin;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class Main : IPlugin
{
    public static bool isLaunched;
    private readonly BackgroundWorker detectionPulse = new BackgroundWorker();
    public static FlightMaster nearestFlightMaster = null;
    public static Vector3 destinationVector = null;

    protected Stopwatch stateAddDelayer = new Stopwatch();

    public static bool inPause;
    public static Stopwatch pauseTimer = new Stopwatch();

    public static FlightMaster from = null;
    public static FlightMaster to = null;
    public static bool shouldTakeFlight = false;

    public static string version = "0.0.01"; // Must match version in Version.txt

    public void Initialize()
    {
        isLaunched = true;

        WFMSettings.Load();
        WFMDeepSettings.Load();

        if (AutoUpdater.CheckUpdate(version))
        {
            Restart();
            return;
        }

        Logger.Log($"Launching version {version} on client {Lua.LuaDoString<string>("v, b, d, t = GetBuildInfo(); return v")}");
        MovementManager.StopMoveNewThread();
        MovementManager.StopMoveToNewThread();

        FlightMasterDB.Initialize();
        SetWRobotSettings();
        DiscoverDefaultNodes();

        detectionPulse.DoWork += BackGroundPulse;
        detectionPulse.RunWorkerAsync();

        FiniteStateMachineEvents.OnAfterRunState += AddStates;
        MovementEvents.OnMovementPulse += MovementEventsOnOnMovementPulse;
        EventsLuaWithArgs.OnEventsLuaWithArgs += MessageHandler;
    }

    private void MessageHandler(LuaEventsId id, List<string> args)
    {
        if (isLaunched && id == LuaEventsId.UI_INFO_MESSAGE)
        {
            if (args[0] == "There is no direct path to that destination!")
            {
                Logger.Log($"Unconnected flight");
                PausePlugin();
            }
        }
    }

    public void Restart()
    {
        Logger.Log("Restarting");
        new Thread(() =>
        {
            Products.ProductStop();
            Thread.Sleep(1000);
            Products.ProductStart();
        }).Start();
    }

    public void Dispose()
    {
        MovementEvents.OnMovementPulse -= MovementEventsOnOnMovementPulse;
        detectionPulse.DoWork -= BackGroundPulse;
        EventsLuaWithArgs.OnEventsLuaWithArgs -= MessageHandler;
        detectionPulse.Dispose();
        Logger.Log("Disposed");
        stateAddDelayer.Reset();
        isLaunched = false;
    }

    private void AddStates(Engine engine, State state)
    {
        if (engine.States.Count <= 5)
            return;

        if (stateAddDelayer.ElapsedMilliseconds <= 0 || stateAddDelayer.ElapsedMilliseconds > 3000)
        {
            stateAddDelayer.Restart();

            ToolBox.AddState(engine, new TakeTaxiState(), "Flight master discover");
            ToolBox.AddState(engine, new DiscoverFlightMasterState(), "Flight master discover");
            ToolBox.AddState(engine, new DiscoverContinentFlightsState(), "Flight master discover");
            ToolBox.AddState(engine, new WaitOnTaxiState(), "Flight master discover");

            // Double check because some profiles modify WRobot settings
            SetWRobotSettings();
            /*
            Logger.Log($"****************************");
            foreach (State s in engine.States)
            {
                Logger.Log($"{s.Priority} : {s.DisplayName}");
            }
            Logger.Log($"****************************");
            */
        }
    }

    private void DiscoverDefaultNodes()
    {
        if (ObjectManager.Me.PlayerRace == PlayerFactions.Orc || ObjectManager.Me.PlayerRace == PlayerFactions.Troll)
            FlightMasterDB.SetFlightMasterToKnown(3310);
        if (ObjectManager.Me.PlayerRace == PlayerFactions.Tauren)
            FlightMasterDB.SetFlightMasterToKnown(2995);
        if (ObjectManager.Me.PlayerRace == PlayerFactions.Undead)
            FlightMasterDB.SetFlightMasterToKnown(4551);
        // TODO Ajouter Blood elf
    }

    private void SetWRobotSettings()
    {
        if (!wManagerSetting.CurrentSetting.FlightMasterTaxiUse 
            && !wManagerSetting.CurrentSetting.FlightMasterTaxiUseOnlyIfNear
            && wManagerSetting.CurrentSetting.FlightMasterDiscoverRange == 1)
            return;

        Logger.Log("Disabling WRobot's Taxi");
        wManagerSetting.CurrentSetting.FlightMasterTaxiUse = false;
        wManagerSetting.CurrentSetting.FlightMasterTaxiUseOnlyIfNear = false;
        wManagerSetting.CurrentSetting.FlightMasterDiscoverRange = 1;
    }

    public void Settings()
    {
        WFMSettings.Load();
        WFMSettings.CurrentSettings.ToForm();
        WFMSettings.CurrentSettings.Save();
    }

    private void BackGroundPulse(object sender, DoWorkEventArgs args)
    {
        while (isLaunched)
        {
            try
            {
                if (inPause && pauseTimer.ElapsedMilliseconds > WFMSettings.CurrentSettings.PauseLengthInSeconds * 1000)
                {
                    Logger.Log($"{WFMSettings.CurrentSettings.PauseLengthInSeconds} seconds elapsed in pause");
                    UnPausePlugin();
                    MovementManager.StopMoveNewThread();
                    MovementManager.StopMoveToNewThread();
                }

                if (Conditions.InGameAndConnectedAndProductStartedNotInPause 
                    && !ObjectManager.Me.InCombatFlagOnly
                    && !ObjectManager.Me.IsOnTaxi 
                    && ObjectManager.Me.IsAlive)
                {
                    nearestFlightMaster = GetNearestFlightMaster();
                }
            }
            catch (Exception arg)
            {
                Logger.LogError(string.Concat(arg));
            }
            Thread.Sleep(3000);
        }
    }

    private FlightMaster GetNearestFlightMaster()
    {
        foreach (FlightMaster flightMaster in FlightMasterDB.FlightMasterList)
        {
            if ((ContinentId)Usefuls.ContinentId == flightMaster.Continent
            && ObjectManager.Me.Position.DistanceTo(flightMaster.Position) < (double)WFMSettings.CurrentSettings.DetectTaxiDistance)
            {
                return flightMaster;
            }
        }
        return null;
    }

    private static float CalculatePathTotalDistance(Vector3 from, Vector3 to)
    {
        float distance = 0.0f;
        List<Vector3> path = PathFinder.FindPath(from, to, false);

        for (int index = 0; index < path.Count - 1; ++index)
        {
            distance += path[index].DistanceTo2D(path[index + 1]);
        }

        return distance;
    }

    public static FlightMaster GetClosestFlightMasterFrom()
    {
        float num = 99999f;
        FlightMaster result = null;

        foreach (FlightMaster flightMaster in FlightMasterDB.FlightMasterList)
        {
            if (flightMaster.IsDiscovered()
                && flightMaster.Position.DistanceTo(ObjectManager.Me.Position) < num
                && flightMaster.Continent == (ContinentId)Usefuls.ContinentId)
            {
                num = flightMaster.Position.DistanceTo(ObjectManager.Me.Position);
                result = flightMaster;
            }
        }

        return result;
    }

    public static FlightMaster GetClosestFlightMasterTo()
    {
        float num = 99999f;
        FlightMaster result = null;

        foreach (FlightMaster flightMaster in FlightMasterDB.FlightMasterList)
        {
            if (flightMaster.IsDiscovered()
                && flightMaster.Position.DistanceTo(destinationVector) < num
                && flightMaster.Continent == (ContinentId)Usefuls.ContinentId)
            {
                num = flightMaster.Position.DistanceTo(destinationVector);
                result = flightMaster;
            }
        }
        return result;
    }

    // Requires FM map open
    public static FlightMaster GetBestAlternativeTo(List<string> reachableTaxis)
    {
        float num = 99999f;
        FlightMaster result = null;
        foreach (FlightMaster flightMaster in FlightMasterDB.FlightMasterList)
        {
            if (flightMaster.Position.DistanceTo(destinationVector) < num
                && reachableTaxis.Contains(flightMaster.Name)
                && flightMaster.Position.DistanceTo(destinationVector) < from.Position.DistanceTo(destinationVector))
            {
                num = flightMaster.Position.DistanceTo(destinationVector);
                result = flightMaster;
            }
        }
        return result;
    }

    private static void MovementEventsOnOnMovementPulse(List<Vector3> points, CancelEventArgs cancelable)
    {
        if (!ObjectManager.Me.IsAlive || ObjectManager.Me.IsOnTaxi || shouldTakeFlight || !isLaunched || inPause)
            return;

        // If we have detected a potential FP travel
        if (ObjectManager.Me.Position.DistanceTo(points.Last()) > (double)WFMSettings.CurrentSettings.TaxiTriggerDistance)
        {
            if (WFMSettings.CurrentSettings.SkipIfFollowPath
                && Logging.Status.Contains("Follow Path")
                && !Logging.Status.Contains("Resurrect")
                && CalculatePathTotalDistance(ObjectManager.Me.Position, points.Last()) < (double)WFMSettings.CurrentSettings.SkipIfFollowPathDistance)
            {
                Logger.Log("Currently following path or distance to start (" + CalculatePathTotalDistance(ObjectManager.Me.Position, ((IEnumerable<Vector3>)points).Last()) + " yards) is smaller than setting value (" + WFMSettings.CurrentSettings.SkipIfFollowPathDistance + " yards)");
                Thread.Sleep(1000);
                return;
            }

            destinationVector = points.Last();
            float _saveDistance = CalculatePathTotalDistance(ObjectManager.Me.Position, points.Last());
            Thread.Sleep(Usefuls.Latency + 500);

            from = GetClosestFlightMasterFrom();
            to = GetClosestFlightMasterTo();

            Thread.Sleep(1000);

            if (to != null
                && from != null
                && !from.Equals(to)
                && CalculatePathTotalDistance(ObjectManager.Me.Position, from.Position) 
                + (double)CalculatePathTotalDistance(to.Position, destinationVector) 
                + WFMSettings.CurrentSettings.ShorterMinDistance <= _saveDistance)
            {
                Logger.Log("Shorter path detected, taking Taxi from " + from.Name + " to " + to.Name);
                cancelable.Cancel = true;
                shouldTakeFlight = true;
            }
            else
            {
                Logger.Log("No shorter path available, skip flying");
            }
        }
    }

    public static void PausePlugin()
    {
        Logger.Log($"Pausing plugin for {WFMSettings.CurrentSettings.PauseLengthInSeconds} seconds");
        pauseTimer.Restart();
        inPause = true;
    }

    public static void UnPausePlugin()
    {
        Logger.Log("Unpausing plugin");
        pauseTimer.Reset();
        inPause = false;
    }
}
