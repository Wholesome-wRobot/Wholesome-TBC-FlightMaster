using robotManager.Helpful;
using System.Collections.Generic;
using System.Diagnostics;
using wManager.Wow.Enums;

public class FlightMaster
{
    public int NPCId { get; set; }
    public Vector3 Position { get; set; }
    public string Name { get; set; }
    public ContinentId Continent { get; set; }

    private Stopwatch disableTimer = new Stopwatch();

    public FlightMaster(string name, int npcId, Vector3 position, ContinentId continent)
    {
        Name = name;
        NPCId = npcId;
        Position = position;
        Continent = continent;
        disableTimer.Reset();
    }

    public bool IsDiscovered => WFMSettings.CurrentSettings.KnownFlightsList.Contains(Name);

    public bool IsDisabledByPlugin()
    {
        bool isDisabled = disableTimer.IsRunning && disableTimer.ElapsedMilliseconds < WFMSettings.CurrentSettings.PauseLengthInSeconds * 1000;
        if (disableTimer.ElapsedMilliseconds >= WFMSettings.CurrentSettings.PauseLengthInSeconds * 1000)
        {
            disableTimer.Reset();
            isDisabled = false;
        }
        return isDisabled;
    }

    public void Disable(string reason)
    {
        Logger.Log($"Disabling {Name} for {WFMSettings.CurrentSettings.PauseLengthInSeconds} seconds. ({reason})");
        disableTimer.Restart();
    }
}
