using robotManager.Helpful;
using wManager.Wow.Enums;

public class FlightMaster
{
    public int NPCId { get; set; }
    public Vector3 Position { get; set; }
    public string Name { get; set; }
    public ContinentId Continent { get; set; }

    public FlightMaster(string name, int npcId, Vector3 position, ContinentId continent)
    {
        Name = name;
        NPCId = npcId;
        Position = position;
        Continent = continent;
    }

    public bool IsDiscovered()
    {
        if (WFMSettings.CurrentSettings.KnownFlightsList.Contains(Name))
            return true;
        return false;
    }
}
