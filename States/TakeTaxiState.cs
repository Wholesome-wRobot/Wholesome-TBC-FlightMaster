using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class TakeTaxiState : State
{
    public override string DisplayName => "WFM Taking Taxi";

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
                && !ObjectManager.Me.InTransport
                && !Main.from.IsDisabledByPlugin()
                && ToolBox.ExceptionConditionsAreMet(Main.from)
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

        FlightMaster flightmasterFrom = Main.from;
        FlightMaster flightmasterTo = Main.to;

        if (GoToTask.ToPositionAndIntecractWithNpc(flightmasterFrom.Position, flightmasterFrom.NPCId))
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

            if (FlightMasterDB.UpdateKnownFMs(flightmasterFrom))
            {
                Logger.Log("Flightmaster list has changed. Trying to find a new path.");
                Main.to = null;
                Main.shouldTakeFlight = false;
                return;
            }

            List<string> reachableTaxis = new List<string>();
            // Look for current To and record reachables in case we can't find it
            for (int i = 0; i < 50; i++)
            {
                string nodeStatus = Lua.LuaDoString<string>($"return TaxiNodeGetType({i})");
                string nodeName = Lua.LuaDoString<string>($"return TaxiNodeName({i})");

                if (nodeStatus == "REACHABLE")
                {
                    if (nodeName == flightmasterTo.Name)
                    {
                        TakeTaxi(flightmasterFrom, nodeName);
                        return;
                    }
                    reachableTaxis.Add(nodeName);
                }
            }

            // Find an alternative
            Logger.Log($"{flightmasterTo.Name} is unreachable, trying to find an alternative");
            FlightMaster alternativeFm = Main.GetBestAlternativeTo(reachableTaxis);
            if (alternativeFm != null)
            {
                Logger.Log($"Found an alternative flight : {alternativeFm.Name}");
                TakeTaxi(flightmasterFrom, alternativeFm.Name);
                return;
            }
            else
            {
                Main.shouldTakeFlight = false;
                ToolBox.PausePlugin("Couldn't find an alternative flight");
            }
        }

        // Check if FM is here or dead
        if (!ToolBox.FMIsNearbyAndAlive(flightmasterFrom))
        {
            ToolBox.PausePlugin("FlightMaster is absent or dead");
            return;
        }
    }

    private void TakeTaxi(FlightMaster fm, string taxiNodeName)
    {
        string clickNodeLua = "TakeTaxiNode(" + Lua.LuaDoString<int>("for i=0,30 do if string.find(TaxiNodeName(i),'" + taxiNodeName.Replace("'", "\\'") + "') then return i end end", "").ToString() + ")";
        Lua.LuaDoString(clickNodeLua, false);
        Thread.Sleep(500);

        // 3 tries to click on node if it failed
        for (int i = 1; i <= 3; i++)
        {
            if (!Main.clickNodeError)
                break;
            else
            {
                Logger.Log($"Taking taxi failed. Retrying ({i})");
                Lua.LuaDoString($"CloseTaxiMap(); CloseGossip();");
                Main.clickNodeError = false;
                Thread.Sleep(500);
                if (GoToTask.ToPositionAndIntecractWithNpc(fm.Position, fm.NPCId))
                    Thread.Sleep(500);
                Usefuls.SelectGossipOption(GossipOptionsType.taxi);
                Thread.Sleep(500);
                Lua.LuaDoString(clickNodeLua, false);
                Thread.Sleep(500);
            }
        }

        if (Main.clickNodeError)
            ToolBox.PausePlugin("Taking taxi failed");
        else
            Logger.Log($"Flying to {taxiNodeName}");

        Thread.Sleep(Usefuls.Latency + 500);
        Main.shouldTakeFlight = false;
        Main.clickNodeError = false;
        Thread.Sleep(Usefuls.Latency + 500);

        if (!ObjectManager.Me.IsOnTaxi)
            ToolBox.PausePlugin("Taking taxi failed");
    }
}
