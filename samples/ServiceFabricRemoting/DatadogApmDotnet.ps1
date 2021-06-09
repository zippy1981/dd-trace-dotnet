Configuration DatadogApmDotnet
{
    # See local install / run steps at https://github.com/DataDog/dd-trace-dotnet/tree/lpimentel/powershell-dsc/tools/PowerShell-DSC
    # Adapted from https://github.com/DataDog/dd-trace-dotnet/blob/lpimentel/powershell-dsc/tools/PowerShell-DSC/DatadogApmDotnet.ps1
    Import-DscResource -ModuleName PSDscResources -Name MsiPackage
    Import-DscResource -ModuleName PSDscResources -Name Environment

    # Version of the Tracer package to be installed
    $TracerVersion = '1.26.3'

    Node "localhost"
    {
        MsiPackage 'dd-trace-dotnet' {
            Path      = "https://github.com/DataDog/dd-trace-dotnet/releases/download/v$TracerVersion/datadog-dotnet-apm-$TracerVersion-x64.msi"
            ProductId = 'E6BF28A9-0CBB-42FF-B793-C87A4D602868'
            Ensure    = 'Present'
        }

        Environment 'COR_PROFILER' {
            Name   = 'COR_PROFILER'
            Value  = '{846F5F1C-F9AE-4B07-969E-05C26BC060D8}'
            Ensure = 'Present'
            Path   = $false
            Target = @('Machine')
        }

        Environment 'CORECLR_PROFILER' {
            Name   = 'CORECLR_PROFILER'
            Value  = '{846F5F1C-F9AE-4B07-969E-05C26BC060D8}'
            Ensure = 'Present'
            Path   = $false
            Target = @('Machine')
        }
    }
}