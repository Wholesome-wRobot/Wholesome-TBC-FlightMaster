using robotManager.Events;
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
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Math = System.Math;

public class Main : IPlugin
{
    public static bool isLaunched;
    private readonly BackgroundWorker detectionPulse = new BackgroundWorker();
    public static FlightMaster nearestFlightMaster = null;
    public static FlightMaster flightMasterToDiscover = null;
    public static Vector3 destinationVector = null;
    private static State currentState = null;

    protected Stopwatch stateAddDelayer = new Stopwatch();

    public static bool inPause;
    public static Stopwatch pauseTimer = new Stopwatch();

    public static FlightMaster from = null;
    public static FlightMaster to = null;
    public static bool shouldTakeFlight = false;
    public static bool isHorde;
    public static bool isFMMapOpen;
    public static bool isGossipOpen;

    // Errors handling
    public static bool errorTooFarAwayFromTaxiStand = false;
    private int stuckCount = 0;
    private DateTime lastStuck = DateTime.Now;

    public static string version = "1.1.06"; // Must match version in Version.txt

    // Saved settings
    public static bool saveFlightMasterTaxiUse = false;
    public static bool saveFlightMasterTaxiUseOnlyIfNear = false;
    public static float saveFlightMasterDiscoverRange = 1;

    // Custom states
    public static State discoverFlightMasterState = new DiscoverFlightMasterState();
    public static State takeTaxiState = new TakeTaxiState();
    public static State waitOnTaxiState = new WaitOnTaxiState();

    public void Initialize()
    {
        if (!Products.ProductName.Equals("Quester") 
            && !Products.ProductName.Equals("Grinder")
            && !Products.ProductName.Equals("Wholesome Professions WotLK"))
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
        WFMSetup.SetBlacklistedZonesAndOffMeshConnections();
        WFMSetup.DiscoverDefaultNodes();

        detectionPulse.DoWork += BackGroundPulse;
        detectionPulse.RunWorkerAsync();

        FiniteStateMachineEvents.OnRunState += StateEventHandler;
        MovementEvents.OnMovementPulse += MovementEventsOnMovementPulse;
        MovementEvents.OnSeemStuck += SeemStuckHandler;
        EventsLuaWithArgs.OnEventsLuaWithArgs += ToolBox.MessageHandler;

        EventsLua.AttachEventLua((LuaEventsId)Enum.Parse(typeof(LuaEventsId), "TAXIMAP_OPENED"), (e) => isFMMapOpen = true);
        EventsLua.AttachEventLua((LuaEventsId)Enum.Parse(typeof(LuaEventsId), "TAXIMAP_CLOSED"), (e) => isFMMapOpen = false);
        EventsLua.AttachEventLua((LuaEventsId)Enum.Parse(typeof(LuaEventsId), "GOSSIP_SHOW"), (e) => isGossipOpen = true);
        EventsLua.AttachEventLua((LuaEventsId)Enum.Parse(typeof(LuaEventsId), "GOSSIP_CLOSED"), (e) => isGossipOpen = false);
    }

    public void Dispose()
    {
        MovementEvents.OnMovementPulse -= MovementEventsOnMovementPulse;
        EventsLuaWithArgs.OnEventsLuaWithArgs -= ToolBox.MessageHandler;
        detectionPulse.DoWork -= BackGroundPulse;

        WFMSetup.RestoreWRobotSettings();
        detectionPulse.Dispose();
        Logger.Log("Disposed");
        stateAddDelayer.Stop();
        isLaunched = false;
    }

    private void SeemStuckHandler()
    {
        if (DateTime.Now.Ticks / 10000000 - lastStuck.Ticks / 10000000 < 5
            && (currentState == discoverFlightMasterState || currentState == takeTaxiState))
        {
            stuckCount++;
            Logger.Log($"You're stuck ({stuckCount}/10)");

            if (stuckCount > 9)
            {
                if (currentState == discoverFlightMasterState)
                {
                    MovementManager.StopMove();
                    nearestFlightMaster.Disable("Unreachable");
                }
                if (currentState == takeTaxiState)
                {
                    MovementManager.StopMove();
                    from?.Disable("Unreachable");
                }
            }
        }
        else
            stuckCount = 0;

        lastStuck = DateTime.Now;
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

            WFMSetup.AddState(engine, takeTaxiState, "FlightMaster: Take taxi");
            WFMSetup.AddState(engine, discoverFlightMasterState, "FlightMaster: Take taxi");
            WFMSetup.AddState(engine, waitOnTaxiState, "FlightMaster: Take taxi");

            // Double check because some profiles modify WRobot settings
            WFMSetup.SetWRobotSettings();
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

                    // Mark flightmaster as To be discovered
                    if (nearestFlightMaster != null
                        && !nearestFlightMaster.IsDisabledByPlugin()
                        && ToolBox.ExceptionConditionsAreMet(nearestFlightMaster)
                        && !WFMSettings.CurrentSettings.KnownFlightsList.Contains(nearestFlightMaster.Name)
                        && ToolBox.CalculatePathTotalDistance(ObjectManager.Me.Position, nearestFlightMaster.Position) < WFMSettings.CurrentSettings.DetectTaxiDistance * 1.2)
                        flightMasterToDiscover = nearestFlightMaster;

                    // Hook for HMP states locks and others
                    if (discoverFlightMasterState.NeedToRun && currentState?.Priority < discoverFlightMasterState.Priority)
                    {
                        Logger.Log("Stop on tracks to ensure discovery");
                        MovementManager.StopMove();
                        MovementManager.StopMoveTo();
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
        List<FlightMaster> orderedFMList = FlightMasterDB.FlightMasterList
            .FindAll(fm => ObjectManager.Me.Position.DistanceTo(fm.Position) < (double)WFMSettings.CurrentSettings.DetectTaxiDistance && ToolBox.FMIsOnMyContinent(fm))
            .OrderBy(fm => fm.Position.DistanceTo(ObjectManager.Me.Position))
            .ToList();

        return orderedFMList.Count > 0 ? orderedFMList.First() : null;
    }

    public static FlightMaster GetClosestFlightMasterFrom(float maxRadius)
    {
        FlightMaster result = null;

        // Pre order the list
        List<FlightMaster> orderedListFM = FlightMasterDB.FlightMasterList
            .FindAll(fm => (fm.IsDiscovered || WFMSettings.CurrentSettings.TakeUndiscoveredTaxi) 
                && ToolBox.FMIsOnMyContinent(fm)
                && !fm.IsDisabledByPlugin())
            .OrderBy(fm => fm.Position.DistanceTo(ObjectManager.Me.Position)).ToList();

        foreach (FlightMaster flightMaster in orderedListFM)
        {
            if (flightMaster.Position.DistanceTo(ObjectManager.Me.Position) < maxRadius)
            {
                float realDist = ToolBox.CalculatePathTotalDistance(ObjectManager.Me.Position, flightMaster.Position);
                Logger.Log($"[FROM] {flightMaster.Name} is {Math.Round(realDist)} yards away");
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
            .FindAll(fm => fm.IsDiscovered && fm.NPCId != from.NPCId && ToolBox.FMIsOnMyContinent(fm))
            .OrderBy(fm => fm.Position.DistanceTo(destinationVector)).ToList();

        foreach (FlightMaster flightMaster in orderedListFM)
        {
            if (flightMaster.Position.DistanceTo(destinationVector) < maxRadius)
            {
                float realDist = ToolBox.CalculatePathTotalDistance(flightMaster.Position, destinationVector);
                Logger.Log($"[TO] {flightMaster.Name} is {Math.Round(realDist)} yards away from destination");
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
                float realDist = ToolBox.CalculatePathTotalDistance(flightMaster.Position, destinationVector);
                Logger.Log($"[TO2] {flightMaster.Name} is {Math.Round(realDist)} yards away from destination");
                if (realDist < num)
                {
                    num = realDist;
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
        float totalWalkingDistance = ToolBox.CalculatePathTotalDistance(ObjectManager.Me.Position, points.Last());

            // If the path is shorter than setting, we skip
        if (totalWalkingDistance < (double)WFMSettings.CurrentSettings.TaxiTriggerDistance)
        {
            //Logger.LogDebug($"Path ({Math.Round(totalWalkingDistance)} yards) is shorter than trigger setting {WFMSettings.CurrentSettings.TaxiTriggerDistance}. Let's walk.");
            return;
        }

        Logger.Log($"{Math.Round(totalWalkingDistance)} yards path is longer than trigger setting {WFMSettings.CurrentSettings.TaxiTriggerDistance}. " +
            $"Searching for flights.");

        if (Logging.Status.Contains("Follow Path")
            && !Logging.Status.Contains("Resurrect")
            && totalWalkingDistance < (double)WFMSettings.CurrentSettings.SkipIfFollowPathDistance)
        {
            Logger.Log($"Currently following path. {totalWalkingDistance} yards is smaller than trigger setting {WFMSettings.CurrentSettings.SkipIfFollowPathDistance} yards. Ignoring flights.");
            return;
        }

        destinationVector = points.Last();

        from = GetClosestFlightMasterFrom(totalWalkingDistance);

        if (from == null)
        {
            Logger.Log("No FROM found");
            return;
        }

        double distanceToNearestFM = ToolBox.CalculatePathTotalDistance(ObjectManager.Me.Position, from.Position);
        double departureDistance = distanceToNearestFM + WFMSettings.CurrentSettings.MinimumDistanceSaving;

        to = GetClosestFlightMasterTo(from.Position.DistanceTo(destinationVector));

        //Logger.Log($"Best FROM is {from?.Name}");
        //Logger.Log($"Best TO is {to?.Name}");

        if (from.Equals(to))
            to = null;

        // Calculate total real distance FROM/TO
        // if no TO found, we set the total distance back to walking distance
        double processedDistance;
        if (to == null)
            processedDistance = totalWalkingDistance;
        else
            processedDistance = departureDistance + ToolBox.CalculatePathTotalDistance(to.Position, destinationVector);

        //Logger.Log($"Walking distance is {Math.Round(totalWalkingDistance)}");
        //Logger.Log($"Processed distance is {Math.Round(processedDistance)}");

        // If total real distance does not save any distance or is longer, try to find alternative
        if (processedDistance >= totalWalkingDistance
            || to == null)
        {
            if (to == null)
                Logger.Log($"No direct flight path, trying to find an alternative, please wait");
            else
                Logger.Log($"Flight from {from.Name} to {to.Name} would save {Math.Round(totalWalkingDistance - processedDistance + WFMSettings.CurrentSettings.MinimumDistanceSaving)} yards. " +
                    $"You set a minimum of {WFMSettings.CurrentSettings.MinimumDistanceSaving} yards. Trying to find an alternative.");

            // Pre order the list
            List<FlightMaster> orderedListFM = FlightMasterDB.FlightMasterList
                .FindAll(fm => fm.IsDiscovered && fm.NPCId != from.NPCId && ToolBox.FMIsOnMyContinent(fm))
                .OrderBy(fm => fm.Position.DistanceTo(destinationVector)).ToList();

            foreach (FlightMaster flightMaster in orderedListFM)
            {
                if (flightMaster.Position.DistanceTo(destinationVector) + departureDistance < processedDistance)
                {
                    // Look for the closest available FM near destination
                    double alternativeDistance = departureDistance + ToolBox.CalculatePathTotalDistance(flightMaster.Position, destinationVector);

                    Logger.Log($"Alternative TO : {flightMaster.Name} ({Math.Round(alternativeDistance)} yards total)");
                    if (alternativeDistance < processedDistance)
                    {
                        processedDistance = alternativeDistance;
                        to = flightMaster;
                    }
                }
            }
        }

        if (to != null
            && from != null
            && !from.Equals(to)
            && processedDistance <= totalWalkingDistance)
        {
            double realProcessedDistance = Math.Round(processedDistance - WFMSettings.CurrentSettings.MinimumDistanceSaving);
            Logger.Log($"Flight found for {Math.Round(totalWalkingDistance)} yards path. Processed distance is {realProcessedDistance} yards." +
                $" Taking Taxi from {from.Name} to {to.Name}. (You will save {Math.Round(totalWalkingDistance) - realProcessedDistance} yards)");
            MovementManager.StopMoveNewThread();
            MovementManager.StopMoveToNewThread();
            cancelable.Cancel = true;
            shouldTakeFlight = true;
        }
        else
        {
            Logger.Log($"No relevant flight found for {Math.Round(totalWalkingDistance)} yards path");
        }
        Logger.Log($"Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");
    }
}
