using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGetUtilities.Core;
using NuGetUtilities.Core.Enums;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace NuGetUtilities.Test.Integration
{
    [ExcludeFromCodeCoverage]
    [Category("Integration")]
    [TestFixture]
    public class PackageTests
    {
        [Test]
        public async Task InstallSinglePackageTest()
        {
            // ARRANGE

            // ACT
            var mailKitPackage          = await SetupFixture.PackageRetriever!.Retrieve(SetupFixture.MailKitPackageIdentity, SetupFixture.Net5NuGetFramework);
            var mailKitPackageInstalled = await SetupFixture.PackageInstaller.Install(mailKitPackage!);

            // ASSERT
            Assert.NotNull(mailKitPackageInstalled);
        }

        [Test]
        public async Task InstallSinglePackageThrowsExceptionTest()
        {
            // ARRANGE

            // ACT
            SetupFixture.PackageInstaller.Uninstall(SetupFixture.NewtonsoftJsonPackageIdentity);
            var newtonsoftJsonPackage         = await SetupFixture.PackageRetriever!.Retrieve(SetupFixture.NewtonsoftJsonPackageIdentity, SetupFixture.Net5NuGetFramework);
            var newtonsoftJsonPackageIdentity = new PackageIdentity(newtonsoftJsonPackage!.PackageIdentity.Id, newtonsoftJsonPackage.PackageIdentity.Version);
            var badNewtonsoftJsonPackage      = new Package(newtonsoftJsonPackageIdentity, newtonsoftJsonPackage.NuGetFramework, newtonsoftJsonPackage.Dependencies);

            // ASSERT
            Assert.ThrowsAsync<Exception>(async () => await SetupFixture.PackageInstaller.Install(badNewtonsoftJsonPackage!));
        }

        [Test]
        public async Task InstallSinglePackageFromNupkgFileTest()
        {
            // ARRANGE
            var tempNupkgFilePath = "";

            // ACT and ASSERT
            try
            {
                var package          = await SetupFixture.PackageRetriever!.Retrieve(SetupFixture.NuGetFrameworksPackageIdentity, SetupFixture.Net5NuGetFramework);
                var packageInstalled = await SetupFixture.PackageInstaller.Install(package!);
                var packageContents  = SetupFixture.PackageReader.Read(packageInstalled!);
                var installedPath    = packageInstalled.InstalledPath;
                tempNupkgFilePath = Path.GetTempFileName();
                File.Delete(tempNupkgFilePath);
                File.Copy(packageContents.NupkgFilePath, tempNupkgFilePath);

                var              retryCount                = 0;
                InstalledPackage packageInstalledFromNupkg = null;
                while (retryCount < 30 && packageInstalledFromNupkg is null)
                {
                    try
                    {
                        packageInstalledFromNupkg = await SetupFixture.PackageInstaller.Install(tempNupkgFilePath, SetupFixture.Net5NuGetFramework, CancellationToken.None);
                    }
                    catch (IOException e)
                    {
                        retryCount++;
                        await TestExecutionContext.CurrentContext.OutWriter.WriteLineAsync($"Failed to install the package on attempt {retryCount} at {DateTime.UtcNow.ToLongTimeString()}.");
                        await TestExecutionContext.CurrentContext.OutWriter.WriteLineAsync(e.Message);
                        await Task.Delay(TimeSpan.FromSeconds(5));
                    }
                }
                Assert.AreEqual(installedPath.ToLower(), packageInstalledFromNupkg?.InstalledPath.ToLower());
                Assert.True(Directory.Exists(installedPath));
            }
            finally
            {
                if (File.Exists(tempNupkgFilePath))
                {
                    File.Delete(tempNupkgFilePath);
                }
            }
        }

        [Test]
        public async Task RetrieveTransitiveDependenciesFromPackageIdentityTest()
        {
            // ARRANGE

            // ACT
            var transitiveDependencies = await SetupFixture.PackageRetriever!.RetrieveTransitiveDependencies(SetupFixture.AngleSharpPackageIdentity, SetupFixture.Net5NuGetFramework);

            // ASSERT
            Assert.IsNotNull(transitiveDependencies);
            Assert.That(transitiveDependencies.Count() == 4);
        }

        [Test]
        public async Task BuildDependencyTreeTest()
        {
            // ARRANGE
            var configuration = "Release";
#if (DEBUG)
            configuration = "Debug";
#endif
            var version           = typeof(PackageItemType).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var solutionRoot      = new DirectoryInfo(SetupFixture.GetThisFilePath()).Parent!.Parent!.Parent!.ToString();
            var csprojPath        = Path.Combine(solutionRoot, "src",    "Core", "Core.csproj");
            var outDirPack        = Path.Combine(solutionRoot, "nupkgs", "test");
            var nupkgFilePath     = Path.Combine(outDirPack,   $"NuGetUtilities.Core.{version}.nupkg");
            var nuGetFramework    = NuGetFramework.Parse("net5.0");
            var runtimeIdentifier = GetRuntimeIdentifier() ?? "win-x64";
            var packArgs          = $"pack --configuration {configuration} --runtime {runtimeIdentifier} -p:Version={version} --nologo --include-symbols --include-source --output \"{outDirPack}\" \"{csprojPath}\"";
            var info = new ProcessStartInfo
                       {
                           UseShellExecute        = false
                         , CreateNoWindow         = false
                         , RedirectStandardOutput = true
                         , WorkingDirectory       = solutionRoot
                         , FileName               = "dotnet"
                         , Arguments              = packArgs
                       };
            var process = Process.Start(info);
            if (process is null)
            {
                Assert.Fail("Failed to create process to run the dotnet pack command");
            }

            process.WaitForExit(30000);
            await TestExecutionContext.CurrentContext.OutWriter.WriteLineAsync($"dotnet {packArgs}");
            while (!process.StandardOutput.EndOfStream)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                await TestExecutionContext.CurrentContext.OutWriter.WriteLineAsync(line);
            }

            // ACT
            var package                = await SetupFixture.PackageInstaller.Install(nupkgFilePath, nuGetFramework, CancellationToken.None);
            var transitiveDependencies = (await SetupFixture.PackageRetriever.RetrieveTransitiveDependencies(package, nuGetFramework)).ToList();
            var rootNode               = SetupFixture.DependencyTreeBuilder.Build(package, transitiveDependencies);
            var display                = SetupFixture.DependencyTreeBuilder.Display;

            // ASSERT
            Assert.IsNotNull(rootNode);
            Assert.That(display.Length > 0);
        }

        [Test]
        public async Task ComparePackagesTest()
        {
            // ARRANGE

            // ACT
            var mailKitPackage = await SetupFixture.PackageRetriever!.Retrieve(SetupFixture.MailKitPackageIdentity, SetupFixture.Net5NuGetFramework);
            var mimeKitPackage = await SetupFixture.PackageRetriever!.Retrieve(SetupFixture.MimeKitPackageIdentity, SetupFixture.Net5NuGetFramework);

            var mailKitInstalledPackage = await SetupFixture.PackageInstaller!.Install(mailKitPackage!);
            var mimeKitInstalledPackage = await SetupFixture.PackageInstaller!.Install(mimeKitPackage!);

            // ASSERT
            Assert.NotNull(mailKitPackage);
            Assert.NotNull(mimeKitPackage);

            Assert.NotNull(mailKitInstalledPackage);
            Assert.NotNull(mimeKitInstalledPackage);

            Assert.IsFalse(mailKitPackage.Equals(mimeKitPackage));
            Assert.IsTrue(mailKitPackage.Equals(mailKitPackage));
            Assert.IsTrue(mailKitPackage.Equals((object)mailKitPackage));

            Assert.IsFalse(mailKitInstalledPackage.Equals(mimeKitInstalledPackage));
            Assert.IsTrue(mailKitInstalledPackage.Equals(mailKitInstalledPackage));
            Assert.IsTrue(mailKitInstalledPackage.Equals((object)mailKitInstalledPackage));

            Assert.AreNotEqual(mailKitInstalledPackage.GetHashCode(), mimeKitInstalledPackage.GetHashCode());
            Assert.AreNotEqual(mailKitInstalledPackage.GetHashCode(), mimeKitInstalledPackage.GetHashCode());

            Assert.IsTrue(mailKitPackage.CompareTo(mimeKitPackage) < 0);
            Assert.AreEqual(0, mailKitPackage.CompareTo(mailKitPackage));
            Assert.IsTrue(mimeKitPackage.CompareTo(mailKitPackage) > 0);

            Assert.IsTrue(mailKitInstalledPackage.CompareTo(mimeKitInstalledPackage) < 0);
            Assert.AreEqual(0, mailKitInstalledPackage.CompareTo(mailKitInstalledPackage));
            Assert.IsTrue(mimeKitInstalledPackage.CompareTo(mailKitInstalledPackage) > 0);
        }

        /// <summary>
        ///     Returns the current RID (Runtime IDentifier) where this applications runs.
        ///     See https://docs.microsoft.com/en-us/dotnet/core/rid-catalog for possible values, e.g. "ubuntu.18.04-x64".
        ///     The value is read from the first found .deps.json file in the application folder, at the path
        ///     "runtimeTarget"/"name" the value behind the last "/".
        ///     When the file or the value behind the last "/" is missing, this application folder was not compiled
        ///     for a specific runtime, and null is returned.
        ///     Credit: https://stackoverflow.com/a/60835275
        /// </summary>
        public static string GetRuntimeIdentifier()
        {
            try
            {
                // Find first (and probably only) .deps.json file in the application's folder.
                var dir   = AppDomain.CurrentDomain.BaseDirectory;
                var files = Directory.GetFiles(dir, "*.deps.json");
                if (files.Length == 0)
                {
                    return null;
                }

                // Read JSON content
                var json = JObject.Parse(File.ReadAllText(Path.Combine(dir, files[0])));
                var name = json["runtimeTarget"]["name"].ToString();

                // Read RID after slash
                var slashPos = name.LastIndexOf('/');
                return slashPos == -1 ? null : name[(slashPos + 1)..];
            }
            catch
            {
                // Unexpected file format or other problem
                return null;
            }
        }
    }
}