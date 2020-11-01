using robotManager.FiniteStateMachine;
using System;
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
                && Main.nearestFlightMaster != null
                && ((ContinentId)Usefuls.ContinentId == ContinentId.Azeroth && !WholesomeFlightMasterDeepSettings.CurrentSettings.EKDiscoveredFlights
                || (ContinentId)Usefuls.ContinentId == ContinentId.Kalimdor && !WholesomeFlightMasterDeepSettings.CurrentSettings.KalimdorDiscoveredFlights
                || (ContinentId)Usefuls.ContinentId == ContinentId.Expansion01 && !WholesomeFlightMasterDeepSettings.CurrentSettings.OutlandsDiscoveredFlights
                || (ContinentId)Usefuls.ContinentId == ContinentId.Northrend && !WholesomeFlightMasterDeepSettings.CurrentSettings.NorthrendDiscoveredFlights))
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
        WholesomeTBCWotlkFlightMasterSettings settings = WholesomeTBCWotlkFlightMasterSettings.CurrentSettings;

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
                    WholesomeFlightMasterDeepSettings.CurrentSettings.EKDiscoveredFlights = true;
                if ((ContinentId)Usefuls.ContinentId == ContinentId.Kalimdor)
                    WholesomeFlightMasterDeepSettings.CurrentSettings.KalimdorDiscoveredFlights = true;
                if ((ContinentId)Usefuls.ContinentId == ContinentId.Expansion01)
                    WholesomeFlightMasterDeepSettings.CurrentSettings.OutlandsDiscoveredFlights = true;
                if ((ContinentId)Usefuls.ContinentId == ContinentId.Northrend)
                    WholesomeFlightMasterDeepSettings.CurrentSettings.NorthrendDiscoveredFlights = true;

                WholesomeFlightMasterDeepSettings.CurrentSettings.Save();
                Logger.Log("Known flight paths succesfully recorded");
                Thread.Sleep(1000);
            }
        }
    }
}
