using robotManager.FiniteStateMachine;
using System.Threading;
using wManager.Wow.Bot.Tasks;
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
                && Main.nearestFlightMaster != null
                && !Main.nearestFlightMaster.IsDisabled()
                && ToolBox.ExceptionConditionsAreMet(Main.nearestFlightMaster)
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

        // We go to the position
        if (GoToTask.ToPosition(flightMaster.Position, 0.5f))
        {
            // Dismount
            if (ObjectManager.Me.IsMounted)
                MountTask.DismountMount();

            if (!ToolBox.FMIsNearbyAndAlive(flightMaster))
            {
                Logger.Log($"FlightMaster is absent or dead. Disabling it for {WFMSettings.CurrentSettings.PauseLengthInSeconds} seconds");
                flightMaster.Disable();
                return;
            }

            if (GoToTask.ToPositionAndIntecractWithNpc(flightMaster.Position, flightMaster.NPCId, 1))
            {
                Thread.Sleep(500);
                FlightMasterDB.SetFlightMasterToKnown(flightMaster.NPCId);
                ToolBox.UnPausePlugin();
                Main.shouldTakeFlight = false;
                Thread.Sleep(500);
            }
        }
    }
}
