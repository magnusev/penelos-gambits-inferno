using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using InfernoWow.API;

namespace InfernoWow.Modules
{

public class ShadowPriestPvE : Rotation
{
private string _queuedAction = null;
private string _lastLoggedAction = null;
private Dictionary<string, long> _throttleTimestamps = new Dictionary<string, long>();
private string _logFile = null;
private bool CastOnFocus(string unit, string macro) 
{ 
    if (_queuedAction != null) return false;
    Inferno.Cast("focus_" + unit); 
    _queuedAction = macro; 
    return true; 
}
private bool CastPersonal(string spell) { Inferno.Cast(spell); return true; }
private bool CastOnEnemy(string spell) { Inferno.Cast(spell); return true; }
private bool ProcessQueue()
{
    if (_queuedAction == null) return false;
    string action = _queuedAction; 
    _queuedAction = null;
    Inferno.Cast(action, QuickDelay: true);
    return true;
}
private long NowMs() { return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; }
private bool ThrottleIsOpen(string key, int milliseconds) 
{ 
    if (!_throttleTimestamps.ContainsKey(key)) return true; 
    return (NowMs() - _throttleTimestamps[key]) >= milliseconds; 
}
private void ThrottleRestart(string key) { _throttleTimestamps[key] = NowMs(); }
private void Log(string msg)
{
    if (!GetCheckBox("Enable Logging")) return;
    if (msg == _lastLoggedAction && !msg.StartsWith("Tick:")) return;
    _lastLoggedAction = msg;
    Inferno.PrintMessage(msg, Color.White);
    if (_logFile != null) 
    { 
        try 
        { 
            File.AppendAllText(_logFile,
                DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\n");
        } 
        catch { } 
    }
}

private const int MAP_PROVING_GROUNDS = 480;
private const int MAP_ALGETHAR_ACADEMY_1 = 2511;
private const int MAP_ALGETHAR_ACADEMY_2 = 2515;
private const int MAP_ALGETHAR_ACADEMY_3 = 2516;
private const int MAP_ALGETHAR_ACADEMY_4 = 2517;
private const int MAP_ALGETHAR_ACADEMY_5 = 2518;
private const int MAP_ALGETHAR_ACADEMY_6 = 2519;
private const int MAP_ALGETHAR_ACADEMY_7 = 2520;
private const int MAP_SKYREACH_1 = 601;
private const int MAP_SKYREACH_2 = 602;
private const int MAP_PIT_OF_SARON = 823;
private const int MAP_MAISARA_CAVERNS_1 = 2492;
private const int MAP_MAISARA_CAVERNS_2 = 2493;
private const int MAP_MAISARA_CAVERNS_3 = 2494;
private const int MAP_MAISARA_CAVERNS_4 = 2496;
private const int MAP_MAISARA_CAVERNS_5 = 2497;
private const int MAP_MAISARA_CAVERNS_6 = 2498;
private const int MAP_MAISARA_CAVERNS_7 = 2499;
private const int MAP_WINDRUNNER_SPIRE = 2501;
private const int MAP_MAGISTERS_TERRACE_1 = 2097;
private const int MAP_MAGISTERS_TERRACE_2 = 2098;
private const int MAP_MAGISTERS_TERRACE_3 = 2099;

private const int MANA = 0;
private const int HEALTHSTONE_ID = 5512;
private const int DIAGNOSTIC_LOG_INTERVAL_MS = 2000;
private const string MACRO_TARGET_ENEMY = "target_enemy";
private const string MACRO_USE_HEALTHSTONE = "use_healthstone";
private const string MACRO_FOCUS_PLAYER = "focus_player";
private const string MACRO_FOCUS_PREFIX_PARTY = "focus_party";
private const string MACRO_FOCUS_PREFIX_RAID = "focus_raid";

private void InitializeFocusMacros()
{
    Macros.Add("focus_player", "/focus player");
    for (int i = 1; i <= 4; i++)
    {
        Macros.Add("focus_party" + i, "/focus party" + i);
    }
    for (int i = 1; i <= 28; i++)
    {
        Macros.Add("focus_raid" + i, "/focus raid" + i);
    }
}
private void InitializeUtilityMacros()
{
    Macros.Add(MACRO_TARGET_ENEMY, "/targetenemy");
    Macros.Add(MACRO_USE_HEALTHSTONE, "/use Healthstone");
}
private void InitializeHealthstoneFunction()
{
    string hasHealthstoneCode = "return GetItemCount(" + HEALTHSTONE_ID + ") > 0 and 1 or 0";
    CustomFunctions.Add("HasHealthstone", hasHealthstoneCode);
}
private void InitializeSharedComponents()
{
    InitializeFocusMacros();
    InitializeUtilityMacros();
    InitializeHealthstoneFunction();
}

private bool IsInCombat()
{
    return Inferno.InCombat("player");
}
private bool IsSpellReady(string spellName)
{
    return Inferno.SpellCooldown(spellName) <= 200;
}
private bool CanCast(string spellName, string target = "target")
{
    return Inferno.CanCast(spellName, target);
}
private bool CanCastSpell(string spellName)
{
    return Inferno.CanCast(spellName);
}
private int SpellCharges(string spellName)
{
    return Inferno.SpellCharges(spellName);
}
private bool HasBuff(string buffName, string unit = "player")
{
    return Inferno.HasBuff(buffName, unit, true);
}
private bool IsMoving()
{
    return Inferno.IsMoving("player");
}
private bool IsItemReady(int itemId)
{
    return Inferno.ItemCooldown(itemId) == 0;
}
private bool IsSettingOn(string settingName)
{
    return GetCheckBox(settingName);
}
private bool HasHealthstone()
{
    return Inferno.CustomFunction("HasHealthstone") == 1;
}
private bool TargetIsEnemy()
{
    return Inferno.UnitCanAttack("player", "target");
}
private bool UnitUnder(string unit, int percent)
{
    return HealthPct(unit) < percent;
}
private bool EnemiesInMelee(int count)
{
    return Inferno.EnemiesNearUnit(8, "player") >= count;
}
private bool PowerAtLeast(int amount, int powerType)
{
    return Inferno.Power("player", powerType) >= amount;
}
private bool PowerLessThan(int amount, int powerType)
{
    return Inferno.Power("player", powerType) < amount;
}
private bool GroupMembersUnder(int percent, int minCount)
{
    return GetGroupMembers().Count(unit => !Inferno.IsDead(unit) && HealthPct(unit) < percent) >= minCount;
}
private bool AnyAllyHasDebuff(string debuff)
{
    return GetGroupMembers().Any(unit => !Inferno.IsDead(unit) && Inferno.HasDebuff(debuff, unit, false));
}
private bool AnyAllyHasDebuff(string debuff, int stacks)
{
    return GetGroupMembers().Any(unit => !Inferno.IsDead(unit) && Inferno.HasDebuff(debuff, unit, false) && Inferno.DebuffStacks(debuff, unit, false) >= stacks);
}
private bool CanCastWhileMoving(string spell)
{
    if (!IsMoving())
        return true;
    if (spell == "Flash of Light" && HasBuff("Infusion of Light"))
        return true;
    if (spell == "Holy Light" && HasBuff("Hand of Divinity"))
        return true;
    return false;
}
private bool TargetIsCasting()
{
    return Inferno.CastingID("target") != 0 && Inferno.IsInterruptable("target");
}
private int CastingElapsed()
{
    return Inferno.CastingElapsed("target");
}
private int CastingRemaining()
{
    return Inferno.CastingRemaining("target");
}
private int TargetCastingID()
{
    return Inferno.CastingID("target");
}
private bool IsChanneling()
{
    return Inferno.IsChanneling("player");
}
private string PlayerCastingName()
{
    return Inferno.CastingName("player");
}
private bool CanUseTrinket(int slot)
{
    return Inferno.CanUseEquippedItem(slot);
}
private bool IsCustomCommandOn(string command)
{
    return Inferno.IsCustomCodeOn(command);
}
private bool IsTalentKnown(string talentName)
{
    return Inferno.IsSpellKnown(talentName);
}
private int EnemiesNearPlayer()
{
    return Inferno.EnemiesNearUnit(8f, "player");
}
private bool UnitCastingAtPercent(string unit, int minPct, int maxPct = 100)
{
    if (Inferno.CastingID(unit) == 0 || !Inferno.IsInterruptable(unit))
        return false;
    int elapsed = Inferno.CastingElapsed(unit);
    int remaining = Inferno.CastingRemaining(unit);
    int total = elapsed + remaining;
    if (total <= 0) 
        return false;
    int castPct = (elapsed * 100) / total;
    return castPct >= minPct && castPct <= maxPct;
}

private List<string> GetGroupMembers()
{
    List<string> result = new List<string>();
    if (Inferno.InRaid()) 
    { 
        int size = Inferno.GroupSize(); 
        for (int i = 1; i <= size; i++) 
        { 
            string token = "raid" + i; 
            if (Inferno.UnitName(token) != "") result.Add(token); 
        } 
    }
    else if (Inferno.InParty()) 
    { 
        result.Add("player"); 
        int size = Inferno.GroupSize(); 
        for (int i = 1; i < size; i++) 
        { 
            string token = "party" + i; 
            if (Inferno.UnitName(token) != "") result.Add(token); 
        } 
    }
    else { result.Add("player"); }
    return result;
}
private string LowestAllyUnder(int percent, string spell)
{
    return GetGroupMembers()
        .Where(unit => !Inferno.IsDead(unit) && HealthPct(unit) < percent && Inferno.CanCast(spell, unit))
        .OrderBy(unit => HealthPct(unit))
        .FirstOrDefault();
}
private string LowestAllyInRange(string spell)
{
    return GetGroupMembers()
        .Where(unit => !Inferno.IsDead(unit) && Inferno.CanCast(spell, unit))
        .OrderBy(unit => HealthPct(unit))
        .FirstOrDefault();
}
private string GetAllyWithDebuff(string debuff, string spell)
{
    return GetGroupMembers()
        .Where(unit => !Inferno.IsDead(unit) && Inferno.HasDebuff(debuff, unit, false) && Inferno.SpellInRange(spell, unit))
        .FirstOrDefault();
}
private string GetAllyWithMostStacks(string debuff, string spell)
{
    return GetGroupMembers()
        .Where(unit => !Inferno.IsDead(unit) && Inferno.HasDebuff(debuff, unit, false) && Inferno.SpellInRange(spell, unit))
        .OrderByDescending(unit => Inferno.DebuffStacks(debuff, unit, false))
        .FirstOrDefault();
}

private int HealthPct(string unit) 
{ 
    int maxHealth = Inferno.MaxHealth(unit); 
    if (maxHealth < 1) maxHealth = 1; 
    return (Inferno.Health(unit) * 100) / maxHealth; 
}
private int PowerCurrent(int powerType)
{
    return Inferno.Power("player", powerType);
}
private int PowerMax(int powerType)
{
    return Inferno.MaxPower("player", powerType);
}
private int TargetHealthPct()
{
    int maxHealth = Inferno.MaxHealth("target");
    if (maxHealth < 1) maxHealth = 1;
    return (Inferno.Health("target") * 100) / maxHealth;
}
private int FullRechargeTime(string spellName, int baseRecharge)
{
    return Inferno.FullRechargeTime(spellName, baseRecharge);
}
private int GCD()
{
    return Inferno.GCD();
}
private int GCDMAX()
{
    int gcd = (int)(1500f / (1f + Inferno.Haste("player") / 100f));
    return gcd < 750 ? 750 : gcd;
}
private int BuffRemaining(string buffName, string unit = "player")
{
    return Inferno.BuffRemaining(buffName, unit, true);
}
private int DebuffRemaining(string debuffName, string unit = "target")
{
    return Inferno.DebuffRemaining(debuffName, unit, true);
}
private int CombatTime()
{
    return Inferno.CombatTime();
}
private int SpellCooldown(string spellName)
{
    return Inferno.SpellCooldown(spellName);
}

private Dictionary<string, int> _interruptCastID = new Dictionary<string, int>();
private Dictionary<string, int> _interruptTargetPct = new Dictionary<string, int>();
private bool HandleInterrupt()
{
    return HandleInterruptOnUnit("target", INTERRUPT_SPELL);
}
private bool HandleInterruptOnUnit(string unit, string spell)
{
    try 
    {
        if (!IsSettingOn("Auto Interrupt")) return false;
    }
    catch 
    {
        return false;
    }
    int castingID = Inferno.CastingID(unit);
    if (!Inferno.IsInterruptable(unit) || castingID == 0)
    {
        if (_interruptCastID.ContainsKey(unit))
        {
            _interruptCastID.Remove(unit);
            _interruptTargetPct.Remove(unit);
        }
        return false;
    }
    if (!_interruptCastID.ContainsKey(unit) || _interruptCastID[unit] != castingID)
    {
        int minPct = GetSlider("Interrupt at cast % (min)");
        int maxPct = GetSlider("Interrupt at cast % (max)");
        if (maxPct < minPct) maxPct = minPct;
        int targetPct = _rng.Next(minPct, maxPct + 1);
        _interruptCastID[unit] = castingID;
        _interruptTargetPct[unit] = targetPct;
    }
    int interruptPct = _interruptTargetPct[unit];
    if (UnitCastingAtPercent(unit, interruptPct) && Inferno.CanCast(spell, IgnoreGCD: true))
    {
        int elapsed = Inferno.CastingElapsed(unit);
        int remaining = Inferno.CastingRemaining(unit);
        int total = elapsed + remaining;
        int castPct = total > 0 ? (elapsed * 100) / total : 0;
        Log("Interrupting " + unit + " at " + castPct + "% (target: " + interruptPct + "%)");
        Inferno.Cast(spell, QuickDelay: true);
        _interruptCastID.Remove(unit);
        _interruptTargetPct.Remove(unit);
        return true;
    }
    return false;
}
private bool ShouldInterruptCast(string unit, int minPct, int maxPct = 100)
{
    return UnitCastingAtPercent(unit, minPct, maxPct);
}

private bool HandleRacials()
{
    if (IsCustomCommandOn("NoCDs")) return false;
    if (Inferno.CanCast("Berserking", IgnoreGCD: true))
    {
        Log("Casting Berserking (racial)");
        Inferno.Cast("Berserking", QuickDelay: true);
        return true;
    }
    if (Inferno.CanCast("Blood Fury", IgnoreGCD: true))
    {
        Log("Casting Blood Fury (racial)");
        Inferno.Cast("Blood Fury", QuickDelay: true);
        return true;
    }
    if (Inferno.CanCast("Ancestral Call", IgnoreGCD: true))
    {
        Log("Casting Ancestral Call (racial)");
        Inferno.Cast("Ancestral Call", QuickDelay: true);
        return true;
    }
    if (Inferno.CanCast("Fireblood", IgnoreGCD: true))
    {
        Log("Casting Fireblood (racial)");
        Inferno.Cast("Fireblood", QuickDelay: true);
        return true;
    }
    if (CanCast("Lights Judgment", "target"))
    {
        Log("Casting Lights Judgment (racial)");
        return CastOnEnemy("Lights Judgment");
    }
    return false;
}

private void LogMapChange(int currentMapId)
{
    if (_lastMapId != 0 && _lastMapId != currentMapId)
    {
        Log("Map changed: " + _lastMapId + " -> " + currentMapId);
    }
    _lastMapId = currentMapId;
}
private void LogBossInformation()
{
    for (int i = 1; i <= 4; i++)
    {
        string boss = "boss" + i;
        int bossHealth = Inferno.Health(boss);
        if (bossHealth > 0)
        {
            string bossName = "Unknown";
            try
            {
                bossName = Inferno.UnitName(boss);
            }
            catch { }
            int castingId = Inferno.CastingID(boss);
            string logMsg = "Boss" + i + ": " + bossName + " Health: " + bossHealth + "%";
            if (castingId != 0)
            {
                string castName = "Unknown";
                try
                {
                    castName = Inferno.CastingName(boss);
                }
                catch { }
                bool interruptable = Inferno.IsInterruptable(boss);
                bool channeling = Inferno.IsChanneling(boss);
                int elapsed = Inferno.CastingElapsed(boss);
                int remaining = Inferno.CastingRemaining(boss);
                logMsg += " | CASTING: " + castName + " (ID:" + castingId + ")";
                logMsg += " Interruptable:" + interruptable;
                logMsg += " Channeling:" + channeling;
                logMsg += " Elapsed:" + elapsed + "ms";
                logMsg += " Remaining:" + remaining + "ms";
            }
            Log(logMsg);
        }
    }
}

private const int INSANITY = 13;
private const string INTERRUPT_SPELL = "Silence";
private Random _rng = new Random();
private int _lastMapId = 0;
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
    Spellbook.Add("Desperate Prayer");
    Spellbook.Add("Dispersion");
    Spellbook.Add("Power Word: Shield");
    Spellbook.Add("Vampiric Embrace");
    Spellbook.Add("Power Word: Fortitude");
    Spellbook.Add("Shadowform");
    Spellbook.Add("Silence");
    Spellbook.Add("Ancestral Call");
    Spellbook.Add("Berserking");
    Spellbook.Add("Blood Fury");
    Spellbook.Add("Fireblood");
    Spellbook.Add("Lights Judgment");
    Macros.Add("trinket1", "/use 13");
    Macros.Add("trinket2", "/use 14");
    CustomFunctions.Add("PetIsActive", "return (UnitExists('pet') and not UnitIsDead('pet')) and 1 or 0");
    InitializeSharedComponents();
    CustomCommands.Add("NoCDs");
    CustomCommands.Add("ForceST");
    Inferno.Latency = 250;
    _logFile = "penelos_priest_shadow_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    Inferno.PrintMessage("Penelos Gambits - Shadow Priest loaded!", Color.DarkViolet);
    Log("Initialize complete");
}

private bool CastOffensive(string spellName)
{
    if (CanCast(spellName, "target"))
    {
        Log("Casting " + spellName);
        return CastOnEnemy(spellName);
    }
    return false;
}
private bool CastCooldown(string spellName)
{
    if (IsCustomCommandOn("NoCDs")) return false;
    if (CanCast(spellName, "target"))
    {
        Log("Casting " + spellName + " (cooldown)");
        return CastOnEnemy(spellName);
    }
    return false;
}

private int GetInsanity()
{
    int insanity = PowerCurrent(INSANITY);
    string casting = PlayerCastingName();
    if (casting == "Vampiric Touch") insanity += 4;
    if (casting == "Mind Blast") insanity += 6;
    if (casting == "Void Blast") insanity += 6;
    return insanity;
}
private int GetInsanityDeficit()
{
    return PowerMax(INSANITY) - GetInsanity();
}
private bool HasVTOnTarget()
{
    if (PlayerCastingName() == "Vampiric Touch") return true;
    return DebuffRemaining("Vampiric Touch") > GCD();
}
private bool IsVTRefreshable()
{
    if (PlayerCastingName() == "Vampiric Touch") return false;
    return DebuffRemaining("Vampiric Touch") < 6300;
}
private bool IsSWPRefreshable()
{
    return DebuffRemaining("Shadow Word: Pain") < 4800;
}
private bool HasDotsUp()
{
    return HasVTOnTarget() && DebuffRemaining("Shadow Word: Pain") > GCD();
}
private bool IsPetActive()
{
    return Inferno.CustomFunction("PetIsActive") == 1;
}

private bool HandleDefensives()
{
    if (!IsSettingOn("Use Defensives")) return false;
    int playerHp = HealthPct("player");
    if (playerHp <= GetSlider("Dispersion HP %") && CanCastSpell("Dispersion"))
    {
        Log("Casting Dispersion (player " + playerHp + "%)");
        return CastPersonal("Dispersion");
    }
    if (playerHp <= GetSlider("Vampiric Embrace HP %") && Inferno.CanCast("Vampiric Embrace", IgnoreGCD: true))
    {
        Log("Casting Vampiric Embrace (player " + playerHp + "%)");
        Inferno.Cast("Vampiric Embrace", QuickDelay: true);
        return true;
    }
    if (playerHp <= GetSlider("Desperate Prayer HP %") && Inferno.CanCast("Desperate Prayer", IgnoreGCD: true))
    {
        Log("Casting Desperate Prayer (player " + playerHp + "%)");
        Inferno.Cast("Desperate Prayer", QuickDelay: true);
        return true;
    }
    if (playerHp <= GetSlider("Healthstone HP %") && HasHealthstone() && IsItemReady(HEALTHSTONE_ID))
    {
        Log("Using Healthstone (player " + playerHp + "%)");
        Inferno.Cast(MACRO_USE_HEALTHSTONE, QuickDelay: true);
        return true;
    }
    if (playerHp <= GetSlider("Power Word: Shield HP %") && BuffRemaining("Power Word: Shield") < GCD() && CanCastSpell("Power Word: Shield"))
    {
        Log("Casting Power Word: Shield (player " + playerHp + "%)");
        return CastPersonal("Power Word: Shield");
    }
    return false;
}

private bool HandleShadowform()
{
    if (!IsSettingOn("Auto Shadowform")) return false;
    if (!Inferno.HasBuff("Shadowform") && CanCastSpell("Shadowform"))
    {
        Log("Casting Shadowform");
        return CastPersonal("Shadowform");
    }
    return false;
}
private bool HandlePowerWordFortitude()
{
    if (!IsSettingOn("Auto Power Word: Fortitude")) return false;
    if (BuffRemaining("Power Word: Fortitude") < GCD() && CanCastSpell("Power Word: Fortitude"))
    {
        Log("Casting Power Word: Fortitude");
        return CastPersonal("Power Word: Fortitude");
    }
    return false;
}

public override bool OutOfCombatTick()
{
    if (HandlePowerWordFortitude()) return true;
    if (TargetIsEnemy() && HandleShadowform()) return true;
    return false;
}

public override bool CombatTick()
{
    if (Inferno.IsDead("player") || Inferno.IsGhost("player")) return false;
    int mapId = Inferno.GetMapID();
    LogMapChange(mapId);
    if (ThrottleIsOpen("boss_log", DIAGNOSTIC_LOG_INTERVAL_MS))
    {
        ThrottleRestart("boss_log");
        LogBossInformation();
    }
    if (HandleDefensives()) return true;
    if (HandleInterrupt()) return true;
    if (IsChanneling())
    {
        string castName = PlayerCastingName();
        if (castName == "Void Torrent" || castName == "Mind Flay: Insanity")
            return false;
    }
    string currentCast = PlayerCastingName();
    if (currentCast.Contains("Puzzle Box") || currentCast.Contains("Emberwing")) 
        return false;
    if (HandleShadowform()) return true;
    if (HandlePowerWordFortitude()) return true;
    if (!TargetIsEnemy()) return false;
    if (HandleTrinkets()) return true;
    if ((HasBuff("Voidform") || HasBuff("Dark Ascension")) && HandleRacials()) 
        return true;
    int enemies = Inferno.EnemiesNearUnit(10f, "target");
    if (enemies < 1) enemies = 1;
    if (IsCustomCommandOn("ForceST")) enemies = 1;
    if (HandleCooldowns()) return true;
    if (enemies >= GetSlider("AoE enemy count threshold"))
        return RunAoERotation(enemies);
    return RunMainRotation(enemies);
}
public override void OnStop() 
{ 
    Log("Rotation stopped"); 
}

private bool HandleCooldowns()
{
    if (IsCustomCommandOn("NoCDs")) return false;
    bool dotsUp = HasDotsUp();
    bool voidformActive = HasBuff("Voidform");
    bool hasVoidformTalent = IsTalentKnown("Voidform");
    bool powerInfusionActive = BuffRemaining("Power Infusion") > GCD();
    if (IsSettingOn("Use Halo"))
    {
        if (CastCooldown("Halo")) return true;
    }
    if (IsSettingOn("Use Voidform") && !voidformActive && dotsUp)
    {
        if (CastCooldown("Voidform")) return true;
    }
    if (IsSettingOn("Use Power Infusion") && (voidformActive || !hasVoidformTalent) && !powerInfusionActive)
    {
        if (Inferno.CanCast("Power Infusion", IgnoreGCD: true))
        {
            Log("Casting Power Infusion (cooldown)");
            Inferno.Cast("Power Infusion", QuickDelay: true);
            return true;
        }
    }
    return false;
}
private bool HandleTrinkets()
{
    if (!IsSettingOn("Use Trinkets") || IsCustomCommandOn("NoCDs")) 
        return false;
    bool voidformActive = HasBuff("Voidform");
    bool powerInfusionLong = BuffRemaining("Power Infusion") >= 10000;
    bool entropicRift = HasBuff("Entropic Rift");
    if (!voidformActive && !powerInfusionLong && !entropicRift) 
        return false;
    if (CanUseTrinket(13))
    {
        Log("Using Trinket 1");
        Inferno.Cast("trinket1");
        return true;
    }
    if (CanUseTrinket(14))
    {
        Log("Using Trinket 2");
        Inferno.Cast("trinket2");
        return true;
    }
    return false;
}

private bool RunMainRotation(int enemies)
{
    int insanity = GetInsanity();
    int insanityDeficit = GetInsanityDeficit();
    int gcdMax = GCDMAX();
    int targetHp = TargetHealthPct();
    bool mindDevourer = HasBuff("Mind Devourer");
    bool entropicRift = HasBuff("Entropic Rift");
    bool hasMindDevourerTalent = IsTalentKnown("Mind Devourer");
    bool hasDevourMatter = IsTalentKnown("Devour Matter");
    bool hasInvokedNightmare = IsTalentKnown("Invoked Nightmare");
    bool hasVoidApparitions = IsTalentKnown("Void Apparitions");
    bool hasMaddeningTentacles = IsTalentKnown("Maddening Tentacles");
    bool hasInescapableTorment = IsTalentKnown("Inescapable Torment");
    int swmRemaining = DebuffRemaining("Shadow Word: Madness");
    int swmCost = 40;
    if (hasDevourMatter && targetHp <= 20)
    {
        if (CastOffensive("Shadow Word: Death")) return true;
    }
    if (swmRemaining <= gcdMax || insanityDeficit <= 35 || mindDevourer || (entropicRift && swmCost > 0))
    {
        if (CastOffensive("Shadow Word: Madness")) return true;
    }
    if (CastOffensive("Void Volley")) return true;
    if (CastOffensive("Void Blast")) return true;
    if (IsVTRefreshable() || FullRechargeTime("Tentacle Slam", 20000) <= GCDMAX() * 2)
    {
        if (CastOffensive("Tentacle Slam")) return true;
    }
    if (HasDotsUp())
    {
        if (CastOffensive("Void Torrent")) return true;
    }
    if (hasInvokedNightmare && IsSWPRefreshable() && HasVTOnTarget())
    {
        if (CastOffensive("Shadow Word: Pain")) return true;
    }
    if (!mindDevourer || !hasMindDevourerTalent)
    {
        if (CastOffensive("Mind Blast")) return true;
    }
    if (HasBuff("Mind Flay: Insanity"))
    {
        if (CastOffensive("Mind Flay: Insanity")) return true;
    }
    if (hasVoidApparitions || hasMaddeningTentacles)
    {
        bool madOk = !hasMaddeningTentacles || (insanity + 6) >= swmCost || DebuffRemaining("Shadow Word: Madness") == 0;
        if (madOk)
        {
            if (CastOffensive("Tentacle Slam")) return true;
        }
    }
    if (IsVTRefreshable())
    {
        if (CastOffensive("Vampiric Touch")) return true;
    }
    int executeThreshold = 20 + (IsTalentKnown("Deathspeaker") ? 15 : 0);
    bool petUp = IsPetActive();
    if ((petUp && hasInescapableTorment) || (targetHp < executeThreshold && IsTalentKnown("Shadowfiend") && IsTalentKnown("Idol of Y'Shaarj")))
    {
        if (CastOffensive("Shadow Word: Death")) return true;
    }
    if (PlayerCastingName() != "Mind Flay")
    {
        if (CastOffensive("Mind Flay")) return true;
    }
    if (CastOffensive("Tentacle Slam")) return true;
    if (targetHp < 20 && CastOffensive("Shadow Word: Death")) return true;
    if (CastOffensive("Shadow Word: Death")) return true;
    if (CastOffensive("Shadow Word: Pain")) return true;
    return false;
}

private bool RunAoERotation(int enemies)
{
    int insanity = GetInsanity();
    int insanityDeficit = GetInsanityDeficit();
    int gcdMax = GCDMAX();
    int targetHp = TargetHealthPct();
    bool mindDevourer = HasBuff("Mind Devourer");
    bool entropicRift = HasBuff("Entropic Rift");
    bool hasMindDevourerTalent = IsTalentKnown("Mind Devourer");
    bool hasDevourMatter = IsTalentKnown("Devour Matter");
    bool hasInvokedNightmare = IsTalentKnown("Invoked Nightmare");
    bool hasVoidApparitions = IsTalentKnown("Void Apparitions");
    bool hasMaddeningTentacles = IsTalentKnown("Maddening Tentacles");
    bool hasInescapableTorment = IsTalentKnown("Inescapable Torment");
    int swmRemaining = DebuffRemaining("Shadow Word: Madness");
    int swmCost = 40;
    if (hasDevourMatter && targetHp <= 20)
    {
        if (CastOffensive("Shadow Word: Death")) return true;
    }
    if (swmRemaining <= gcdMax || insanityDeficit <= 35 || mindDevourer || (entropicRift && swmCost > 0))
    {
        if (CastOffensive("Shadow Word: Madness")) return true;
    }
    if (CastOffensive("Void Volley")) return true;
    if (CastOffensive("Void Blast")) return true;
    if (hasVoidApparitions || hasMaddeningTentacles || IsVTRefreshable())
    {
        bool madOk = !hasMaddeningTentacles || (insanity + 6) >= swmCost || DebuffRemaining("Shadow Word: Madness") == 0;
        if (madOk)
        {
            if (CastOffensive("Tentacle Slam")) return true;
        }
    }
    if (HasDotsUp())
    {
        if (CastOffensive("Void Torrent")) return true;
    }
    if (hasInvokedNightmare && IsSWPRefreshable() && HasVTOnTarget())
    {
        if (CastOffensive("Shadow Word: Pain")) return true;
    }
    if (!mindDevourer || !hasMindDevourerTalent)
    {
        if (CastOffensive("Mind Blast")) return true;
    }
    if (HasBuff("Mind Flay: Insanity"))
    {
        if (CastOffensive("Mind Flay: Insanity")) return true;
    }
    if (IsVTRefreshable())
    {
        if (CastOffensive("Vampiric Touch")) return true;
    }
    bool petUp = IsPetActive();
    if ((petUp && hasInescapableTorment) || targetHp < 20)
    {
        if (CastOffensive("Shadow Word: Death")) return true;
    }
    if (PlayerCastingName() != "Mind Flay")
    {
        if (CastOffensive("Mind Flay")) return true;
    }
    if (CastOffensive("Tentacle Slam")) return true;
    if (CastOffensive("Shadow Word: Death")) return true;
    if (CastOffensive("Shadow Word: Pain")) return true;
    return false;
}
}

}
