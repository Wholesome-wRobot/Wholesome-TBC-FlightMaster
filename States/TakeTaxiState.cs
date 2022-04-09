using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using WholesomeToolbox;

public class TakeTaxiState : State
{
    public override string DisplayName => "WFM Taking Taxi";

    public override bool NeedToRun
    {
        get
        {
            if (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
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

        if (WFMMoveInteract.GoInteractwithFM(flightmasterFrom, true))
        {
            if (FlightMasterDB.UpdateKnownFMs(flightmasterFrom))
            {
                Logger.Log("Flightmaster list has changed. Trying to find a new path.");
                Main.to = null;
                Main.shouldTakeFlight = false;
                return;
            }

            List<string> reachableTaxis = new List<string>();
            // Look for current To and record reachables in case we can't find it
            for (int i = 0; i < 120; i++)
            {
                string nodeStatus = WTTaxi.GetTaxiNodeType(i);
                string nodeName = WTTaxi.GetTaxiNodeName(i);

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
    }

    private void TakeTaxi(FlightMaster fm, string taxiNodeName)
    {
        WTTaxi.TakeTaxi(taxiNodeName);
        Thread.Sleep(500);

        // 5 tries to click on node if it failed
        for (int i = 1; i <= 5; i++)
        {
            if (ObjectManager.Me.IsCast)
            {
                Usefuls.WaitIsCasting();
                i = 1;
                Logger.Log("You're casting, wait");
                continue;
            }

            if (ObjectManager.Me.IsOnTaxi || Main.inPause)
            {
                break;
            }
            else
            {
                Logger.Log($"Taking taxi failed. Retrying ({i}/5)");
                Lua.LuaDoString($"CloseTaxiMap(); CloseGossip();");
                Main.errorTooFarAwayFromTaxiStand = false;
                Thread.Sleep(500);
                if (WFMMoveInteract.GoInteractwithFM(fm))
                {
                    Thread.Sleep(500);
                }
                Usefuls.SelectGossipOption(GossipOptionsType.taxi);
                Thread.Sleep(500);
                WTTaxi.TakeTaxi(taxiNodeName);
                Thread.Sleep(500);
            }
        }

        if (Main.inPause)
        {
            return;
        }

        if (Main.errorTooFarAwayFromTaxiStand)
        {
            ToolBox.PausePlugin("Taking taxi failed (error clicking node)");
        }
        else
        {
            Logger.Log($"Flying to {taxiNodeName}");
        }

        Thread.Sleep(Usefuls.Latency + 500);
        Main.shouldTakeFlight = false;
        Main.errorTooFarAwayFromTaxiStand = false;
        Thread.Sleep(Usefuls.Latency + 500);

        if (!ObjectManager.Me.IsOnTaxi)
        {
            ToolBox.PausePlugin("Taking taxi failed");
        }
    }
}
