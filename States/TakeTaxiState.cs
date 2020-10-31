using robotManager.FiniteStateMachine;
using System;
using System.Threading;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class TakeTaxiState : State
{
    public override string DisplayName => "Taking Taxi";
    public static bool StateAddedToFSM { get; set; }

    public TakeTaxiState() { }

    public override bool NeedToRun
    {
        get
        {
            if (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                && Main._isLaunched
                && Main.shouldTakeFlight
                && Main.to != null
                && Main.from != null
                && !ObjectManager.Me.IsOnTaxi)
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
        if (GoToTask.ToPositionAndIntecractWithNpc(Main.from.Position, Main.from.NPCId, (int)GossipOptionsType.taxi))
        {
            Lua.LuaDoString("TakeTaxiNode(" + (Lua.LuaDoString<int>("for i=0,20 do if string.find(TaxiNodeName(i),'" + Main.to.Name.Replace("'", "\\'") + "') then return i end end", "")).ToString() + ")", false);
            Logger.Log("Taking Taxi from " + Main.from.Name + " to " + Main.to.Name);
            Thread.Sleep(Usefuls.Latency + 500);
            Main.shouldTakeFlight = false;
        }
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

                TakeTaxiState discoverContinentFlightsState = new TakeTaxiState { Priority = taxiState.Priority };
                engine.AddState(discoverContinentFlightsState);
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
