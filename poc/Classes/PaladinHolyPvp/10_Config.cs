// ========================================
// PALADIN HOLY PVP - CONFIGURATION
// ========================================

// Paladin-specific constants
private const int HOLY_POWER = 9;
private const string INTERRUPT_SPELL = "Hammer of Justice";

// Interrupt tracking
private Random _rng = new Random();
private int _lastCastingID = 0;
private int _interruptTargetPct = 0;

public override void LoadSettings()
{
    Settings.Add(new Setting("=== Holy Paladin PVP Arena ==="));
    Settings.Add(new Setting("Enable Logging", true));
    Settings.Add(new Setting("Use Avenging Crusader", true));
    Settings.Add(new Setting("Use Divine Toll", true));
    Settings.Add(new Setting("Use Trinkets", true));
    
    Settings.Add(new Setting("=== Defensives ==="));
    Settings.Add(new Setting("Use Defensives", true));
    Settings.Add(new Setting("Auto Blessing of Protection < 30%", true));
    Settings.Add(new Setting("Auto Blessing of Sacrifice < 40%", true));
    Settings.Add(new Setting("Auto Lay on Hands < 25%", true));
    Settings.Add(new Setting("Auto Word of Glory < 80%", true));
    Settings.Add(new Setting("Auto Flash of Light on Infusion Proc < 88%", true));
    Settings.Add(new Setting("Protect Focus", true));
    Settings.Add(new Setting("Healthstone HP %", 1, 100, 45));
    
    Settings.Add(new Setting("=== Interrupt ==="));
    Settings.Add(new Setting("Auto Interrupt", true));
    Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
    Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 85));
}

public override void Initialize()
{
    // Offensive abilities
    Spellbook.Add("Avenging Crusader");
    Spellbook.Add("Crusader Strike");
    Spellbook.Add("Divine Toll");
    Spellbook.Add("Flash of Light");
    Spellbook.Add("Holy Light");
    Spellbook.Add("Holy Prism");
    Spellbook.Add("Holy Shock");
    Spellbook.Add("Judgment");
    Spellbook.Add("Light of Dawn");
    Spellbook.Add("Word of Glory");
    
    // Defensive/Utility
    Spellbook.Add("Beacon of Light");
    Spellbook.Add("Blessing of Freedom");
    Spellbook.Add("Blessing of Protection");
    Spellbook.Add("Blessing of Sacrifice");
    Spellbook.Add("Cleanse");
    Spellbook.Add("Divine Shield");
    Spellbook.Add("Hammer of Justice");
    Spellbook.Add("Lay on Hands");
    
    // Racials
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
    
    Inferno.Latency = 185;
    _logFile = "penelos_paladin_holy_pvp_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    Inferno.PrintMessage("Penelos Gambits - Holy Paladin PvP loaded!", Color.Gold);
    Log("Initialize complete");
}

