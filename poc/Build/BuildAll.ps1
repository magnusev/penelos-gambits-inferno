# BuildAll.ps1 - Build all configured rotations

param([switch]$LocalOnly)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "╔════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   Penelos Gambits - Build All Rotations   ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Define all classes to build
$classes = @(
    @{ Class = "PaladinHoly"; ClassName = "HolyPaladinPvE" }
    # Add more classes here as you create them:
    # @{ Class = "PriestHoly"; ClassName = "HolyPriestPvE" }
    # @{ Class = "DruidRestoration"; ClassName = "RestorationDruidPvE" }
)

$buildScript = Join-Path (Split-Path -Parent $PSCommandPath) "BuildRotation.ps1"

$successCount = 0
$failCount = 0

foreach ($cfg in $classes) {
    try {
        if ($LocalOnly) {
            & $buildScript -Class $cfg.Class -ClassName $cfg.ClassName -LocalOnly
        } else {
            & $buildScript -Class $cfg.Class -ClassName $cfg.ClassName
        }
        $successCount++
    } catch {
        Write-Host "❌ Failed to build $($cfg.Class): $_" -ForegroundColor Red
        $failCount++
    }
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Summary: $successCount succeeded, $failCount failed" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

