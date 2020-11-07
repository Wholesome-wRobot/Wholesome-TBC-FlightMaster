using robotManager.FiniteStateMachine;
using System.Threading;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;

public class DiscoverContinentFlightsState : State
{
    public override string DisplayName => "WFM Discovering Continent Flights";

    public DiscoverContinentFlightsState() { }

    public override bool NeedToRun
    {
        get
        {
            if (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                && Main.isLaunched
                && !Main.inPause
                && Main.nearestFlightMaster != null
                && Main.nearestFlightMaster.NPCId != 18930 // horde dark portal
                && Main.nearestFlightMaster.NPCId != 18931 // alliance dark portal
                && !ToolBox.PlayerInBloodElfStartingZone()
                && !ToolBox.PlayerInDraneiStartingZone()
                && ((ContinentId)Usefuls.ContinentId == ContinentId.Azeroth && !WFMDeepSettings.CurrentSettings.EKDiscoveredFlights
                || (ContinentId)Usefuls.ContinentId == ContinentId.Kalimdor && !WFMDeepSettings.CurrentSettings.KalimdorDiscoveredFlights
                || (ContinentId)Usefuls.ContinentId == ContinentId.Expansion01 && !WFMDeepSettings.CurrentSettings.OutlandsDiscoveredFlights
                || (ContinentId)Usefuls.ContinentId == ContinentId.Northrend && !WFMDeepSettings.CurrentSettings.NorthrendDiscoveredFlights))
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
        Logger.Log($"Discovering known flights on continent {(ContinentId)Usefuls.ContinentId} at {flightMaster.Name}");

        bool allInvalid = true;

        for(int j = 0; j < 3; j++) // failsafe
        {
            if (GoToTask.ToPositionAndIntecractWithNpc(flightMaster.Position, flightMaster.NPCId, /*(int)GossipOptionsType.taxi)*/1))
            {
                Thread.Sleep(3000);
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
                    Logger.Log($"All flight paths are invalid, retrying ({j+1})");
                    Thread.Sleep(1000);
                    continue;
                }
                else
                {
                    if ((ContinentId)Usefuls.ContinentId == ContinentId.Azeroth)
                        WFMDeepSettings.CurrentSettings.EKDiscoveredFlights = true;
                    if ((ContinentId)Usefuls.ContinentId == ContinentId.Kalimdor)
                        WFMDeepSettings.CurrentSettings.KalimdorDiscoveredFlights = true;
                    if ((ContinentId)Usefuls.ContinentId == ContinentId.Expansion01)
                        WFMDeepSettings.CurrentSettings.OutlandsDiscoveredFlights = true;
                    if ((ContinentId)Usefuls.ContinentId == ContinentId.Northrend)
                        WFMDeepSettings.CurrentSettings.NorthrendDiscoveredFlights = true;

                    WFMDeepSettings.CurrentSettings.Save();
                    Logger.Log("Known flight paths succesfully recorded");
                    Thread.Sleep(1000);
                    return;
                }
            }
        }
        // all invalid
        ToolBox.PausePlugin("Couldn't find a valid flight path");
    }
}
