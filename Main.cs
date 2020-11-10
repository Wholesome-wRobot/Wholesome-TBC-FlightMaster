﻿using robotManager.Events;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using robotManager.Products;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using wManager.Events;
using wManager.Plugin;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static wManager.Wow.Helpers.PathFinder;
using Math = System.Math;

public class Main : IPlugin
{
    public static bool isLaunched;
    private readonly BackgroundWorker detectionPulse = new BackgroundWorker();
    public static FlightMaster nearestFlightMaster = null;
    public static Vector3 destinationVector = null;
    private static State currentState = null;

    protected Stopwatch stateAddDelayer = new Stopwatch();

    public static bool inPause;
    public static Stopwatch pauseTimer = new Stopwatch();

    public static FlightMaster from = null;
    public static FlightMaster to = null;
    public static bool shouldTakeFlight = false;
    public static bool isTaxiMapOpened = false;
    public static bool isHorde;

    public static string version = "0.0.195"; // Must match version in Version.txt

    // BANNED points
    static Vector3 TBjumpPoint = new Vector3(-1005.205f, 302.6988f, 135.8554f, "None");

    // Saved settings
    public static bool saveFlightMasterTaxiUse = false;
    public static bool saveFlightMasterTaxiUseOnlyIfNear = false;
    public static float saveFlightMasterDiscoverRange = 1;

    // Custom states
    private State discoverContinentFlightState = new DiscoverContinentFlightsState();
    private State discoverFlightMasterState = new DiscoverFlightMasterState();
    private State takeTaxiState = new TakeTaxiState();
    private State waitOnTaxiState = new WaitOnTaxiState();

    public void Initialize()
    {
        if (!Products.ProductName.Equals("Quester") 
            && !Products.ProductName.Equals("Grinder"))
            return;

        isLaunched = true;

        isHorde = ToolBox.GetIsHorde();

        WFMSettings.Load();
        WFMDeepSettings.Load();

        if (AutoUpdater.CheckUpdate(version))
        {
            Logger.LogWarning("New version downloaded, restarting WRobot, please wait");
            ToolBox.Restart();
            return;
        }

        Logger.Log($"Launching version {version} on client {ToolBox.GetWoWVersion()}");
        MovementManager.StopMoveNewThread();
        MovementManager.StopMoveToNewThread();

        FlightMasterDB.Initialize();
        //ToolBox.SetWRobotSettings();
        ToolBox.DiscoverDefaultNodes();

        detectionPulse.DoWork += BackGroundPulse;
        detectionPulse.RunWorkerAsync();

        FiniteStateMachineEvents.OnRunState += StateEventHandler;
        MovementEvents.OnMovementPulse += MovementEventsOnMovementPulse;
        EventsLuaWithArgs.OnEventsLuaWithArgs += ToolBox.MessageHandler;
    }

    public void Dispose()
    {
        MovementEvents.OnMovementPulse -= MovementEventsOnMovementPulse;
        EventsLuaWithArgs.OnEventsLuaWithArgs -= ToolBox.MessageHandler;
        detectionPulse.DoWork -= BackGroundPulse;

        ToolBox.RestoreWRobotSettings();
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
                ToolBox.SoftRestart(); // hack to wait for correct engine to trigger
            return;
        }

        currentState = state;

        if (stateAddDelayer.ElapsedMilliseconds <= 0 || stateAddDelayer.ElapsedMilliseconds > 3000 || !stateAddDelayer.IsRunning)
        {
            stateAddDelayer.Restart();

            ToolBox.AddState(engine, takeTaxiState, "FlightMaster: Take taxi");
            ToolBox.AddState(engine, discoverFlightMasterState, "FlightMaster: Take taxi");
            ToolBox.AddState(engine, discoverContinentFlightState, "FlightMaster: Take taxi");
            ToolBox.AddState(engine, waitOnTaxiState, "FlightMaster: Take taxi");

            // Double check because some profiles modify WRobot settings
            ToolBox.SetWRobotSettings();
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
                    ToolBox.UnPausePlugin();
                    MovementManager.StopMoveNewThread();
                    MovementManager.StopMoveToNewThread();
                }

                if (Conditions.InGameAndConnectedAndProductStartedNotInPause 
                    && !ObjectManager.Me.InCombatFlagOnly
                    && !ObjectManager.Me.IsOnTaxi 
                    && ObjectManager.Me.IsAlive)
                {
                    nearestFlightMaster = GetNearestFlightMaster();

                    // Hook for HMP states locks
                    if (currentState.DisplayName.Contains("Training")
                        && (discoverFlightMasterState.NeedToRun || discoverContinentFlightState.NeedToRun))
                    {
                        Logger.Log("Stop on training tracks");
                        MovementManager.StopMove();
                    }
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
        FlightMaster nearest = null;
        foreach (FlightMaster flightMaster in FlightMasterDB.FlightMasterList)
        {
            if (ToolBox.FMIsOnMyContinent(flightMaster)
            && ObjectManager.Me.Position.DistanceTo(flightMaster.Position) < (double)WFMSettings.CurrentSettings.DetectTaxiDistance
            && (nearest == null || nearest.Position.DistanceTo(ObjectManager.Me.Position) > flightMaster.Position.DistanceTo(ObjectManager.Me.Position)))
            {
                nearest = flightMaster;
            }
        }
        return nearest;
    }

    private static float CalculatePathTotalDistance(Vector3 from, Vector3 to)
    {
        float distance = 0.0f;
        List<Vector3> path = FindPath(from, to, false);

        for (int i = 0; i < path.Count - 1; ++i)
        {
            distance += path[i].DistanceTo2D(path[i + 1]);
            
            // FIX FOR TB JUMP OFF
            if (path[i].DistanceTo(TBjumpPoint) < 50 && path[i + 1].DistanceTo(TBjumpPoint) > 200)
            {
                Logger.Log("Jump off TB detected, skipping");
                return 999999999f;
            }
        }

        return distance;
    }

    public static FlightMaster GetClosestFlightMasterFrom(float maxRadius)
    {
        FlightMaster result = null;

        // Pre order the list
        List<FlightMaster> orderedListFM = FlightMasterDB.FlightMasterList
            .FindAll(fm => (fm.IsDiscovered() || WFMSettings.CurrentSettings.TakeUndiscoveredTaxi) && ToolBox.FMIsOnMyContinent(fm))
            .OrderBy(fm => fm.Position.DistanceTo(ObjectManager.Me.Position)).ToList();

        foreach (FlightMaster flightMaster in orderedListFM)
        {
            if (flightMaster.Position.DistanceTo(ObjectManager.Me.Position) < maxRadius)
            {
                float realDist = CalculatePathTotalDistance(ObjectManager.Me.Position, flightMaster.Position);
                Logger.Log($"[FROM] Me to {flightMaster.Name} is {Math.Round(realDist)} yards");
                if (realDist < maxRadius)
                {
                    maxRadius = realDist;
                    result = flightMaster;
                }
            }
        }
        return result;
    }

    public static FlightMaster GetClosestFlightMasterTo(float maxRadius)
    {
        FlightMaster result = null;

        // Pre order the list
        List<FlightMaster> orderedListFM = FlightMasterDB.FlightMasterList
            .FindAll(fm => fm.IsDiscovered() && ToolBox.FMIsOnMyContinent(fm))
            .OrderBy(fm => fm.Position.DistanceTo(destinationVector)).ToList();

        foreach (FlightMaster flightMaster in orderedListFM)
        {
            if (flightMaster.Position.DistanceTo(destinationVector) < maxRadius)
            {
                float realDist = CalculatePathTotalDistance(flightMaster.Position, destinationVector);
                Logger.Log($"[TO] {flightMaster.Name} to destination is {Math.Round(realDist)} yards");
                if (realDist < maxRadius)
                {
                    maxRadius = realDist;
                    result = flightMaster;
                }
            }
        }
        return result;
    }

    // Requires FM map open
    public static FlightMaster GetBestAlternativeTo(List<string> reachableTaxis)
    {
        float num = ObjectManager.Me.Position.DistanceTo(destinationVector);
        FlightMaster resultFM = null;

        // Pre order the list
        List<FlightMaster> orderedListFM = FlightMasterDB.FlightMasterList
            .FindAll(fm => reachableTaxis.Contains(fm.Name))
            .OrderBy(fm => fm.Position.DistanceTo(destinationVector)).ToList();

        foreach (FlightMaster flightMaster in orderedListFM)
        {
            if (flightMaster.Position.DistanceTo(destinationVector) < num)
            {
                float realDist = CalculatePathTotalDistance(flightMaster.Position, destinationVector);
                Logger.Log($"[TO2] {flightMaster.Name} to destination is {Math.Round(realDist)} yards");
                if (realDist < num)
                {
                    num = flightMaster.Position.DistanceTo(destinationVector);
                    resultFM = flightMaster;
                }
            }
        }
        return resultFM;
    }

    private static void MovementEventsOnMovementPulse(List<Vector3> points, CancelEventArgs cancelable)
    {
        if (shouldTakeFlight 
            && points.Last() == destinationVector 
            && !inPause)
        {
            Logger.Log("Cancelled move to " + destinationVector);
            cancelable.Cancel = true;
        }

        if (!ObjectManager.Me.IsAlive || ObjectManager.Me.IsOnTaxi || shouldTakeFlight || !isLaunched || inPause)
            return;

        DateTime dateBegin = DateTime.Now;

        // If we have detected a potential FP travel
        float totalWalkingDistance = CalculatePathTotalDistance(ObjectManager.Me.Position, points.Last());
        Logger.Log($"Path detected - {totalWalkingDistance} yards");

        // If the path is shorter than setting, we skip
        if (totalWalkingDistance < (double)WFMSettings.CurrentSettings.TaxiTriggerDistance)
        {
            Logger.Log($"Path ({Math.Round(totalWalkingDistance)} yards) is shorter than setting {WFMSettings.CurrentSettings.TaxiTriggerDistance}. Let's walk.");
            return;
        }

        if (WFMSettings.CurrentSettings.SkipIfFollowPath
            && Logging.Status.Contains("Follow Path")
            && !Logging.Status.Contains("Resurrect")
            && totalWalkingDistance < (double)WFMSettings.CurrentSettings.SkipIfFollowPathDistance)
        {
            Logger.Log($"Currently following path. {totalWalkingDistance} yards is smaller than setting {WFMSettings.CurrentSettings.SkipIfFollowPathDistance} yards. Ignoring flights.");
            return;
        }

        destinationVector = points.Last();
        Logger.Log($"Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");

        from = GetClosestFlightMasterFrom(totalWalkingDistance);
        Logger.Log($"Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");

        if (from == null)
            return;

        to = GetClosestFlightMasterTo(totalWalkingDistance);
        Logger.Log($"Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");

        Logger.Log($"Best FROM is {from?.Name}");
        Logger.Log($"Best TO is {to?.Name}");

        if (from.Equals(to))
            to = null;

        double distanceToNearestFM = CalculatePathTotalDistance(ObjectManager.Me.Position, from.Position);
        double obligatoryDistance = distanceToNearestFM + WFMSettings.CurrentSettings.ShorterMinDistance;

        // Calculate total real distance FROM/TO
        // if no TO found, we set the total distance back to walking distance
        double totalDistance;
        if (to != null)
            totalDistance = obligatoryDistance + CalculatePathTotalDistance(to.Position, destinationVector);
        else
            totalDistance = totalWalkingDistance;

        Logger.Log($"Walking distance is {Math.Round(totalWalkingDistance)}");
        Logger.Log($"Processed distance is {Math.Round(totalDistance)}");

        // If total real distance does not save any distance or is longer, try to find alternative
        if (totalDistance >= totalWalkingDistance
            || to == null)
        {
            Logger.Log("Direct flight path is impossible, trying to find an alternative, please wait");
            foreach (FlightMaster flightMaster in FlightMasterDB.FlightMasterList)
            {
                if (ToolBox.FMIsOnMyContinent(flightMaster)
                    && flightMaster.Position.DistanceTo(destinationVector) + obligatoryDistance < totalWalkingDistance
                    && flightMaster.IsDiscovered()
                    && !flightMaster.Equals(from))
                {
                    // Look for the closest available FM near destination
                    double alternativeDistance = obligatoryDistance + CalculatePathTotalDistance(flightMaster.Position, destinationVector);

                    Logger.Log($"Alternative TO : {flightMaster.Name} ({Math.Round(alternativeDistance)} yards total)");
                    if (alternativeDistance < totalDistance)
                    {
                        totalDistance = alternativeDistance;
                        to = flightMaster;
                    }
                }
            }
        }

        if (to != null
            && from != null
            && !from.Equals(to)
            && totalDistance <= totalWalkingDistance)
        {
            Logger.Log($"Flight found for {Math.Round(totalWalkingDistance)} yards path, processed distance is {Math.Round(totalDistance)} yards, taking Taxi from " + from.Name + " to " + to.Name);
            MovementManager.StopMoveNewThread();
            MovementManager.StopMoveToNewThread();
            cancelable.Cancel = true;
            shouldTakeFlight = true;
            Logger.Log($"Process time : {(DateTime.Now.Ticks - dateBegin.Ticks)/10000} ms");
        }
        else
        {
            Logger.Log($"No relevant flight found for {Math.Round(totalWalkingDistance)} yards path");
            Logger.Log($"Process time : {(DateTime.Now.Ticks - dateBegin.Ticks)/10000} ms");
        }
    }
}
