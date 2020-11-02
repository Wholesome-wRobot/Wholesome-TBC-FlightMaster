using robotManager.Helpful;
using System.Collections.Generic;
using wManager.Wow.Enums;

public class FlightMasterDB
{
    public static List<FlightMaster> FlightMasterList { get; private set; }

    public static FlightMaster GetFlightMasterByName(string name)
    {
        foreach (FlightMaster fm in FlightMasterList)
        {
            if (fm.Name == name)
                return fm;
        }
        Logger.LogError($"Couldn't find Flight Master {name} in the DB");
        return null;
    }

    public static FlightMaster GetFlightMasterById(int npcId)
    {
        foreach (FlightMaster fm in FlightMasterList)
        {
            if (fm.NPCId == npcId)
                return fm;
        }
        Logger.LogError($"Couldn't find Flight Master with id {npcId} in the DB ");
        return null;
    }

    public static void SetFlightMasterToKnown(string flightMasterName)
    {
        if (!WFMSettings.CurrentSettings.KnownFlightsList.Contains(flightMasterName))
        {
            Logger.Log($"Adding {flightMasterName} to known flights list");
            WFMSettings.CurrentSettings.KnownFlightsList.Add(flightMasterName);
            WFMSettings.CurrentSettings.Save();
        }
    }
    public static void SetFlightMasterToKnown(int flightMasterID)
    {
        FlightMaster flightMaster = GetFlightMasterById(flightMasterID);
        SetFlightMasterToKnown(flightMaster.Name);
    }

    public static void SetFlightMasterToUnknown(string flightMasterName)
    {
        if (WFMSettings.CurrentSettings.KnownFlightsList.Contains(flightMasterName))
        {
            Logger.Log($"Removing {flightMasterName} from known flights list");
            WFMSettings.CurrentSettings.KnownFlightsList.Remove(flightMasterName);
            WFMSettings.CurrentSettings.Save();
        }
    }
    public static void SetFlightMasterToUnknown(int flightMasterID)
    {
        FlightMaster flightMaster = GetFlightMasterById(flightMasterID);
        SetFlightMasterToUnknown(flightMaster.Name);
    }

    public static void Initialize()
    {
        FlightMasterList = new List<FlightMaster>()
        {
            // EK
            new FlightMaster("Grom'gol, Stranglethorn", 1387, new Vector3(-12417.5f, 144.474f, 3.36881f, "None"), ContinentId.Azeroth),
            new FlightMaster("Flame Crest, Burning Steppes", 13177, new Vector3(-7504.06f, -2190.77f, 165.302f, "None"), ContinentId.Azeroth),
            new FlightMaster("Booty Bay, Stranglethorn", 2858, new Vector3(-14448.6f, 506.129f, 26.3565f, "None"), ContinentId.Azeroth),
            new FlightMaster("The Sepulcher, Silverpine Forest", 2226, new Vector3(473.939f, 1533.95f, 131.96f, "None"), ContinentId.Azeroth),
            new FlightMaster("Tarren Mill, Hillsbrad", 2389, new Vector3(2.67557f, -857.919f, 58.889f, "None"), ContinentId.Azeroth),
            new FlightMaster("Hammerfall, Arathi", 2851, new Vector3(-917.658, -3496.93994140625, 70.4505004882813, "None"), ContinentId.Azeroth),
            new FlightMaster("Kargath, Badlands", 2861, new Vector3(-6632.22021484375, -2178.419921875, 244.227, "None"), ContinentId.Azeroth),
            new FlightMaster("Thorium Point, Searing Gorge", 3305, new Vector3(-6559.26f, -1100.23f, 310.353f, "None"), ContinentId.Azeroth),
            new FlightMaster("Revantusk Village, The Hinterlands", 4314, new Vector3(-631.736f, -4720.6f, 5.48226f, "None"), ContinentId.Azeroth),
            new FlightMaster("Stonard, Swamp of Sorrows", 6026, new Vector3(-10459.2f, -3279.76f, 21.5445f, "None"), ContinentId.Azeroth),
            new FlightMaster("Undercity, Tirisfal", 4551, new Vector3(1567.12f, 266.345f, -43.0194f, "None"), ContinentId.Azeroth),
            // Kalimdor
            new FlightMaster("Sun Rock Retreat, Stonetalon Mountains", 4312, new Vector3(968.077f, 1042.29f, 104.563f, "None"), ContinentId.Kalimdor),
            new FlightMaster("Moonglade", 12740, new Vector3(7466.15f, -2122.08f, 492.427f, "None"), ContinentId.Kalimdor),
            new FlightMaster("Bloodvenom Post, Felwood", 11900, new Vector3(5064.72f, -338.845f, 367.463f, "None"), ContinentId.Kalimdor),
            new FlightMaster("Brackenwall Village, Dustwallow Marsh", 11899, new Vector3(-3149.14f, -2842.13f, 34.6649f, "None"), ContinentId.Kalimdor),
            new FlightMaster("Everlook, Winterspring", 11139, new Vector3(6815.12f, -4610.12f, 710.759f, "None"), ContinentId.Kalimdor),
            new FlightMaster("Valormok, Azshara", 8610, new Vector3(3664.02f, -4390.45f, 113.169f, "None"), ContinentId.Kalimdor),
            new FlightMaster("Ratchet, The Barrens", 16227, new Vector3(-898.246f, -3769.65f, 11.7932f, "None"), ContinentId.Kalimdor),
            new FlightMaster("Thunder Bluff, Mulgore", 2995, new Vector3(-1196.75f, 26.0777f, 177.033f, "None"), ContinentId.Kalimdor),
            new FlightMaster("Orgrimmar, Durotar", 3310, new Vector3(1676.25f, -4313.45f, 61.7176f, "None"), ContinentId.Kalimdor),
            new FlightMaster("Crossroads, The Barrens", 3615, new Vector3(-437.137f, -2596f, 95.8708f, "None"), ContinentId.Kalimdor),
            new FlightMaster("Camp Taurajo, The Barrens", 10378, new Vector3(-2384.08f, -1880.94f, 95.9336f, "None"), ContinentId.Kalimdor),
            new FlightMaster("Freewind Post, Thousand Needles", 4317, new Vector3(-5407.12f, -2419.61f, 89.7094f, "None"), ContinentId.Kalimdor),
            new FlightMaster("Shadowprey Village, Desolace", 6726, new Vector3(-1770.37f, 3262.19f, 5.10852f, "None"), ContinentId.Kalimdor),
            new FlightMaster("Gadgetzan, Tanaris", 7824, new Vector3(-7045.24f, -3779.4f, 10.3158f, "None"), ContinentId.Kalimdor),
            new FlightMaster("Camp Mojache, Feralas", 8020, new Vector3(-4421.94f, 198.146f, 25.1863f, "None"), ContinentId.Kalimdor),
            new FlightMaster("Zoram'gar Outpost, Ashenvale", 11901, new Vector3(3373.69f, 994.351f, 5.36158f, "None"), ContinentId.Kalimdor),
            new FlightMaster("Splintertree Post, Ashenvale", 12616, new Vector3(2305.64f, -2520.15f, 103.893f, "None"), ContinentId.Kalimdor),
            new FlightMaster("Cenarion Hold, Silithus", 15178, new Vector3(-6810.2f, 841.704f, 49.7481f, "None"), ContinentId.Kalimdor),
            new FlightMaster("Marshal's Refuge, Un'Goro Crater", 10583, new Vector3(-6110.54f, -1140.35f, -186.866f, "None"), ContinentId.Kalimdor),
            // Outlands
            new FlightMaster("Thrallmar, Hellfire Peninsula", 16587, new Vector3(228.5f, 2633.57f, 87.67f, "None"), ContinentId.Expansion01),
            new FlightMaster("Falcon Watch, Hellfire Peninsula", 18942, new Vector3(-587.41f, 4101.01f, 91.37f, "None"), ContinentId.Expansion01),
            new FlightMaster("Zabra'jin, Zangarmarsh", 18791, new Vector3(219.45f, 7816f, 22.72f, "None"), ContinentId.Expansion01),
            new FlightMaster("Garadar, Nagrand", 18808, new Vector3(-1261.09f, 7133.39f, 57.34f, "None"), ContinentId.Expansion01),
            new FlightMaster("Area 52, Netherstorm", 18938, new Vector3(3082.31f, 3596.11f, 144.02f, "None"), ContinentId.Expansion01),
            new FlightMaster("Shadowmoon Village, Shadowmoon Valley", 19317, new Vector3(-3018.62f, 2557.09f, 79.09f, "None"), ContinentId.Expansion01),
            new FlightMaster("Stonebreaker Hold, Terokkar Forest", 18807, new Vector3(-2567.33f, 4423.83f, 39.33f, "None"), ContinentId.Expansion01),
            new FlightMaster("Thunderlord Stronghold, Blade's Edge Mountains", 18953, new Vector3(2446.37f, 6020.93f, 154.34f, "None"), ContinentId.Expansion01),
            new FlightMaster("Shattrath, Terokkar Forest", 18940, new Vector3(-1837.23f, 5301.9f, -12.43f, "None"), ContinentId.Expansion01),
            new FlightMaster("The Stormspire, Netherstorm", 19583, new Vector3(4157.58f, 2959.69f, 352.08f, "None"), ContinentId.Expansion01),
            new FlightMaster("Sanctum of the Stars, Shadowmoon Valley", 21766, new Vector3(-4073.17f, 1123.61f, 42.47f, "None"), ContinentId.Expansion01),
            new FlightMaster("Spinebreaker Ridge, Hellfire Peninsula", 19558, new Vector3(-1316.84f, 2358.62f, 88.96f, "None"), ContinentId.Expansion01),
            new FlightMaster("Mok'Nathal Village, Blade's Edge Mountains", 22455, new Vector3(2028.79f, 4705.27f, 150.51f, "None"), ContinentId.Expansion01),
            new FlightMaster("Evergrove, Blade's Edge Mountains", 22216, new Vector3(2976.01f, 5501.13f, 143.67f, "None"), ContinentId.Expansion01),
            new FlightMaster("Swamprat Post, Zangarmarsh", 20762, new Vector3(91.67f, 5214.92f, 23.1f, "None"), ContinentId.Expansion01),
            new FlightMaster("Hellfire Peninsula, The Dark Portal", 18930, new Vector3(-178.09f, 1026.72f, 54.19f, "None"), ContinentId.Expansion01)
        };
    }
}
