using System;

namespace Microsoft.DotNet.Cli.Build
{
    public class TestPackage
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public Func<bool> IsApplicable { get; set; }
    }
}
