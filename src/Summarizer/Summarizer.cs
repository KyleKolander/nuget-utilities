using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using NuGetUtilities.Core;

// ReSharper disable UnusedVariable

// ReSharper disable InvertIf
// ReSharper disable MemberCanBePrivate.Global

namespace NuGetUtilities.Summarizer
{
    [ExcludeFromCodeCoverage]
    public class Summarizer : IHostedService
    {
        public Summarizer(ILogger<Summarizer> logger
                        , IHostApplicationLifetime appLifetime
                        , PackageRetriever packageRetriever
                        , PackageInstaller packageInstaller
                        , PackageReader packageReader
                        , DependencyTreeBuilder dependencyTreeBuilder)
        {
            Logger = logger;
            AppLifetime = appLifetime;
            PackageRetriever = packageRetriever;
            PackageInstaller = packageInstaller;
            PackageReader = packageReader;
            DependencyTreeBuilder = dependencyTreeBuilder;
        }

        private ILogger<Summarizer> Logger { get; }
        private IHostApplicationLifetime AppLifetime { get; }
        private PackageRetriever PackageRetriever { get; }
        public PackageInstaller PackageInstaller { get; }
        public PackageReader PackageReader { get; }
        private DependencyTreeBuilder DependencyTreeBuilder { get; }
        private int ExitCode { get; set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogDebug($"Starting with arguments: {string.Join(" ", Environment.GetCommandLineArgs().Skip(1).ToArray())}");

            // "commandLineArgs": "--id MailKit --version 2.15.0 --framework net5.0 --install --transitive --tree --contents --assemblies --powerShell",

            AppLifetime.ApplicationStarted.Register(() => _ = Task.Run(Run, cancellationToken));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogDebug($"Exiting with return code: {ExitCode}");
            Environment.ExitCode = ExitCode;
            return Task.CompletedTask;
        }

        private async Task? Run()
        {
            try
            {
                var rootCommand = new RootCommand
                                  {
                                      new Option<string>("--id",      "NuGet package ID (e.g. MailKit)")
                                    , new Option<string>("--version", "NuGet package version (e.g. 2.15.0)")
                                    , new Option<string>("--framework", "NuGet package framework (e.g. net5.0)")
                                      {
                                          IsRequired = true
                                      }
                                    , new Option<string>("--nupkg", @"NuGet package file path (e.g. C:\Packages\MyPackage.1.0.0.nupkg)")
                                    , new Option<bool>("--install",    "Flag indicating whether to install the NuGet package(s)")
                                    , new Option<bool>("--transitive", "Flag indicating whether to retrieve transitive dependencies of the specified NuGet package")
                                    , new Option<bool>("--tree",       "Flag indicating whether to display the dependency tree")
                                    , new Option<bool>("--contents",   "Flag indicating whether to display the contents of the specified NuGet package (and transitive dependencies if --transitive flag is supplied)")
                                    , new Option<bool>("--assemblies", "Flag indicating whether to display the assemblies of the specified NuGet package (and transitive dependencies if --transitive flag is supplied)")
                                    , new Option<bool>("--powerShell", "Flag indicating whether to display the PowerShell commands needed to use the specified NuGet package")
                                  };
                rootCommand.Description = "NuGet package summarizer";
                rootCommand.Handler = CommandHandler.Create<string, string, string, string, bool, bool, bool, bool, bool, bool>(Run);
                await rootCommand.InvokeAsync(Environment.GetCommandLineArgs().Skip(1).ToArray());
            }
            catch (Exception e)
            {
                Logger.LogCritical(e, "Unhandled exception!");
                ExitCode = 1;
            }
            finally
            {
                AppLifetime.StopApplication();
            }
        }

        [SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "For illustration and debugging")]
        public async Task Run(string id, string version, string framework, string nupkg, bool install, bool transitive, bool tree, bool contents, bool assemblies, bool powerShell)
        {
            if (string.IsNullOrWhiteSpace(nupkg) && (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(version))
             || !string.IsNullOrWhiteSpace(nupkg) && (!string.IsNullOrWhiteSpace(id) || !string.IsNullOrWhiteSpace(version)))
            {
                var message = "Must either specify the path to a nupkg file or provide the ID and Version of a package to retrieve from a repository, but not both";
                Logger.LogCritical(message);
                throw new(message);
            }

            if (tree && !transitive)
            {
                transitive = true;
                Logger.LogDebug("--tree specified but not --transitive, which is a prerequisite, so adding --transitive");
            }

            if (powerShell && !assemblies)
            {
                assemblies = true;
                Logger.LogDebug("--powerShell specified but not --assemblies, which is a prerequisite, so adding --assemblies");
            }

            if (powerShell && !transitive)
            {
                transitive = true;
                Logger.LogDebug("--powerShell specified but not --transitive, which is a prerequisite, so adding --transitive");
            }

            if (assemblies && !contents)
            {
                contents = true;
                Logger.LogDebug("--assemblies specified but not --contents, which is a prerequisite, so adding --contents");
            }

            if (contents && !install)
            {
                install = true;
                Logger.LogDebug("--contents specified but not --install, which is a prerequisite, so adding --install");
            }

            var nuGetFramework = NuGetFramework.ParseFolder(framework);
            Package package;
            PackageIdentity packageIdentity;

            if (string.IsNullOrWhiteSpace(nupkg))
            {
                packageIdentity = new(id, NuGetVersion.Parse(version));
                package = await HandleRetrieve(packageIdentity, nuGetFramework);
            }
            else
            {
                package = await PackageInstaller.Install(nupkg, nuGetFramework, CancellationToken.None);
                packageIdentity = package.PackageIdentity;
            }

            var transitiveDependencies = await HandleTransitiveDependencies(transitive, package, nuGetFramework);
            var rootNode = HandleTree(tree, transitive, package, transitiveDependencies);
            var installedPackages = await HandleInstall(install, package, transitiveDependencies);
            var installedPackageContents = HandleContents(contents, installedPackages);
            var assemblyItems = HandleAssemblies(assemblies, installedPackageContents);
            var powerShellCommands = HandlePowerShell(powerShell, package, installedPackageContents);
        }

        private async Task<Package> HandleRetrieve(PackageIdentity packageIdentity, NuGetFramework nuGetFramework)
        {
            var package = await PackageRetriever.Retrieve(packageIdentity, nuGetFramework);

            if (package is null)
            {
                throw new($"Unable to retrieve package: {packageIdentity} {{{nuGetFramework.GetShortFolderName()}}}");
            }

            Console.WriteLine($"{Environment.NewLine}Successfully retrieved package: {packageIdentity} {{{nuGetFramework.GetShortFolderName()}}} from {package.DownloadUri}{Environment.NewLine}");

            return package;
        }

        private async Task<List<Package>> HandleTransitiveDependencies(bool transitive, Package package, NuGetFramework nuGetFramework)
        {
            Logger.LogDebug($"Started {nameof(HandleTransitiveDependencies)}");

            List<Package> transitiveDependencies = new();

            if (transitive)
            {
                transitiveDependencies = (await PackageRetriever.RetrieveTransitiveDependencies(package, nuGetFramework)).ToList();

                StringBuilder sb = new();
                sb.AppendLine();
                sb.AppendLine("Transitive Dependencies");
                sb.AppendLine("============");

                foreach (var dependencyPackage in transitiveDependencies)
                {
                    sb.AppendLine(dependencyPackage.ToString());
                }

                var dependenciesDisplay = sb.ToString();
                Console.WriteLine(dependenciesDisplay);
            }

            Logger.LogDebug($"Finished {nameof(HandleTransitiveDependencies)}");

            return transitiveDependencies;
        }

        private DependencyTreeNode? HandleTree(bool tree, bool transitive, Package package, IEnumerable<Package> transitiveDependencies)
        {
            Logger.LogDebug($"Started {nameof(HandleTree)}");

            DependencyTreeNode? rootNode = null;

            if (tree)
            {
                rootNode = DependencyTreeBuilder.Build(package, transitiveDependencies);

                StringBuilder sb = new();
                sb.AppendLine();
                sb.AppendLine("Dependency Tree");
                sb.AppendLine("============");
                sb.AppendLine(DependencyTreeBuilder.Display);
                var dependencyTreeDisplay = sb.ToString();
                Console.WriteLine(dependencyTreeDisplay);
            }

            Logger.LogDebug($"Finished {nameof(HandleTree)}");

            return rootNode;
        }

        private async Task<List<InstalledPackage>> HandleInstall(bool install, Package package, List<Package> transitiveDependencies)
        {
            Logger.LogDebug($"Started {nameof(HandleInstall)}");

            var installedPackages = new List<InstalledPackage>();

            if (!transitiveDependencies.Any())
            {
                transitiveDependencies.Add(package);
            }

            if (install)
            {
                StringBuilder sb = new();
                sb.AppendLine();
                sb.AppendLine("Installed Paths");
                sb.AppendLine("============");

                installedPackages.AddRange(await PackageInstaller.Install(transitiveDependencies));
                installedPackages.ForEach(p => sb.AppendLine(p.InstalledPath));

                var display = sb.ToString();
                Console.WriteLine(display);

                if (!installedPackages.Any())
                {
                    throw new("Unable to install the package(s)");
                }
            }

            Logger.LogDebug($"Finished {nameof(HandleInstall)}");

            return installedPackages;
        }

        private List<InstalledPackageContents> HandleContents(bool contents, List<InstalledPackage> installedPackages)
        {
            Logger.LogDebug($"Started {nameof(HandleContents)}");

            var installedPackageContentsList = new List<InstalledPackageContents>();

            if (contents)
            {
                StringBuilder sb = new();
                sb.AppendLine();
                sb.AppendLine("Installed Package Contents");
                sb.AppendLine("==========================");

                foreach (var installedPackage in installedPackages)
                {
                    var installedPackageContents = PackageReader.Read(installedPackage!);
                    installedPackageContentsList.Add(installedPackageContents);

                    sb.AppendLine(installedPackageContents.PackageIdentityDisplayNoDependencies);
                    sb.AppendLine($"    {installedPackageContents.NupkgFilePath}");
                    sb.AppendLine($"    {installedPackageContents.NuspecFilePath}");
                    sb.AppendLine($"    {installedPackageContents.LibItems.Items.Count} Lib items");
                    if (installedPackageContents.LibItems.Items.Any())
                    {
                        foreach (var libItem in installedPackageContents.LibItems.Items)
                        {
                            sb.AppendLine($"        {libItem.FullPath}");
                        }
                    }
                    sb.AppendLine($"    {installedPackageContents.FrameworkItems.Items.Count} Framework items");
                    if (installedPackageContents.FrameworkItems.Items.Any())
                    {
                        foreach (var frameworkItem in installedPackageContents.FrameworkItems.Items)
                        {
                            sb.AppendLine($"        {frameworkItem.FullPath}");
                        }
                    }
                    sb.AppendLine($"    {installedPackageContents.LibItems.Assemblies.Count} Assemblies");
                    if (installedPackageContents.LibItems.Assemblies.Any())
                    {
                        foreach (var assembly in installedPackageContents.LibItems.Assemblies)
                        {
                            sb.AppendLine($"        {assembly.FullPath}");
                        }
                    }
                }

                var display = sb.ToString();
                Console.WriteLine(display);

                Logger.LogDebug($"Finished {nameof(HandleContents)}");
            }

            return installedPackageContentsList;
        }

        private List<PackageItem> HandleAssemblies(bool assemblies, IEnumerable<InstalledPackageContents> installedPackages)
        {
            Logger.LogDebug($"Started {nameof(HandleAssemblies)}");

            var assemblyItems = new List<PackageItem>();

            if (assemblies)
            {
                StringBuilder sb = new();
                sb.AppendLine();
                sb.AppendLine("Assemblies");
                sb.AppendLine("==========================");

                assemblyItems.AddRange(installedPackages.SelectMany(p => p.LibItems.Assemblies));
                assemblyItems.ForEach(a => sb.AppendLine(a.FullPath));

                var display = sb.ToString();
                Console.WriteLine(display);
            }

            Logger.LogDebug($"Finished {nameof(HandleAssemblies)}");

            return assemblyItems;
        }

        private List<string> HandlePowerShell(bool powerShell, Package package, IEnumerable<InstalledPackageContents> installedPackages)
        {
            Logger.LogDebug($"Started {nameof(HandlePowerShell)}");

            var commands = new List<string>();

            if (powerShell)
            {
                commands.AddRange(PowerShell.GetAddTypeCommands(installedPackages));

                StringBuilder sb = new();
                sb.AppendLine();
                sb.AppendLine($"PowerShell Commands to use NuGet Package: {package.PackageIdentityDisplayNoDependencies}");
                sb.AppendLine("================================================================================");
                commands.ForEach(c => sb.AppendLine(c));
                var display = sb.ToString();
                Console.WriteLine(display);
            }

            Logger.LogDebug($"Finished {nameof(HandlePowerShell)}");

            return commands;
        }
    }
}