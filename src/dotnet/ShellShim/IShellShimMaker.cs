using System.Transactions;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ShellShim
{
    public interface IShellShimMaker
    {
        void EnsureCommandNameUniqueness(string shellCommandName);
        void CreateShim(FilePath packageExecutable, string shellCommandName);
    }
}
