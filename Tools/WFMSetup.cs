using WholesomeToolbox;
using wManager;
using wManager.Wow.Enums;
using wManager.Wow.ObjectManager;

public class WFMSetup
{
    public static void DiscoverDefaultNodes()
    {
        if (Main.isHorde)
        {
            if (ObjectManager.Me.PlayerRace == PlayerFactions.Orc || ObjectManager.Me.PlayerRace == PlayerFactions.Troll)
                FlightMasterDB.SetFlightMasterToKnown(3310);
            if (ObjectManager.Me.PlayerRace == PlayerFactions.Tauren)
                FlightMasterDB.SetFlightMasterToKnown(2995);
            if (ObjectManager.Me.PlayerRace == PlayerFactions.Undead)
                FlightMasterDB.SetFlightMasterToKnown(4551);
            if (ObjectManager.Me.PlayerRace == PlayerFactions.BloodElf)
                FlightMasterDB.SetFlightMasterToKnown(16192);
        }
        else
        {
            if (ObjectManager.Me.PlayerRace == PlayerFactions.Gnome || ObjectManager.Me.PlayerRace == PlayerFactions.Dwarf)
                FlightMasterDB.SetFlightMasterToKnown(1573);
            if (ObjectManager.Me.PlayerRace == PlayerFactions.Human)
                FlightMasterDB.SetFlightMasterToKnown(352);
        }
    }

    public static void SetWRobotSettings()
    {
        bool settingchanged = false;
        if (wManagerSetting.CurrentSetting.FlightMasterTaxiUse)
        {
            Main.saveFlightMasterTaxiUse = wManagerSetting.CurrentSetting.FlightMasterTaxiUse;
            wManagerSetting.CurrentSetting.FlightMasterTaxiUse = false;
            settingchanged = true;
        }
        if (wManagerSetting.CurrentSetting.FlightMasterTaxiUseOnlyIfNear)
        {
            Main.saveFlightMasterTaxiUseOnlyIfNear = wManagerSetting.CurrentSetting.FlightMasterTaxiUseOnlyIfNear;
            wManagerSetting.CurrentSetting.FlightMasterTaxiUseOnlyIfNear = false;
            settingchanged = true;
        }
        if (wManagerSetting.CurrentSetting.FlightMasterDiscoverRange > 1)
        {
            Main.saveFlightMasterDiscoverRange = wManagerSetting.CurrentSetting.FlightMasterDiscoverRange;
            wManagerSetting.CurrentSetting.FlightMasterDiscoverRange = 1;
            settingchanged = true;
        }
        if (settingchanged)
        {
            Logger.Log("Disabling WRobot's Taxi");
            wManagerSetting.CurrentSetting.Save();
            ToolBox.SoftRestart();
        }
    }

    public static void RestoreWRobotSettings()
    {
        wManagerSetting.CurrentSetting.FlightMasterDiscoverRange = Main.saveFlightMasterDiscoverRange;
        wManagerSetting.CurrentSetting.FlightMasterTaxiUse = Main.saveFlightMasterTaxiUse;
        wManagerSetting.CurrentSetting.FlightMasterTaxiUseOnlyIfNear = Main.saveFlightMasterTaxiUseOnlyIfNear;
        wManagerSetting.CurrentSetting.Save();
    }

    public static void SetBlacklistedZonesAndOffMeshConnections()
    {
        WTSettings.AddRecommendedBlacklistZones();
        WTSettings.AddRecommendedOffmeshConnections();
    }
}
