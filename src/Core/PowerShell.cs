using System.Collections.Generic;
using System.Linq;

namespace NuGetUtilities.Core
{
    public static class PowerShell
    {
        public static IEnumerable<string> GetAddTypeCommands(IEnumerable<InstalledPackageContents> installedPackages)
        {
            var assemblyItems = installedPackages.SelectMany(p => p.LibItems.Assemblies);

            return assemblyItems.Select(assemblyItem => $"Add-Type -Path \'{assemblyItem.FullPath}\'");
        }
    }
}