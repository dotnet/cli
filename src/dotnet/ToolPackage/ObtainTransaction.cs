using System;
using System.Transactions;

namespace Microsoft.DotNet.ToolPackage
{
    internal class ObtainTransaction : IObtainTransaction
    {
        private readonly Func<ToolConfigurationAndExecutablePath> _obtainAndReturnExecutablePath;
        private readonly Action _commit;
        private readonly Action<PreparingEnlistment> _prepare;
        private readonly Action _rollback;

        internal ObtainTransaction(
            Func<ToolConfigurationAndExecutablePath> obtainAndReturnExecutablePath,
            Action commit,
            Action<PreparingEnlistment> prepare,
            Action rollback)
        {
            _obtainAndReturnExecutablePath = obtainAndReturnExecutablePath ??
                                             throw new ArgumentNullException(nameof(obtainAndReturnExecutablePath));
            _commit = commit ?? throw new ArgumentNullException(nameof(commit));
            _prepare = prepare ?? throw new ArgumentNullException(nameof(prepare));
            _rollback = rollback ?? throw new ArgumentNullException(nameof(rollback));
        }

        public ToolConfigurationAndExecutablePath ObtainAndReturnExecutablePath()
        {
            return _obtainAndReturnExecutablePath();
        }

        public void Commit(Enlistment enlistment)
        {
            _commit();

            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            Rollback(enlistment);
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            _prepare(preparingEnlistment);
        }

        public void Rollback(Enlistment enlistment)
        {
            _rollback();

            enlistment.Done();
        }
    }
}
