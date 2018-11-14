// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DotNet.PlatformAbstractions;
using RuntimeEnvironment = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;

namespace Microsoft.DotNet.Cli.Utils
{
    internal class MarkOfTheWebDetector : IMarkOfTheWebDetector
    {
        private const string ZoneIdentifierStreamName = "Zone.Identifier";
        private const string ZoneIdIs3String = "ZoneId=3";

        public bool HasMarkOfTheWeb(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"{filePath} cannot be found");
            }

            if (RuntimeEnvironment.OperatingSystemPlatform != Platform.Windows)
            {
                return false;
            }

            return ZoneIdIs3(filePath);
        }

        private static bool ZoneIdIs3(string filePath)
        {
            return AlternateStream
                            .ReadAlternateStream(filePath, ZoneIdentifierStreamName)
                            .Split(new [] { Environment.NewLine }, StringSplitOptions.None)
                            .Any(l => l.Equals(ZoneIdIs3String, StringComparison.Ordinal));
        }

        private static class AlternateStream
        {
            private const uint GenericRead = 0x80000000;

            public static string ReadAlternateStream(string filePath, string altStreamName)
            {
                if (altStreamName == null)
                {
                    return null;
                }

                string returnString = string.Empty;
                string altStream = filePath + ":" + altStreamName;

                SafeFileHandle fileHandle
                    = CreateFile(
                        filename: altStream,
                        desiredAccess: GenericRead,
                        shareMode: 0,
                        attributes: IntPtr.Zero,
                        creationDisposition: (uint)FileMode.Open,
                        flagsAndAttributes: 0,
                        templateFile: IntPtr.Zero);
                if (!fileHandle.IsInvalid)
                {
                    using (StreamReader reader = new StreamReader(new FileStream(fileHandle, FileAccess.Read)))
                    {
                        returnString = reader.ReadToEnd();
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

                return returnString;
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
