using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using NuGetUtilities.Core;
using NUnit.Framework;
using ILogger = NuGet.Common.ILogger;

namespace NuGetUtilities.Test.Integration
{
    [ExcludeFromCodeCoverage]
    [SetUpFixture]
    public class SetupFixture
    {
        public static ServiceProvider       ServiceProvider                { get; set; }
        public static IConfigurationRoot    ConfigurationRoot              { get; set; }
        public static PackageRetriever      PackageRetriever               { get; set; }
        public static PackageInstaller      PackageInstaller               { get; set; }
        public static PackageReader         PackageReader                  { get; set; }
        public static DependencyTreeBuilder DependencyTreeBuilder          { get; set; }
        public static string                GlobalPackagesFolder           { get; set; }
        public static PackageIdentity       MailKitPackageIdentity         { get; set; }
        public static PackageIdentity       MimeKitPackageIdentity         { get; set; }
        public static PackageIdentity       NewtonsoftJsonPackageIdentity  { get; set; }
        public static PackageIdentity       AngleSharpPackageIdentity      { get; set; }
        public static PackageIdentity       NuGetFrameworksPackageIdentity { get; set; }
        public static NuGetFramework        Net5NuGetFramework             { get; set; }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            ConfigurationRoot = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                          .AddJsonFile("appsettings.json", false, false)
                                                          .AddEnvironmentVariables("NuGetUtilitiesSummarizerIntegrationTests_")
                                                          .Build();

            ServiceProvider = new ServiceCollection().AddSingleton(_ => ConfigurationRoot)
                                                     .AddLogging(logging =>
                                                                 {
                                                                     logging.AddConsole();
                                                                     logging.AddDebug();
                                                                 })
                                                     .AddTransient<Summarizer.Summarizer>()
                                                     .AddSingleton<PackageRetriever>()
                                                     .AddSingleton<PackageInstaller>()
                                                     .AddSingleton<PackageReader>()
                                                     .AddTransient<DependencyTreeBuilder>()
                                                     .AddTransient<ILogger, NullLogger>()
                                                     .AddTransient(_ => (IHostApplicationLifetime)null)
                                                     .BuildServiceProvider();

            PackageRetriever      = ServiceProvider.GetService<PackageRetriever>();
            PackageInstaller      = ServiceProvider.GetService<PackageInstaller>();
            PackageReader         = ServiceProvider.GetService<PackageReader>();
            DependencyTreeBuilder = ServiceProvider.GetService<DependencyTreeBuilder>();
            GlobalPackagesFolder  = Path.TrimEndingDirectorySeparator(PackageInstaller!.GlobalPackagesFolder);

            MailKitPackageIdentity         = new("MailKit", NuGetVersion.Parse("2.15.0"));
            MimeKitPackageIdentity         = new("MimeKit", NuGetVersion.Parse("2.15.0"));
            NewtonsoftJsonPackageIdentity  = new("NewtonSoft.Json", NuGetVersion.Parse("13.0.1"));
            AngleSharpPackageIdentity      = new("AngleSharp", NuGetVersion.Parse("0.16.0"));
            NuGetFrameworksPackageIdentity = new("NuGet.Frameworks", NuGetVersion.Parse("5.11.0"));

            Net5NuGetFramework = NuGetFramework.ParseFolder("net5.0");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ServiceProvider?.Dispose();
        }

        internal static string GetThisFilePath([CallerFilePath] string path = null)
        {
            return path;
        }
    }
}