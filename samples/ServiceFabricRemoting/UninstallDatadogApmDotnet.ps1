Configuration UninstallDatadogApmDotnet
{
    # See local install / run steps at https://github.com/DataDog/dd-trace-dotnet/tree/lpimentel/powershell-dsc/tools/PowerShell-DSC
    # Adapted from https://github.com/DataDog/dd-trace-dotnet/blob/lpimentel/powershell-dsc/tools/PowerShell-DSC/DatadogApmDotnet.ps1
    Import-DscResource -ModuleName PSDscResources -Name MsiPackage
    Import-DscResource -ModuleName PSDscResources -Name Environment

    # Version of the Agent package to be installed
    $AgentVersion = '7.27.0'

    # Version of the Tracer package to be installed
    $TracerVersion = '1.26.1'

    Node 'localhost'
    {
        # Agent msi installer
        MsiPackage 'dd-agent' {
            Path      = "https://s3.amazonaws.com/ddagent-windows-stable/ddagent-cli-$AgentVersion.msi"
            ProductId = 'B55FFED6-0CAD-4F94-AA07-5B74A5776C1C'
            Ensure    = 'Present'
        }

        # .NET Tracer msi installer
        MsiPackage 'dd-trace-dotnet' {
            Path      = "https://github.com/DataDog/dd-trace-dotnet/releases/download/v$TracerVersion/datadog-dotnet-apm-$TracerVersion-x64.msi"
            ProductId = '00B19BDB-EC40-4ADF-A73F-789A7721247A'
            Ensure    = 'Absent'
        }

        Environment 'COR_PROFILER' {
            Name   = 'COR_PROFILER'
            Ensure = 'Absent'
            Path   = $false
            Target = @('Machine')
        }

        Environment 'COR_ENABLE_PROFILING' {
            Name   = 'COR_ENABLE_PROFILING'
            Ensure = 'Absent'
            Path   = $false
            Target = @('Machine')
        }

        Environment 'CORECLR_PROFILER' {
            Name   = 'CORECLR_PROFILER'
            Ensure = 'Absent'
            Path   = $false
            Target = @('Machine')
        }

        Environment 'CORECLR_ENABLE_PROFILING' {
            Name   = 'CORECLR_ENABLE_PROFILING'
            Ensure = 'Absent'
            Path   = $false
            Target = @('Machine')
        }
    }
}