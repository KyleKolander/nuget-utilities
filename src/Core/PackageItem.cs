using System.Diagnostics.CodeAnalysis;
using System.IO;
using NuGetUtilities.Core.Enums;

// ReSharper disable MemberCanBePrivate.Global

namespace NuGetUtilities.Core
{
    public class PackageItem
    {
        public PackageItem(PackageItemType itemType, string framework, string installedPath, string relativePath)
        {
            ItemType      = itemType;
            Framework     = framework;
            InstalledPath = installedPath;
            RelativePath  = relativePath;
            FullPath      = Path.Combine(InstalledPath, RelativePath);
            IsAssembly    = RelativePath.ToLower().EndsWith(".dll");
        }

        public PackageItemType ItemType      { get; }
        public string          Framework     { get; }
        public string          InstalledPath { get; }
        public string          RelativePath  { get; }
        public string          FullPath      { get; }
        public bool            IsAssembly    { get; }

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            return $"{ItemType,9} -> {FullPath}";
        }
    }
}