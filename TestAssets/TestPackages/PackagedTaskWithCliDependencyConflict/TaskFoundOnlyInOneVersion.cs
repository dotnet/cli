using Microsoft.Build.Framework;
using Microsoft.DotNet.Cli.Utils;

namespace TaskFoundOnlyInOneVersion.Task
{
    public class TaskFoundOnlyInOneVersion : Microsoft.Build.Utilities.Task
    {
        [Output]
        public string TaskOutput { get; private set; }

        public override bool Execute()
        {
			TaskOutput = ClassThatIsntInMicrosoftDotNetCliUtils.GetValue();
          	return true;
        }
    }
}
