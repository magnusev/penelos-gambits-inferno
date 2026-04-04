param(
    [string]$Class = "PaladinHoly",
    [string]$ClassName = "HolyPaladinPvE",
    [string]$OutputDir = "C:\libs\Live\Rotations\Retail",
    [switch]$LocalOnly
)

$ErrorActionPreference = "Stop"

# Function to strip comments (helps avoid quote checker bugs)
function Strip-Comments {
    param([string]$code)
    
    $lines = $code -split "`r?`n"
    $result = @()
    
    foreach ($line in $lines) {
        # Skip lines that are ONLY comments (whitespace + //)
        if ($line -match '^\s*//') {
            continue
        }
        
        # For other lines, try to remove inline comments
        # This is tricky because // might be inside strings
        # Simple heuristic: if // appears and there are no quotes before it, remove it
        $trimmed = $line
        $slashPos = $line.IndexOf('//')
        if ($slashPos -ge 0) {
            $beforeSlash = $line.Substring(0, $slashPos)
            $quoteCount = ($beforeSlash.ToCharArray() | Where-Object { $_ -eq '"' }).Count
            # If even number of quotes before //, it's probably outside a string
            if ($quoteCount % 2 -eq 0) {
                $trimmed = $beforeSlash.TrimEnd()
            }
        }
        
        # Keep non-empty lines
        if ($trimmed.Trim() -ne '') {
            $result += $trimmed
        }
    }
    
    return $result -join "`n"
}

# Paths
$scriptDir = Split-Path -Parent $PSCommandPath
$pocRoot = Split-Path -Parent $scriptDir
$componentsDir = Join-Path $pocRoot "Components"
$classDir = Join-Path $pocRoot "Classes\$Class"
$localOutput = Join-Path $pocRoot "Output"
$rotationFolder = Join-Path $OutputDir "Penelos$Class"
$outputFile = Join-Path $rotationFolder "rotation.cs"
$localFile = Join-Path $localOutput "${Class}_rotation.cs"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building $Class rotation..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Verify directories exist
if (-not (Test-Path $componentsDir)) {
    Write-Host "❌ Components directory not found: $componentsDir" -ForegroundColor Red
    exit 1
}
if (-not (Test-Path $classDir)) {
    Write-Host "❌ Class directory not found: $classDir" -ForegroundColor Red
    exit 1
}

# Header template
$header = @"
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using InfernoWow.API;

namespace InfernoWow.Modules
{

public class $ClassName : Rotation
{
"@

$footer = @"
}

}
"@

# Get all component files (sorted by name - 00_, 01_, etc.)
$componentFiles = Get-ChildItem "$componentsDir\*.cs" | Sort-Object Name

# Add class-specific component folder if it exists (e.g., Components/Paladin for all Paladin specs)
$classFamily = $Class -replace '(Paladin|Priest|Druid|Shaman|Mage|Warlock|Rogue|Warrior|Hunter|DeathKnight|DemonHunter|Monk|Evoker).*', '$1'
$classFamilyComponentDir = Join-Path $componentsDir $classFamily
if (Test-Path $classFamilyComponentDir) {
    $classFamilyFiles = Get-ChildItem "$classFamilyComponentDir\*.cs" | Sort-Object Name
    $componentFiles = @($componentFiles) + @($classFamilyFiles)
}

$classFiles = Get-ChildItem "$classDir\*.cs" | Sort-Object Name

if ($componentFiles.Count -eq 0) {
    Write-Host "⚠️ No component files found in $componentsDir" -ForegroundColor Yellow
}
if ($classFiles.Count -eq 0) {
    Write-Host "⚠️ No class files found in $classDir" -ForegroundColor Yellow
}

Write-Host "📦 Combining files:" -ForegroundColor White
Get-ChildItem "$componentsDir\*.cs" | Sort-Object Name | ForEach-Object { Write-Host "   ✓ Components\$($_.Name)" -ForegroundColor Gray }
if (Test-Path $classFamilyComponentDir) {
    Get-ChildItem "$classFamilyComponentDir\*.cs" | Sort-Object Name | ForEach-Object { Write-Host "   ✓ Components\$classFamily\$($_.Name)" -ForegroundColor Gray }
}
$classFiles | ForEach-Object { Write-Host "   ✓ Classes\$Class\$($_.Name)" -ForegroundColor Gray }

# Build the content
$content = $header

# Add shared components
foreach ($file in $componentFiles) {
    $content += "`n"
    $fileContent = Get-Content $file.FullName -Raw
    $fileContent = Strip-Comments $fileContent
    $content += $fileContent
    $content += "`n"
}

# Add class-specific files
foreach ($file in $classFiles) {
    $content += "`n"
    $fileContent = Get-Content $file.FullName -Raw
    $fileContent = Strip-Comments $fileContent
    $content += $fileContent
    $content += "`n"
}

$content += $footer

# Create output directories
New-Item -ItemType Directory -Force -Path $localOutput | Out-Null

# Write to local output (always)
Set-Content -Path $localFile -Value $content
Write-Host "✅ Local output: $localFile" -ForegroundColor Green

# Run security validation
Write-Host "" 
Write-Host "🔒 Running security validation..." -ForegroundColor Yellow
$validatorProject = Join-Path $pocRoot "Tools\SecurityValidator\SecurityValidator.csproj"
if (Test-Path $validatorProject) {
    $validationResult = & dotnet run --project $validatorProject -- $localFile 2>&1
    $validationOutput = $validationResult -join "`n"
    
    # Check if validation actually failed (look for "Errors: " line with non-zero count)
    if ($validationOutput -match "Errors:\s+(\d+)" -and [int]$matches[1] -gt 0) {
        Write-Host "❌ Security validation FAILED:" -ForegroundColor Red
        Write-Host $validationOutput -ForegroundColor Red
        Write-Host ""
        Write-Host "Build completed but rotation has security issues." -ForegroundColor Yellow
        Write-Host "Fix the issues in component files and rebuild." -ForegroundColor Yellow
        exit 1
    } elseif ($validationOutput -match "PASSED") {
        Write-Host "✅ Security validation PASSED" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Security validation output unclear:" -ForegroundColor Yellow
        Write-Host $validationOutput
    }
} else {
    Write-Host "⚠️ Security validator not found, skipping validation" -ForegroundColor Yellow
}

# Write to bot directory (unless -LocalOnly)
if (-not $LocalOnly) {
    if (Test-Path $OutputDir) {
        New-Item -ItemType Directory -Force -Path $rotationFolder | Out-Null
        Set-Content -Path $outputFile -Value $content
        Write-Host "✅ Bot output: $outputFile" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Bot directory not found: $OutputDir" -ForegroundColor Yellow
        Write-Host "   Use -LocalOnly to skip bot deployment" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build complete! Lines: $((($content -split "`n").Count))" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan


