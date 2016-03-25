// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Xunit.Abstractions;
using Xunit;

namespace Microsoft.DotNet.InstallScripts.Tests
{
    public class InstallScriptsTests
    {
        private readonly ITestOutputHelper _output;
        
        public InstallScriptsTests(ITestOutputHelper output)
        {
            _output = output;
        }
        
        private static Stream CreateZipWithContent(Dictionary<string, string> pathToContent)
        {
            MemoryStream ret = new MemoryStream();
            
            using (var zip = new ZipArchive(ret, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var kp in pathToContent)
                {
                    var entry = zip.CreateEntry(kp.Key);
                    
                    using (var fileContent = entry.Open())
                    {
                        fileContent.WriteAllText(kp.Value);
                    }
                }
            }
            
            ret.Position = 0;
            return ret;
        }
        
        private static void ValidateFilesHaveProperContent(string installDir, Dictionary<string, string> pathToContent)
        {
            foreach (var kp in pathToContent)
            {
                string path = Path.Combine(installDir, kp.Key);
                Assert.True(File.Exists(path));
                Assert.Equal(kp.Value, File.ReadAllText(path));
            }
        }
        
        private object _tempDirLock = new object();
        private string CreateTempDirectory()
        {
            lock (_tempDirLock)
            {
                Exception lastException = null;
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        string path = Path.GetRandomFileName(); 
                        Path.Combine(Path.GetTempPath(), path); 
                        Directory.CreateDirectory(path); 
                        return path;
                    }
                    catch (Exception e)
                    {
                        lastException = e;
                    }
                }
                
                if (lastException == null)
                {
                    throw new Exception("test error");
                }
                
                throw lastException;
            }            
        }
        
        [Fact]
        public void InstallScriptExists()
        {
            Assert.True(File.Exists(InstallScript.InstallScriptPath));
        }
        
        [Fact]
        public void DryRunDisplaysLinkWithDefaultChannel()
        {
            using (TestServer s = TestServer.Create())
            {
                string version = "1.2.3";
                string architecture = "x64";
                string versionFile = InstallScript.VersionFilePath(architecture: architecture);
                string zipPath = InstallScript.DotnetDevZipPath(version: version, architecture: architecture);
                
                s[versionFile] = TestServer.SendText($"abc\r\n{version}");
                
                InstallScript installer = InstallScript.Install(_output, $"-DryRun -AzureFeed {s.Url} -Architecture {architecture}");
                Assert.True(installer.StdOut.Contains($"{s.Url}{zipPath}"));
                Assert.Equal(0, installer.ExitCode);
                Assert.True(string.IsNullOrEmpty(installer.StdErr));

                Assert.Equal(1, s.RequestCounts[versionFile]);
                Assert.Equal(0, s.PageNotFoundHits);
            }
        }
        
        [Fact]
        public void DryRunDisplaysLinkWithSpecificChannel()
        {
            using (TestServer s = TestServer.Create())
            {
                string version = "1.2.3";
                string architecture = "x64";
                string channel = "preview";
                string versionFile = InstallScript.VersionFilePath(architecture: architecture, channel: channel);
                string zipPath = InstallScript.DotnetDevZipPath(version: version, architecture: architecture, channel: channel);
                
                s[versionFile] = TestServer.SendText($"abc\r\n{version}");
                
                InstallScript installer = InstallScript.Install(_output, $"-DryRun -AzureFeed {s.Url} -Architecture {architecture} -Channel {channel}");
                Assert.True(installer.StdOut.Contains($"{s.Url}{zipPath}"));
                Assert.Equal(0, installer.ExitCode);
                Assert.True(string.IsNullOrEmpty(installer.StdErr));

                Assert.Equal(1, s.RequestCounts[versionFile]);
                Assert.Equal(0, s.PageNotFoundHits);
            }
        }
        
        [Fact]
        public void InstallationToTemporaryDirectoryWorks()
        {
            using (TestServer s = TestServer.Create())
            {
                string installDir = null;
                try
                {
                    installDir = CreateTempDirectory();
                    string version = "1.2.3";
                    string architecture = "x64";
                    string versionFile = InstallScript.VersionFilePath(architecture: architecture);
                    string zipPath = InstallScript.DotnetDevZipPath(version: version, architecture: architecture);
                    
                    s[versionFile] = TestServer.SendText($"abc\r\n{version}");
                    
                    Dictionary<string, string> zipContent = new Dictionary<string, string>()
                        {
                            { "test.txt", "file in non-versioned path" },
                            { "a/b/1.0.0/test.txt", "file in versioned path" }
                        };
                    
                    s[zipPath] = TestServer.SendStream(CreateZipWithContent(zipContent));
                    
                    InstallScript installer = InstallScript.Install(_output, $"-AzureFeed {s.Url} -Architecture {architecture} -InstallDir {installDir}");
                    Assert.True(installer.StdOut.Contains($"{s.Url}{zipPath}"));
                    Assert.Equal(0, installer.ExitCode);
                    Assert.True(string.IsNullOrEmpty(installer.StdErr));
    
                    Assert.Equal(1, s.RequestCounts[versionFile]);
                    Assert.Equal(1, s.RequestCounts[zipPath]);
                    Assert.Equal(0, s.PageNotFoundHits);
                    
                    ValidateFilesHaveProperContent(installDir, zipContent);
                }
                finally
                {
                    if (installDir != null)
                    {
                        Directory.Delete(installDir, recursive: true);
                    }
                }
            }
        }
    }
}
