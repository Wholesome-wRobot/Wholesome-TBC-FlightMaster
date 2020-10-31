using robotManager.Events;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public static bool _isLaunched;
    private readonly BackgroundWorker _detectionPulse = new BackgroundWorker();
    public static FlightMaster nearestFlightMaster = null;
    public static Vector3 destinationVector = null;

    public static FlightMaster from = null;
    public static FlightMaster to = null;
    public static bool shouldTakeFlight = false;

    public void Initialize()
    {
        Logger.Log("Initialization");
        _isLaunched = true;

        WholesomeTBCFlightMasterSettings.Load();
        FlightMasterDB.Initialize();
        SetWRobotSettings();

        _detectionPulse.DoWork += DetectionPulse;
        _detectionPulse.RunWorkerAsync();

        FiniteStateMachineEvents.OnAfterRunState += AddStates;
        MovementEvents.OnMovementPulse += MovementEventsOnOnMovementPulse;
    }

    public void Dispose()
    {
        MovementEvents.OnMovementPulse -= MovementEventsOnOnMovementPulse;
        _detectionPulse.DoWork -= DetectionPulse;
        _detectionPulse.Dispose();
        Logger.Log("Disposed");
        _isLaunched = false;
    }

    private void AddStates(Engine engine, State state)
    {
        DiscoverContinentFlightsState.AddState(engine, state);
        DiscoverFlightMasterState.AddState(engine, state);
        TakeTaxiState.AddState(engine, state);
        WaitOnTaxiState.AddState(engine, state);
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
        if (!wManagerSetting.CurrentSetting.FlightMasterTaxiUse && !wManagerSetting.CurrentSetting.FlightMasterTaxiUseOnlyIfNear)
            return;

        Logger.Log("Disabling WRobot's Taxi");
        wManagerSetting.CurrentSetting.FlightMasterTaxiUse = false;
        wManagerSetting.CurrentSetting.FlightMasterTaxiUseOnlyIfNear = false;
    }

    public void Settings()
    {
        WholesomeTBCFlightMasterSettings.Load();
        WholesomeTBCFlightMasterSettings.CurrentSettings.ToForm();
        WholesomeTBCFlightMasterSettings.CurrentSettings.Save();
    }

    // Detection pulse
    private void DetectionPulse(object sender, DoWorkEventArgs args)
    {
        while (_isLaunched)
        {
            try
            {
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
            Thread.Sleep(1000);
        }
    }

    private FlightMaster GetNearestFlightMaster()
    {
        foreach (FlightMaster flightMaster in FlightMasterDB.FlightMasterList)
        {
            if ((ContinentId)Usefuls.ContinentId == flightMaster.Continent
            && ObjectManager.Me.Position.DistanceTo(flightMaster.Position) < (double)WholesomeTBCFlightMasterSettings.CurrentSettings.DetectTaxiDistance)
            {
                return flightMaster;
            }
        }
        return null;
    }

    private static float CalculatePathTotalDistance(Vector3 from, Vector3 to)
    {
        float distance = 0.0f;
        List<Vector3> vectorList = new List<Vector3>();
        List<Vector3> path = PathFinder.FindPath(from, to);

        for (int index = 0; index < path.Count - 1; ++index)
            distance += path[index].DistanceTo2D(path[index + 1]);

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
        /*
        if (result == null)
            Logger.Log("Closest FROM FlightMaster is null");
        else
            Logger.Log("Closest FROM FlightMaster is " + result.Name);*/

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
        /*
        if (result == null)
            Logger.Log("Closest TO FlightMaster is null");
        else
            Logger.Log("Closest TO FlightMaster is " + result.Name);
        */
        return result;
    }
    private static void MovementEventsOnOnMovementPulse(List<Vector3> points, CancelEventArgs cancelable)
    {
        if (!ObjectManager.Me.IsAlive || ObjectManager.Me.IsOnTaxi)
            return;

        // If we have detected a potential FP travel
        if (ObjectManager.Me.Position.DistanceTo(((IEnumerable<Vector3>)points).Last()) > (double)WholesomeTBCFlightMasterSettings.CurrentSettings.TaxiTriggerDistance)
        {
            if (WholesomeTBCFlightMasterSettings.CurrentSettings.SkipIfFollowPath
                && Logging.Status.Contains("Follow Path")
                && !Logging.Status.Contains("Resurrect")
                && CalculatePathTotalDistance(ObjectManager.Me.Position, ((IEnumerable<Vector3>)points).Last()) < (double)WholesomeTBCFlightMasterSettings.CurrentSettings.SkipIfFollowPathDistance)
            {
                Logger.Log("Currently following path or distance to start (" + CalculatePathTotalDistance(ObjectManager.Me.Position, ((IEnumerable<Vector3>)points).Last()) + " yards) is smaller than setting value (" + WholesomeTBCFlightMasterSettings.CurrentSettings.SkipIfFollowPathDistance + " yards)");
                Thread.Sleep(1000);
                return;
            }

            destinationVector = ((IEnumerable<Vector3>)points).Last();
            float _saveDistance = CalculatePathTotalDistance(ObjectManager.Me.Position, ((IEnumerable<Vector3>)points).Last());
            Thread.Sleep(Usefuls.Latency + 500);

            from = GetClosestFlightMasterFrom();
            to = GetClosestFlightMasterTo();

            Thread.Sleep(1000);

            if (to != null
                && from != null
                && !from.Equals(to)
                && CalculatePathTotalDistance(ObjectManager.Me.Position, from.Position) + (double)CalculatePathTotalDistance(to.Position, destinationVector) + WholesomeTBCFlightMasterSettings.CurrentSettings.ShorterMinDistance <= _saveDistance)
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
}
