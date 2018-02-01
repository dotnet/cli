using System.Transactions;

namespace Microsoft.DotNet.ToolPackage
{
    internal interface IObtainTransaction: IEnlistmentNotification
    {
        ToolConfigurationAndExecutablePath ObtainAndReturnExecutablePath();
    }
}
