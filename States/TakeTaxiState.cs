using robotManager.FiniteStateMachine;
using robotManager.Products;
using System;
using System.Collections.Generic;
using System.Threading;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class TakeTaxiState : State
{
    public override string DisplayName => "WFM Taking Taxi";

    public TakeTaxiState() { }

    public override bool NeedToRun
    {
        get
        {
            if (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                && Main.isLaunched
                && !Main.inPause
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
        MovementManager.StopMoveToNewThread();
        if (GoToTask.ToPositionAndIntecractWithNpc(Main.from.Position, Main.from.NPCId, (int)GossipOptionsType.taxi))
        {
            MountTask.DismountMount();

            List<string> reachableTaxis = new List<string>();

            // Look for current To and record reachables in case we don't find him
            for (int i = 0; i < 30; i++)
            {
                string nodeStatus = Lua.LuaDoString<string>($"return TaxiNodeGetType({i})");
                string nodeName = Lua.LuaDoString<string>($"return TaxiNodeName({i})");
                if (nodeStatus == "REACHABLE")
                {
                    if (nodeName == Main.to.Name)
                    {
                        TakeTaxi(nodeName);
                        return;
                    }
                    reachableTaxis.Add(nodeName);
                }
            }

            // Find an alternative
            Logger.Log($"{Main.to.Name} is unreachable, trying to find an alternative");
            FlightMaster alternativeFm = Main.GetBestAlternativeTo(reachableTaxis);
            if (alternativeFm != null)
            {
                Logger.Log($"Found an alternative flight : {alternativeFm.Name}");
                TakeTaxi(alternativeFm.Name);
            }
            else
            {
                Main.shouldTakeFlight = false;
                Main.PausePlugin("Couldn't find an alternative flight");
            }
        }
    }

    private void TakeTaxi(string taxiNodeName)
    {
        Lua.LuaDoString("TakeTaxiNode(" + Lua.LuaDoString<int>("for i=0,20 do if string.find(TaxiNodeName(i),'" + taxiNodeName.Replace("'", "\\'") + "') then return i end end", "").ToString() + ")", false);
        Logger.Log("Flying to " + taxiNodeName);
        Thread.Sleep(Usefuls.Latency + 500);
        Main.shouldTakeFlight = false;
    }
}
