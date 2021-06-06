Param (
    [Parameter(Mandatory=$false)][string[]]$AdditionalEnvironmentVariables = @(),
    [Parameter(Mandatory=$false)][Switch]$EnableDotNetFrameworkMachineWide, # Unused but present for compatibility with install script
    [Parameter(Mandatory=$false)][Switch]$EnableDotNetCoreMachineWide, # Unused but present for compatibility with install script
    [Parameter(Mandatory=$false)][Switch]$DryRun
)

$installDirectory = Join-Path $env:ProgramFiles (Join-Path 'Datadog' '.NET Tracer') # PS 5 and earlier only accepts one child path
$msiPath = Join-Path $installDirectory 'datadog-dotnet-apm-x64.msi'

Write-Host "[DatadogCustomScriptExtensionUninstall.ps1] Uninstalling Datadog .NET Tracer"
if (!$DryRun.IsPresent) {
  if (Test-Path $msiPath) {
    Start-Process -Wait msiexec -ArgumentList "/qn /x `"$msiPath`""
    Remove-Item -Recurse -Force $installDirectory
  }
}

function Remove-MachineEnvironmentVariable {
    param(
      [string]$name
    )

    Write-Host "[DatadogCustomScriptExtensionUninstall.ps1] Removing environment variable $name"
    if (!$DryRun.IsPresent) {
      [System.Environment]::SetEnvironmentVariable($name, $null, [System.EnvironmentVariableTarget]::Machine)
    }
}

Remove-MachineEnvironmentVariable 'COR_PROFILER' '{846F5F1C-F9AE-4B07-969E-05C26BC060D8}'
Remove-MachineEnvironmentVariable 'COR_ENABLE_PROFILING' '1'

Remove-MachineEnvironmentVariable 'CORECLR_PROFILER' '{846F5F1C-F9AE-4B07-969E-05C26BC060D8}'
Remove-MachineEnvironmentVariable 'CORECLR_ENABLE_PROFILING' '1'

# Remove environment variables for all items matching format "name" or "name=value"
# The "name=value" format is copied from the install script, so customers can remove any
# environment variables set by the install script by only changing the script file path and
# not the script arguments
$AdditionalEnvironmentVariables | ForEach-Object {
  $stringArr = $_.Split("=")
  if ($stringArr.Length -ge 1) {
    Remove-MachineEnvironmentVariable $stringArr[0] $stringArr[1]
  }
}

Write-Host "[DatadogCustomScriptExtensionUninstall.ps1] Running 'iisreset'"
if (!$DryRun.IsPresent) {
  iisreset
}