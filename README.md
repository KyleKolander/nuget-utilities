# NuGet Utilities

#### Powerful cross-platform NuGet Utilities library and Summarizer app

[![Build](https://github.com/KyleKolander/nuget-utilities/actions/workflows/ci.yml/badge.svg)](https://github.com/KyleKolander/nuget-utilities/actions/workflows/ci.yml) [![Release](https://github.com/KyleKolander/nuget-utilities/actions/workflows/release.yml/badge.svg)](https://github.com/KyleKolander/nuget-utilities/actions/workflows/release.yml) [![NuGet](https://img.shields.io/nuget/v/NuGetUtilities.Core.svg)](https://www.nuget.org/packages/NuGetUtilities.Core/) [![GitHub license](https://img.shields.io/badge/license-MIT%20-blue.svg)](https://raw.githubusercontent.com/KyleKolander/nuget-utilities/main/LICENSE)

Use the Core library to programmatically retrieve, install and read NuGet packages, determine transitive dependencies, and build the entire dependency tree. The Summarizer console app demonstrates how to use the library and makes it simple to ascertain any of this information without writing code.  I used various packages from the [NuGet.Client](https://github.com/NuGet/NuGet.Client) repo.  While there is plenty of documentation on how to use the resulting tool(s), there is very little documentation on how to use the libraries.

## Motivation

PowerShell lacks the ability to use a NuGet package with transitive dependencies.  It can be done, but only if you add each of the transitive dependencies to the PowerShell session as well.  There isn't a good solution to this problem, so I set out to create one and give back to the community.  The NuGetUtilities.Core library will be used in a PowerShell module to make all of this as easy as possible.

## Features

- Full package dependency tree with parent/child relationships
- Formatted summary of a package and its transitive dependencies
- Complete set of assembly file paths for all transitive dependencies
- PowerShell commands needed to import a package (and its required dependencies)
- Retrieve, install and read a package

## Getting Started

##### Clone the repo

```powershell
git clone https://github.com/KyleKolander/nuget-utilities
cd nuget-utilities
```

##### Restore, build, test, pack and publish

```powershell
scripts\build.ps1 -Runtime win-x64 -Version 0.0.2
```

##### Run the Summarizer project

```powershell
cd src\Summarizer\bin\Release\net5.0\win-x64

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

##### Summarizer output

```
Transitive Dependencies
============
Microsoft.NETCore.Platforms.2.0.0
Portable.BouncyCastle.1.8.10
System.Buffers.4.5.1
System.Reflection.TypeExtensions.4.4.0
System.Security.Cryptography.Cng.4.7.0
System.Security.Cryptography.Pkcs.4.7.0 : System.Security.Cryptography.Cng [4.7.0, )
System.Text.Encoding.CodePages.4.4.0 : Microsoft.NETCore.Platforms [2.0.0, )
MimeKit.2.15.0 : Portable.BouncyCastle [1.8.10, ), System.Buffers [4.5.1, ), System.Reflection.TypeExtensions [4.4.0, ), System.Security.Cryptography.Pkcs [4.7.0, ), System.Text.Encoding.CodePages [4.4.0, )
MailKit.2.15.0 : MimeKit [2.15.0, )

Dependency Tree
============
mailkit.2.15.0  {net5.0}
    mimekit.2.15.0  [2.15.0, )
        system.security.cryptography.pkcs.4.7.0  [4.7.0, )
            system.security.cryptography.cng.4.7.0  [4.7.0, )
        system.reflection.typeextensions.4.4.0  [4.4.0, )
        system.text.encoding.codepages.4.4.0  [4.4.0, )
            microsoft.netcore.platforms.2.0.0  [2.0.0, )
        system.buffers.4.5.1  [4.5.1, )
        portable.bouncycastle.1.8.10  [1.8.10, )

PowerShell Commands to use NuGet Package: mailkit.2.15.0
================================================================================
Add-Type -Path 'c:\users\kyle\.nuget\packages\portable.bouncycastle\1.8.10\lib\netstandard2.0\BouncyCastle.Crypto.dll'
Add-Type -Path 'c:\users\kyle\.nuget\packages\system.security.cryptography.cng\4.7.0\lib\netcoreapp3.0\System.Security.Cryptography.Cng.dll'
Add-Type -Path 'c:\users\kyle\.nuget\packages\system.security.cryptography.pkcs\4.7.0\lib\netcoreapp3.0\System.Security.Cryptography.Pkcs.dll'
Add-Type -Path 'c:\users\kyle\.nuget\packages\system.text.encoding.codepages\4.4.0\lib\netstandard2.0\System.Text.Encoding.CodePages.dll'
Add-Type -Path 'c:\users\kyle\.nuget\packages\mimekit\2.15.0\lib\net50\MimeKit.dll'
Add-Type -Path 'c:\users\kyle\.nuget\packages\mailkit\2.15.0\lib\net50\MailKit.dll'

*** not all output shown
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

![Flow Diagrams](https://raw.githubusercontent.com/KyleKolander/nuget-utilities/main/diagrams/Flow.png)
