using robotManager.Helpful;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using wManager;
using wManager.Events;
using wManager.Plugin;
using wManager.Wow;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class Main : IPlugin
{
    private string version = "1.7";
    public static int timer = 0;
    public static bool _isLaunched;
    private static float _saveDistance;
    public static Vector3 destinationVector = new Vector3(0.0f, 0.0f, 0.0f, "None");
    public static bool inProcessing = false;
    public static bool _takenTaxi = false;
    private static FlightMaster from = null;
    private static FlightMaster to = null;
    private static FlightMaster discoverTaxiNode = null;
    public static bool _timer = false;
    public static bool _discoverTaxiTimer = false;
    public static bool changer = true;
    public static bool _updateNodes;
    public static bool checkPath = true;
    public static bool checkPathActive = false;
    public static FlightMaster checkPathActiveFM = null;
    public static bool cancelCheckPathThread = false;
    public static bool pauseCheckPathThread = false;
    public static string status = "";
    public static string statusDiscover = "";
    public static bool _runScan = false;
    public static FlightMaster taxiToDiscover = null;
    public static bool _taxiToDiscover = false;
    public static bool _discoverInProcess = false;
    public static int stuckCounter = 0;

    public static bool _copySettings { get; set; }

    public void Initialize()
    {
        Logger.Log("Initialized - v" + version);

        _isLaunched = true;
        inProcessing = false;
        _copySettings = true;
        _runScan = true;
        _updateNodes = false;
        cancelCheckPathThread = false;

        IngameSettings();
        WatchForEvents();
        WholesomeTBCFlightMasterSettings.Load();
        ApplyDefaultNodes();

        MovementEvents.OnMovementPulse += MovementEventsOnOnMovementPulse;
        MovementEvents.OnSeemStuck += MovementEventsOnOnSeemStuck;

        ScanNearbyTaxi.Start();
        FlightMasterLoop();
    }

    private Thread ScanNearbyTaxi = new Thread(() =>
    {
        int millisecondsTimeout = 10000;
        List<FlightMaster> FlightMasterList = FillDB();
        Logger.Log("Taxi scan started");

        while (robotManager.Products.Products.IsStarted)
        {
            Logger.Log("SCAN LOOP");
            if (_discoverTaxiTimer || _discoverInProcess)
            {
                Logger.Log("Discover in processing or scan for nearby nodes paused");
                for (int pauseTaxiTime = WholesomeTBCFlightMasterSettings.CurrentSettings.PauseTaxiTime; pauseTaxiTime > 0; pauseTaxiTime -= 1000)
                    Thread.Sleep(1000);
                _discoverTaxiTimer = false;
            }

            // Pause in combat
            while (InCombat() || InCombatPet())
                Thread.Sleep(5000);

            // Pause when HMP is doing First Aid
            while (Logging.Status.Contains("First Aid") && Usefuls.MapZoneName.Contains("Teldrassil"))
            {
                Logger.Log("HumanMasterPlugin trying to train First Aid. Pausing undiscovered node scan for five minutes to avoid conflicts");
                Thread.Sleep(300000);
            }

            if (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                && !_taxiToDiscover
                && !ObjectManager.Me.IsOnTaxi)
            {
                foreach (FlightMaster flightMaster in FlightMasterList)
                {
                    if (GetCurrentContinent() == flightMaster.Continent
                    && !flightMaster.AlreadyDiscovered
                    && ObjectManager.Me.Position.DistanceTo(flightMaster.Position) < (double)WholesomeTBCFlightMasterSettings.CurrentSettings.DetectTaxiDistance)
                    {
                        taxiToDiscover = flightMaster;
                        discoverTaxiNode = flightMaster;
                        _taxiToDiscover = true;
                        Logger.Log("Nearby undiscovered Taxi node found: " + flightMaster.Name);
                        Thread.Sleep(1000 + Usefuls.Latency);
                        while (!MovementManager.InMovement)
                            Thread.Sleep(100);
                        Reenable();
                    }
                }
            }
            Thread.Sleep(Usefuls.Latency * 10);
            FlightMasterList = FillDB();
            Thread.Sleep(millisecondsTimeout);
        }
    });

    public void Dispose()
    {
        _runScan = false;
        cancelCheckPathThread = true;
        _isLaunched = false;
        _updateNodes = false;

        MovementEvents.OnMovementPulse -= MovementEventsOnOnMovementPulse;
        MovementEvents.OnSeemStuck -= MovementEventsOnOnSeemStuck;

        WholesomeTBCFlightMasterSettings.CurrentSettings.Save();
        Logger.Log("Disposed");
    }

    public void Settings()
    {
        WholesomeTBCFlightMasterSettings.Load();
        WholesomeTBCFlightMasterSettings.CurrentSettings.ToForm();
        WholesomeTBCFlightMasterSettings.CurrentSettings.Save();
    }

    public static void IngameSettings()
    {
        if (!wManagerSetting.CurrentSetting.FlightMasterTaxiUse && !wManagerSetting.CurrentSetting.FlightMasterTaxiUseOnlyIfNear)
            return;

        Logger.Log("Disabling WRobot's Taxi");
        wManagerSetting.CurrentSetting.FlightMasterTaxiUse = false;
        wManagerSetting.CurrentSetting.FlightMasterTaxiUseOnlyIfNear = false;
    }

    public static void ApplyDefaultNodes()
    {
        if (ObjectManager.Me.PlayerRace == PlayerFactions.Orc || ObjectManager.Me.PlayerRace == PlayerFactions.Troll)
            WholesomeTBCFlightMasterSettings.CurrentSettings.Orgrimmar = true;
        if (ObjectManager.Me.PlayerRace == PlayerFactions.Tauren)
            WholesomeTBCFlightMasterSettings.CurrentSettings.Mulgore = true;
        if (ObjectManager.Me.PlayerRace == PlayerFactions.Undead)
            WholesomeTBCFlightMasterSettings.CurrentSettings.Undercity = true;
        // TODO Ajouter Blood elf
    }

    private void FlightMasterLoop()
    {
        while (robotManager.Products.Products.IsStarted && _isLaunched)
        {
            if (!robotManager.Products.Products.InPause && _takenTaxi || _timer)
            {
                while (ObjectManager.Me.IsOnTaxi)
                    Thread.Sleep(1000);

                for (int pauseTaxiTime = WholesomeTBCFlightMasterSettings.CurrentSettings.PauseTaxiTime; pauseTaxiTime > 0 && _timer; pauseTaxiTime -= 1000)
                    Thread.Sleep(1000);

                if (!ScanNearbyTaxi.IsAlive)
                {
                    Logger.Log("Taxi scan not running, restarting...");
                    ScanNearbyTaxi.Start();
                }

                ResetTaxi();
            }
            Thread.Sleep(5000);
        }
        Dispose();
    }

    private static void ResetTaxi()
    {
        while (ObjectManager.Me.IsOnTaxi)
            Thread.Sleep(5000);

        Thread.Sleep(Usefuls.Latency * 3 + 1500);
        Logger.Log("Reset taxi");
        _takenTaxi = false;
        from = null;
        to = null;
        _timer = false;
        checkPath = true;
        checkPathActive = false;
        checkPathActiveFM = null;
    }

    private void WatchForEvents() => EventsLuaWithArgs.OnEventsLuaWithArgs += (id, args) =>
    {
        if (id != LuaEventsId.CHAT_MSG_PET_INFO
        || !WholesomeTBCFlightMasterSettings.CurrentSettings.UpdateTaxi
        || _updateNodes)
            return;

        _updateNodes = true;
        List<FlightMaster> flightMasterDbList = FillDB();

        foreach (FlightMaster flightMaster in flightMasterDbList)
        {
            if (flightMaster.Continent.Equals(GetCurrentContinent()))
            {
                int num2 = Lua.LuaDoString<int>("for i=0,30 do if string.find(TaxiNodeName(i),'" + flightMaster.Name + "') then return i end end return -1");
                if (num2 == -1 && flightMaster.AlreadyDiscovered)
                {
                    Logger.Log("Taxi node " + flightMaster.Name + " has not been discovered so far");
                    flightMaster.AlreadyDiscovered = false;
                    WholesomeTBCFlightMasterSettings.FlightMasterSaveChanges(flightMaster, false);
                }
                else if (num2 != -1 && !flightMaster.AlreadyDiscovered)
                {
                    Logger.Log("Taxi node " + flightMaster.Name + " has already been discovered");
                    flightMaster.AlreadyDiscovered = true;
                    WholesomeTBCFlightMasterSettings.FlightMasterSaveChanges(flightMaster, true);
                }
            }
        }
        _updateNodes = false;
        Thread.Sleep(Usefuls.Latency * 5 + 5000);
    };

    private static void MovementEventsOnOnSeemStuck()
    {
        Vector3 vector3 = new Vector3(-6033.529f, -2490.157f, 310.9456f, "None");

        if ((Usefuls.MapZoneName.Contains("Loch Modan") || Usefuls.MapZoneName.Contains("Searing Gorge"))
            && ObjectManager.Me.Position.DistanceTo2D(vector3) < 50.0
            && WholesomeTBCFlightMasterSettings.CurrentSettings.PauseSearingGorge)
        {
            ++stuckCounter;
            if (stuckCounter >= 5)
            {
                Logger.Log("Repeated stucks detected at the locked gate between Loch Modan and Searing Gorge. Going to stop bot, to prevent getting caught");
                stuckCounter = 0;
                robotManager.Products.Products.ProductStop();
            }
        }
        else
            stuckCounter = 0;

        if (!_timer && !_takenTaxi)
            return;

        Logger.Log("SeemStuck detected, reset taxi to help solving it");
        ResetTaxi();
    }

    private static void MovementEventsOnOnMovementPulse(List<Vector3> points, CancelEventArgs cancelable)
    {
        statusDiscover = Logging.Status;

        if (_taxiToDiscover
            && !discoverTaxiNode.Equals(null)
            && !_discoverInProcess
            && !_updateNodes
            && !statusDiscover.Contains("Boat")
            && !statusDiscover.Contains("Ship"))
        {
            _discoverInProcess = true;
            Thread.Sleep(Usefuls.Latency + 500);
            cancelable.Cancel = true;
            checkPathActive = true;
            checkPathActiveFM = discoverTaxiNode;
            DiscoverTaxi(discoverTaxiNode);
            Thread.Sleep(Usefuls.Latency * 3);
            cancelable.Cancel = false;
            checkPathActive = false;
        }

        if (!changer || _updateNodes || inProcessing || !ObjectManager.Me.IsAlive)
            return;

        changer = false;
        if (!_taxiToDiscover && !_timer && !_takenTaxi && ObjectManager.Me.Position.DistanceTo(((IEnumerable<Vector3>)points).Last()) > (double)WholesomeTBCFlightMasterSettings.CurrentSettings.TaxiTriggerDistance)
        {
            status = Logging.Status;

            if (WholesomeTBCFlightMasterSettings.CurrentSettings.SkipIfFollowPath
                && status.Contains("Follow Path")
                && !status.Contains("Resurrect")
                && CalculatePathTotalDistance(ObjectManager.Me.Position, ((IEnumerable<Vector3>)points).Last()) < (double)WholesomeTBCFlightMasterSettings.CurrentSettings.SkipIfFollowPathDistance)
            {
                Logger.Log("Currently following path or distance to start (" + CalculatePathTotalDistance(ObjectManager.Me.Position, ((IEnumerable<Vector3>)points).Last()) + " yards) is smaller than setting value (" + WholesomeTBCFlightMasterSettings.CurrentSettings.SkipIfFollowPathDistance + " yards)");
                Thread.Sleep(1000);
                cancelable.Cancel = false;
                inProcessing = false;
                checkPathActive = true;
                changer = true;
                _timer = true;
                return;
            }

            destinationVector = ((IEnumerable<Vector3>)points).Last();
            _saveDistance = CalculatePathTotalDistance(ObjectManager.Me.Position, ((IEnumerable<Vector3>)points).Last());
            Thread.Sleep(Usefuls.Latency + 500);
            cancelable.Cancel = true;

            if (!inProcessing)
            {
                from = GetClosestFlightMasterFrom();
                to = GetClosestFlightMasterTo();
            }

            Thread.Sleep(1000);
            if (to != null
                && from != null
                && !from.Equals(to)
                && CalculatePathTotalDistance(ObjectManager.Me.Position, from.Position) + (double)CalculatePathTotalDistance(to.Position, destinationVector) + WholesomeTBCFlightMasterSettings.CurrentSettings.ShorterMinDistance <= _saveDistance)
            {
                Logger.Log("Shorter path detected, taking Taxi from " + from.Name + " to " + to.Name);
                inProcessing = true;
                checkPathActive = true;
                checkPathActiveFM = from;
                TakeTaxi(from, to);
                Thread.Sleep(1000);
                cancelable.Cancel = false;
                inProcessing = false;
                checkPathActive = true;
            }
            else
            {
                Logger.Log("No shorter path available, skip flying");
                cancelable.Cancel = false;
                _timer = true;
                inProcessing = false;
            }
        }
        changer = true;
    }

    public static bool InCombat() => Lua.LuaDoString<bool>("return UnitAffectingCombat('player');", "");

    public static bool InCombatPet() => Lua.LuaDoString<bool>("return UnitAffectingCombat('pet');", "");

    private static async void Reenable() => await Task.Run((() =>
    {
        robotManager.Products.Products.InPause = true;

        if (ObjectManager.Me.WowClass == WoWClass.Hunter)
            Lua.LuaDoString("RotaOn = false", false);

        MovementManager.StopMove();
        MovementManager.CurrentPath.Clear();
        MovementManager.CurrentPathOrigine.Clear();

        Thread.Sleep(1000);
        robotManager.Products.Products.InPause = false;

        if (ObjectManager.Me.WowClass == WoWClass.Hunter)
            Lua.LuaDoString("RotaOn = true", false);

        Logger.Log("Resetting pathing");
    }));

    private static float CalculatePathTotalDistance(Vector3 from, Vector3 to)
    {
        float distance = 0.0f;
        List<Vector3> vectorList = new List<Vector3>();
        List<Vector3> path = PathFinder.FindPath(from, to);

        for (int index = 0; index < path.Count - 1; ++index)
            distance += path[index].DistanceTo2D(path[index + 1]);

        return distance;
    }

    public static FlightMaster GetClosestFlightMasterFrom()
    {
        List<FlightMaster> flightMasterList = FillDB();
        float num = 99999f;
        FlightMaster result = null;

        foreach (FlightMaster flightMaster in flightMasterList)
        {
            if (flightMaster.AlreadyDiscovered
                && flightMaster.Position.DistanceTo(ObjectManager.Me.Position) < num
                && flightMaster.Continent == GetCurrentContinent())
            {
                num = flightMaster.Position.DistanceTo(ObjectManager.Me.Position);
                result = flightMaster;
            }
        }

        if (result == null)
            Logger.Log("Closest FROM FlightMaster is null");
        else
            Logger.Log("Closest FROM FlightMaster is " + result.Name);

        return result;
    }

    public static FlightMaster GetClosestFlightMasterTo()
    {
        List<FlightMaster> flightMasterList = FillDB();
        float num = 99999f;
        FlightMaster result = null;

        foreach (FlightMaster flightMaster in flightMasterList)
        {
            if (flightMaster.AlreadyDiscovered
                && flightMaster.Position.DistanceTo(destinationVector) < num
                && flightMaster.Continent == GetCurrentContinent())
            {
                num = flightMaster.Position.DistanceTo(destinationVector);
                result = flightMaster;
            }
        }

        if (result == null)
            Logger.Log("Closest TO FlightMaster is null");
        else
            Logger.Log("Closest TO FlightMaster is " + result.Name);

        return result;
    }

    public static ContinentId GetCurrentContinent() => (ContinentId)Usefuls.ContinentId;

    public static void WaitFlying(string destinationFlightMaster)
    {
        while (ObjectManager.Me.IsOnTaxi)
        {
            Logger.Log("On taxi, waiting");
            Thread.Sleep(3000);
        }

        _takenTaxi = true;
        inProcessing = false;
        Thread.Sleep(5000);
        Reenable();
        Logger.Log("Arrived at destination " + destinationFlightMaster + " , finished waiting");
    }

    public static List<FlightMaster> FillDB() => new List<FlightMaster>()
    {
        new FlightMaster("Grom'gol", 1387, new Vector3(-12417.5f, 144.474f, 3.36881f, "None"), ContinentId.Azeroth, WholesomeTBCFlightMasterSettings.CurrentSettings.StranglethornGromgol),
        new FlightMaster("Booty Bay", 2858, new Vector3(-14448.6f, 506.129f, 26.3565f, "None"), ContinentId.Azeroth, WholesomeTBCFlightMasterSettings.CurrentSettings.StranglethornBootyBay),
        new FlightMaster("Silverpine Forest", 2226, new Vector3(473.939f, 1533.95f, 131.96f, "None"), ContinentId.Azeroth, WholesomeTBCFlightMasterSettings.CurrentSettings.SilverpineForest),
        new FlightMaster("Hillsbrad Foothills", 2389, new Vector3(2.67557f, -857.919f, 58.889f, "None"), ContinentId.Azeroth, WholesomeTBCFlightMasterSettings.CurrentSettings.HillsbradFoothills),
        new FlightMaster("Arathi Highlands", 2851, new Vector3(-917.658, -3496.93994140625, 70.4505004882813, "None"), ContinentId.Azeroth, WholesomeTBCFlightMasterSettings.CurrentSettings.ArathiHighlands),
        new FlightMaster("Badlands", 2861, new Vector3(-6632.22021484375, -2178.419921875, 244.227, "None"), ContinentId.Azeroth, WholesomeTBCFlightMasterSettings.CurrentSettings.Badlands),
        new FlightMaster("Mulgore", 2995, new Vector3(-1196.75f, 26.0777f, 177.033f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.Mulgore),
        new FlightMaster("Searing Gorge", 3305, new Vector3(-6559.26f, -1100.23f, 310.353f, "None"), ContinentId.Azeroth, WholesomeTBCFlightMasterSettings.CurrentSettings.SearingGorge),
        new FlightMaster("Orgrimmar", 3310, new Vector3(1676.25f, -4313.45f, 61.7176f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.Orgrimmar),
        new FlightMaster("Crossroads", 3615, new Vector3(-437.137f, -2596f, 95.8708f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.BarrensCrossroads),
        new FlightMaster("Camp Taurajo", 10378, new Vector3(-2384.08f, -1880.94f, 95.9336f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.BarrensTaurajo),
        new FlightMaster("Ratchet", 16227, new Vector3(-898.246f, -3769.65f, 11.7932f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.BarrensRatchet),
        new FlightMaster("Sun Rock Retreat", 4312, new Vector3(968.077f, 1042.29f, 104.563f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.StonetalonSunRockRetreat),
        new FlightMaster("The Hinterlands", 4314, new Vector3(-631.736f, -4720.6f, 5.48226f, "None"), ContinentId.Azeroth, WholesomeTBCFlightMasterSettings.CurrentSettings.TheHinterlands),
        new FlightMaster("Thousand Needles", 4317, new Vector3(-5407.12f, -2419.61f, 89.7094f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.ThousandNeedles),
        new FlightMaster("Undercity", 4551, new Vector3(1567.12f, 266.345f, -43.0194f, "None"), ContinentId.Azeroth, WholesomeTBCFlightMasterSettings.CurrentSettings.Undercity),
        new FlightMaster("Swamp of Sorrows", 6026, new Vector3(-10459.2f, -3279.76f, 21.5445f, "None"), ContinentId.Azeroth, WholesomeTBCFlightMasterSettings.CurrentSettings.SwampofSorrows),
        new FlightMaster("Desolace", 6726, new Vector3(-1770.37f, 3262.19f, 5.10852f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.Desolace),
        new FlightMaster("Tanaris", 7824, new Vector3(-7045.24f, -3779.4f, 10.3158f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.Tanaris),
        new FlightMaster("Feralas", 8020, new Vector3(-4421.94f, 198.146f, 25.1863f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.Feralas),
        new FlightMaster("Azshara", 8610, new Vector3(3664.02f, -4390.45f, 113.169f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.Azshara),
        new FlightMaster("Winterspring", 11139, new Vector3(6815.12f, -4610.12f, 710.759f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.Winterspring),
        new FlightMaster("Dustwallow Marsh", 11899, new Vector3(-3149.14f, -2842.13f, 34.6649f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.DustwallowMarsh),
        new FlightMaster("Felwood", 11900, new Vector3(5064.72f, -338.845f, 367.463f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.Felwood),
        new FlightMaster("Zoram'gar Outpost", 11901, new Vector3(3373.69f, 994.351f, 5.36158f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.AshenvaleZoramgar),
        new FlightMaster("Splintertree Post", 12616, new Vector3(2305.64f, -2520.15f, 103.893f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.AshenvaleSplintertree),
        new FlightMaster("Moonglade", 12740, new Vector3(7466.15f, -2122.08f, 492.427f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.Moonglade),
        new FlightMaster("Burning Steppes", 13177, new Vector3(-7504.06f, -2190.77f, 165.302f, "None"), ContinentId.Azeroth, WholesomeTBCFlightMasterSettings.CurrentSettings.BurningSteppes),
        new FlightMaster("Silithus", 15178, new Vector3(-6810.2f, 841.704f, 49.7481f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.Silithus),
        new FlightMaster("Un'Goro Crater", 10583, new Vector3(-6110.54f, -1140.35f, -186.866f, "None"), ContinentId.Kalimdor, WholesomeTBCFlightMasterSettings.CurrentSettings.UngoroCrater),
        new FlightMaster("Thrallmar", 16587, new Vector3(228.5f, 2633.57f, 87.67f, "None"), ContinentId.Expansion01, WholesomeTBCFlightMasterSettings.CurrentSettings.Thrallmar),
        new FlightMaster("Falcon Watch", 18942, new Vector3(-587.41f, 4101.01f, 91.37f, "None"), ContinentId.Expansion01, WholesomeTBCFlightMasterSettings.CurrentSettings.FalconWatch),
        new FlightMaster("Zabra'jin", 18791, new Vector3(219.45f, 7816f, 22.72f, "None"), ContinentId.Expansion01, WholesomeTBCFlightMasterSettings.CurrentSettings.Zabrajin),
        new FlightMaster("Garadar", 18808, new Vector3(-1261.09f, 7133.39f, 57.34f, "None"), ContinentId.Expansion01, WholesomeTBCFlightMasterSettings.CurrentSettings.Garadar),
        new FlightMaster("Area 52", 18938, new Vector3(3082.31f, 3596.11f, 144.02f, "None"), ContinentId.Expansion01, WholesomeTBCFlightMasterSettings.CurrentSettings.Area52),
        new FlightMaster("Shadowmoon Village", 19317, new Vector3(-3018.62f, 2557.09f, 79.09f, "None"), ContinentId.Expansion01, WholesomeTBCFlightMasterSettings.CurrentSettings.ShadowmoonVillage),
        new FlightMaster("Stonebreaker Hold", 18807, new Vector3(-2567.33f, 4423.83f, 39.33f, "None"), ContinentId.Expansion01, WholesomeTBCFlightMasterSettings.CurrentSettings.StonebreakerHold),
        new FlightMaster("Thunderlord Stronghold", 18953, new Vector3(2446.37f, 6020.93f, 154.34f, "None"), ContinentId.Expansion01, WholesomeTBCFlightMasterSettings.CurrentSettings.ThunderlordStronghold),
        new FlightMaster("Shattrath", 18940, new Vector3(-1837.23f, 5301.9f, -12.43f, "None"), ContinentId.Expansion01, WholesomeTBCFlightMasterSettings.CurrentSettings.Shattrath),
        new FlightMaster("The Stormspire", 19583, new Vector3(4157.58f, 2959.69f, 352.08f, "None"), ContinentId.Expansion01, WholesomeTBCFlightMasterSettings.CurrentSettings.TheStormspire),
        new FlightMaster("Altar of Sha'tar", 19581, new Vector3(-3065.6f, 749.42f, -10.1f, "None"), ContinentId.Expansion01, WholesomeTBCFlightMasterSettings.CurrentSettings.AltarofShatar),
        new FlightMaster("Cosmowrench", 20515, new Vector3(2974.95f, 1848.24f, 141.28f, "None"), ContinentId.Expansion01, WholesomeTBCFlightMasterSettings.CurrentSettings.Cosmowrench),
        new FlightMaster("Sanctum of the Stars", 21766, new Vector3(-4073.17f, 1123.61f, 42.47f, "None"), ContinentId.Expansion01, WholesomeTBCFlightMasterSettings.CurrentSettings.SanctumOfTheStars),
        new FlightMaster("Spinebreaker Ridge", 19558, new Vector3(-1316.84f, 2358.62f, 88.96f, "None"), ContinentId.Expansion01, WholesomeTBCFlightMasterSettings.CurrentSettings.SpinebreakerPost),
        new FlightMaster("Mok'Nathal Village", 22455, new Vector3(2028.79f, 4705.27f, 150.51f, "None"), ContinentId.Expansion01, WholesomeTBCFlightMasterSettings.CurrentSettings.MokNathalVillage),
        new FlightMaster("Evergrove", 22216, new Vector3(2976.01f, 5501.13f, 143.67f, "None"), ContinentId.Expansion01, WholesomeTBCFlightMasterSettings.CurrentSettings.Evergrove),
        new FlightMaster("Swamprat Post", 20762, new Vector3(91.67f, 5214.92f, 23.1f, "None"), ContinentId.Expansion01, WholesomeTBCFlightMasterSettings.CurrentSettings.SwampratPost),
        new FlightMaster("The Dark Portal", 18930, new Vector3(-178.09f, 1026.72f, 54.19f, "None"), ContinentId.Expansion01, WholesomeTBCFlightMasterSettings.CurrentSettings.TheDarkPortal)
    };

    private static void TakeTaxi(FlightMaster from, FlightMaster to)
    {
        if (!GoToTask.ToPosition(from.Position, conditionExit: context => Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause && !Conditions.IsAttackedAndCannotIgnore) || !GoToTask.ToPositionAndIntecractWithNpc(from.Position, from.NPCId, conditionExit: context => Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause && !Conditions.IsAttackedAndCannotIgnore))
            return;

        while (!ObjectManager.Me.IsOnTaxi)
        {
            if (ObjectManager.Me.IsMounted)
                MountTask.DismountMount(false, false, 100);

            Usefuls.SelectGossipOption((GossipOptionsType)7);
            Thread.Sleep(Usefuls.Latency + 1500);

            while (_updateNodes)
            {
                Logger.Log("Taxi node update in progress, waiting...");
                Thread.Sleep(10000);
            }

            Lua.LuaDoString("TakeTaxiNode(" + (Lua.LuaDoString<int>("for i=0,20 do if string.find(TaxiNodeName(i),'" + to.Name.Replace("'", "\\'") + "') then return i end end", "")).ToString() + ")", false);
            Logger.Log("Taking Taxi from " + from.Name + " to " + to.Name);
            Thread.Sleep(Usefuls.Latency + 500);

            Keyboard.DownKey(Memory.WowMemory.Memory.WindowHandle, Keys.Escape);

            Thread.Sleep(Usefuls.Latency + 2500);
            if (!ObjectManager.Me.IsOnTaxi)
                GoToTask.ToPositionAndIntecractWithNpc(from.Position, from.NPCId, conditionExit: context => Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause && !Conditions.IsAttackedAndCannotIgnore);
        }
        if (ObjectManager.Me.IsOnTaxi)
            WaitFlying(to.Name);
    }

    private static void DiscoverTaxi(FlightMaster flightMasterToDiscover)
    {
        WholesomeTBCFlightMasterSettings.Load();
        FillDB();

        if (GoToTask.ToPosition(flightMasterToDiscover.Position, conditionExit: context => Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause && !Conditions.IsAttackedAndCannotIgnore))
        {
            GoToTask.ToPosition(flightMasterToDiscover.Position, conditionExit: context => Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause && !Conditions.IsAttackedAndCannotIgnore);

            if (GoToTask.ToPositionAndIntecractWithNpc(flightMasterToDiscover.Position, flightMasterToDiscover.NPCId, conditionExit: context => Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause && !Conditions.IsAttackedAndCannotIgnore))
            {
                wManagerSetting.ClearBlacklistOfCurrentProductSession();

                GoToTask.ToPositionAndIntecractWithNpc(flightMasterToDiscover.Position, flightMasterToDiscover.NPCId, conditionExit: context => Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause && !Conditions.IsAttackedAndCannotIgnore);
                if (ObjectManager.Me.IsMounted)
                    MountTask.DismountMount(false, false, 100);

                Usefuls.SelectGossipOption((GossipOptionsType)7);
                Thread.Sleep(Usefuls.Latency + 1500);

                while (_updateNodes)
                {
                    Logger.Log("Taxi node update in progress...");
                    Thread.Sleep(10000);
                }

                Logger.Log("Flight Master " + flightMasterToDiscover.Name + " discovered");
                flightMasterToDiscover.AlreadyDiscovered = true;
                WholesomeTBCFlightMasterSettings.FlightMasterSaveChanges(flightMasterToDiscover, true);
                Thread.Sleep(Usefuls.Latency * 5);
                timer = 0;
                discoverTaxiNode = null;
                _taxiToDiscover = false;
                _discoverInProcess = false;
                _discoverTaxiTimer = true;
                Reenable();
                return;
            }
        }
        _discoverInProcess = false;
    }
}
