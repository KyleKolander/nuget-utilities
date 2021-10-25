using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NuGetUtilities.Core.Enums;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global

namespace NuGetUtilities.Core
{
    public class PackageItems
    {
        public List<PackageItem> Items      { get; } = new();
        public List<PackageItem> Assemblies => Items.Where(i => i.IsAssembly).ToList();

        public string AddItem(PackageItemType itemType, string framework, string installedPath, string relativePath)
        {
            var packageItem = new PackageItem(itemType, framework, installedPath, relativePath);
            Items.Add(packageItem);
            return packageItem.FullPath;
        }

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            return $"{Items.Count} items";
        }
    }
}