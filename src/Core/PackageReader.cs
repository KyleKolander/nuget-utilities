using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGetUtilities.Core.Enums;
using NuGetUtilities.Core.Extensions;

namespace NuGetUtilities.Core
{
    public class PackageReader
    {
        public PackageReader(ILogger<PackageReader> logger)
        {
            Logger = logger;
        }

        private ILogger<PackageReader> Logger { get; }

        public InstalledPackageContents Read(InstalledPackage installedPackage)
        {
            Logger.LogDebug($"{installedPackage.PackageIdentity}");

            using var packageFolderReader = new PackageFolderReader(installedPackage.InstalledPath);

            var nupkgFilePath = Path.Combine(installedPackage.InstalledPath, $"{installedPackage.PackageIdentityDisplayNoDependencies}.nupkg");
            Logger.LogDebug(nupkgFilePath.Indent(1));

            var nuspecFilePath = packageFolderReader.GetNuspecFile();
            Logger.LogDebug(nuspecFilePath.Indent(1));
            var nuspecReader = packageFolderReader.NuspecReader;

            var allLibItems              = packageFolderReader.GetLibItems().ToList();
            var nearestFrameworkLibItems = GetNearestFramework(PackageItemType.Lib, allLibItems, installedPackage.NuGetFramework);
            var libItems                 = FilterByFramework(PackageItemType.Lib, allLibItems, installedPackage.InstalledPath, nearestFrameworkLibItems);

            var allFrameworkItems              = packageFolderReader.GetFrameworkItems().ToList();
            var nearestFrameworkFrameworkItems = GetNearestFramework(PackageItemType.Framework, allFrameworkItems, installedPackage.NuGetFramework);
            var frameworkItems                 = FilterByFramework(PackageItemType.Framework, allFrameworkItems, installedPackage.InstalledPath, nearestFrameworkFrameworkItems);

            var nearestFramework = nearestFrameworkLibItems?.GetShortFolderName() ?? nearestFrameworkFrameworkItems?.GetShortFolderName() ?? "";

            var installedPackageContents = new InstalledPackageContents(installedPackage, nupkgFilePath, nuspecFilePath, nuspecReader, libItems, frameworkItems, nearestFramework);

            return installedPackageContents;
        }

        private NuGetFramework? GetNearestFramework(PackageItemType itemType, IList<FrameworkSpecificGroup> allItems, NuGetFramework packageFramework)
        {
            var frameworkReducer = new FrameworkReducer();
            var targetFrameworks = allItems.Select(x => x.TargetFramework).ToList();
            var nearestFramework = frameworkReducer.GetNearest(packageFramework, targetFrameworks);
            Logger.LogDebug($"{itemType.ToString().Indent(1)} nearest framework folder: {(nearestFramework is not null ? nearestFramework.GetShortFolderName() : "n/a")}");
            return nearestFramework;
        }

        private PackageItems FilterByFramework(PackageItemType itemType, IList<FrameworkSpecificGroup> groupItems, string installedPath, NuGetFramework? nearestFramework)
        {
            PackageItems packageItems = new();

            var nearestFrameworkSpecificGroup = groupItems.FirstOrDefault(x => x.TargetFramework.Equals(nearestFramework));
            var isMatch                       = nearestFrameworkSpecificGroup is not null;

            var targetFrameworks = string.Join(", ", groupItems.Select(i => i.TargetFramework.GetShortFolderName()));
            Logger.LogDebug($"{itemType.ToString().Indent(1)} folders: {targetFrameworks}");

            if (!isMatch)
            {
                Logger.LogDebug("0 items".Indent(2));
            }
            else
            {
                var nearestFrameworkName = nearestFramework!.GetShortFolderName();
                Logger.LogDebug($"{nearestFrameworkSpecificGroup!.Items.Count().ToString().Indent(2)} items");

                foreach (var relativePath in nearestFrameworkSpecificGroup!.Items)
                {
                    var fullPath = packageItems.AddItem(itemType
                                                      , nearestFrameworkName
                                                      , installedPath
                                                      , relativePath.Replace('/', Path.DirectorySeparatorChar));
                    Logger.LogDebug(fullPath.Indent(3));
                }
            }
            return packageItems;
        }

        //var metadata    = nuspecReader.GetMetadata().ToList();
        //var authors     = GetNuspecValue(metadata, "authors").Split(',').ToArray();
        //var title       = GetNuspecValue(metadata, "title");
        //var description = GetNuspecValue(metadata, "description");
        //
        //private static string GetNuspecValue(IEnumerable<KeyValuePair<string, string>> metadata, string field)
        //{
        //    var node = metadata.FirstOrDefault(e => StringComparer.Ordinal.Equals(field, (string?)e.Key));

        //    return node.Value ?? string.Empty;
        //}
    }
}