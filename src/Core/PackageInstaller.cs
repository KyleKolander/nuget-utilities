using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.PackageExtraction;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGetUtilities.Core.Extensions;
using ILogger = NuGet.Common.ILogger;

// ReSharper disable MemberCanBePrivate.Global

namespace NuGetUtilities.Core
{
    public class PackageInstaller
    {
        public PackageInstaller(ILogger<PackageInstaller> logger, ILogger nuGetLogger)
        {
            Logger      = logger;
            NuGetLogger = nuGetLogger;

            var nuGetSettings = Settings.LoadDefaultSettings(null);
            var cacheContext  = new SourceCacheContext();

            PackageDownloadContext = new(cacheContext);
            GlobalPackagesFolder   = SettingsUtility.GetGlobalPackagesFolder(nuGetSettings);
            PackagePathResolver    = new(GlobalPackagesFolder, false);
        }

        private ILogger<PackageInstaller> Logger                 { get; }
        private ILogger                   NuGetLogger            { get; }
        private PackageDownloadContext    PackageDownloadContext { get; }
        public  string                    GlobalPackagesFolder   { get; }
        private PackagePathResolver       PackagePathResolver    { get; }

        public async Task<InstalledPackage?> Install(Package package)
        {
            return await Install(package, package.PackageIdentity.ToString().Length, GetInstallPath(package.PackageIdentity).Length);
        }

        public async Task<List<InstalledPackage>> Install(List<Package> packages)
        {
            List<InstalledPackage> installedPackages = new();

            var maxLengthPackageIdentity = packages.Max(p => p.PackageIdentity.ToString().Length);
            var maxLengthInstallPath     = packages.Max(p => GetInstallPath(p.PackageIdentity).Length);

            foreach (var package in packages)
            {
                var installedPackage = await Install(package, maxLengthPackageIdentity, maxLengthInstallPath);
                installedPackages.Add(installedPackage);
            }

            return installedPackages;
        }

        public async Task<InstalledPackage> Install(string nupkgFilePath, NuGetFramework targetFramework, CancellationToken token)
        {
            ILogger                   logger                    = NullLogger.Instance;
            VersionFolderPathResolver versionFolderPathResolver = new(GlobalPackagesFolder);
            SourceCacheContext        cacheContext              = new();
            PackageDownloadContext    downloadContext           = new(cacheContext);
            ClientPolicyContext       clientPolicyContext       = downloadContext.ClientPolicyContext;
            PackageExtractionContext  packageExtractionContext  = new(PackageSaveMode.Defaultv3, PackageExtractionBehavior.XmlDocFileSaveMode, clientPolicyContext, logger);
            var                       packageSaveMode           = packageExtractionContext.PackageSaveMode;

            await using FileStream     nupkgStream   = new(nupkgFilePath, FileMode.Open, FileAccess.Read);
            using PackageArchiveReader packageReader = new(nupkgStream);

            PackageIdentity packageIdentity       = packageReader.NuspecReader.GetIdentity();
            string          targetPath            = versionFolderPathResolver.GetInstallPath(packageIdentity.Id, packageIdentity.Version);
            string          targetNuspec          = versionFolderPathResolver.GetManifestFilePath(packageIdentity.Id, packageIdentity.Version);
            string          targetNupkg           = versionFolderPathResolver.GetPackageFilePath(packageIdentity.Id, packageIdentity.Version);
            string          hashPath              = versionFolderPathResolver.GetHashPath(packageIdentity.Id, packageIdentity.Version);
            string          nupkgMetadataFilePath = versionFolderPathResolver.GetNupkgMetadataPath(packageIdentity.Id, packageIdentity.Version);
            string          tempHashPath          = Path.Combine(targetPath, Path.GetRandomFileName());

            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, true);
            }

            string nuspecFile = await packageReader.GetNuspecFileAsync(token);

            if ((packageSaveMode & PackageSaveMode.Nuspec) == PackageSaveMode.Nuspec)
            {
                packageReader.ExtractFile(nuspecFile, targetNuspec, logger);
            }

            if ((packageSaveMode & PackageSaveMode.Files) == PackageSaveMode.Files)
            {
                string               hashFileName          = Path.GetFileName(hashPath);
                string               nupkgMetadataFileName = Path.GetFileName(nupkgMetadataFilePath);
                List<string>         packageFiles          = packageReader.GetFiles().Where(file => ShouldInclude(file, hashFileName, nupkgMetadataFileName)).ToList();
                PackageFileExtractor packageFileExtractor  = new(packageFiles, packageExtractionContext.XmlDocFileSaveMode);
                packageReader.CopyFiles(targetPath, packageFiles, packageFileExtractor.ExtractPackageFile, logger, token);
            }

            nupkgStream.Position = 0L;
            string packageHash = Convert.ToBase64String(new CryptoHashProvider("SHA512").CalculateHash(nupkgStream));
            await File.WriteAllTextAsync(tempHashPath, packageHash, token);

            if ((packageExtractionContext.PackageSaveMode & PackageSaveMode.Nupkg) == PackageSaveMode.Nupkg)
            {
                File.Copy(nupkgFilePath, targetNupkg);
            }

            File.Move(tempHashPath, hashPath);
            LocalFolderUtility.GenerateNupkgMetadataFile(targetNupkg, targetPath, hashPath, nupkgMetadataFilePath);

            await using var nuspecStream = await packageReader.GetNuspecAsync(token);

            var nuspecReader                 = packageReader.NuspecReader;
            var allDependencies              = nuspecReader.GetDependencyGroups(true).ToList();
            var frameworkReducer             = new FrameworkReducer();
            var targetFrameworks             = allDependencies.Select(x => x.TargetFramework).ToList();
            var nearestFramework             = frameworkReducer.GetNearest(targetFramework, targetFrameworks);
            var nearestFrameworkDependencies = allDependencies.First(d => d.TargetFramework.Equals(nearestFramework)).Packages;

            Package          package          = new(packageIdentity, targetFramework, nearestFrameworkDependencies);
            InstalledPackage installedPackage = new(package, targetPath);

            return installedPackage;
        }

        public void Uninstall(PackageIdentity packageIdentity)
        {
            var installedPath = GetInstalledPath(packageIdentity);
            if (!string.IsNullOrWhiteSpace(installedPath))
            {
                Directory.Delete(installedPath, true);
            }
        }

        public bool IsInstalled(PackageIdentity packageIdentity)
        {
            var installedPath = GetInstalledPath(packageIdentity);
            return !string.IsNullOrWhiteSpace(installedPath);
        }

        public string GetInstalledPath(PackageIdentity packageIdentity)
        {
            // This has a bug when not using side-by-side installation - it doesn't append the version to the path.  So I wrote this method.
            //    PackagePathResolver.GetInstalledPath(package.PackageIdentity);

            var installedPath = GetInstallPath(packageIdentity);
            if (!Directory.Exists(installedPath))
            {
                installedPath = "";
            }
            return installedPath;
        }

        public string GetInstallPath(PackageIdentity packageIdentity)
        {
            var installPath = PackagePathResolver.GetInstallPath(packageIdentity);
            return Path.Combine(installPath, packageIdentity.Version.ToNormalizedString()).ToLower();
        }

        private async Task<InstalledPackage> Install(Package package, int maxLengthPackageIdentity, int maxLengthInstallPath)
        {
            var packageIdentityDisplay = package.PackageIdentity.ToString().PadRightOrTruncate(maxLengthPackageIdentity);
            var installPath            = GetInstallPath(package.PackageIdentity);
            var installPathDisplay     = installPath.PadRightOrTruncate(maxLengthInstallPath);

            if (IsInstalled(package.PackageIdentity))
            {
                Logger.LogDebug($"{packageIdentityDisplay}  was already installed at: {installPath}");
                return new(package, installPath);
            }

            if (package.PackageIdentity is not SourcePackageDependencyInfo sourcePackageDependencyInfo)
            {
                throw new("Package must contain a SourcePackageDependencyInfo to install it from a repository");
            }

            var downloadResource   = await sourcePackageDependencyInfo.Source.GetResourceAsync<DownloadResource>(CancellationToken.None);
            var downloadResourceV3 = (DownloadResourceV3)downloadResource;
            _ = await downloadResourceV3.GetDownloadResourceResultAsync(sourcePackageDependencyInfo
                                                                      , PackageDownloadContext
                                                                      , GlobalPackagesFolder
                                                                      , NuGetLogger
                                                                      , CancellationToken.None);
            Logger.LogDebug($"{packageIdentityDisplay}              installed to: {installPathDisplay}   from: {package.DownloadUri}");
            return new(sourcePackageDependencyInfo, package.NuGetFramework, installPath);
        }

        private static bool ShouldInclude(
            string fullName,
            string hashFileName,
            string nupkgMetadataFileName)
        {
            return Path.GetFileName(fullName) switch
                   {
                       ".rels"               => false
                     , "[Content_Types].xml" => false
                     , _ => Path.GetExtension(fullName) != ".psmdcp"
                         && !string.Equals(fullName, hashFileName, StringComparison.OrdinalIgnoreCase)
                         && (!PackageHelper.IsRoot(fullName) || !PackageHelper.IsNuspec(fullName) && !fullName.EndsWith(PackagingCoreConstants.NupkgExtension, StringComparison.OrdinalIgnoreCase))
                         && !string.Equals(fullName, nupkgMetadataFileName, StringComparison.OrdinalIgnoreCase)
                   };
        }
    }
}