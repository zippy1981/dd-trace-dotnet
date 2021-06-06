Param (
    [Parameter(Mandatory=$false)][string]$Version = '1.27.0',
    [Parameter(Mandatory=$false)][string[]]$AdditionalEnvironmentVariables = @(),
    [Parameter(Mandatory=$false)][Switch]$EnableDotNetFrameworkMachineWide,
    [Parameter(Mandatory=$false)][Switch]$EnableDotNetCoreMachineWide,
    [Parameter(Mandatory=$false)][Switch]$DryRun
)

$installDirectory = Join-Path $env:ProgramFiles (Join-Path 'Datadog' '.NET Tracer') # PS 5 and earlier only accepts one child path
$msiPath = Join-Path $installDirectory 'datadog-dotnet-apm-x64.msi'
$downloadUrl = "https://github.com/DataDog/dd-trace-dotnet/releases/download/v$Version/datadog-dotnet-apm-$Version-x64.msi"

Write-Host "[DatadogCustomScriptExtensionInstall.ps1] Downloading Datadog .NET Tracer v$Version from $downloadUrl"
if (!$DryRun.IsPresent) {
  mkdir -Force $installDirectory | Out-Null
  Invoke-WebRequest $downloadUrl -OutFile $msiPath -UseBasicParsing
}

if (!$DryRun.IsPresent) {
  Write-Host "Start-Process -Wait msiexec -ArgumentList /qn /i `"$msiPath`""
  Start-Process -Wait msiexec -ArgumentList "/qn /i `"$msiPath`""
}

function Set-MachineEnvironmentVariable {
    param(
      [string]$name,
      [string]$value
    )

    Write-Host "[DatadogCustomScriptExtensionInstall.ps1] Setting environment variable $name=$value"
    if (!$DryRun.IsPresent) {
      [System.Environment]::SetEnvironmentVariable($name, $value, [System.EnvironmentVariableTarget]::Machine)
    }
}

Set-MachineEnvironmentVariable 'COR_PROFILER' '{846F5F1C-F9AE-4B07-969E-05C26BC060D8}'
if ($EnableDotNetFrameworkMachineWide.IsPresent) {
  Set-MachineEnvironmentVariable'COR_ENABLE_PROFILING' '1'
}

Set-MachineEnvironmentVariable 'CORECLR_PROFILER' '{846F5F1C-F9AE-4B07-969E-05C26BC060D8}'
if ($EnableDotNetCoreMachineWide.IsPresent) {
  Set-MachineEnvironmentVariable 'CORECLR_ENABLE_PROFILING' '1'
}

# Set environment variables for all items matching format "name=value"
$AdditionalEnvironmentVariables | ForEach-Object {
  $stringArr = $_.Split("=")
  if ($stringArr.Length -eq 2) {
    Set-MachineEnvironmentVariable $stringArr[0] $stringArr[1]
  }
}

Write-Host "[DatadogCustomScriptExtensionInstall.ps1] Running 'iisreset'"
if (!$DryRun.IsPresent) {
  iisreset
}