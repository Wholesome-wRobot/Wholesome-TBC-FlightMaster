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
    public static bool isTaxiMapOpened = false;
    public static bool isHorde;

    // BANNED points
    static Vector3 TBjumpPoint = new Vector3(-1005.205f, 302.6988f, 135.8554f, "None");
    static Vector3 DesolacePointAfterTBJump = new Vector3(-706.7505f, 579.7277f, 154.6033f, "None");

    public static string version = "0.0.180"; // Must match version in Version.txt

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

        Logger.Log($"Launching version {version} on client {Lua.LuaDoString<string>("v, b, d, t = GetBuildInfo(); return v")}");
        MovementManager.StopMoveNewThread();
        MovementManager.StopMoveToNewThread();

        FlightMasterDB.Initialize();
        ToolBox.SetWRobotSettings();
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

        if (stateAddDelayer.ElapsedMilliseconds <= 0 || stateAddDelayer.ElapsedMilliseconds > 3000 || !stateAddDelayer.IsRunning)
        {
            stateAddDelayer.Restart();

            ToolBox.AddState(engine, new TakeTaxiState(), "FlightMaster: Take taxi");
            ToolBox.AddState(engine, new DiscoverFlightMasterState(), "FlightMaster: Take taxi");
            ToolBox.AddState(engine, new DiscoverContinentFlightsState(), "FlightMaster: Take taxi");
            ToolBox.AddState(engine, new WaitOnTaxiState(), "FlightMaster: Take taxi");

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
            if (path[i].DistanceTo(TBjumpPoint) < 50 && path[i + 1].DistanceTo(DesolacePointAfterTBJump) < 50)
            {
                Logger.Log("Jump off TB detected, skipping");
                return 999999999f;
            }
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
                && ToolBox.FMIsOnMyContinent(flightMaster))
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
                && ToolBox.FMIsOnMyContinent(flightMaster))
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
        if (shouldTakeFlight 
            && points.Last() == destinationVector 
            && !inPause)
        {
            Logger.Log("Cancelled move to " + destinationVector);
            cancelable.Cancel = true;
        }

        if (!ObjectManager.Me.IsAlive || ObjectManager.Me.IsOnTaxi || shouldTakeFlight || !isLaunched || inPause)
            return;

        // If we have detected a potential FP travel
        float totalWalkingDistance = CalculatePathTotalDistance(ObjectManager.Me.Position, points.Last());            
        Logger.Log($"Path detected : {totalWalkingDistance} yards.");

        if (totalWalkingDistance > (double)WFMSettings.CurrentSettings.TaxiTriggerDistance)
        {
            if (WFMSettings.CurrentSettings.SkipIfFollowPath
                && Logging.Status.Contains("Follow Path")
                && !Logging.Status.Contains("Resurrect")
                && totalWalkingDistance < (double)WFMSettings.CurrentSettings.SkipIfFollowPathDistance)
            {
                Logger.Log($"Currently following path. {totalWalkingDistance} yards is smaller than setting {WFMSettings.CurrentSettings.SkipIfFollowPathDistance} yards. Ignoring flights.");
                Thread.Sleep(1000);
                return;
            }

            destinationVector = points.Last();
            Thread.Sleep(Usefuls.Latency + 500);

            from = GetClosestFlightMasterFrom();
            to = GetClosestFlightMasterTo();

            if (from == null)
                return;

            if (from.Equals(to))
                to = null;

            double obligatoryDistance = CalculatePathTotalDistance(ObjectManager.Me.Position, from.Position) + WFMSettings.CurrentSettings.ShorterMinDistance;

            // Calculate total real distance FROM/TO
            // if no TO found, we set it back to walking distance
            double totalDistance;
            if (to != null)
                totalDistance = obligatoryDistance + CalculatePathTotalDistance(to.Position, destinationVector);
            else
                totalDistance = totalWalkingDistance;

            // If total real distance does not save any distance or is longer, try to find alternative
            if (totalDistance >= totalWalkingDistance
                || to == null)
            {
                //Logger.Log("Direct flight path is impossible, trying to find an alternative, please wait");
                foreach (FlightMaster flightMaster in FlightMasterDB.FlightMasterList)
                {
                    if (ToolBox.FMIsOnMyContinent(flightMaster)
                        && flightMaster.Position.DistanceTo(destinationVector) < totalWalkingDistance
                        && flightMaster.IsDiscovered()
                        && !flightMaster.Equals(from))
                    {
                        // Look for the closest available FM near destination
                        double alternativeDistance = obligatoryDistance + CalculatePathTotalDistance(flightMaster.Position, destinationVector);

                        if (alternativeDistance < totalDistance)
                        {
                            totalDistance = alternativeDistance;
                            to = flightMaster;
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
        else
        {
            Logger.Log($"Travel distance {totalWalkingDistance} is shorter than setting {WFMSettings.CurrentSettings.TaxiTriggerDistance}. Let's walk.");
        }
    }
}
