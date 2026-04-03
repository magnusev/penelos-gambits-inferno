// ========================================
// PALADIN HOLY - CONFIGURATION
// ========================================

// Paladin-specific constants
private const int HOLY_POWER = 9;

// Holy Shock charge tracking (API returns 0 for cd/charges)
private int _hsCharges = 2;
private long _hsLastRechargeMs = 0;
private const int HS_MAX_CHARGES = 2;
private const int HS_RECHARGE_MS = 5000;

public override void LoadSettings()
{
    Settings.Add(new Setting("Enable Logging", true));
    Settings.Add(new Setting("Use Light of Dawn", false));
    Settings.Add(new Setting("Do DPS", false));
    Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
}

public override void Initialize()
{
    Spellbook.Add("Avenging Wrath"); 
    Spellbook.Add("Blessing of Freedom");
    Spellbook.Add("Cleanse"); 
    Spellbook.Add("Divine Protection");
    Spellbook.Add("Divine Toll"); 
    Spellbook.Add("Flash of Light");
    Spellbook.Add("Holy Light"); 
    Spellbook.Add("Holy Shock");
    Spellbook.Add("Judgment"); 
    Spellbook.Add("Light of Dawn");
    Spellbook.Add("Shield of the Righteous"); 
    Spellbook.Add("Word of Glory");

    // Paladin-specific macros
    Macros.Add("cast_fol", "/cast [@focus] Flash of Light");
    Macros.Add("cast_hl", "/cast [@focus] Holy Light");
    Macros.Add("cast_hs", "/cast [@focus] Holy Shock");
    Macros.Add("cast_wog", "/cast [@focus] Word of Glory");
    Macros.Add("cast_dt", "/cast [@focus] Divine Toll");
    Macros.Add("cast_cleanse", "/cast [@focus] Cleanse");
    Macros.Add("cast_bof", "/cast [@focus] Blessing of Freedom");

    // Initialize shared components (focus macros, utility macros, healthstone function)
    InitializeSharedComponents();

    _logFile = "penelos_paladin_holy_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    Inferno.PrintMessage("Penelos Gambits - Holy Paladin loaded!", Color.Green);
    Log("Initialize complete");
}

