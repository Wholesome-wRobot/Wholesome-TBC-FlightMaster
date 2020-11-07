using robotManager.FiniteStateMachine;
using System.Threading;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

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
                && !Main.nearestFlightMaster.IsDisabled()
                && ToolBox.ShatterPointFailSafe(Main.nearestFlightMaster) // Shatter Point
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

            // 3 attempts to find NPC
            bool NPCisHere = false;
            for (int i = 1; i <= 3; i++)
            {
                if (!ToolBox.FMIsNearbyAndAlive(flightMaster))
                    Thread.Sleep(1000);
                else
                    NPCisHere = true;
            }

            if (!NPCisHere)
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
