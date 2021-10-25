using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using NuGet.Packaging.Core;
using NuGetUtilities.Core.Extensions;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InvertIf

namespace NuGetUtilities.Core
{
    public class DependencyTreeNode
    {
        public DependencyTreeNode(DependencyTreeNode? parentNode, Package package, PackageDependency? packageDependency)
        {
            ParentNode        = parentNode;
            Package           = package;
            PackageDependency = packageDependency;
        }

        public DependencyTreeNode?      ParentNode        { get; }
        public Package                  Package           { get; }
        public PackageDependency?       PackageDependency { get; }
        public List<DependencyTreeNode> Children          { get; } = new();

        public DependencyTreeNode AddChildNode(DependencyTreeNode node)
        {
            Children.Add(node);
            return node;
        }

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            var sb      = new StringBuilder();
            var parents = new Stack<DependencyTreeNode>();
            var parent  = ParentNode;

            while (parent is not null)
            {
                parents.Push(parent);
                parent = parent.ParentNode;
            }

            while (parents.TryPop(out var node))
            {
                sb.Append($"{node.Package.PackageIdentity} => ");
            }

            sb.Append(Package.PackageIdentity);

            if (Children.Any())
            {
                sb.Append($"{Children.Count.ToString().Indent(1)} Direct Dependencies [");
                foreach (var child in Children)
                {
                    sb.Append($" {child.Package.PackageIdentity} ");
                }
                sb.Append(']');
            }

            return sb.ToString();
        }
    }
}