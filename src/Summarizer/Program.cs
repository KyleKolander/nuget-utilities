using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGetUtilities.Core;
using ILogger = NuGet.Common.ILogger;

// ReSharper disable ClassNeverInstantiated.Global

namespace NuGetUtilities.Summarizer
{
    [ExcludeFromCodeCoverage]
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).RunConsoleAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                       .ConfigureHostConfiguration(configHost =>
                                                   {
                                                       configHost.SetBasePath(Directory.GetCurrentDirectory());
                                                       configHost.AddJsonFile("appsettings.json", true);
                                                       configHost.AddEnvironmentVariables("NuGetUtilitiesSummarizer_");
                                                       configHost.AddCommandLine(args);
                                                   })
                       .ConfigureLogging(logging =>
                                         {
                                             logging.AddConsole();
                                             logging.AddDebug();
                                         })
                       .ConfigureServices(services =>
                                          {
                                              services.AddHostedService<Summarizer>();
                                              services.AddSingleton<PackageRetriever>();
                                              services.AddSingleton<PackageInstaller>();
                                              services.AddSingleton<PackageReader>();
                                              services.AddTransient<DependencyTreeBuilder>();
                                              services.AddTransient<ILogger, NullLogger>();
                                          });
        }
    }
}