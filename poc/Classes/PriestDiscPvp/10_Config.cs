// ========================================
// PRIEST DISCIPLINE PVP - CONFIGURATION
// ========================================

// Discipline-specific constants
private const int INSANITY = 13;
private const string INTERRUPT_SPELL = "Silence";

// Interrupt tracking
private Random _rng = new Random();

// Map tracking for zone change logging
private int _lastMapId = 0;

public override void LoadSettings()
{
    Settings.Add(new Setting("=== Discipline Priest (VOIDWEAVER ARENA) ==="));
    Settings.Add(new Setting("Enable Logging", true));
    Settings.Add(new Setting("Use Power Infusion", true));
    Settings.Add(new Setting("Use Evangelism Burst", true));
    Settings.Add(new Setting("Use Trinkets", true));
    Settings.Add(new Setting("Auto Radiance on Proc Only", true));
    
    Settings.Add(new Setting("=== Defensives ==="));
    Settings.Add(new Setting("Use Defensives", true));
    Settings.Add(new Setting("Auto Pain Suppression < 55%", true));
    Settings.Add(new Setting("Auto Flash Heal on Surge of Light < 88%", true));
    Settings.Add(new Setting("Auto Desperate Prayer < 35%", true));
    Settings.Add(new Setting("Auto Leap of Faith < 25%", true));
    Settings.Add(new Setting("Protect Focus", true));
    Settings.Add(new Setting("Healthstone HP %", 1, 100, 45));
    
    Settings.Add(new Setting("=== Interrupt / Control ==="));
    Settings.Add(new Setting("Auto Interrupt", true));
    Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 35));
    Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 85));
}

public override void Initialize()
{
    // Offensive abilities
    Spellbook.Add("Evangelism");
    Spellbook.Add("Mind Blast");
    Spellbook.Add("Penance");
    Spellbook.Add("Power Infusion");
    Spellbook.Add("Power Word: Radiance");
    Spellbook.Add("Purge the Wicked");
    Spellbook.Add("Shadow Word: Death");
    Spellbook.Add("Shadow Word: Pain");
    Spellbook.Add("Smite");
    Spellbook.Add("Void Blast");
    Spellbook.Add("Voidwraith");
    
    // Talent checks
    Spellbook.Add("Phase Shift");
    Spellbook.Add("Purge the Wicked");
    Spellbook.Add("Ultimate Radiance");
    
    // Defensive/Utility
    Spellbook.Add("Desperate Prayer");
    Spellbook.Add("Dispel Magic");
    Spellbook.Add("Flash Heal");
    Spellbook.Add("Leap of Faith");
    Spellbook.Add("Mass Dispel");
    Spellbook.Add("Pain Suppression");
    Spellbook.Add("Psychic Scream");
    Spellbook.Add("Silence");
    
    // Racials
    Spellbook.Add("Ancestral Call");
    Spellbook.Add("Berserking");
    Spellbook.Add("Blood Fury");
    Spellbook.Add("Fireblood");
    Spellbook.Add("Lights Judgment");
    
    // Macros
    Macros.Add("trinket1", "/use 13");
    Macros.Add("trinket2", "/use 14");
    
    // Custom functions
    CustomFunctions.Add("PetIsActive", "return (UnitExists('pet') and not UnitIsDead('pet')) and 1 or 0");
    
    // Initialize shared components
    InitializeSharedComponents();
    
    // Custom commands
    CustomCommands.Add("NoCDs");
    CustomCommands.Add("ForceST");
    
    Inferno.Latency = 195;
    _logFile = "penelos_priest_disc_pvp_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    Inferno.PrintMessage("Penelos Gambits - Discipline Priest PvP loaded!", Color.DarkSlateGray);
    Log("Initialize complete");
}

