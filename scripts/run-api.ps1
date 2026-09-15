param(
    [string]$Url = "http://localhost:5000"
)

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$env:DOTNET_CLI_HOME = "$root\.dotnet"

Write-Host "Starting Railway modular monolith API at $Url"
dotnet run --project src/services/Railway.Gateway/Railway.Gateway.csproj --urls $Url
