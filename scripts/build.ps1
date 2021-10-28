[CmdletBinding()]
Param (
    [string]$Version = '0.0.1',
    [string]$Runtime = 'win-x64',
    [string]$Configuration = 'Release',
    [string]$OutDirPublish = 'publish',
    [string]$OutDirPack = 'nupkgs'
)

function Write-Header([string]$Message) {
    Write-Host ''
    Write-Host '***************************************'
    Write-Host "*   $Message"
    Write-Host '***************************************'
}

Write-Header 'Restore'
dotnet restore --verbosity minimal -graphBuild:true

Write-Header 'Build'
dotnet build --configuration $Configuration --runtime $Runtime -p:Version=$Version --no-restore --nologo --verbosity minimal -graphBuild:true -p:SelfContained=false

Write-Header 'Test'
dotnet test --configuration $Configuration --runtime $Runtime -p:Version=$Version --no-build --nologo --verbosity normal -p:SelfContained=false

Write-Header 'Pack'
dotnet pack --configuration $Configuration --runtime $Runtime -p:Version=$Version --nologo --include-symbols --include-source --output $OutDirPack -p:SelfContained=false .\src\Core\Core.csproj

Write-Header 'Publish'
dotnet publish .\src\Summarizer\Summarizer.csproj --output "$OutDirPublish/Summarizer-$Version-$Runtime" --configuration $Configuration --runtime $Runtime -p:Version=$Version --no-build --nologo -p:SelfContained=false -p:PublishSingleFile=true

#
# https://github.com/dotnet/runtime/issues/3569#issuecomment-920285890
#
# Need to use --runtime $Runtime and -p:SelfContained=false on each step except restore.
#
# Without this, the published single file executable will not run and you'll have errors like this:
#
# A fatal error was encountered. The library 'hostpolicy.dll' required to execute the application was not found in 'C:\Program Files\dotnet'.
# Failed to run as a self-contained app.
#   - The application was run as a self-contained app because 'D:\Source\github\KyleKolander\nuget-utilities\publish\Summarizer-0.0.16-win-x64\NuGetUtilities.Summarizer.runtimeconfig.json' was not found.
#   - If this should be a framework-dependent app, add the 'D:\Source\github\KyleKolander\nuget-utilities\publish\Summarizer-0.0.16-win-x64\NuGetUtilities.Summarizer.runtimeconfig.json' file and specify the appropriate framework.
#