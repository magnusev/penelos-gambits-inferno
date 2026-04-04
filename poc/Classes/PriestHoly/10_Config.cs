// ========================================
// PRIEST HOLY - CONFIGURATION
// ========================================

// Interrupt tracking (defined for universal component, but not actively used by healers)
private Random _rng = new Random();
private int _lastCastingID = 0;
private int _interruptTargetPct = 0;
private const string INTERRUPT_SPELL = ""; // Healers don't interrupt

public override void LoadSettings()
{
    Settings.Add(new Setting("Enable Logging", true));
    Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
}

public override void Initialize()
{
    Spellbook.Add("Apotheosis");
    Spellbook.Add("Flash Heal");
    Spellbook.Add("Halo");
    Spellbook.Add("Holy Fire");
    Spellbook.Add("Holy Word: Chastise");
    Spellbook.Add("Holy Word: Serenity");
    Spellbook.Add("Prayer of Mending");
    Spellbook.Add("Purify");
    Spellbook.Add("Smite");

    // Priest-specific macros
    Macros.Add("cast_pom", "/cast [@focus] Prayer of Mending");
    Macros.Add("cast_purify", "/cast [@focus] Purify");
    Macros.Add("cast_flash_heal", "/cast [@focus] Flash Heal");
    Macros.Add("cast_serenity", "/cast [@focus] Holy Word: Serenity");

    // Initialize shared components (focus macros, utility macros, healthstone function)
    InitializeSharedComponents();

    _logFile = "penelos_priest_holy_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    Inferno.PrintMessage("Penelos Gambits - Holy Priest loaded!", Color.Green);
    Log("Initialize complete");
}


