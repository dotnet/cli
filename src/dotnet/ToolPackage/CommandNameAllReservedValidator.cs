// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.ToolPackage
{
    public static class CommandNameAllReservedValidator
    {
        private static CommandNameValidator[] _explicitReservedStrings = new CommandNameValidator[] {
            new CommandNameValidator(false, false, true, "add"),
            new CommandNameValidator(false, false, true, "build"),
            new CommandNameValidator(false, false, true, "clean"),
            new CommandNameValidator(true, true, true, "dev-certs"),
            new CommandNameValidator(false, false, true, "install"),
            new CommandNameValidator(false, false, true, "list"),
            new CommandNameValidator(false, false, true, "migrate"),
            new CommandNameValidator(true, true, true, "msbuild"),
            new CommandNameValidator(false, false, true, "new"),
            new CommandNameValidator(false, false, true, "pack"),
            new CommandNameValidator(false, false, true, "publish"),
            new CommandNameValidator(false, false, true, "remove"),
            new CommandNameValidator(false, false, true, "restore"),
            new CommandNameValidator(false, false, true, "run"),
            new CommandNameValidator(false, false, true, "test"),
            new CommandNameValidator(false, false, true, "uninstall"),
            new CommandNameValidator(false, false, true, "update"),
            new CommandNameValidator(true, true, true, "vstest"),
            new CommandNameValidator(false, false, true, "watch"),

            new CommandNameValidator(true, true, true, "app-insights"),
            new CommandNameValidator(true, true, true, "appinsights"),
            new CommandNameValidator(true, true, true, "asp"),
            new CommandNameValidator(true, true, true, "asp-net"),
            new CommandNameValidator(true, true, true, "aspnet"),
            new CommandNameValidator(true, true, true, "az"),
            new CommandNameValidator(true, true, true, "azure"),
            new CommandNameValidator(true, true, true, "code"),
            new CommandNameValidator(true, true, true, "browserlink"),
            new CommandNameValidator(true, true, true, "browser-link"),
            new CommandNameValidator(true, true, true, "core"),
            new CommandNameValidator(true, true, true, "csc"),
            new CommandNameValidator(true, true, true, "csi"),
            new CommandNameValidator(true, true, true, "devcerts"),
            new CommandNameValidator(true, true, true, "dn"),
            new CommandNameValidator(true, true, true, "dnu"),
            new CommandNameValidator(true, true, true, "dnvm"),
            new CommandNameValidator(true, true, true, "dnx"),
            new CommandNameValidator(true, true, true, "dot"),
            new CommandNameValidator(true, true, true, "dotnet"),
            new CommandNameValidator(true, true, true, "dot-net"),
            new CommandNameValidator(true, true, true, "dx"),
            new CommandNameValidator(true, true, true, "editor-config"),
            new CommandNameValidator(true, true, true, "editorconfig"),
            new CommandNameValidator(true, true, true, "ef"),
            new CommandNameValidator(true, true, true, "entity-framework"),
            new CommandNameValidator(true, true, true, "entityframework"),
            new CommandNameValidator(true, true, true, "etw"),
            new CommandNameValidator(true, true, true, "fsc"),
            new CommandNameValidator(true, true, true, "fsi"),
            new CommandNameValidator(true, true, true, "intellisense"),
            new CommandNameValidator(true, true, true, "intellitest"),
            new CommandNameValidator(true, true, true, "nuget"),
            new CommandNameValidator(true, true, true, "libman"),
            new CommandNameValidator(true, true, true, "lib-man"),
            new CommandNameValidator(true, true, true, "live-test"),
            new CommandNameValidator(true, true, true, "livetest"),
            new CommandNameValidator(true, true, true, "live-unit-test"),
            new CommandNameValidator(true, true, true, "live-unit-testing"),
            new CommandNameValidator(true, true, true, "lut"),
            new CommandNameValidator(true, true, true, "microsoft"),
            new CommandNameValidator(true, true, true, "msft"),
            new CommandNameValidator(true, true, true, "notch"),
            new CommandNameValidator(true, true, true, "razor"),
            new CommandNameValidator(true, true, true, "rzc"),
            new CommandNameValidator(true, true, true, "runtime-store"),
            new CommandNameValidator(true, true, true, "test-impat"),
            new CommandNameValidator(true, true, true, "testimpact"),
            new CommandNameValidator(true, true, true, "vbc"),
            new CommandNameValidator(true, true, true, "visual"),
            new CommandNameValidator(true, true, true, "vso"),
            new CommandNameValidator(true, true, true, "vsts"),

            new CommandNameValidator(false, true, true, "analysis"),
            new CommandNameValidator(false, true, true, "analyze"),
            new CommandNameValidator(false, true, true, "background"),
            new CommandNameValidator(false, true, true, "bg"),
            new CommandNameValidator(false, true, true, "clean-up"),
            new CommandNameValidator(false, true, true, "cleanup"),
            new CommandNameValidator(false, true, true, "code-coverage"),
            new CommandNameValidator(false, true, true, "codecoverage"),
            new CommandNameValidator(false, true, true, "code-gen"),
            new CommandNameValidator(false, true, true, "codegen"),
            new CommandNameValidator(false, true, true, "copy"),
            new CommandNameValidator(false, true, true, "cover"),
            new CommandNameValidator(false, true, true, "debug"),
            new CommandNameValidator(false, true, true, "delete"),
            new CommandNameValidator(false, true, true, "dmp"),
            new CommandNameValidator(false, true, true, "dump"),
            new CommandNameValidator(false, true, true, "do"),
            new CommandNameValidator(false, true, true, "doctor"),
            new CommandNameValidator(false, true, true, "fix"),
            new CommandNameValidator(false, true, true, "fix-all"),
            new CommandNameValidator(false, true, true, "fixall"),
            new CommandNameValidator(false, true, true, "fmt"),
            new CommandNameValidator(false, true, true, "format"),
            new CommandNameValidator(false, true, true, "framework"),
            new CommandNameValidator(false, true, true, "http"),
            new CommandNameValidator(false, true, true, "https"),
            new CommandNameValidator(false, true, true, "info"),
            new CommandNameValidator(false, true, true, "init"),
            new CommandNameValidator(false, true, true, "inspect"),
            new CommandNameValidator(false, true, true, "interactive"),
            new CommandNameValidator(false, true, true, "move"),
            new CommandNameValidator(false, true, true, "package"),
            new CommandNameValidator(false, true, true, "packman"),
            new CommandNameValidator(false, true, true, "pack-man"),
            new CommandNameValidator(false, true, true, "patch"),
            new CommandNameValidator(false, true, true, "pretty"),
            new CommandNameValidator(false, true, true, "project"),
            new CommandNameValidator(false, true, true, "property"),
            new CommandNameValidator(false, true, true, "reference"),
            new CommandNameValidator(false, true, true, "repl"),
            new CommandNameValidator(false, true, true, "runtime"),
            new CommandNameValidator(false, true, true, "scaffold"),
            new CommandNameValidator(false, true, true, "sdk"),
            new CommandNameValidator(false, true, true, "spit"),
            new CommandNameValidator(false, true, true, "shutdown"),
            new CommandNameValidator(false, true, true, "sln"),
            new CommandNameValidator(false, true, true, "solution"),
            new CommandNameValidator(false, true, true, "start"),
            new CommandNameValidator(false, true, true, "stop"),
            new CommandNameValidator(false, true, true, "target"),
            new CommandNameValidator(false, true, true, "template"),
            new CommandNameValidator(false, true, true, "undo"),
            new CommandNameValidator(false, true, true, "version"),
            new CommandNameValidator(false, true, true, "web"),
        };

        public static string[] GenerateError(string commandName)
        {
            IEnumerable<CommandNameValidator> cannotStartWithWordAsSingleLetter = Enumerable
                .Range('a', 'z' - 'a' + 1)
                .Select(i => ((char)i).ToString())
                .Select(s => new CommandNameValidator(false, true, true, s));

            IEnumerable<CommandNameValidator> cannotStartWithWordAsSingleNumber = Enumerable
                .Range(0, 9)
                .Select(i => i.ToString())
                .Select(s => new CommandNameValidator(false, true, true, s));

            return _explicitReservedStrings
                .Concat(cannotStartWithWordAsSingleLetter)
                .Concat(cannotStartWithWordAsSingleNumber)
                .SelectMany(v => v.GenerateError(commandName)).ToArray();
        }
    }
}
