using System;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.IO;

[Serializable]
public class WholesomeFlightMasterDeepSettings : robotManager.Helpful.Settings
{
    public static WholesomeFlightMasterDeepSettings CurrentSettings { get; set; }

    private WholesomeFlightMasterDeepSettings()
    {
        LastUpdateDate = 0;

        EKDiscoveredFlights = false;
        KalimdorDiscoveredFlights = false;
        OutlandsDiscoveredFlights = false;
        NorthrendDiscoveredFlights = false;
    }

    public double LastUpdateDate { get; set; }
    public bool EKDiscoveredFlights { get; set; }
    public bool KalimdorDiscoveredFlights { get; set; }
    public bool OutlandsDiscoveredFlights { get; set; }
    public bool NorthrendDiscoveredFlights { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WholesomeFlightMasterDeepSettings",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logger.LogError("WholesomeFlightMasterDeepSettings > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WholesomeFlightMasterDeepSettings",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSettings = Load<WholesomeFlightMasterDeepSettings>(
                    AdviserFilePathAndName("WholesomeFlightMasterDeepSettings",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSettings = new WholesomeFlightMasterDeepSettings();
        }
        catch (Exception e)
        {
            Logger.LogError("WholesomeFlightMasterDeepSettings > Load(): " + e);
        }
        return false;
    }
}