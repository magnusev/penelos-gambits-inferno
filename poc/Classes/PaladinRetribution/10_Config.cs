// ========================================
// PALADIN RETRIBUTION - CONFIGURATION
// ========================================

// Retribution-specific constants
private const int HOLY_POWER = 9;
private const string INTERRUPT_SPELL = "Rebuke";

// Interrupt tracking
private Random _rng = new Random();
private int _lastCastingID = 0;
private int _interruptTargetPct = 0;

public override void LoadSettings()
{
    Settings.Add(new Setting("=== Retribution Paladin ==="));
    Settings.Add(new Setting("Enable Logging", true));
    
    Settings.Add(new Setting("=== Offensive Cooldowns ==="));
    Settings.Add(new Setting("Use Avenging Wrath", true));
    Settings.Add(new Setting("Use Wake of Ashes", true));
    Settings.Add(new Setting("Use Execution Sentence", true));
    Settings.Add(new Setting("Use Trinkets", true));
    
    Settings.Add(new Setting("=== Defensives ==="));
    Settings.Add(new Setting("Use Defensives", true));
    Settings.Add(new Setting("Divine Shield HP %", 1, 100, 15));
    Settings.Add(new Setting("Lay on Hands HP %", 1, 100, 20));
    Settings.Add(new Setting("Word of Glory HP %", 1, 100, 50));
    Settings.Add(new Setting("Shield of Vengeance HP %", 1, 100, 70));
    Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
    
    Settings.Add(new Setting("=== Interrupt ==="));
    Settings.Add(new Setting("Auto Interrupt", true));
    Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
    Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
}

public override void Initialize()
{
    // Offensive abilities
    Spellbook.Add("Avenging Wrath");
    Spellbook.Add("Blade of Justice");
    Spellbook.Add("Crusader Strike");
    Spellbook.Add("Divine Storm");
    Spellbook.Add("Divine Toll");
    Spellbook.Add("Execution Sentence");
    Spellbook.Add("Hammer of Light");
    Spellbook.Add("Hammer of Wrath");
    Spellbook.Add("Judgment");
    Spellbook.Add("Templar Slash");
    Spellbook.Add("Templar Strike");
    Spellbook.Add("Templar's Verdict");
    Spellbook.Add("Wake of Ashes");
    
    // Talent checks (for conditional logic)
    Spellbook.Add("Art of War");
    Spellbook.Add("Crusading Strikes");
    Spellbook.Add("Empyrean Power");
    Spellbook.Add("Holy Flames");
    Spellbook.Add("Light's Guidance");
    Spellbook.Add("Radiant Glory");
    Spellbook.Add("Righteous Cause");
    Spellbook.Add("Walk Into Light");
    
    // Defensive abilities
    Spellbook.Add("Divine Shield");
    Spellbook.Add("Lay on Hands");
    Spellbook.Add("Shield of Vengeance");
    Spellbook.Add("Word of Glory");
    
    // Utility
    Spellbook.Add("Rebuke");
    Spellbook.Add("Retribution Aura");
    
    // Racial abilities
    Spellbook.Add("Ancestral Call");
    Spellbook.Add("Berserking");
    Spellbook.Add("Blood Fury");
    Spellbook.Add("Fireblood");
    Spellbook.Add("Lights Judgment");
    
    // Macros
    Macros.Add("trinket1", "/use 13");
    Macros.Add("trinket2", "/use 14");
    
    // Initialize shared components
    InitializeSharedComponents();
    
    // Custom commands
    CustomCommands.Add("NoCDs");
    CustomCommands.Add("ForceST");
    
    Inferno.Latency = 250;
    _logFile = "penelos_paladin_ret_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    Inferno.PrintMessage("Penelos Gambits - Retribution Paladin loaded!", Color.Gold);
    Log("Initialize complete");
}

