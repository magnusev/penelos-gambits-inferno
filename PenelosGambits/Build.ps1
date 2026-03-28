. ".\Scripts\CommonFiles.ps1"
. ".\Scripts\LinesToExclude.ps1"
. ".\Scripts\AddToStart.ps1"

$outputFile = "C:\libs\Live\Rotations\Retail\PenelosGambitsPalaHoly\rotation.cs"
$buildName = "PaladinHolyPvE"

$filesToInclude = $commonFiles + @(
    "Paladin/Holy/PaladinHolyPvE.cs"
)

. ".\Scripts\BuildFile.ps1" -outputFile $outputFile -buildName $buildName -filesToInclude $filesToInclude -linesToAddAtStart $linesToAddAtStart -linesToExclude $linesToExclude
