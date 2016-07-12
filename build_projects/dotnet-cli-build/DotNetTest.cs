using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.DotNet.Cli.Build
{
    public class DotNetTest : DotNetTool
    {
        protected override string Command
        {
            get { return "test"; }
        }

        protected override string Args
        {
            get { return $"{GetConfiguration()} {GetXml()} {GetNoTrait()}"; }
        }

        public string Configuration { get; set; }

        public string Xml { get; set; }

        public string NoTrait { get; set; }

        private string GetConfiguration()
        {
            if (!string.IsNullOrEmpty(Configuration))
            {
                return $"--configuration {Configuration}";
            }

            return null;
        }

        private string GetNoTrait()
        {
            if (!string.IsNullOrEmpty(Configuration))
            {
                return $"-notrait {NoTrait}";
            }

            return null;
        }

        private string GetXml()
        {
            if (!string.IsNullOrEmpty(Xml))
            {
                return $"-xml {Xml}";
            }

            return null;
        }
    }
}
