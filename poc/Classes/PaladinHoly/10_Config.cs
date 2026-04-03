// ========================================
// PALADIN HOLY - CONFIGURATION
// ========================================

// Constants
private const int MANA = 0;
private const int HOLY_POWER = 9;
private const int HEALTHSTONE_ID = 5512;
private const int DIAGNOSTIC_LOG_INTERVAL_MS = 2000;

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

    Macros.Add("cast_fol", "/cast [@focus] Flash of Light");
    Macros.Add("cast_hl", "/cast [@focus] Holy Light");
    Macros.Add("cast_hs", "/cast [@focus] Holy Shock");
    Macros.Add("cast_wog", "/cast [@focus] Word of Glory");
    Macros.Add("cast_dt", "/cast [@focus] Divine Toll");
    Macros.Add("cast_cleanse", "/cast [@focus] Cleanse");
    Macros.Add("cast_bof", "/cast [@focus] Blessing of Freedom");
    Macros.Add("focus_player", "/focus player");
    for (int i = 1; i <= 4; i++)
    {
        Macros.Add("focus_party" + i, "/focus party" + i);
    }
    for (int i = 1; i <= 28; i++)
    {
        Macros.Add("focus_raid" + i, "/focus raid" + i);
    }
    Macros.Add("target_enemy", "/targetenemy");
    Macros.Add("use_healthstone", "/use Healthstone");
    
    string hasHealthstoneCode = "return GetItemCount(5512) > 0 and 1 or 0";
    CustomFunctions.Add("HasHealthstone", hasHealthstoneCode);

    _logFile = "penelos_paladin_holy_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    Inferno.PrintMessage("Penelos Gambits - Holy Paladin loaded!", Color.Green);
    Log("Initialize complete");
}

