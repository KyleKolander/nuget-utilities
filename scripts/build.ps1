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
dotnet build --configuration $Configuration --runtime $Runtime -p:Version=$Version --no-restore --nologo --verbosity minimal -graphBuild:true

Write-Header 'Test'
dotnet test --configuration $Configuration --runtime $Runtime -p:Version=$Version --no-build --nologo --verbosity normal

Write-Header 'Pack'
dotnet pack --configuration $Configuration --runtime $Runtime -p:Version=$Version --nologo --include-symbols --include-source --output $OutDirPack .\src\Core\Core.csproj

Write-Header 'Publish'
dotnet publish .\src\Summarizer\Summarizer.csproj --output "$OutDirPublish/Summarizer-$Version-$Runtime" --configuration $Configuration --runtime $Runtime -p:Version=$Version --no-build --nologo --self-contained false -p:PublishSingleFile=true
