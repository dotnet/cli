using System;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Microsoft.DotNet.Tools.Test.Utilities.Assertions
{
    public class BindingRedirectsAssertions
    {
        private AppConfig.BindingRedirect _redirect;

        public BindingRedirectsAssertions(AppConfig.BindingRedirect redirect)
        {
            _redirect = redirect;
        }

        public BindingRedirectsAssertions From(string versionString)
        {
            Execute.Assertion.ForCondition(_redirect.FromVersion == versionString)
                .FailWith($"Expected binding redirect '{_redirect.AssemblyName}' from version '{versionString}', was not found (actual version was {_redirect.FromVersion}).");

            return this;
        }

        public BindingRedirectsAssertions To(string versionString)
        {
            Execute.Assertion.ForCondition(_redirect.ToVersion == versionString)
                .FailWith($"Expected binding redirect '{_redirect.AssemblyName}' to version '{versionString}', was not found (actual version was {_redirect.ToVersion}).");
            return this;
        }
    }
}
