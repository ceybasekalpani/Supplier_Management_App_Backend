param(
    [string]$Url = "http://localhost:5295"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "..")
$solutionPath = Join-Path $repoRoot "TeaFactorySupplierManagement.sln"
$apiProjectPath = Join-Path $repoRoot "TeaFactorySupplierManagement.API"

Write-Host "Checking for running local API processes..."

$runningApiProcesses = Get-Process -Name "TeaFactorySupplierManagement.API" -ErrorAction SilentlyContinue |
    Where-Object {
        try {
            $_.Path -and $_.Path.StartsWith($repoRoot.Path, [System.StringComparison]::OrdinalIgnoreCase)
        }
        catch {
            $false
        }
    }

foreach ($process in $runningApiProcesses) {
    Write-Host "Stopping TeaFactorySupplierManagement.API process $($process.Id)..."
    Stop-Process -Id $process.Id -Force
}

if ($runningApiProcesses) {
    Start-Sleep -Seconds 2
}

Write-Host "Building solution..."
dotnet build $solutionPath

Write-Host "Starting API on $Url..."
dotnet run --project $apiProjectPath --urls $Url
