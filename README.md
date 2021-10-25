# NuGet Utilities

#### Powerful NuGet Utilities library and Summarizer app

[![Build](https://github.com/KyleKolander/nuget-utilities/actions/workflows/ci.yml/badge.svg)](https://github.com/KyleKolander/nuget-utilities/actions/workflows/ci.yml)
[![Release](https://github.com/KyleKolander/nuget-utilities/actions/workflows/release.yml/badge.svg)](https://github.com/KyleKolander/nuget-utilities/actions/workflows/release.yml)
[![NuGet](https://img.shields.io/nuget/v/NuGetUtilities.Core.svg)](https://www.nuget.org/packages/NuGetUtilities.Core/)
[![GitHub license](https://img.shields.io/badge/license-MIT%20-blue.svg)](LICENSE)

The Core library provides the ability to programmatically retrieve, install and read NuGet packages, determine transitive dependencies, and build the entire dependency tree.

The Summarizer console app demonstrates how to use the library and makes it simple to ascertain any of this information without writing code.

I used various packages from the [NuGet.Client](https://github.com/NuGet/NuGet.Client) repo.  While there is plenty of documentation on how to use the resulting tool(s), there is very little documentation on how to use the libraries.

## Motivation

PowerShell lacks the ability to use a NuGet package with transitive dependencies.  It can be done, but only if you add each of the transitive dependencies to the PowerShell session as well.  There isn't a good solution to this problem, so I set out to create one and give back to the community.  The NuGetUtilities.Core library will be used in a PowerShell module to make all of this as easy as possible.

## Features

- Full package dependency tree with parent/child relationships
- Formatted summary of a package and its transitive dependencies
- Complete set of assembly file paths for all transitive dependencies
- PowerShell commands needed to import a package (and its required dependencies)
- Retrieve, install and read a package

## Getting Started

Clone the repo

```powershell
git clone https://github.com/KyleKolander/nuget-utilities
cd nuget-utilities
```

Build the solution

```powershell
dotnet build .\NuGetUtilities.sln --nologo
```

Run the tests

```powershell
dotnet test .\test\Integration\Integration.csproj
```

Install Report Generator as a dotnet global tool (one-time setup)

```powershell
# Run as Administrator in PowerShell Core
dotnet tool install --global dotnet-reportgenerator-globaltool --version 4.8.9 
dotnet tool install dotnet-reportgenerator-globaltool --tool-path tools 
dotnet new tool-manifest 
dotnet tool install dotnet-reportgenerator-globaltool
```

View code coverage report

```powershell
reportgenerator -reports:"./test/CoverageResults/*_coverage.cobertura.xml" `
                -targetdir:./test/CoverageResults `
                -reporttypes:"HtmlInline;TextSummary;Badges;Cobertura" `
                -title:"NuGet Utilties - Code Coverage"

Get-Content "./test/CoverageResults/Summary.txt"

Start-Process chrome -ArgumentList $(Resolve-Path ./test/CoverageResults/index.html)
```

Run the Summarizer project

```powershell
cd src\Summarizer\bin\Debug\net5.0\

.\NuGetUtilities.Summarizer.exe --id MailKit `
                                --version 2.15.0 `
                                --framework net5.0 `
                                --install `
                                --transitive `
                                --tree `
                                --contents `
                                --assemblies `
                                --powerShell
```

## Code Example

You can accomplish most of these tasks with a single line of code.

```csharp
var identity               = new PackageIdentity("MailKit", NuGetVersion.Parse("2.15.0"));
var targetFramework        = NuGetFramework.ParseFolder("net5.0");

var retrievedPackage       = await packageRetriever.Retrieve(identity, targetFramework);

var transitiveDependencies = await packageRetriever.RetrieveTransitiveDependencies(retrievedPackage
                                                                                 , targetFramework);

var installedPackages      = await packageInstaller.Install(transitiveDependencies);

var rootNode               = dependencyTreeBuilder.Build(retrievedPackage, transitiveDependencies);
var display                = dependencyTreeBuilder.Display;

var commands               = PowerShell.GetAddTypeCommands(installedPackages);

var authors                = installedPackageContents.NuspecReader.GetAuthors();
```

## API Reference

![Flow Diagrams](diagrams/Flow.png?raw=true)
