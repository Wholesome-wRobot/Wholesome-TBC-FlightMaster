using robotManager.FiniteStateMachine;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class DiscoverFlightMasterState : State
{
    public override string DisplayName => "WFM Discovering Flight Master";

    public override bool NeedToRun
    {
        get
        {
            if (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                && Main.isLaunched
                && Main.flightMasterToDiscover != null
                && !Main.flightMasterToDiscover.IsDisabledByPlugin()
                && !ObjectManager.Me.InTransport)
                return true;
            else
                return false;
        }
    }

    public override void Run()
    {
        MovementManager.StopMoveNewThread();
        MovementManager.StopMoveToNewThread();
        FlightMaster fmToDiscover = Main.flightMasterToDiscover;
        Logger.Log($"Discovering flight master {fmToDiscover.Name}");

        // We go to the position
        if (WFMMoveInteract.GoInteractwithFM(fmToDiscover.Position, fmToDiscover))
        {
            FlightMasterDB.SetFlightMasterToKnown(fmToDiscover.NPCId);
            Main.flightMasterToDiscover = null;
            ToolBox.UnPausePlugin();
            Main.shouldTakeFlight = false;

            FlightMasterDB.UpdateKnownFMs(fmToDiscover);
            MovementManager.StopMove(); // reset path
        }
    }
}
