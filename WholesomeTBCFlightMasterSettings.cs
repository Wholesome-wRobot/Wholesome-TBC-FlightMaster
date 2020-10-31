using robotManager.Helpful;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class WholesomeTBCFlightMasterSettings : Settings
{
    public WholesomeTBCFlightMasterSettings()
    {
        TaxiTriggerDistance = 1000;
        PauseTaxiTime = 50000;
        DetectTaxiDistance = 50;
        ShorterMinDistance = 1000;
        SkipIfFollowPath = true;
        UpdateTaxi = true;
        SkipIfFollowPathDistance = 5000f;
        PauseSearingGorge = true;

        StranglethornGromgol = false;
        StranglethornBootyBay = false;
        SilverpineForest = false;
        HillsbradFoothills = false;
        ArathiHighlands = false;
        Badlands = false;
        Mulgore = false;
        SearingGorge = false;
        Orgrimmar = false;
        BarrensCrossroads = false;
        BarrensRatchet = false;
        BarrensTaurajo = false;
        StonetalonSunRockRetreat = false;
        TheHinterlands = false;
        ThousandNeedles = false;
        Undercity = false;
        SwampofSorrows = false;
        Desolace = false;
        Tanaris = false;
        Feralas = false;
        Azshara = false;
        Winterspring = false;
        DustwallowMarsh = false;
        Felwood = false;
        AshenvaleSplintertree = false;
        AshenvaleZoramgar = false;
        UngoroCrater = false;
        Silithus = false;
        Moonglade = false;
        BurningSteppes = false;
        Thrallmar = false;
        FalconWatch = false;
        Zabrajin = false;
        Garadar = false;
        Area52 = false;
        Cosmowrench = false;
        Evergrove = false;
        SanctumOfTheStars = false;
        ShadowmoonVillage = false;
        StonebreakerHold = false;
        Shattrath = false;
        ThunderlordStronghold = false;
        TheStormspire = false;
        AltarofShatar = false;
        SpinebreakerPost = false;
        MokNathalVillage = false;
        SwampratPost = false;
        TheDarkPortal = false;
    }
    public static void FlightMasterSaveChanges(FlightMaster needToChange, bool value)
    {
        if (needToChange.Name.Contains("Base"))
            CurrentSettings.StranglethornGromgol = value;
        if (needToChange.Name.Contains("Booty"))
            CurrentSettings.StranglethornBootyBay = value;
        if (needToChange.Name.Contains("Silverpine"))
            CurrentSettings.SilverpineForest = value;
        if (needToChange.Name.Contains("Hillsbrad"))
            CurrentSettings.HillsbradFoothills = value;
        if (needToChange.Name.Contains("Arathi"))
            CurrentSettings.ArathiHighlands = value;
        if (needToChange.Name.Contains("Badlands"))
            CurrentSettings.Badlands = value;
        if (needToChange.Name.Contains("Mulgore"))
            CurrentSettings.Mulgore = value;
        if (needToChange.Name.Contains("Searing"))
            CurrentSettings.SearingGorge = value;
        if (needToChange.Name.Contains("Orgrimmar"))
            CurrentSettings.Orgrimmar = value;
        if (needToChange.Name.Contains("Crossroads"))
            CurrentSettings.BarrensCrossroads = value;
        if (needToChange.Name.Contains("Ratchet"))
            CurrentSettings.BarrensRatchet = value;
        if (needToChange.Name.Contains("Taurajo"))
            CurrentSettings.BarrensTaurajo = value;
        if (needToChange.Name.Contains("Retreat"))
            CurrentSettings.StonetalonSunRockRetreat = value;
        if (needToChange.Name.Contains("Hinterlands"))
            CurrentSettings.TheHinterlands = value;
        if (needToChange.Name.Contains("Thousand"))
            CurrentSettings.ThousandNeedles = value;
        if (needToChange.Name.Contains("Undercity"))
            CurrentSettings.Undercity = value;
        if (needToChange.Name.Contains("Swamp"))
            CurrentSettings.SwampofSorrows = value;
        if (needToChange.Name.Contains("Desolace"))
            CurrentSettings.Desolace = value;
        if (needToChange.Name.Contains("Tanaris"))
            CurrentSettings.Tanaris = value;
        if (needToChange.Name.Contains("Feralas"))
            CurrentSettings.Feralas = value;
        if (needToChange.Name.Contains("Azshara"))
            CurrentSettings.Azshara = value;
        if (needToChange.Name.Contains("Winterspring"))
            CurrentSettings.Winterspring = value;
        if (needToChange.Name.Contains("Dustwallow"))
            CurrentSettings.DustwallowMarsh = value;
        if (needToChange.Name.Contains("Felwood"))
            CurrentSettings.Felwood = value;
        if (needToChange.Name.Contains("Outpost"))
            CurrentSettings.AshenvaleZoramgar = value;
        if (needToChange.Name.Contains("Splintertree"))
            CurrentSettings.AshenvaleSplintertree = value;
        if (needToChange.Name.Contains("Moonglade"))
            CurrentSettings.Moonglade = value;
        if (needToChange.Name.Contains("Burning"))
            CurrentSettings.BurningSteppes = value;
        if (needToChange.Name.Contains("Silithus"))
            CurrentSettings.Silithus = value;
        if (needToChange.Name.Contains("Crater"))
            CurrentSettings.UngoroCrater = value;
        if (needToChange.Name.Contains("Thrallmar"))
            CurrentSettings.Thrallmar = value;
        if (needToChange.Name.Contains("Falcon"))
            CurrentSettings.FalconWatch = value;
        if (needToChange.Name.Contains("Zabra'jin"))
            CurrentSettings.Zabrajin = value;
        if (needToChange.Name.Contains("Garadar"))
            CurrentSettings.Garadar = value;
        if (needToChange.Name.Contains("Area"))
            CurrentSettings.Area52 = value;
        if (needToChange.Name.Contains("Shadowmoon"))
            CurrentSettings.ShadowmoonVillage = value;
        if (needToChange.Name.Contains("Stonebreaker"))
            CurrentSettings.StonebreakerHold = value;
        if (needToChange.Name.Contains("Thunderlord"))
            CurrentSettings.ThunderlordStronghold = value;
        if (needToChange.Name.Contains("Shattrath"))
            CurrentSettings.Shattrath = value;
        if (needToChange.Name.Contains("Stormspire"))
            CurrentSettings.TheStormspire = value;
        if (needToChange.Name.Contains("Altar"))
            CurrentSettings.AltarofShatar = value;
        if (needToChange.Name.Contains("Cosmowrench"))
            CurrentSettings.Cosmowrench = value;
        if (needToChange.Name.Contains("Sanctum"))
            CurrentSettings.SanctumOfTheStars = value;
        if (needToChange.Name.Contains("Spinebreaker"))
            CurrentSettings.SpinebreakerPost = value;
        if (needToChange.Name.Contains("Mok'Nathal"))
            CurrentSettings.MokNathalVillage = value;
        if (needToChange.Name.Contains("Evergrove"))
            CurrentSettings.Evergrove = value;
        if (needToChange.Name.Contains("Swamprat"))
            CurrentSettings.SwampratPost = value;
        if (needToChange.Name.Contains("Portal"))
            CurrentSettings.TheDarkPortal = value;
        CurrentSettings.Save();
        Thread.Sleep(2500);
        try
        {
            CurrentSettings = Load<WholesomeTBCFlightMasterSettings>(AdviserFilePathAndName("WholesomeTBCFlightmasterSettings", ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception ex)
        {
            Logger.Log("Error when trying to reload DB file -> " + ex?.ToString());
        }
        Logger.Log("Settings saved of Flight Master " + needToChange.Name);
    }

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

    [DefaultValue(50)]
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

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool StranglethornGromgol { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool StranglethornBootyBay { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool SilverpineForest { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool HillsbradFoothills { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool ArathiHighlands { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool Badlands { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool Mulgore { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool SearingGorge { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool Orgrimmar { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool BarrensCrossroads { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool BarrensTaurajo { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool BarrensRatchet { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool StonetalonSunRockRetreat { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool TheHinterlands { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool ThousandNeedles { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool Undercity { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool SwampofSorrows { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool Desolace { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool Tanaris { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool Feralas { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool Azshara { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool Winterspring { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool DustwallowMarsh { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool Felwood { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool AshenvaleZoramgar { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool AshenvaleSplintertree { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool Moonglade { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool BurningSteppes { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool Silithus { get; set; }

    [Category("Azeroth Discovered Nodes")]
    [DefaultValue(false)]
    public bool UngoroCrater { get; set; }

    [Category("Outland Discovered Nodes")]
    [DefaultValue(false)]
    public bool Thrallmar { get; set; }

    [Category("Outland Discovered Nodes")]
    [DefaultValue(false)]
    public bool FalconWatch { get; set; }

    [Category("Outland Discovered Nodes")]
    [DefaultValue(false)]
    public bool Zabrajin { get; set; }

    [Category("Outland Discovered Nodes")]
    [DefaultValue(false)]
    public bool Garadar { get; set; }

    [Category("Outland Discovered Nodes")]
    [DefaultValue(false)]
    public bool Area52 { get; set; }

    [Category("Outland Discovered Nodes")]
    [DefaultValue(false)]
    public bool ShadowmoonVillage { get; set; }

    [Category("Outland Discovered Nodes")]
    [DefaultValue(false)]
    public bool StonebreakerHold { get; set; }

    [Category("Outland Discovered Nodes")]
    [DefaultValue(false)]
    public bool Shattrath { get; set; }

    [Category("Outland Discovered Nodes")]
    [DefaultValue(false)]
    public bool ThunderlordStronghold { get; set; }

    [Category("Outland Discovered Nodes")]
    [DefaultValue(false)]
    public bool TheStormspire { get; set; }

    [Category("Outland Discovered Nodes")]
    [DefaultValue(false)]
    public bool AltarofShatar { get; set; }

    [Category("Outland Discovered Nodes")]
    [DefaultValue(false)]
    public bool SpinebreakerPost { get; set; }

    [Category("Outland Discovered Nodes")]
    [DefaultValue(false)]
    public bool MokNathalVillage { get; set; }

    [Category("Outland Discovered Nodes")]
    [DefaultValue(false)]
    public bool SwampratPost { get; set; }

    [Category("Outland Discovered Nodes")]
    [DefaultValue(false)]
    public bool TheDarkPortal { get; set; }

    [Category("Outland Discovered Nodes")]
    [DefaultValue(false)]
    public bool Evergrove { get; set; }

    [Category("Outland Discovered Nodes")]
    [DefaultValue(false)]
    public bool Cosmowrench { get; set; }

    [Category("Outland Discovered Nodes")]
    [DefaultValue(false)]
    public bool SanctumOfTheStars { get; set; }
}
