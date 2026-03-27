. ".\Scripts\CommonFiles.ps1"
. ".\Scripts\LinesToExclude.ps1"
. ".\Scripts\AddToStart.ps1"

$outputFile = "C:\libs\Live\Rotations\Retail\PenelosGambits\rotation.cs"
$buildName = "PaladinHolyPvE"

$filesToInclude = $commonFiles + @(
    "WebSocket/WebSocket.cs"
    "rotation.cs"
)

. ".\Scripts\BuildFile.ps1" -outputFile $outputFile -buildName $buildName -filesToInclude $filesToInclude -linesToAddAtStart $linesToAddAtStart -linesToExclude $linesToExclude
