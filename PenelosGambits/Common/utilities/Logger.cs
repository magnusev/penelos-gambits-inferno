public static class Logger
{
    private static string logFilePath;
    private static string unknownMapFilePath;
    private static string unknownMobFilePath;
    private static string unknownSpellsFilePath;
    private static bool writeToFile = true;
    private static bool writeToConcole = false;

    private static Throttler SpellCastringLogThrottler = new Throttler(500);

    private static List<string> LoggedMaps = new List<string>();
    private static List<string> LoggedMobs = new List<string>();
    private static List<string> LoggedSpells = new List<string>();

    static Logger()
    {
        string now = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        
        logFilePath = now + "-penellosgambits_logger.log";
        unknownMapFilePath = "penellosgambits_logger_unknown-maps.log";
        unknownMobFilePath = "penellosgambits_logger_unknown-mobs.log";
        unknownSpellsFilePath = "penellosgambits_logger_unknown-spells.log";
    }
    
    public static void Log(string message)
    {
        if (!File.Exists(logFilePath))
        {
            // Create the file
            using (StreamWriter sw = File.CreateText(logFilePath))
            {
                sw.WriteLine("Log File Created: " + GetTimeAsString());
            }
        }

        LogMessage(logFilePath, message, writeToConcole);
    }

    public static void LogUnknownMap(string message)
    {
        if (LoggedMaps.Contains(message))
        {
            return;
        }
        
        if (!File.Exists(unknownMapFilePath))
        {
            using (StreamWriter sw = File.CreateText(unknownMapFilePath))
            {
                sw.WriteLine("Log File Created: " + GetTimeAsString());
            }
        }
        
        LogMessage(unknownMapFilePath, message, false);
        LoggedMaps.Add(message);
    }
    
    public static void LogUnknownMob(string message)
    {
        if (LoggedMobs.Contains(message))
        {
            return;
        }
        
        if (!File.Exists(unknownMobFilePath))
        {
            using (StreamWriter sw = File.CreateText(unknownMobFilePath))
            {
                sw.WriteLine("Log File Created: " + GetTimeAsString());
            }
        }

        LogMessage(unknownMobFilePath, message, false);
        LoggedMobs.Add(message);
    }
    
    public static void LogUnknownSpell(string message)
    {
        if (LoggedSpells.Contains(message))
        {
            return;
        }
        
        if (!File.Exists(unknownSpellsFilePath))
        {
            using (StreamWriter sw = File.CreateText(unknownSpellsFilePath))
            {
                sw.WriteLine("Log File Created: " + GetTimeAsString());
            }
        }
        
        LogMessage(unknownSpellsFilePath, message, false);
        LoggedSpells.Add(message);
    }

    private static void LogMessage(string logpath, string message, bool console)
    {
        if (writeToFile)
        {
            // Append a line to the log file
            using (StreamWriter sw = File.AppendText(logpath))
            {
                sw.WriteLine(GetTimeAsString() + ": " + message);
            }
        }

        if (console)
        {
            Inferno.PrintMessage(message);
        }
    }
    
    public static void LogSpellcastingEnemy(int id, int spellId)
    {
        if (SpellCastringLogThrottler.IsOpen())
        {
            Log(id + " is casting spell " + spellId);
        }
    }

    private static string GetTimeAsString()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH.mm:ss");
    }
}