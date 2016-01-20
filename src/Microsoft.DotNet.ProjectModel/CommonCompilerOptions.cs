// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.ProjectModel
{
    public class CommonCompilerOptions
    {
        public IEnumerable<string> Defines { get; set; }

        public string LanguageVersion { get; set; }

        public string Platform { get; set; }

        public bool? AllowUnsafe { get; set; }

        public bool? WarningsAsErrors { get; set; }

        public bool? Optimize { get; set; }

        public string KeyFile { get; set; }

        public bool? DelaySign { get; set; }

        public bool? PublicSign { get; set; }

        public byte[] OssSignKey => (PublicSign == true && KeyFile == null) ? StrongNameKey : null;

        public bool? EmitEntryPoint { get; set; }

        public bool? PreserveCompilationContext { get; set; }

        public bool? GenerateXmlDocumentation { get; set; }

        public IEnumerable<string> SuppressWarnings { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as CommonCompilerOptions;
            return other != null &&
                   LanguageVersion == other.LanguageVersion &&
                   Platform == other.Platform &&
                   AllowUnsafe == other.AllowUnsafe &&
                   WarningsAsErrors == other.WarningsAsErrors &&
                   Optimize == other.Optimize &&
                   KeyFile == other.KeyFile &&
                   DelaySign == other.DelaySign &&
                   PublicSign == other.PublicSign &&
                   EmitEntryPoint == other.EmitEntryPoint &&
                   GenerateXmlDocumentation == other.GenerateXmlDocumentation &&
                   PreserveCompilationContext == other.PreserveCompilationContext &&
                   Enumerable.SequenceEqual(Defines ?? Enumerable.Empty<string>(), other.Defines ?? Enumerable.Empty<string>()) &&
                   Enumerable.SequenceEqual(SuppressWarnings ?? Enumerable.Empty<string>(), other.SuppressWarnings ?? Enumerable.Empty<string>());
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static CommonCompilerOptions Combine(params CommonCompilerOptions[] options)
        {
            var result = new CommonCompilerOptions();
            foreach (var option in options)
            {
                // Skip null options
                if (option == null)
                {
                    continue;
                }

                // Defines and suppressions are always combined
                if (option.Defines != null)
                {
                    var existing = result.Defines ?? Enumerable.Empty<string>();
                    result.Defines = existing.Concat(option.Defines).Distinct();
                }

                if (option.SuppressWarnings != null)
                {
                    var existing = result.SuppressWarnings ?? Enumerable.Empty<string>();
                    result.SuppressWarnings = existing.Concat(option.SuppressWarnings).Distinct();
                }

                if (option.LanguageVersion != null)
                {
                    result.LanguageVersion = option.LanguageVersion;
                }

                if (option.Platform != null)
                {
                    result.Platform = option.Platform;
                }

                if (option.AllowUnsafe != null)
                {
                    result.AllowUnsafe = option.AllowUnsafe;
                }

                if (option.WarningsAsErrors != null)
                {
                    result.WarningsAsErrors = option.WarningsAsErrors;
                }

                if (option.Optimize != null)
                {
                    result.Optimize = option.Optimize;
                }

                if (option.KeyFile != null)
                {
                    result.KeyFile = option.KeyFile;
                }

                if (option.DelaySign != null)
                {
                    result.DelaySign = option.DelaySign;
                }

                if (option.PublicSign != null)
                {
                    result.PublicSign = option.PublicSign;
                }

                if (option.EmitEntryPoint != null)
                {
                    result.EmitEntryPoint = option.EmitEntryPoint;
                }

                if (option.PreserveCompilationContext != null)
                {
                    result.PreserveCompilationContext = option.PreserveCompilationContext;
                }

                if (option.GenerateXmlDocumentation != null)
                {
                    result.GenerateXmlDocumentation = option.GenerateXmlDocumentation;
                }
            }

            return result;
        }

        private static readonly byte[] StrongNameKey = new byte[] {
            0x00, 0x24, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00, 0x94, 0x00, 0x00, 0x00, 0x06, 0x02, 0x00, 0x00,
            0x00, 0x24, 0x00, 0x00, 0x52, 0x53, 0x41, 0x31, 0x00, 0x04, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00,
            0xb1, 0xbe, 0x0d, 0x2b, 0xc7, 0xc1, 0x50, 0xf3, 0xe2, 0x67, 0x10, 0x3e, 0x15, 0x09, 0x9f, 0x35,
            0xb0, 0x16, 0x3d, 0xb1, 0x82, 0x13, 0xc5, 0x01, 0x43, 0xb7, 0x48, 0xda, 0x46, 0xf6, 0x53, 0x0e,
            0x42, 0x50, 0x6e, 0x09, 0x50, 0x33, 0x0c, 0xf4, 0xac, 0xc3, 0xef, 0x24, 0x30, 0x69, 0xf9, 0x74,
            0x23, 0x89, 0x3b, 0x4c, 0x3f, 0x24, 0x85, 0x51, 0xbe, 0x15, 0x50, 0x9c, 0xf6, 0x98, 0x4a, 0xab,
            0xfa, 0x1d, 0xc6, 0x9c, 0xa2, 0x55, 0xc2, 0x15, 0x49, 0x3c, 0xcc, 0x88, 0x16, 0xb3, 0x04, 0x44,
            0xaf, 0x20, 0xbe, 0x56, 0x78, 0x81, 0xcc, 0xd5, 0x3c, 0x3b, 0xce, 0x52, 0x00, 0xbf, 0x76, 0x81,
            0x30, 0xbc, 0xba, 0x41, 0x81, 0x0e, 0x81, 0xb7, 0x79, 0xce, 0xea, 0x51, 0x83, 0xf7, 0x5c, 0x16,
            0x56, 0xf9, 0xcb, 0x3c, 0x4f, 0x2a, 0x7a, 0x60, 0x66, 0xbb, 0x74, 0xbd, 0x5a, 0xfe, 0xb2, 0xcd
        };
    }
}
