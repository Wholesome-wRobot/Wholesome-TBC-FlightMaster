﻿using robotManager.Helpful;
using System.Drawing;

public class Logger
{
    public static void Log(string s)
    {
        Logging.Write($"[Wholesome TBC-WotlK FlightMaster] {s}", Logging.LogType.Normal, Color.DarkCyan);
    }
    public static void LogDebug(string s)
    {
        Logging.WriteDebug($"[Wholesome TBC-WotlK FlightMaster] {s}");
    }
    public static void LogError(string s)
    {
        Logging.WriteError($"[Wholesome TBC-WotlK FlightMaster] {s}");
    }
    public static void LogWarning(string s)
    {
        Logging.Write($"[Wholesome TBC-WotlK FlightMaster] {s}", Logging.LogType.Normal, Color.IndianRed);
    }
}
