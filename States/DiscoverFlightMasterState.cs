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
                && Main.nearestFlightMaster != null
                && !Main.nearestFlightMaster.IsDisabledByPlugin()
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
        if (GoToTask.ToPositionAndIntecractWithNpc(flightMaster.Position, flightMaster.NPCId))
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

            FlightMasterDB.SetFlightMasterToKnown(flightMaster.NPCId);
            ToolBox.UnPausePlugin();
            Main.shouldTakeFlight = false;


            // Check if FM is here or dead
            if (!ToolBox.FMIsNearbyAndAlive(flightMaster))
            {
                Logger.Log($"FlightMaster is absent or dead. Disabling it for {WFMSettings.CurrentSettings.PauseLengthInSeconds} seconds");
                flightMaster.Disable();
                return;
            }
            return;

            /*
            if (!ToolBox.OpenTaxiMapSuccess(flightMaster))
            {
                // Check if FM is here or dead
                if (!ToolBox.FMIsNearbyAndAlive(flightMaster))
                {
                    Logger.Log($"FlightMaster is absent or dead. Disabling it for {WFMSettings.CurrentSettings.PauseLengthInSeconds} seconds");
                    flightMaster.Disable();
                    return;
                }
                return;
            }*/
            FlightMasterDB.UpdateKnownFMs();
            MovementManager.StopMove(); // reset path
        }
    }
}
