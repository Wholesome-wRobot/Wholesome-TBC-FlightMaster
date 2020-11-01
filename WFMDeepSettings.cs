using System;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.IO;

[Serializable]
public class WFMDeepSettings : robotManager.Helpful.Settings
{
    public static WFMDeepSettings CurrentSettings { get; set; }

    private WFMDeepSettings()
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
            return Save(AdviserFilePathAndName("WFMDeepSettings",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logger.LogError("WFMDeepSettings > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WFMDeepSettings",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSettings = Load<WFMDeepSettings>(
                    AdviserFilePathAndName("WFMDeepSettings",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSettings = new WFMDeepSettings();
        }
        catch (Exception e)
        {
            Logger.LogError("WFMDeepSettings > Load(): " + e);
        }
        return false;
    }
}