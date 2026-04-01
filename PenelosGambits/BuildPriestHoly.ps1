. ".\Scripts\CommonFiles.ps1"
. ".\Scripts\LinesToExclude.ps1"
. ".\Scripts\AddToStart.ps1"

$outputFile = "C:\libs\Live\Rotations\Retail\PenelosGambitsPriestHoly\rotation.cs"
$buildName = "PaladinHolyPvE"

$filesToInclude = $commonFiles + @(
    "Priest/Holy/PriestHolyActionBook.cs"
    "Priest/Holy/PriestHolyGambitPicker.cs"
    "Priest/Holy/PriestHolyPvE.cs"
)

. ".\Scripts\BuildFile.ps1" -outputFile $outputFile -buildName $buildName -filesToInclude $filesToInclude -linesToAddAtStart $linesToAddAtStart -linesToExclude $linesToExclude
