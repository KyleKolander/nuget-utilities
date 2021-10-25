using System;
using System.Diagnostics.CodeAnalysis;
using NuGet.Frameworks;
using NuGet.Protocol.Core.Types;

namespace NuGetUtilities.Core
{
    public class InstalledPackage : Package, IComparable<InstalledPackage>, IEquatable<InstalledPackage>
    {
        public InstalledPackage(SourcePackageDependencyInfo sourcePackageDependencyInfo, NuGetFramework nuGetFramework, string installedPath)
            : base(sourcePackageDependencyInfo, nuGetFramework)
        {
            InstalledPath = installedPath;
        }

        public InstalledPackage(Package package, string installedPath)
            : base(package)
        {
            InstalledPath = installedPath;
        }

        protected InstalledPackage(InstalledPackage installedPackage)
            : this(installedPackage, installedPackage.InstalledPath) { }

        public string InstalledPath { get; }

        #region IEquatable

        public bool Equals(InstalledPackage? obj)
        {
            return obj is not null &&
                   base.Equals(obj);
        }

        #endregion

        #region IComparable

        public int CompareTo(InstalledPackage? obj)
        {
            var result = base.CompareTo(obj);
            return result != 0 ? result : Comparer.Compare(InstalledPath, obj?.InstalledPath);
        }

        #endregion

        #region Object overrides

        public override bool Equals(object? obj)
        {
            return Equals(obj as InstalledPackage);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), InstalledPath);
        }

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            return PackageIdentity.ToString();
        }

        #endregion
    }
}