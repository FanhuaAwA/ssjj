param(
    [string]$Configuration = "Release",
    [string]$GameManagedDir = "",
    [string]$AnalysisAssemblyDir = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$argsList = @("build", (Join-Path $root "Bhop02.Source.sln"), "-c", $Configuration)
if ($GameManagedDir) { $argsList += "/p:GameManagedDir=$GameManagedDir" }
if ($AnalysisAssemblyDir) { $argsList += "/p:AnalysisAssemblyDir=$AnalysisAssemblyDir" }
& dotnet @argsList
