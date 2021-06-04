Param (
    [String] $tracer_version
)

$ProgressPreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"

$tracer_home = ""

if ($tracer_version -eq "") {
    # Get the latest release tag from the github releases list
    Write-Output "Getting latest release version..."
    $tracer_version = (Invoke-WebRequest https://api.github.com/repos/datadog/dd-trace-dotnet/releases | ConvertFrom-Json)[0].tag_name.SubString(1)
}

Write-Output "Downloading tracer v$tracer_version..."

# Download the file for the current operating system and extract the content to the "tracer-home-$version" folder
if ($env:os -eq "Windows_NT") {
    $tracer_home = "$(Get-Location)/tracer-home-$tracer_version"
    $url = "https://github.com/DataDog/dd-trace-dotnet/releases/download/v$($tracer_version)/windows-tracer-home.zip"
    $filename = "windows-tracer-home-$tracer_version.zip"
    Invoke-WebRequest -Uri $url -OutFile $filename

    Write-Output "Extracting file $filename..."
    Expand-Archive $filename -DestinationPath $tracer_home
    Remove-Item $filename
}
else {
    # File version is the same as the release version without the prerelease suffix.
    $file_version = $tracer_version.Replace("-prerelease", "")
    $tracer_home = "$(Get-Location)/tracer-home-$file_version"
    $url = "https://github.com/DataDog/dd-trace-dotnet/releases/download/v$($tracer_version)/datadog-dotnet-apm-$($file_version).tar.gz"
    $filename = "linux-tracer-home-$file_version.tar.gz"
    Invoke-WebRequest -Uri $url -OutFile $filename

    Write-Output "Extracting file $filename..."
    New-Item -Path $tracer_home -ItemType Directory -Force
    tar -xvzf $filename -C $tracer_home
    Remove-Item $filename
}


