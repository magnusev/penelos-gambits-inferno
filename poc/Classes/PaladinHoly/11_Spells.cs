// ========================================
// PALADIN HOLY - SPELL-SPECIFIC LOGIC
// ========================================

// Holy Shock charge system (API is bugged, returns 0 for cd/charges)
// We manually track charges and recharge timing
private int HsChargesAvailable()
{
    if (_hsCharges < HS_MAX_CHARGES)
    {
        long elapsed = NowMs() - _hsLastRechargeMs;
        int recharged = (int)(elapsed / HS_RECHARGE_MS);
        if (recharged > 0) 
        { 
            _hsCharges = _hsCharges + recharged; 
            if (_hsCharges > HS_MAX_CHARGES) _hsCharges = HS_MAX_CHARGES; 
            _hsLastRechargeMs = _hsLastRechargeMs + recharged * HS_RECHARGE_MS; 
        }
    }
    return _hsCharges;
}

private void UseHsCharge() 
{ 
    HsChargesAvailable(); 
    _hsCharges = _hsCharges - 1; 
    if (_hsCharges < 0) _hsCharges = 0; 
    if (_hsCharges == HS_MAX_CHARGES - 1) _hsLastRechargeMs = NowMs(); 
}

