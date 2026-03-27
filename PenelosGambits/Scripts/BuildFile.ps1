param (
    [string]$outputFile,
    [string]$buildName,
    [array]$filesToInclude,
    [array]$linesToAddAtStart,
    [array]$linesToExclude
)

# Define the second output file path
$secondOutputFile = ".\rotation-build\$buildName.cs"

# Ensure the directory for the second output file exists
$secondOutputDirectory = Split-Path -Path $secondOutputFile -Parent
if (-not (Test-Path -Path $secondOutputDirectory)) {
    New-Item -Path $secondOutputDirectory -ItemType Directory | Out-Null
}

# Initialize a list to hold the entire content in memory
$outputContent = @()

# Add the specific lines at the beginning of the file once
$outputContent += $linesToAddAtStart
$outputContent += "`n"

# Loop through the specified files and merge their content
foreach ($filePath in $filesToInclude) {
    if (Test-Path $filePath) {
        # Read the entire file content at once
        $content = Get-Content -Path $filePath -Raw

        # Replace specific patterns in the content
#        $content = $content -replace 'CommonNamespace\.', '' # TODO Fix once the build files are not read

        # Split the content into lines and filter out excluded lines in one go
        $filteredContent = $content -split "`n" | Where-Object {
            $exclude = $false
            foreach ($excludeLine in $linesToExclude) {
                if ($_ -like "*$excludeLine*") {
                    $exclude = $true
                    break
                }
            }
            -not $exclude
        }

        # Add filtered content to output
        $outputContent += $filteredContent
        $outputContent += "`n"
    } else {
        Write-Host "File not found: $filePath"
    }
}

# Replace double newlines with a single newline
$finalContent = ($outputContent -join "`n") -replace "(\r?\n){2}", "`n"

# Write the content to both output files
Set-Content -Path $outputFile -Value $finalContent
# Set-Content -Path $secondOutputFile -Value $finalContent

Write-Host "Files merged into $outputFile and $secondOutputFile"
