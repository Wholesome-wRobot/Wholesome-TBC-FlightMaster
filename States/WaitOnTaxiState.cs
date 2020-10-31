using robotManager.FiniteStateMachine;
using System;
using System.Threading;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class WaitOnTaxiState : State
{
    public override string DisplayName => "Waiting on Taxi";
    public static bool StateAddedToFSM { get; set; }
    public WaitOnTaxiState() { }

    public override bool NeedToRun
    {
        get
        {
            if (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                && Main._isLaunched
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
        if (!StateAddedToFSM && engine != null && engine.States.Count > 5)
        {
            try
            {
                State taxiState = engine.States.Find(s => s.DisplayName == "Flight master discover");

                if (taxiState == null)
                {
                    Logger.LogError("Couldn't find taxi state");
                    StateAddedToFSM = true;
                    return;
                }

                WaitOnTaxiState waitOnTaxiState = new WaitOnTaxiState { Priority = taxiState.Priority + 1 };
                engine.AddState(waitOnTaxiState);
                engine.States.Sort();
                StateAddedToFSM = true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Erreur : {0}" + ex.ToString());
            }
        }
    }
}
