. ".\Scripts\CommonFiles.ps1"
. ".\Scripts\LinesToExclude.ps1"
. ".\Scripts\AddToStart.ps1"

$outputFile = "C:\libs\Live\Rotations\Retail\PenelosGambitsPalaHoly\rotation.cs"
$buildName = "PaladinHolyPvE"

$filesToInclude = $commonFiles + @(
    "Paladin/Actions/Holy/CleanseAction.cs"
    "Paladin/Actions/Holy/DivineTollAction.cs"
    "Paladin/Actions/Holy/FlashOfLightAction.cs"
    "Paladin/Actions/Holy/HolyShockDefensiveAction.cs"
    "Paladin/Actions/Holy/JudgmentAction.cs"
    "Paladin/Actions/Holy/WordOfGloryAction.cs"
    "Paladin/Holy/Gambits/HolyPaladinDamageGambitSet.cs"
    "Paladin/Holy/Gambits/HolyPaladinDefaultGambitSet.cs"
    "Paladin/Holy/PaladinHolyActionBook.cs"
    "Paladin/Holy/PaladinHolyGambitPicker.cs"
    "Paladin/Holy/PaladinHolyPvE.cs"
)

. ".\Scripts\BuildFile.ps1" -outputFile $outputFile -buildName $buildName -filesToInclude $filesToInclude -linesToAddAtStart $linesToAddAtStart -linesToExclude $linesToExclude
