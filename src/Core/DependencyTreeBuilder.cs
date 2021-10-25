using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using NuGet.Packaging.Core;
using NuGetUtilities.Core.Extensions;

// ReSharper disable InvertIf

namespace NuGetUtilities.Core
{
    public class DependencyTreeBuilder
    {
        public DependencyTreeBuilder(ILogger<DependencyTreeBuilder> logger)
        {
            Logger = logger;
        }

        private ILogger<DependencyTreeBuilder> Logger  { get; }
        public  string                         Display { get; private set; } = "";

        public DependencyTreeNode Build(Package rootPackage, IEnumerable<Package> transitiveDependencies)
        {
            Logger.LogDebug("Started building dependency tree");

            var transitiveDependenciesLookup = transitiveDependencies.ToDictionary(d => d.PackageIdentity.Id);
            var rootNode                     = new DependencyTreeNode(null, rootPackage, null);
            var sb                           = new StringBuilder();
            var message                      = $"{rootNode.Package.PackageIdentityDisplayNoDependencies}  {{{rootNode.Package.NuGetFramework.GetShortFolderName()}}}";

            sb.AppendLine(message);
            Logger.LogDebug(message);

            foreach (var dependency in rootNode.Package.Dependencies!)
            {
                Build(rootNode, dependency, transitiveDependenciesLookup, 1, sb);
            }

            Display = sb.ToString();

            Logger.LogDebug("Finished building dependency tree");

            return rootNode;
        }

        private void Build(DependencyTreeNode parentNode, PackageDependency packageDependency, IReadOnlyDictionary<string, Package> transitiveDependenciesLookup, int level, StringBuilder sb)
        {
            var package = transitiveDependenciesLookup[packageDependency.Id];
            var node    = parentNode.AddChildNode(new(parentNode, package, packageDependency));
            var message = $"{node.Package.PackageIdentityDisplayNoDependencies.Indent(level)}  {node.PackageDependency!.VersionRange}";

            sb.AppendLine(message);
            Logger.LogDebug(message);

            foreach (var dependency in node.Package.Dependencies!)
            {
                Build(node, dependency, transitiveDependenciesLookup, level + 1, sb);
            }
        }
    }
}