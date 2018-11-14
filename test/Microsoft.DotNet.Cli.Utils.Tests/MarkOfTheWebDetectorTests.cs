// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Microsoft.DotNet.Cli.Utils.Tests
{
    public class MarkOfTheWebDetectorTests : TestBase
    {
        [WindowsOnlyFact]
        public void ItShouldDetectFileWithMarkOfTheWeb()
        {
            var testFile = Path.Combine(TempRoot.Root, Path.GetRandomFileName());
            File.WriteAllText(testFile, string.Empty);
            AlternateStream.WriteAlternateStream(
                testFile,
                "Zone.Identifier",
                "[ZoneTransfer]\r\nZoneId=3\r\nReferrerUrl=C:\\Users\\test.zip\r\n");

            new MarkOfTheWebDetector().HasMarkOfTheWeb(testFile).Should().BeTrue();
        }

        [Fact]
        public void WhenThereIsNoFileItThrows()
        {
            var testFile = Path.Combine(TempRoot.Root, Path.GetRandomFileName());

            Action a = () => new MarkOfTheWebDetector().HasMarkOfTheWeb(testFile);
            a.ShouldThrow<FileNotFoundException>();
        }

        [NonWindowsOnlyFact]
        public void WhenRunOnNonWindowsReturnFalse()
        {
            var testFile = Path.Combine(TempRoot.Root, Path.GetRandomFileName());
            File.WriteAllText(testFile, string.Empty);

            new MarkOfTheWebDetector().HasMarkOfTheWeb(testFile).Should().BeFalse();
        }

        private static class AlternateStream
        {
            private const uint GenericWrite = 0x40000000;

            public static void WriteAlternateStream(string filePath, string altStreamName, string content)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("message", nameof(filePath));
                }

                if (string.IsNullOrWhiteSpace(altStreamName))
                {
                    throw new ArgumentException("message", nameof(altStreamName));
                }

                if (content == null)
                {
                    throw new ArgumentNullException(nameof(content));
                }

                string altStream = filePath + ":" + altStreamName;

                SafeFileHandle fileHandle
                    = CreateFile(
                        filename: altStream,
                        desiredAccess: GenericWrite,
                        shareMode: 0,
                        attributes: IntPtr.Zero,
                        creationDisposition: (uint)FileMode.CreateNew,
                        flagsAndAttributes: 0,
                        templateFile: IntPtr.Zero);

                if (!fileHandle.IsInvalid)
                {
                    using (var streamWriter = new StreamWriter(new FileStream(fileHandle, FileAccess.Write)))
                    {
                        streamWriter.WriteLine(content);
                        streamWriter.Flush();
                    }
                }
                else
                {
                    Exception ex = new Win32Exception(Marshal.GetLastWin32Error());
                    if (!ex.Message.Contains("cannot find the file"))
                    {
                        throw ex;
                    }
                }
            }

            [DllImport("kernel32", SetLastError = true)]
            private static extern SafeFileHandle CreateFile(
                string filename,
                uint desiredAccess,
                uint shareMode,
                IntPtr attributes,
                uint creationDisposition,
                uint flagsAndAttributes,
                IntPtr templateFile);
        }
    }
}
