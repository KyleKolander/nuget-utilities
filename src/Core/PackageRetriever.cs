using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;

// ReSharper disable InvertIf

namespace NuGetUtilities.Core
{
    public class PackageRetriever : IDisposable
    {
        public PackageRetriever(ILogger logger)
        {
            Logger = logger;

#pragma warning disable 618
            var nuGetSettings            = Settings.LoadDefaultSettings(null);
            var sourceRepositoryProvider = new SourceRepositoryProvider(nuGetSettings, Repository.Provider.GetCoreV3());
#pragma warning restore 618

            GlobalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(nuGetSettings);
            CacheContext         = new();
            Repositories         = sourceRepositoryProvider.GetRepositories().ToList();
            PackageSources       = sourceRepositoryProvider.GetRepositories().Select(s => s.PackageSource).ToList();
        }

        private ILogger                                    Logger                            { get; }
        public  string                                     GlobalPackagesFolder              { get; }
        private SourceCacheContext                         CacheContext                      { get; }
        private List<SourceRepository>                     Repositories                      { get; }
        private List<PackageSource>                        PackageSources                    { get; }
        private Dictionary<string, DependencyInfoResource> RepositoryDependencyInfoResources { get; } = new();

        public async Task<Package?> Retrieve(PackageIdentity packageIdentity, NuGetFramework nuGetFramework)
        {
            await LoadDependencyInfoResources();

            var packages = await Retrieve(packageIdentity, nuGetFramework, true);

            return packages.FirstOrDefault();
        }

        public async Task<IEnumerable<Package>> RetrieveTransitiveDependencies(PackageIdentity packageIdentity, NuGetFramework nuGetFramework)
        {
            await LoadDependencyInfoResources();

            HashSet<SourcePackageDependencyInfo> transitiveDependencies = new(PackageIdentityComparer.Default);

            await RetrieveTransitiveDependencies(packageIdentity, nuGetFramework, transitiveDependencies, null);

            var resolvedPackages = ResolvePackages(new[] {packageIdentity.Id}, transitiveDependencies);

            return resolvedPackages.Select(d => new Package(d, nuGetFramework));
        }

        public async Task<IEnumerable<Package>> RetrieveTransitiveDependencies(Package package, NuGetFramework nuGetFramework)
        {
            await LoadDependencyInfoResources();

            HashSet<SourcePackageDependencyInfo> transitiveDependencies = new(PackageIdentityComparer.Default);

            await RetrieveTransitiveDependencies(package.PackageIdentity, nuGetFramework, transitiveDependencies, package);

            // The NuGet PackageResolver requires that the root package be part of the PackageContext.AvailablePackages collection.
            // Since we don't have a SourcePackageDependencyInfo for it (local nupkg file), we'll have to fake it.
            // Create it, add it, resolve the packages, remove it (so the correct constructor is invoked) and add the original Package to the result.

            var rootPackage = new SourcePackageDependencyInfo(package.PackageIdentity.Id, package.PackageIdentity.Version, package.Dependencies, false, null);
            transitiveDependencies.Add(rootPackage);

            var resolvedPackages = ResolvePackages(new[] {package.PackageIdentity.Id}, transitiveDependencies).ToList();

            resolvedPackages.Remove(rootPackage);

            var result = resolvedPackages.Select(d => new Package(d, nuGetFramework)).ToList();

            result.Add(package);

            return result;
        }

        private async Task LoadDependencyInfoResources()
        {
            if (!RepositoryDependencyInfoResources.Any())
            {
                foreach (var repository in Repositories)
                {
                    RepositoryDependencyInfoResources[repository.ToString()] = await repository.GetResourceAsync<DependencyInfoResource>();
                }
            }
        }

        private async Task<IEnumerable<Package>> Retrieve(PackageIdentity packageIdentity, NuGetFramework nuGetFramework, bool onlyReturnFirst)
        {
            await LoadDependencyInfoResources();

            HashSet<SourcePackageDependencyInfo> packages = new(PackageIdentityComparer.Default);

            foreach (var repository in Repositories)
            {
                var sourcePackageDependencyInfo = await RepositoryDependencyInfoResources[repository.ToString()].ResolvePackage(packageIdentity, nuGetFramework, CacheContext, Logger, CancellationToken.None);
                if (sourcePackageDependencyInfo is not null)
                {
                    packages.Add(sourcePackageDependencyInfo);

                    if (onlyReturnFirst)
                    {
                        break;
                    }
                }
            }

            return packages.Select(p => new Package(p, nuGetFramework));
        }

        private async Task RetrieveTransitiveDependencies(PackageIdentity                   packageIdentity
                                                        , NuGetFramework                    nuGetFramework
                                                        , ISet<SourcePackageDependencyInfo> transitiveDependencies
                                                        , Package?                          rootPackage)
        {
            if (rootPackage is not null && packageIdentity.Equals(rootPackage.PackageIdentity))
            {
                foreach (var dependency in rootPackage.Dependencies!)
                {
                    await RetrieveTransitiveDependencies(new(dependency.Id, dependency.VersionRange.MinVersion), nuGetFramework, transitiveDependencies, rootPackage);
                }
                return;
            }

            if (!transitiveDependencies.Contains(packageIdentity))
            {
                var packages = await Retrieve(packageIdentity, nuGetFramework, false);

                foreach (var package in packages)
                {
                    transitiveDependencies.Add((SourcePackageDependencyInfo)package.PackageIdentity);
                    foreach (var dependency in package.Dependencies!)
                    {
                        await RetrieveTransitiveDependencies(new(dependency.Id, dependency.VersionRange.MinVersion), nuGetFramework, transitiveDependencies, rootPackage);
                    }
                }
            }
        }

        private IEnumerable<SourcePackageDependencyInfo> ResolvePackages(IEnumerable<string> packageIds, IReadOnlyCollection<SourcePackageDependencyInfo> transitiveDependencies)
        {
            PackageResolverContext context = new(DependencyBehavior.Lowest
                                               , packageIds
                                               , Enumerable.Empty<string>()
                                               , Enumerable.Empty<PackageReference>()
                                               , Enumerable.Empty<PackageIdentity>()
                                               , transitiveDependencies
                                               , PackageSources
                                               , Logger);
            var packageIdentities = new PackageResolver().Resolve(context, CancellationToken.None);
            return packageIdentities.Select(p => transitiveDependencies.Single(x => PackageIdentityComparer.Default.Equals(x, p)));
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                CacheContext?.Dispose();
            }
        }

        #endregion
    }
}