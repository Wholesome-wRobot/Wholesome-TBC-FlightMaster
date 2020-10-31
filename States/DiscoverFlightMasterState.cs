using robotManager.FiniteStateMachine;
using System;
using System.Threading;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;

public class DiscoverFlightMasterState : State
{
    public override string DisplayName => "Discovering FlightMaster";
    public static bool StateAddedToFSM { get; set; }

    public DiscoverFlightMasterState() { }

    public override bool NeedToRun
    {
        get
        {
            if (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                && Main._isLaunched
                && Main.nearestFlightMaster != null
                && !WholesomeTBCFlightMasterSettings.CurrentSettings.KnownFlightsList.Contains(Main.nearestFlightMaster.Name))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public override void Run()
    {
        MovementManager.StopMoveNewThread();
        FlightMaster flightMaster = Main.nearestFlightMaster;
        Logger.Log($"Discovering flight master {flightMaster.Name}");

        WholesomeTBCFlightMasterSettings settings = WholesomeTBCFlightMasterSettings.CurrentSettings;

        if (GoToTask.ToPositionAndIntecractWithNpc(flightMaster.Position, flightMaster.NPCId, (int)GossipOptionsType.taxi))
        {
            Thread.Sleep(1500);
            FlightMasterDB.SetFlightMasterToKnown(flightMaster.NPCId);
            Thread.Sleep(1000);
        }
    }

    public static void AddState(Engine engine, State state)
    {
        if (!StateAddedToFSM && engine != null && engine.States.Count > 5)
        {
            try
            {
                State taxiState = engine.States.Find(s => s.DisplayName == "Flight master discover");

                if (taxiState == null)
                {
                    Logger.LogError("Couldn't find taxi state");
                    StateAddedToFSM = true;
                    return;
                }

                DiscoverFlightMasterState discoverContinentFlightsState = new DiscoverFlightMasterState { Priority = taxiState.Priority };
                engine.AddState(discoverContinentFlightsState);
                engine.States.Sort();
                StateAddedToFSM = true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur : {0}" + ex.ToString());
            }
        }
    }
}
