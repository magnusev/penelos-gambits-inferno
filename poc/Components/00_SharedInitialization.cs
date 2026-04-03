// ========================================
// SHARED INITIALIZATION
// ========================================
// Common macros and functions used across all healing rotations

// Initializes focus macros for party and raid targeting
private void InitializeFocusMacros()
{
    Macros.Add("focus_player", "/focus player");
    
    // Party focus macros (party1-4)
    for (int i = 1; i <= 4; i++)
    {
        Macros.Add("focus_party" + i, "/focus party" + i);
    }
    
    // Raid focus macros (raid1-28, supports up to 28 raid members)
    for (int i = 1; i <= 28; i++)
    {
        Macros.Add("focus_raid" + i, "/focus raid" + i);
    }
}

// Initializes common utility macros
private void InitializeUtilityMacros()
{
    Macros.Add(MACRO_TARGET_ENEMY, "/targetenemy");
    Macros.Add(MACRO_USE_HEALTHSTONE, "/use Healthstone");
}

// Initializes the Healthstone check custom function
private void InitializeHealthstoneFunction()
{
    string hasHealthstoneCode = "return GetItemCount(" + HEALTHSTONE_ID + ") > 0 and 1 or 0";
    CustomFunctions.Add("HasHealthstone", hasHealthstoneCode);
}

// Initializes all shared components (call this in class-specific Initialize)
private void InitializeSharedComponents()
{
    InitializeFocusMacros();
    InitializeUtilityMacros();
    InitializeHealthstoneFunction();
}

