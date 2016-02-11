using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.Test.Utilities.Assertions
{
    public class AppConfigAssertions
    {
        private AppConfig _config;

        public AppConfigAssertions(AppConfig config)
        {
            _config = config;
        }

        public BindingRedirectsAssertions BindRedirect(string assemblyName, string publicKeyToken, string culture = "neutral")
        {
            var redirect = _config.BindingRedirects.FirstOrDefault(
                    r => r.AssemblyName == assemblyName && r.PublicKeyToken == publicKeyToken && r.Culture == culture);
            Execute.Assertion.ForCondition(redirect != null)
                .FailWith($"Expected binding redirect for '{assemblyName}' was not found in the application configuration file.");

            return new BindingRedirectsAssertions(redirect);
        }
    }
}
