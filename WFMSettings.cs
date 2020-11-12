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
        MinimumDistanceSaving = 500;
        SkipIfFollowPath = true;
        SkipIfFollowPathDistance = 5000f;
        PauseLengthInSeconds = 600;
        TakeUndiscoveredTaxi = false;
        //PauseSearingGorge = true;

        KnownFlightsList = new List<string>();
        DisabledFlightsList = new string[] { };

        ConfigWinForm(new System.Drawing.Point(400, 400), "Wholesome FlightMaster Settings");
    }

    [Category("Lists")]
    [DisplayName("Discovered nodes")]
    [Description("List of already known nodes. You cannot modify this list.")]
    public List<string> KnownFlightsList { get; set; }

    [Category("Lists")]
    [DisplayName("Blacklisted nodes")]
    [Description("You can add flight nodes you want to disable completely in this list. The name can be partial (ex : mojach for Camp Mojache)")]
    public string[] DisabledFlightsList { get; set; }

    [DefaultValue(false)]
    [Category("Settings")]
    [DisplayName("Take undiscovered taxi")]
    [Description("Will choose the nearest taxi for a flight even if you haven't discovered it. WARNING : can take you to dangerous zones")]
    public bool TakeUndiscoveredTaxi { get; set; }

    [DefaultValue(600)]
    [Category("Settings")]
    [DisplayName("Pause length in seconds")]
    [Description("In case of an unconnected flight or dead FlightMaster, set how long the plugin should be paused (in seconds)")]
    public int PauseLengthInSeconds { get; set; }

    [DefaultValue(600)]
    [Category("Settings")]
    [DisplayName("Trigger Distance")]
    [Description("Sets the minimum walking distance to your destination to trigger use of taxi")]
    public int TaxiTriggerDistance { get; set; }

    [DefaultValue(400)]
    [Category("Settings")]
    [DisplayName("Discover Distance")]
    [Description("Maximum distance to discover a taxi node")]
    public int DetectTaxiDistance { get; set; }

    [DefaultValue(500)]
    [Category("Settings")]
    [DisplayName("Minimum distance saving")]
    [Description("Sets how much shorter a path has to be for a flight to be taken")]
    public int MinimumDistanceSaving { get; set; }

    [DefaultValue(true)]
    [Category("Follow Path")]
    [DisplayName("Disable if Follow Path / Boat step")]
    [Description("Disable flights if currently executing a Follow Path or Boat Quester step")]
    public bool SkipIfFollowPath { get; set; }

    [DefaultValue(5000f)]
    [Category("Follow Path")]
    [DisplayName("Minimum Follow Path distance")]
    [Description("Minimum Follow Path distance to be considered for a flight")]
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
