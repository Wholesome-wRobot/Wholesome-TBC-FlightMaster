using robotManager.FiniteStateMachine;
using System;
using System.Threading;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class WaitOnTaxiState : State
{
    public override string DisplayName => "WFM Waiting on Taxi";
    public WaitOnTaxiState() { }

    public override bool NeedToRun
    {
        get
        {
            if (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                && Main.isLaunched
                && ObjectManager.Me.IsOnTaxi)
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

    public static void AddState(Engine engine, State state)
    {
        bool statedAdded = engine.States.Exists(s => s.DisplayName == "WFM Waiting on Taxi");
        if (!statedAdded && engine != null && engine.States.Count > 5)
        {
            try
            {
                State taxiState = engine.States.Find(s => s.DisplayName == "Flight master discover");

                if (taxiState == null)
                {
                    Logger.LogError("Couldn't find taxi state");
                    return;
                }

                WaitOnTaxiState waitOnTaxiState = new WaitOnTaxiState { Priority = taxiState.Priority + 1 };
                engine.AddState(waitOnTaxiState);
                engine.States.Sort();
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur : {0}" + ex.ToString());
            }
        }
    }
}
