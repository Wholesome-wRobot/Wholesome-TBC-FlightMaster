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
        DetectTaxiDistance = 200;
        ShorterMinDistance = 1000;
        SkipIfFollowPath = true;
        SkipIfFollowPathDistance = 5000f;
        //PauseSearingGorge = true;

        EKDiscoveredFlights = false;
        KalimdorDiscoveredFlights = false;
        OutlandsDiscoveredFlights = false;
        NorthrendDiscoveredFlights = false;

        KnownFlightsList = new List<string>();

        ConfigWinForm(new System.Drawing.Point(400, 400), "Wholesome FlightMaster Settings");
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

    [DefaultValue(200)]
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
    [DisplayName("Skip if Follow Path / Boat step")]
    [Description("Skips take taxi, if currently executing a Follow Path or Boat Quester step. When running a profile with dedicated paths")]
    public bool SkipIfFollowPath { get; set; }

    [DefaultValue(5000f)]
    [Category("2 - Useful")]
    [DisplayName("Skip if ... min distance")]
    [Description("Won't skip taxi min distance to destination")]
    public float SkipIfFollowPathDistance { get; set; }
    /*
    [DefaultValue(true)]
    [Category("2 - Useful")]
    [DisplayName("Stop bot at Searing Gorge gate")]
    [Description("Stops the bot, to prevent it from running into the Searing Gorge gate from Loch Modan and getting stuck over and over again")]
    public bool PauseSearingGorge { get; set; }
    */
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
