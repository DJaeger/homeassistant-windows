# Home Assistant Windows Companion - Local Build Script
# This script builds, publishes, and packages the app into a ZIP for redistribution.
# Matches Home Assistant's versioning standards (Stable, Beta, Dev).

param (
    [Parameter(Mandatory=$false)]
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"

$ProjectDir = "$PSScriptRoot\src\HAWindowsCompanion.App"
$PublishDir = "$PSScriptRoot\publish_local"
$SolutionFile = "$PSScriptRoot\HAWindowsCompanion.sln"

# 1. Determine Version
if ([string]::IsNullOrEmpty($Version)) {
    # Default to dev release with current date
    $DateSuffix = Get-Date -Format "yyyyMMdd"
    $Version = "2026.3.0.dev$DateSuffix"
}

$ZipName = "HAWindowsCompanion_v$Version.zip"
$ZipPath = "$PSScriptRoot\$ZipName"

function Check-LastCommand {
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n[ERROR] Command failed with exit code $LASTEXITCODE. Build aborted." -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

function Check-Prerequisites {
    Write-Host "[0/4] Checking environment prerequisites..." -ForegroundColor Gray
    
    # 1. Check for Windows SDK (required for XamlCompiler)
    $WinSdkPath = "C:\Program Files (x86)\Windows Kits\10"
    if (-not (Test-Path $WinSdkPath)) {
        Write-Host "`n[ERROR] Windows 10/11 SDK is missing!" -ForegroundColor Red
        Write-Host "The WinUI 3 XamlCompiler requires the Windows SDK to satisfy system types and WinRT metadata." -ForegroundColor Yellow
        Write-Host "`n🚀 QUICK INSTALL via winget:" -ForegroundColor Cyan
        Write-Host "  winget source update" -ForegroundColor Gray
        Write-Host "  winget install Microsoft.WindowsSDK.10.0.19041" -ForegroundColor White
        Write-Host "  winget install Microsoft.VisualStudio.Community --override ""--add Microsoft.VisualStudio.Workload.NetDesktop --add Microsoft.VisualStudio.Workload.Azure --add Microsoft.VisualStudio.Component.Windows10SDK.19041 --add Microsoft.VisualStudio.Component.VC.UniversalWindowsPlatform --passive --norestart""" -ForegroundColor White
        Write-Host "`nOr manually via Visual Studio Installer (2026):" -ForegroundColor Gray
        Write-Host "  1. Workload: 'Windows App Development'" -ForegroundColor Gray
        Write-Host "  2. Workload: '.NET Desktop Development'" -ForegroundColor Gray
        Write-Host "  3. Component: 'C++ (v143) Universal Windows Platform tools'" -ForegroundColor Gray
        exit 1
    }

    # 2. Check for .NET 10 SDK
    $DotnetSdks = dotnet --list-sdks
    if (-not ($DotnetSdks -match "10\.0")) {
        Write-Host "`n[WARNING] .NET 10 SDK not detected in 'dotnet --list-sdks'." -ForegroundColor Yellow
        Write-Host "While you may have other versions, .NET 10 is the project standard." -ForegroundColor Gray
    }

    Write-Host "Environment: OK" -ForegroundColor Green
}

Write-Host "`n--- Starting Build Process for Home Assistant Windows Companion ---" -ForegroundColor Cyan
Write-Host "Target Version: $Version" -ForegroundColor Yellow
Write-Host "Developer: FaserF" -ForegroundColor Green

Check-Prerequisites

# 2. Clean
Write-Host "`n[1/4] Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }
dotnet clean $SolutionFile -c Release -p:Platform=x64
Check-LastCommand

# 3. Restore
Write-Host "`n[2/4] Restoring dependencies..." -ForegroundColor Yellow
dotnet restore $SolutionFile -p:Platform=x64
Check-LastCommand

# 4. Publish
Write-Host "`n[3/4] Publishing single-file executable..." -ForegroundColor Yellow

# Parse numeric part for AssemblyVersion (2026.3.0.0)
$NumericVersion = $Version -replace '\.dev.*$', '' -replace 'b.*$', ''
if ($NumericVersion -match '^\d+\.\d+\.\d+$') { $NumericVersion += ".0" }

Write-Host "Assembly Version: $NumericVersion" -ForegroundColor Gray
Write-Host "Informational Version: $Version" -ForegroundColor Gray

dotnet publish $ProjectDir `
    -c Release `
    -r win-x64 `
    -p:Platform=x64 `
    --output $PublishDir `
    -p:PublishSingleFile=false `
    -p:SelfContained=true `
    -p:WindowsPackageType=None `
    -p:WindowsAppSDKSelfContained=true `
    -p:AssemblyVersion=$NumericVersion `
    -p:FileVersion=$NumericVersion `
    -p:InformationalVersion=$Version `
    -p:Version=$NumericVersion
Check-LastCommand

# 5. ZIP
Write-Host "`n[4/4] Packaging into $(Split-Path $ZipPath -Leaf)..." -ForegroundColor Yellow
if (Test-Path $ZipPath) { Remove-Item $ZipPath }
Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipPath
Check-LastCommand

Write-Host "`n--- BUILD SUCCESSFUL ---" -ForegroundColor Green
Write-Host "Target: $Version"
Write-Host "Location: $ZipPath"
Write-Host "Published files (uncompressed): $PublishDir" -ForegroundColor Gray
Write-Host "`n"
