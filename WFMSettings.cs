using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class WFMSettings : Settings
{
    public WFMSettings()
    {
        TaxiTriggerDistance = 600;
        DetectTaxiDistance = 400;
        ShorterMinDistance = 500;
        SkipIfFollowPath = true;
        SkipIfFollowPathDistance = 5000f;
        PauseLengthInSeconds = 600;
        TakeUndiscoveredTaxi = false;
        //PauseSearingGorge = true;

        KnownFlightsList = new List<string>();

        ConfigWinForm(new System.Drawing.Point(400, 400), "Wholesome FlightMaster Settings");
    }

    [Category("1 - Main")]
    [DisplayName("List known nodes")]
    [Description("")]
    public List<string> KnownFlightsList { get; set; }

    [DefaultValue(false)]
    [Category("1 - Main")]
    [DisplayName("Take undiscovered taxi")]
    [Description("Will choose the nearest taxi for a flight even if you haven't discovered it. WARNING : can take you to dangerous zones")]
    public bool TakeUndiscoveredTaxi { get; set; }

    [DefaultValue(600)]
    [Category("1 - Main")]
    [DisplayName("Pause length in seconds")]
    [Description("In case of an unconnected flight, set how long the plugin should be paused (in seconds)")]
    public int PauseLengthInSeconds { get; set; }

    [DefaultValue(600)]
    [Category("1 - Main")]
    [DisplayName("Trigger Distance")]
    [Description("Sets how long your distance to your destination has to be, to trigger use of taxi")]
    public int TaxiTriggerDistance { get; set; }

    [DefaultValue(400)]
    [Category("1 - Main")]
    [DisplayName("Discover Distance")]
    [Description("Min distance to discover an undiscovered taxi node")]
    public int DetectTaxiDistance { get; set; }

    [DefaultValue(500)]
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
    public static WFMSettings CurrentSettings { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WFMSettings",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception ex)
        {
            Logger.Log("WFMSettings > Save(): " + ex);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WFMSettings",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSettings = Load<WFMSettings>(
                    AdviserFilePathAndName("WFMSettings",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSettings = new WFMSettings();
        }
        catch (Exception ex)
        {
            Logging.WriteDebug("WFMSettings > Load(): " + ex);
        }
        return false;
    }
}
