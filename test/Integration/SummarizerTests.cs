using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace NuGetUtilities.Test.Integration
{
    [ExcludeFromCodeCoverage]
    [Category("Integration")]
    [TestFixture]
    public class SummarizerTests
    {
        [SetUp]
        public void SetUp()
        {
            StringBuilder = new();
            StringWriter  = new(StringBuilder);
            Console.SetOut(StringWriter);
        }

        [TearDown]
        public async Task TearDown()
        {
            await StringWriter.DisposeAsync();
            await using var standardOutStream = Console.OpenStandardOutput();
            await using var standardOutStreamWriter = new StreamWriter(standardOutStream)
                                                      {
                                                          AutoFlush = true
                                                      };
            Console.SetOut(standardOutStreamWriter);
        }

        private StringBuilder StringBuilder { get; set; }
        private StringWriter  StringWriter  { get; set; }

        [Test]
        public async Task SummarizerTest()
        {
            // ARRANGE
            var summarizer               = SetupFixture.ServiceProvider.GetService<Summarizer.Summarizer>();
            var expectedSummarizerOutput = GetExpectedSummarizerOutput().ToLower();

            // ACT
            SetupFixture.PackageInstaller.Uninstall(SetupFixture.MimeKitPackageIdentity);
            await summarizer!.Run(SetupFixture.MailKitPackageIdentity.Id, SetupFixture.MailKitPackageIdentity.Version.ToNormalizedString(), SetupFixture.Net5NuGetFramework.GetShortFolderName(), "", true, true, true, true, true, true);
            var summarizerOutput = StringBuilder.ToString().ToLower();

            // ASSERT
            Assert.AreEqual(expectedSummarizerOutput, summarizerOutput);
        }

        private string GetExpectedSummarizerOutput()
        {
            return _expectedSummarizerOutputWithTokensToBeReplaced.Replace("GLOBAL_PACKAGES_FOLDER", SetupFixture.GlobalPackagesFolder).Replace('\\', Path.DirectorySeparatorChar);
        }

        private readonly string _expectedSummarizerOutputWithTokensToBeReplaced = @"
Successfully retrieved package: MailKit.2.15.0 {net5.0} from https://api.nuget.org/v3-flatcontainer/mailkit/2.15.0/mailkit.2.15.0.nupkg


Transitive Dependencies
============
Microsoft.NETCore.Platforms.2.0.0
Portable.BouncyCastle.1.8.10
System.Buffers.4.5.1
System.Reflection.TypeExtensions.4.4.0
System.Security.Cryptography.Cng.4.7.0
System.Security.Cryptography.Pkcs.4.7.0 : System.Security.Cryptography.Cng [4.7.0, )
System.Text.Encoding.CodePages.4.4.0 : Microsoft.NETCore.Platforms [2.0.0, )
MimeKit.2.15.0 : Portable.BouncyCastle [1.8.10, ), System.Buffers [4.5.1, ), System.Reflection.TypeExtensions [4.4.0, ), System.Security.Cryptography.Pkcs [4.7.0, ), System.Text.Encoding.CodePages [4.4.0, )
MailKit.2.15.0 : MimeKit [2.15.0, )


Dependency Tree
============
MailKit.2.15.0  {net5.0}
    MimeKit.2.15.0  [2.15.0, )
        System.Security.Cryptography.Pkcs.4.7.0  [4.7.0, )
            System.Security.Cryptography.Cng.4.7.0  [4.7.0, )
        System.Reflection.TypeExtensions.4.4.0  [4.4.0, )
        System.Text.Encoding.CodePages.4.4.0  [4.4.0, )
            Microsoft.NETCore.Platforms.2.0.0  [2.0.0, )
        System.Buffers.4.5.1  [4.5.1, )
        Portable.BouncyCastle.1.8.10  [1.8.10, )



Installed Paths
============
GLOBAL_PACKAGES_FOLDER\Microsoft.NETCore.Platforms\2.0.0
GLOBAL_PACKAGES_FOLDER\Portable.BouncyCastle\1.8.10
GLOBAL_PACKAGES_FOLDER\System.Buffers\4.5.1
GLOBAL_PACKAGES_FOLDER\System.Reflection.TypeExtensions\4.4.0
GLOBAL_PACKAGES_FOLDER\System.Security.Cryptography.Cng\4.7.0
GLOBAL_PACKAGES_FOLDER\System.Security.Cryptography.Pkcs\4.7.0
GLOBAL_PACKAGES_FOLDER\System.Text.Encoding.CodePages\4.4.0
GLOBAL_PACKAGES_FOLDER\MimeKit\2.15.0
GLOBAL_PACKAGES_FOLDER\MailKit\2.15.0


Installed Package Contents
==========================
Microsoft.NETCore.Platforms.2.0.0
    GLOBAL_PACKAGES_FOLDER\Microsoft.NETCore.Platforms\2.0.0\Microsoft.NETCore.Platforms.2.0.0.nupkg
    GLOBAL_PACKAGES_FOLDER\Microsoft.NETCore.Platforms\2.0.0\microsoft.netcore.platforms.nuspec
    0 Lib items
    0 Framework items
    0 Assemblies
Portable.BouncyCastle.1.8.10
    GLOBAL_PACKAGES_FOLDER\Portable.BouncyCastle\1.8.10\Portable.BouncyCastle.1.8.10.nupkg
    GLOBAL_PACKAGES_FOLDER\Portable.BouncyCastle\1.8.10\portable.bouncycastle.nuspec
    2 Lib items
        GLOBAL_PACKAGES_FOLDER\Portable.BouncyCastle\1.8.10\lib\netstandard2.0\BouncyCastle.Crypto.dll
        GLOBAL_PACKAGES_FOLDER\Portable.BouncyCastle\1.8.10\lib\netstandard2.0\BouncyCastle.Crypto.xml
    0 Framework items
    1 Assemblies
        GLOBAL_PACKAGES_FOLDER\Portable.BouncyCastle\1.8.10\lib\netstandard2.0\BouncyCastle.Crypto.dll
System.Buffers.4.5.1
    GLOBAL_PACKAGES_FOLDER\System.Buffers\4.5.1\System.Buffers.4.5.1.nupkg
    GLOBAL_PACKAGES_FOLDER\System.Buffers\4.5.1\system.buffers.nuspec
    0 Lib items
    0 Framework items
    0 Assemblies
System.Reflection.TypeExtensions.4.4.0
    GLOBAL_PACKAGES_FOLDER\System.Reflection.TypeExtensions\4.4.0\System.Reflection.TypeExtensions.4.4.0.nupkg
    GLOBAL_PACKAGES_FOLDER\System.Reflection.TypeExtensions\4.4.0\system.reflection.typeextensions.nuspec
    0 Lib items
    0 Framework items
    0 Assemblies
System.Security.Cryptography.Cng.4.7.0
    GLOBAL_PACKAGES_FOLDER\System.Security.Cryptography.Cng\4.7.0\System.Security.Cryptography.Cng.4.7.0.nupkg
    GLOBAL_PACKAGES_FOLDER\System.Security.Cryptography.Cng\4.7.0\system.security.cryptography.cng.nuspec
    2 Lib items
        GLOBAL_PACKAGES_FOLDER\System.Security.Cryptography.Cng\4.7.0\lib\netcoreapp3.0\System.Security.Cryptography.Cng.dll
        GLOBAL_PACKAGES_FOLDER\System.Security.Cryptography.Cng\4.7.0\lib\netcoreapp3.0\System.Security.Cryptography.Cng.xml
    0 Framework items
    1 Assemblies
        GLOBAL_PACKAGES_FOLDER\System.Security.Cryptography.Cng\4.7.0\lib\netcoreapp3.0\System.Security.Cryptography.Cng.dll
System.Security.Cryptography.Pkcs.4.7.0
    GLOBAL_PACKAGES_FOLDER\System.Security.Cryptography.Pkcs\4.7.0\System.Security.Cryptography.Pkcs.4.7.0.nupkg
    GLOBAL_PACKAGES_FOLDER\System.Security.Cryptography.Pkcs\4.7.0\system.security.cryptography.pkcs.nuspec
    2 Lib items
        GLOBAL_PACKAGES_FOLDER\System.Security.Cryptography.Pkcs\4.7.0\lib\netcoreapp3.0\System.Security.Cryptography.Pkcs.dll
        GLOBAL_PACKAGES_FOLDER\System.Security.Cryptography.Pkcs\4.7.0\lib\netcoreapp3.0\System.Security.Cryptography.Pkcs.xml
    0 Framework items
    1 Assemblies
        GLOBAL_PACKAGES_FOLDER\System.Security.Cryptography.Pkcs\4.7.0\lib\netcoreapp3.0\System.Security.Cryptography.Pkcs.dll
System.Text.Encoding.CodePages.4.4.0
    GLOBAL_PACKAGES_FOLDER\System.Text.Encoding.CodePages\4.4.0\System.Text.Encoding.CodePages.4.4.0.nupkg
    GLOBAL_PACKAGES_FOLDER\System.Text.Encoding.CodePages\4.4.0\system.text.encoding.codepages.nuspec
    1 Lib items
        GLOBAL_PACKAGES_FOLDER\System.Text.Encoding.CodePages\4.4.0\lib\netstandard2.0\System.Text.Encoding.CodePages.dll
    0 Framework items
    1 Assemblies
        GLOBAL_PACKAGES_FOLDER\System.Text.Encoding.CodePages\4.4.0\lib\netstandard2.0\System.Text.Encoding.CodePages.dll
MimeKit.2.15.0
    GLOBAL_PACKAGES_FOLDER\MimeKit\2.15.0\MimeKit.2.15.0.nupkg
    GLOBAL_PACKAGES_FOLDER\MimeKit\2.15.0\mimekit.nuspec
    3 Lib items
        GLOBAL_PACKAGES_FOLDER\MimeKit\2.15.0\lib\net50\MimeKit.dll
        GLOBAL_PACKAGES_FOLDER\MimeKit\2.15.0\lib\net50\MimeKit.pdb
        GLOBAL_PACKAGES_FOLDER\MimeKit\2.15.0\lib\net50\MimeKit.xml
    0 Framework items
    1 Assemblies
        GLOBAL_PACKAGES_FOLDER\MimeKit\2.15.0\lib\net50\MimeKit.dll
MailKit.2.15.0
    GLOBAL_PACKAGES_FOLDER\MailKit\2.15.0\MailKit.2.15.0.nupkg
    GLOBAL_PACKAGES_FOLDER\MailKit\2.15.0\mailkit.nuspec
    3 Lib items
        GLOBAL_PACKAGES_FOLDER\MailKit\2.15.0\lib\net50\MailKit.dll
        GLOBAL_PACKAGES_FOLDER\MailKit\2.15.0\lib\net50\MailKit.pdb
        GLOBAL_PACKAGES_FOLDER\MailKit\2.15.0\lib\net50\MailKit.xml
    0 Framework items
    1 Assemblies
        GLOBAL_PACKAGES_FOLDER\MailKit\2.15.0\lib\net50\MailKit.dll


Assemblies
==========================
GLOBAL_PACKAGES_FOLDER\Portable.BouncyCastle\1.8.10\lib\netstandard2.0\BouncyCastle.Crypto.dll
GLOBAL_PACKAGES_FOLDER\System.Security.Cryptography.Cng\4.7.0\lib\netcoreapp3.0\System.Security.Cryptography.Cng.dll
GLOBAL_PACKAGES_FOLDER\System.Security.Cryptography.Pkcs\4.7.0\lib\netcoreapp3.0\System.Security.Cryptography.Pkcs.dll
GLOBAL_PACKAGES_FOLDER\System.Text.Encoding.CodePages\4.4.0\lib\netstandard2.0\System.Text.Encoding.CodePages.dll
GLOBAL_PACKAGES_FOLDER\MimeKit\2.15.0\lib\net50\MimeKit.dll
GLOBAL_PACKAGES_FOLDER\MailKit\2.15.0\lib\net50\MailKit.dll


PowerShell Commands to use NuGet Package: MailKit.2.15.0
================================================================================
Add-Type -Path 'GLOBAL_PACKAGES_FOLDER\Portable.BouncyCastle\1.8.10\lib\netstandard2.0\BouncyCastle.Crypto.dll'
Add-Type -Path 'GLOBAL_PACKAGES_FOLDER\System.Security.Cryptography.Cng\4.7.0\lib\netcoreapp3.0\System.Security.Cryptography.Cng.dll'
Add-Type -Path 'GLOBAL_PACKAGES_FOLDER\System.Security.Cryptography.Pkcs\4.7.0\lib\netcoreapp3.0\System.Security.Cryptography.Pkcs.dll'
Add-Type -Path 'GLOBAL_PACKAGES_FOLDER\System.Text.Encoding.CodePages\4.4.0\lib\netstandard2.0\System.Text.Encoding.CodePages.dll'
Add-Type -Path 'GLOBAL_PACKAGES_FOLDER\MimeKit\2.15.0\lib\net50\MimeKit.dll'
Add-Type -Path 'GLOBAL_PACKAGES_FOLDER\MailKit\2.15.0\lib\net50\MailKit.dll'

";
    }
}