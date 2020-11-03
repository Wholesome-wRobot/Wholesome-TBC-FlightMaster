using robotManager.FiniteStateMachine;
using System.Threading;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;

public class DiscoverFlightMasterState : State
{
    public override string DisplayName => "WFM Discovering Flight Master";

    public DiscoverFlightMasterState() { }

    public override bool NeedToRun
    {
        get
        {
            if (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                && Main.isLaunched
                && Main.nearestFlightMaster != null
                && !WFMSettings.CurrentSettings.KnownFlightsList.Contains(Main.nearestFlightMaster.Name))
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
        MovementManager.StopMoveToNewThread();
        FlightMaster flightMaster = Main.nearestFlightMaster;
        Logger.Log($"Discovering flight master {flightMaster.Name}");

        if (GoToTask.ToPositionAndIntecractWithNpc(flightMaster.Position, flightMaster.NPCId, (int)GossipOptionsType.taxi))
        {
            Thread.Sleep(1500);
            FlightMasterDB.SetFlightMasterToKnown(flightMaster.NPCId);
            ToolBox.UnPausePlugin();
            Main.shouldTakeFlight = false;
            Thread.Sleep(1000);
        }
    }
}
