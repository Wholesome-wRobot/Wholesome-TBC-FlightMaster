using robotManager.FiniteStateMachine;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
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
        FlightMaster flightMasterToDiscover = Main.flightMasterToDiscover;
        Logger.Log($"Discovering flight master {flightMasterToDiscover.Name}");

        // We go to the position
        if (GoToTask.ToPositionAndIntecractWithNpc(flightMasterToDiscover.Position, flightMasterToDiscover.NPCId))
        {
            // Dismount
            if (ObjectManager.Me.IsMounted)
                MountTask.DismountMount();

            if (ObjectManager.Me.InCombatFlagOnly)
            {
                Logger.Log("You are in combat");
                return;
            }

            Usefuls.SelectGossipOption(GossipOptionsType.taxi);

            FlightMasterDB.SetFlightMasterToKnown(flightMasterToDiscover.NPCId);
            Main.flightMasterToDiscover = null;
            ToolBox.UnPausePlugin();
            Main.shouldTakeFlight = false;

            FlightMasterDB.UpdateKnownFMs(flightMasterToDiscover);
            MovementManager.StopMove(); // reset path
        }

        // Check if FM is here or dead
        if (!ToolBox.FMIsNearbyAndAlive(flightMasterToDiscover))
        {
            flightMasterToDiscover.Disable("FlightMaster is absent or dead.");
            Main.flightMasterToDiscover = null;
            return;
        }
    }
}
