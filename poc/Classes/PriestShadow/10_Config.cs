// ========================================
// PRIEST SHADOW - CONFIGURATION
// ========================================

// Shadow-specific constants
private const int INSANITY = 13;
private const string INTERRUPT_SPELL = "Silence";

// Interrupt tracking
private Random _rng = new Random();
private int _lastCastingID = 0;
private int _interruptTargetPct = 0;

public override void LoadSettings()
{
    Settings.Add(new Setting("=== Shadow Priest ==="));
    Settings.Add(new Setting("Enable Logging", true));
    Settings.Add(new Setting("Use Voidform", true));
    Settings.Add(new Setting("Use Halo", true));
    Settings.Add(new Setting("Use Power Infusion", true));
    Settings.Add(new Setting("Use Trinkets", true));
    Settings.Add(new Setting("Auto Shadowform", true));
    Settings.Add(new Setting("Auto Power Word: Fortitude", true));
    Settings.Add(new Setting("AoE enemy count threshold", 2, 10, 3));
    
    Settings.Add(new Setting("=== Defensives ==="));
    Settings.Add(new Setting("Use Defensives", true));
    Settings.Add(new Setting("Dispersion HP %", 1, 100, 25));
    Settings.Add(new Setting("Vampiric Embrace HP %", 1, 100, 50));
    Settings.Add(new Setting("Desperate Prayer HP %", 1, 100, 60));
    Settings.Add(new Setting("Power Word: Shield HP %", 1, 100, 70));
    Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
    
    Settings.Add(new Setting("=== Interrupt ==="));
    Settings.Add(new Setting("Auto Interrupt", true));
    Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
    Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
}

public override void Initialize()
{
    // Offensive abilities
    Spellbook.Add("Halo");
    Spellbook.Add("Mind Blast");
    Spellbook.Add("Mind Flay");
    Spellbook.Add("Mind Flay: Insanity");
    Spellbook.Add("Mindbender");
    Spellbook.Add("Power Infusion");
    Spellbook.Add("Shadow Word: Death");
    Spellbook.Add("Shadow Word: Madness");
    Spellbook.Add("Shadow Word: Pain");
    Spellbook.Add("Tentacle Slam");
    Spellbook.Add("Vampiric Touch");
    Spellbook.Add("Void Blast");
    Spellbook.Add("Void Torrent");
    Spellbook.Add("Void Volley");
    Spellbook.Add("Voidform");
    Spellbook.Add("Voidwraith");
    
    // Talent checks
    Spellbook.Add("Art of War");
    Spellbook.Add("Deathspeaker");
    Spellbook.Add("Devour Matter");
    Spellbook.Add("Distorted Reality");
    Spellbook.Add("Idol of Y'Shaarj");
    Spellbook.Add("Inescapable Torment");
    Spellbook.Add("Invoked Nightmare");
    Spellbook.Add("Maddening Tentacles");
    Spellbook.Add("Mind Devourer");
    Spellbook.Add("Shadowfiend");
    Spellbook.Add("Void Apparitions");
    
    // Defensive abilities
    Spellbook.Add("Desperate Prayer");
    Spellbook.Add("Dispersion");
    Spellbook.Add("Power Word: Shield");
    Spellbook.Add("Vampiric Embrace");
    
    // Utility
    Spellbook.Add("Power Word: Fortitude");
    Spellbook.Add("Shadowform");
    Spellbook.Add("Silence");
    
    // Racial abilities
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
    
    Inferno.Latency = 250;
    _logFile = "penelos_priest_shadow_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    Inferno.PrintMessage("Penelos Gambits - Shadow Priest loaded!", Color.DarkViolet);
    Log("Initialize complete");
}


