using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class WholesomeTBCFlightMasterSettings : Settings
{
    public WholesomeTBCFlightMasterSettings()
    {
        TaxiTriggerDistance = 1000;
        PauseTaxiTime = 50000;
        DetectTaxiDistance = 500;
        ShorterMinDistance = 1000;
        SkipIfFollowPath = true;
        UpdateTaxi = true;
        SkipIfFollowPathDistance = 5000f;
        PauseSearingGorge = true;

        EKDiscoveredFlights = false;
        KalimdorDiscoveredFlights = false;
        OutlandsDiscoveredFlights = false;
        NorthrendDiscoveredFlights = false;

        KnownFlightsList = new List<string>();
    }

    public bool EKDiscoveredFlights { get; set; }
    public bool KalimdorDiscoveredFlights { get; set; }
    public bool OutlandsDiscoveredFlights { get; set; }
    public bool NorthrendDiscoveredFlights { get; set; }

    [Category("1 - Main")]
    [DisplayName("List known nodes")]
    [Description("")]
    public List<string> KnownFlightsList { get; set; }

    [DefaultValue(1000)]
    [Category("1 - Main")]
    [DisplayName("Trigger Distance")]
    [Description("Sets how long your distance to your destination has to be, to trigger use of taxi")]
    public int TaxiTriggerDistance { get; set; }

    [DefaultValue(50000)]
    [Category("1 - Main")]
    [DisplayName("Pause Taxi Time")]
    [Description("Sets how long taxi is paused after use, to avoid loops. Only change it, if you experience issues")]
    public int PauseTaxiTime { get; set; }

    [DefaultValue(500)]
    [Category("1 - Main")]
    [DisplayName("Discover Distance")]
    [Description("Min distance to discover an undiscovered taxi node")]
    public int DetectTaxiDistance { get; set; }

    [DefaultValue(1000)]
    [Category("1 - Main")]
    [DisplayName("Shorter Path Min")]
    [Description("Sets how much shorter a path has to be, to trigger taxi")]
    public int ShorterMinDistance { get; set; }

    [DefaultValue(true)]
    [Category("2 - Useful")]
    [DisplayName("1. Skip if Follow Path / Boat step")]
    [Description("Skips take taxi, if currently executing a Follow Path or Boat Quester step. When running a profile with dedicated paths")]
    public bool SkipIfFollowPath { get; set; }

    [DefaultValue(true)]
    [Category("2 - Useful")]
    [DisplayName("2. Update taxi nodes")]
    [Description("Scans and updates all entries on the taxi map of the current continent, if they have already been discovered. Triggers, when the taxi map is opened")]
    public bool UpdateTaxi { get; set; }

    [DefaultValue(5000f)]
    [Category("2 - Useful")]
    [DisplayName("1.1 Skip if ... min distance")]
    [Description("Won't skip taxi min distance to destination")]
    public float SkipIfFollowPathDistance { get; set; }

    [DefaultValue(true)]
    [Category("2 - Useful")]
    [DisplayName("3. Stop bot at Searing Gorge gate")]
    [Description("Stops the bot, to prevent it from running into the Searing Gorge gate from Loch Modan and getting stuck over and over again")]
    public bool PauseSearingGorge { get; set; }


    public static WholesomeTBCFlightMasterSettings CurrentSettings { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WholesomeTBCFlightmasterSettings",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception ex)
        {
            Logger.Log("WholesomeTBCFlightmasterSettings > Save(): " + ex);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WholesomeTBCFlightmasterSettings",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSettings = Load<WholesomeTBCFlightMasterSettings>(
                    AdviserFilePathAndName("WholesomeTBCFlightmasterSettings",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSettings = new WholesomeTBCFlightMasterSettings();
        }
        catch (Exception ex)
        {
            Logging.WriteDebug("WholesomeTBCFlightmasterSettings > Load(): " + ex);
        }
        return false;
    }
}
