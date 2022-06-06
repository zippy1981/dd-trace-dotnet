$resultFolder = "./tracer/build_data/results";
$service= "dd-trace-dotnet";
$files = [System.IO.Directory]::GetFiles($resultFolder, "junit-result.xml", [System.IO.SearchOption]::AllDirectories);
$osArchitecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant();
$runtimeArchitecture = $env:ARCHITECTURE

if ($osArchitecture.Contains("arm")) {
    Write-Output "Installing NPM datadog-ci";
    npm install -g @datadog/datadog-ci
} else {
    Write-Output "Downloading datadog-ci..."
    $datadogCliReleases = "https://github.com/DataDog/datadog-ci/releases/latest/download"
    if ($IsLinux) {
        if (!(Test-Path "./datadog-ci")) {
            Invoke-WebRequest -Uri "$datadogCliReleases/datadog-ci_linux-x64" -OutFile "./datadog-ci"
            chmod +x ./datadog-ci
        }
    } elseif ($IsMacOS) { 
        if (!(Test-Path "./datadog-ci")) {
            Invoke-WebRequest -Uri "$datadogCliReleases/datadog-ci_darwin-x64" -OutFile "./datadog-ci"
            chmod +x ./datadog-ci
        }
    } else {
        if (!(Test-Path "datadog-ci.exe")) {
            Invoke-WebRequest -Uri "$datadogCliReleases/datadog-ci_win-x64.exe" -OutFile "datadog-ci.exe"
        }
    }
}

Write-Output "Processing junit files..."
foreach ($file in $files)
{
    $fileInfo = New-Object -TypeName System.IO.FileInfo -ArgumentList $file
    $file = $fileInfo.FullName;
    $targetFramework = $fileInfo.Directory.Name;

    $osPlatform = "windows";
    if ($IsLinux) { $osPlatform = "linux"; }
    if ($IsMacOS) { $osPlatform = "macos"; }

    $runtimeName = ".NET";
    $runtimeVersion = $targetFramework;
    switch ( $targetFramework )
    {
        NETCoreApp21 { $runtimeVersion = "2.1.0"; }
        NETCoreApp30 { $runtimeVersion = "3.0.0"; }
        NETCoreApp31 { $runtimeVersion = "3.1.0"; }
        NETCoreApp50 { $runtimeVersion = "5.0.0"; }
        NETCoreApp60 { $runtimeVersion = "6.0.0"; }
        NETFramework461 { $runtimeVersion = "4.6.1"; $runtimeName = ".NET Framework"; }
    }

    $env:DD_TAGS="os.platform:$osPlatform,os.architecture:$osArchitecture,runtime.name:$runtimeName,runtime.version:$runtimeVersion,runtime.architecture:$runtimeArchitecture,runtime.vendor:$targetFramework,language:dotnet"

    Write-Output $file;
    Write-Output $env:DD_TAGS;

    if ($IsLinux) { 
        if ($osArchitecture.Contains("arm")) {
            datadog-ci junit upload --service $service $file
        } else {
            ./datadog-ci junit upload --service $service $file
        }
    } elseif ($IsMacOS) { 
        ./datadog-ci junit upload --service $service $file
    } else {
        ./datadog-ci.exe junit upload --service $service $file
    }

    Write-Output "Removing $file"
    Remove-Item $file -Force -ErrorAction Ignore
    if ($IsLinux -or $IsMacOS) {
        sh -c "rm -f $file"
    } else {
        cmd /c "erase /f /q $file"
    }
    Write-Output "";
}

Write-Output "Removing datadog-ci..."
if (Test-Path "./datadog-ci") {
  Remove-Item "./datadog-ci" -Force -ErrorAction Ignore
  sh -c "rm -f ./datadog-ci"
}
if (Test-Path "datadog-ci.exe") {
  Remove-Item "datadog-ci.exe" -Force -ErrorAction Ignore
  cmd /c "erase /f /q datadog-ci.exe"
}

Write-Output "Done."