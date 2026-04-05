// ========================================
// PALADIN PROTECTION - CONFIGURATION
// ========================================

// Protection-specific constants
private const int HOLY_POWER = 9;
private const string INTERRUPT_SPELL = "Rebuke";

// Interrupt tracking
private Random _rng = new Random();

// Map tracking for zone change logging
private int _lastMapId = 0;

public override void LoadSettings()
{
    Settings.Add(new Setting("=== Protection Paladin ==="));
    Settings.Add(new Setting("Enable Logging", true));
    Settings.Add(new Setting("Use Avenging Wrath", true));
    Settings.Add(new Setting("Use Trinkets", true));
    
    Settings.Add(new Setting("=== Defensives ==="));
    Settings.Add(new Setting("Use Defensives", true));
    Settings.Add(new Setting("Word of Glory HP %", 1, 100, 50));
    Settings.Add(new Setting("Ardent Defender HP %", 1, 100, 35));
    Settings.Add(new Setting("Guardian of Ancient Kings HP %", 1, 100, 25));
    Settings.Add(new Setting("Lay on Hands HP %", 1, 100, 15));
    Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
    
    Settings.Add(new Setting("=== Interrupt ==="));
    Settings.Add(new Setting("Auto Interrupt", true));
    Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
    Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
}

public override void Initialize()
{
    // Offensive abilities
    Spellbook.Add("Avenger's Shield");
    Spellbook.Add("Avenging Wrath");
    Spellbook.Add("Blessed Hammer");
    Spellbook.Add("Consecration");
    Spellbook.Add("Divine Toll");
    Spellbook.Add("Hammer of Light");
    Spellbook.Add("Hammer of the Righteous");
    Spellbook.Add("Hammer of Wrath");
    Spellbook.Add("Holy Armaments");
    Spellbook.Add("Judgment");
    Spellbook.Add("Shield of the Righteous");
    
    // Defensive abilities
    Spellbook.Add("Ardent Defender");
    Spellbook.Add("Divine Shield");
    Spellbook.Add("Guardian of Ancient Kings");
    Spellbook.Add("Lay on Hands");
    Spellbook.Add("Word of Glory");
    
    // Utility
    Spellbook.Add("Devotion Aura");
    Spellbook.Add("Rebuke");
    
    // Racial abilities
    Spellbook.Add("Ancestral Call");
    Spellbook.Add("Berserking");
    Spellbook.Add("Blood Fury");
    Spellbook.Add("Fireblood");
    Spellbook.Add("Lights Judgment");
    
    // Macros
    Macros.Add("trinket1", "/use 13");
    Macros.Add("trinket2", "/use 14");
    
    // Initialize shared components (focus macros, utility macros, healthstone function)
    InitializeSharedComponents();
    
    // Custom commands
    CustomCommands.Add("NoCDs");
    CustomCommands.Add("ForceST");
    
    _logFile = "penelos_paladin_prot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    Inferno.PrintMessage("Penelos Gambits - Protection Paladin loaded!", Color.Gold);
    Log("Initialize complete");
}

