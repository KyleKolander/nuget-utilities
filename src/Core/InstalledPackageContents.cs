using NuGet.Packaging;

namespace NuGetUtilities.Core
{
    public class InstalledPackageContents : InstalledPackage
    {
        public InstalledPackageContents(InstalledPackage installedPackage, string nupkgFilePath, string nuspecFilePath, NuspecReader nuspecReader, PackageItems libItems, PackageItems frameworkItems, string nearestFramework)
            : base(installedPackage)
        {
            NupkgFilePath    = nupkgFilePath;
            NuspecFilePath   = nuspecFilePath;
            NuspecReader     = nuspecReader;
            LibItems         = libItems;
            FrameworkItems   = frameworkItems;
            NearestFramework = nearestFramework;
        }

        public string       NupkgFilePath    { get; }
        public string       NuspecFilePath   { get; }
        public NuspecReader NuspecReader     { get; }
        public PackageItems LibItems         { get; }
        public PackageItems FrameworkItems   { get; }
        public string       NearestFramework { get; }
    }
}