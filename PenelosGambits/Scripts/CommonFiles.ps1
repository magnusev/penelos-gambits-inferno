# CommonFiles.ps1
# These files are included in every build
# DO NOT include inferno/* files - they are only for development/IntelliSense

$commonFiles = @(
    "Common/group/Group.cs",
    "Common/group/PartyGroup.cs",
    "Common/group/RaidGroup.cs",
    "Common/group/Solo.cs",
    "Common/unit/PartyUnit.cs",
    "Common/unit/RaidUnit.cs",
    "Common/unit/PlayerUnit.cs",
    "Common/unit/Unit.cs"
)
