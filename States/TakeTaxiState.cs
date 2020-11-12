using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using wManager.Wow.Bot.Tasks;
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

        // We go to the position
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

            if (!ToolBox.OpenTaxiMapSuccess(flightmasterFrom))
                return;

            List<string> reachableTaxis = new List<string>();
            // Look for current To and record reachables in case we don't find him
            for (int i = 0; i < 30; i++)
            {
                string nodeStatus = Lua.LuaDoString<string>($"return TaxiNodeGetType({i})");
                string nodeName = Lua.LuaDoString<string>($"return TaxiNodeName({i})");
                if (nodeStatus == "REACHABLE")
                {
                    if (nodeName == flightmasterTo.Name)
                    {
                        TakeTaxi(nodeName);
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
                TakeTaxi(alternativeFm.Name);
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

    private void TakeTaxi(string taxiNodeName)
    {
        Lua.LuaDoString("TakeTaxiNode(" + Lua.LuaDoString<int>("for i=0,20 do if string.find(TaxiNodeName(i),'" + taxiNodeName.Replace("'", "\\'") + "') then return i end end", "").ToString() + ")", false);
        Logger.Log("Flying to " + taxiNodeName);
        Thread.Sleep(Usefuls.Latency + 500);
        Main.shouldTakeFlight = false;
    }
}
