// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using static Microsoft.DotNet.Cli.Utils.ToolPath;

namespace Microsoft.DotNet.Tools.MSBuild
{
    public class MSBuildForwardingApp
    {
        internal const string TelemetrySessionIdEnvironmentVariableName = "DOTNET_CLI_TELEMETRY_SESSIONID";

        private readonly ForwardingApp _forwardingApp;

        private readonly Dictionary<string, string> _msbuildRequiredEnvironmentVariables =
            new Dictionary<string, string>
            {
                { "CscToolExe", GetRunCscPath() }
            };

        private readonly IEnumerable<string> _msbuildRequiredParameters =
            new List<string> { "/m", "/v:m" };

        public MSBuildForwardingApp(IEnumerable<string> argsToForward)
        {
            if (Telemetry.CurrentSessionId != null)
            {
                try
                {
                    Type loggerType = typeof(MSBuildLogger);

                    argsToForward = argsToForward.Concat(new[]
                    {
                        $"/Logger:{loggerType.FullName},{loggerType.GetTypeInfo().Assembly.Location}"
                    });
                }
                catch (Exception)
                {
                    // Exceptions during telemetry shouldn't cause anything else to fail
                }
            }

            _forwardingApp = new ForwardingApp(
                MSBuildDll().FullName,
                _msbuildRequiredParameters.Concat(argsToForward),
                environmentVariables: _msbuildRequiredEnvironmentVariables);
        }

        public int Execute()
        {
            try
            {
                Environment.SetEnvironmentVariable(TelemetrySessionIdEnvironmentVariableName, Telemetry.CurrentSessionId);

                return _forwardingApp.Execute();
            }
            finally
            {
                Environment.SetEnvironmentVariable(TelemetrySessionIdEnvironmentVariableName, null);
            }
        }

        internal static CommandOption AddVerbosityOption(CommandLineApplication app)
        {
            return app.Option("-v|--verbosity", LocalizableStrings.VerbosityOptionDescription, CommandOptionType.SingleValue);
        }

        private static string GetRunCscPath()
        {
            var scriptExtension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : ".sh";
            
            return Path.Combine(
                RoslynPath().FullName,
                $"RunCsc{scriptExtension}");
        }
    }
}
