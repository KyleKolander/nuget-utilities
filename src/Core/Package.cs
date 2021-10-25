using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

// ReSharper disable MemberCanBePrivate.Global

namespace NuGetUtilities.Core
{
    public class Package : IComparable<Package>, IEquatable<Package>

    {
        public Package(SourcePackageDependencyInfo sourcePackageDependencyInfo, NuGetFramework nuGetFramework)
        {
            PackageIdentity = sourcePackageDependencyInfo;
            NuGetFramework  = nuGetFramework;
            DownloadUri     = sourcePackageDependencyInfo.DownloadUri;
            Source          = sourcePackageDependencyInfo.Source;
            Dependencies    = sourcePackageDependencyInfo.Dependencies.ToList();
        }

        public Package(PackageIdentity packageIdentity, NuGetFramework nuGetFramework, IEnumerable<PackageDependency> dependencies)
        {
            PackageIdentity = packageIdentity;
            NuGetFramework  = nuGetFramework;
            Dependencies    = dependencies.ToList();
        }

        public Package(Package package)
            : this(package.PackageIdentity, package.NuGetFramework, package.Dependencies) { }

        public PackageIdentity         PackageIdentity { get; }
        public NuGetFramework          NuGetFramework  { get; }
        public Uri?                    DownloadUri     { get; }
        public SourceRepository?       Source          { get; }
        public List<PackageDependency> Dependencies    { get; }

        public string PackageIdentityDisplayNoDependencies
        {
            get
            {
                var packageIdentity = new PackageIdentity(PackageIdentity.Id, PackageIdentity.Version);
                return packageIdentity.ToString().ToLower();
            }
        }

        #region IEquatable

        public bool Equals(Package? obj)
        {
            return obj is not null
                && PackageIdentity.Equals(obj.PackageIdentity)
                && NuGetFramework.Equals(obj.NuGetFramework);
        }

        #endregion

        #region IComparable

        protected CaseInsensitiveComparer Comparer { get; } = new();

        public int CompareTo(Package? obj)
        {
            var result = PackageIdentity.CompareTo(obj?.PackageIdentity);
            return result != 0 ? result : Comparer.Compare(NuGetFramework.GetShortFolderName(), obj?.NuGetFramework.GetShortFolderName());
        }

        #endregion

        #region Object overrides

        public override bool Equals(object? obj)
        {
            return Equals(obj as Package);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PackageIdentity, NuGetFramework);
        }

        public override string ToString()
        {
            //var packageIdentity = new PackageIdentity(PackageIdentity.Id, PackageIdentity.Version);
            //return packageIdentity.ToString();
            return PackageIdentity.ToString();
        }

        #endregion
    }
}