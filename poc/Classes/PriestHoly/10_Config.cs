// ========================================
// PRIEST HOLY - CONFIGURATION
// ========================================

// Priest has no class-specific constants beyond shared ones

public override void LoadSettings()
{
    Settings.Add(new Setting("Enable Logging", true));
    Settings.Add(new Setting("Use Circle of Healing", true));
    Settings.Add(new Setting("Use Prayer of Mending", true));
    Settings.Add(new Setting("Do DPS", false));
    Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
}

public override void Initialize()
{
    Spellbook.Add("Circle of Healing");
    Spellbook.Add("Desperate Prayer");
    Spellbook.Add("Dispel Magic");
    Spellbook.Add("Divine Hymn");
    Spellbook.Add("Divine Star");
    Spellbook.Add("Flash Heal");
    Spellbook.Add("Guardian Spirit");
    Spellbook.Add("Halo");
    Spellbook.Add("Heal");
    Spellbook.Add("Holy Fire");
    Spellbook.Add("Holy Word: Sanctify");
    Spellbook.Add("Holy Word: Serenity");
    Spellbook.Add("Mindgames");
    Spellbook.Add("Power Word: Fortitude");
    Spellbook.Add("Power Word: Life");
    Spellbook.Add("Power Word: Shield");
    Spellbook.Add("Prayer of Healing");
    Spellbook.Add("Prayer of Mending");
    Spellbook.Add("Renew");
    Spellbook.Add("Shadow Word: Death");
    Spellbook.Add("Shadow Word: Pain");
    Spellbook.Add("Smite");

    // Priest-specific macros
    Macros.Add("cast_fh", "/cast [@focus] Flash Heal");
    Macros.Add("cast_heal", "/cast [@focus] Heal");
    Macros.Add("cast_renew", "/cast [@focus] Renew");
    Macros.Add("cast_pws", "/cast [@focus] Power Word: Shield");
    Macros.Add("cast_pom", "/cast [@focus] Prayer of Mending");
    Macros.Add("cast_serenity", "/cast [@focus] Holy Word: Serenity");
    Macros.Add("cast_pwl", "/cast [@focus] Power Word: Life");
    Macros.Add("cast_gs", "/cast [@focus] Guardian Spirit");
    Macros.Add("cast_dispel", "/cast [@focus] Dispel Magic");

    // Initialize shared components (focus macros, utility macros, healthstone function)
    InitializeSharedComponents();

    _logFile = "penelos_priest_holy_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    Inferno.PrintMessage("Penelos Gambits - Holy Priest loaded!", Color.Green);
    Log("Initialize complete");
}


