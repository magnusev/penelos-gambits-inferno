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
    "Common/unit/Unit.cs",
    "Common/utilities/TargetingMacros.cs",
    "Common/utilities/JsonParser.cs",
    "Common/messages/MessageType.cs",
    "Common/messages/MessageBase.cs",
    "Common/messages/StateUpdateMessage.cs",
    "Common/messages/CommandMessage.cs",
    "Common/messages/QueryMessage.cs",
    "Common/messages/QueryResponseMessage.cs",
    "Common/messages/ExecutionResultMessage.cs",
    "Common/messages/ConnectMessage.cs",
    "Common/Boss.cs",
    "Common/Environment.cs",
    "Common/Target.cs"
)
