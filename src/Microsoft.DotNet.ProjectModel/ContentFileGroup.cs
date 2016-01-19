using System.Linq;
using Microsoft.DotNet.ProjectModel.Files;

namespace Microsoft.DotNet.ProjectModel
{
    public class ProjectContentFileGroup
    {
        private PatternGroup _patternGroup;

        public string[] Include { get; set; }

        public string[] Exclude { get; set; }

        public string BuildAction { get; set; }

        public string OutputPath { get; set; }

        public bool CopyToOutput { get; set; }

        public bool Flatten { get; set; }

        public string Language { get; set; }

        public string Target { get; set; }

        public PatternGroup PatternGroup
        {
            get
            {
                if (_patternGroup == null)
                {
                    _patternGroup = new PatternGroup(Include, Exclude, Enumerable.Empty<string>());
                }

                return _patternGroup;
            }
        }
    }
}