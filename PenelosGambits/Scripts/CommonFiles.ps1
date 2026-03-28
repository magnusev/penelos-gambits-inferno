# CommonFiles.ps1
# These files are included in every build
# DO NOT include inferno/* files - they are only for development/IntelliSense

$commonFiles = @(
    "Common/Action/Action.cs",
    "Common/Action/FriendlyTargetedAction.cs",
    "Common/group/Group.cs",
    "Common/group/PartyGroup.cs",
    "Common/group/RaidGroup.cs",
    "Common/group/Solo.cs",
    "Common/unit/PartyUnit.cs",
    "Common/unit/RaidUnit.cs",
    "Common/unit/PlayerUnit.cs",
    "Common/unit/Unit.cs",
    "Common/utilities/ActionQueuer.cs",
    "Common/utilities/JsonParser.cs",
    "Common/utilities/TargetingMacros.cs",
    "Common/utilities/Throttler.cs",
    "Common/ActionBook.cs",
    "Common/Boss.cs",
    "Common/Environment.cs",
    "Common/PeneloRotation.cs",
    "Common/Target.cs",
    "Common/SpellMacroRegistry.cs"
)
