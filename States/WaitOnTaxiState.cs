using robotManager.FiniteStateMachine;
using System.Threading;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class WaitOnTaxiState : State
{
    public override string DisplayName => "WFM Waiting on Taxi";

    public override bool NeedToRun
    {
        get
        {
            if (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                && Main.isLaunched
                && ObjectManager.Me.IsOnTaxi
                && ToolBox.CountItemStacks("Seaforium PU-36 Explosive Nether Modulator") == 0
                && ToolBox.CountItemStacks("Area 52 Special") == 0)
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
        Thread.Sleep(1000);
    }
}
