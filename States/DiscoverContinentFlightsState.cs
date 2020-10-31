using robotManager.FiniteStateMachine;
using System;
using System.Threading;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;

public class DiscoverContinentFlightsState : State
{
    public override string DisplayName => "Discovering Continent Flights";
    public static bool StateAddedToFSM { get; set; }

    public DiscoverContinentFlightsState() { }

    public override bool NeedToRun
    {
        get
        {
            if (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                && Main._isLaunched
                && Main.nearestFlightMaster != null
                && ((ContinentId)Usefuls.ContinentId == ContinentId.Azeroth && !WholesomeTBCFlightMasterSettings.CurrentSettings.EKDiscoveredFlights
                || (ContinentId)Usefuls.ContinentId == ContinentId.Kalimdor && !WholesomeTBCFlightMasterSettings.CurrentSettings.KalimdorDiscoveredFlights
                || (ContinentId)Usefuls.ContinentId == ContinentId.Expansion01 && !WholesomeTBCFlightMasterSettings.CurrentSettings.OutlandsDiscoveredFlights
                || (ContinentId)Usefuls.ContinentId == ContinentId.Northrend && !WholesomeTBCFlightMasterSettings.CurrentSettings.NorthrendDiscoveredFlights))
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
        Logger.Log($"Discovering known flights on continent {(ContinentId)Usefuls.ContinentId} ({flightMaster.Name})");
        WholesomeTBCFlightMasterSettings settings = WholesomeTBCFlightMasterSettings.CurrentSettings;

        if (GoToTask.ToPositionAndIntecractWithNpc(flightMaster.Position, flightMaster.NPCId, (int)GossipOptionsType.taxi))
        {
            Thread.Sleep(3000);
            bool allInvalid = true;
            for (int i = 0; i < 30; i++)
            {
                string nodeName = Lua.LuaDoString<string>($"return TaxiNodeName({i})");
                if (nodeName != "INVALID")
                {
                    allInvalid = false;
                    FlightMasterDB.SetFlightMasterToKnown(nodeName);
                }
                else
                {
                    FlightMasterDB.SetFlightMasterToUnknown(nodeName);
                }
            }

            if (allInvalid)
            {
                Logger.Log("All flight paths are invalid, retrying");
                Thread.Sleep(1000);
                return;
            }
            else
            {
                if ((ContinentId)Usefuls.ContinentId == ContinentId.Azeroth)
                    WholesomeTBCFlightMasterSettings.CurrentSettings.EKDiscoveredFlights = true;
                if ((ContinentId)Usefuls.ContinentId == ContinentId.Kalimdor)
                    WholesomeTBCFlightMasterSettings.CurrentSettings.KalimdorDiscoveredFlights = true;
                if ((ContinentId)Usefuls.ContinentId == ContinentId.Expansion01)
                    WholesomeTBCFlightMasterSettings.CurrentSettings.OutlandsDiscoveredFlights = true;
                if ((ContinentId)Usefuls.ContinentId == ContinentId.Northrend)
                    WholesomeTBCFlightMasterSettings.CurrentSettings.NorthrendDiscoveredFlights = true;

                WholesomeTBCFlightMasterSettings.CurrentSettings.Save();
                Logger.Log("Known flight paths succesfully recorded");
                Thread.Sleep(1000);
            }
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

                DiscoverContinentFlightsState discoverContinentFlightsState = new DiscoverContinentFlightsState { Priority = taxiState.Priority };
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
