﻿using robotManager.Events;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using robotManager.Products;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using wManager;
using wManager.Events;
using wManager.Plugin;
using wManager.Wow.Enums;
using wManager.Wow.Forms;
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

    public static string version = "0.0.160"; // Must match version in Version.txt

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

        FiniteStateMachineEvents.OnRunState += StateEventHandler;
        MovementEvents.OnMovementPulse += MovementEventsOnMovementPulse;
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
            if (args[0] == "You don't have enough money!")
            {
                Logger.Log($"Not enough money, bruh :(");
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

    public static void SoftRestart()
    {
        Products.InPause = true;
        Thread.Sleep(1000);
        Products.InPause = false;
    }

    public void Dispose()
    {
        MovementEvents.OnMovementPulse -= MovementEventsOnMovementPulse;
        EventsLuaWithArgs.OnEventsLuaWithArgs -= MessageHandler;
        detectionPulse.DoWork -= BackGroundPulse;

        detectionPulse.Dispose();
        Logger.Log("Disposed");
        stateAddDelayer.Stop();
        isLaunched = false;
    }

    private void StateEventHandler(Engine engine, State state, CancelEventArgs canc)
    {
        if (engine.States.Count <= 5)
        {
            if (!stateAddDelayer.IsRunning)
                SoftRestart(); // hack to wait for correct engine to trigger
            return;
        }

        if (stateAddDelayer.ElapsedMilliseconds <= 0 || stateAddDelayer.ElapsedMilliseconds > 3000 || !stateAddDelayer.IsRunning)
        {
            stateAddDelayer.Restart();

            ToolBox.AddState(engine, new TakeTaxiState(), "FlightMaster: Take taxi");
            ToolBox.AddState(engine, new DiscoverFlightMasterState(), "FlightMaster: Take taxi");
            ToolBox.AddState(engine, new DiscoverContinentFlightsState(), "FlightMaster: Take taxi");
            ToolBox.AddState(engine, new WaitOnTaxiState(), "FlightMaster: Take taxi");

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
        float num = float.MaxValue;
        FlightMaster result = null;

        foreach (FlightMaster flightMaster in FlightMasterDB.FlightMasterList)
        {
            if ((flightMaster.IsDiscovered() || WFMSettings.CurrentSettings.TakeUndiscoveredTaxi)
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
        float num = float.MaxValue;
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
        float num = float.MaxValue;
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

    private static void MovementEventsOnMovementPulse(List<Vector3> points, CancelEventArgs cancelable)
    {
        if (shouldTakeFlight && points.Last() == destinationVector)
            cancelable.Cancel = true;

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
            float totalWalkingDistance = CalculatePathTotalDistance(ObjectManager.Me.Position, points.Last());
            //Logger.Log("Total walking distance for this path : " + totalWalkingDistance);
            Thread.Sleep(Usefuls.Latency + 500);

            from = GetClosestFlightMasterFrom();
            to = GetClosestFlightMasterTo();

            double obligatoryDistance = CalculatePathTotalDistance(ObjectManager.Me.Position, from.Position) + WFMSettings.CurrentSettings.ShorterMinDistance;

            // Calculate total real distance
            double totalDistance = obligatoryDistance + CalculatePathTotalDistance(to.Position, destinationVector);

            // If total real distance does not save any distance or is longer, try to find alternative
            if (totalDistance >= totalWalkingDistance)
            {
                foreach (FlightMaster fm in FlightMasterDB.FlightMasterList)
                {
                    if (fm.Continent == (ContinentId)Usefuls.ContinentId
                        && fm.Position.DistanceTo(destinationVector) < totalWalkingDistance
                        && fm.IsDiscovered())
                    {
                        // Look for the closest available FM near destination
                        double alternativeDistance = obligatoryDistance + CalculatePathTotalDistance(fm.Position, destinationVector);

                        //Logger.Log($"ALT Destination from {fm.Name} is {alternativeDistance}");
                        if (alternativeDistance < totalDistance)
                        {
                            totalDistance = alternativeDistance;
                            to = fm;
                        }
                    }
                }
            }

            Thread.Sleep(1000);

            if (to != null
                && from != null
                && !from.Equals(to)
                && totalDistance <= totalWalkingDistance)
            {
                Logger.Log("Flight path found, taking Taxi from " + from.Name + " to " + to.Name);
                MovementManager.StopMoveNewThread();
                MovementManager.StopMoveToNewThread();
                cancelable.Cancel = true;
                shouldTakeFlight = true;
            }
            else
            {
                Logger.Log("No relevant flight path found");
            }
        }
    }

    public static void PausePlugin()
    {
        if (!inPause)
        {
            Logger.Log($"Pausing plugin for {WFMSettings.CurrentSettings.PauseLengthInSeconds} seconds");
            pauseTimer.Restart();
            inPause = true;
        }
    }

    public static void UnPausePlugin()
    {
        if (inPause)
        {
            Logger.Log("Unpausing plugin");
            pauseTimer.Reset();
            inPause = false;
        }
    }
}
