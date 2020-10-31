using robotManager.Helpful;
using System.Drawing;

public class Logger
{
    public static void Log(string s)
    {
        Logging.Write($"[Wholesome TBC FlightMaster] {s}", Logging.LogType.Normal, Color.DarkCyan);
    }
}
