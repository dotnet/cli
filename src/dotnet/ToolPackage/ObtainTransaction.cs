using System;
using System.Transactions;

namespace Microsoft.DotNet.ToolPackage
{
    internal class ObtainTransaction : IObtainTransaction
    {
        private readonly Action _rollback;
        private readonly Action<PreparingEnlistment> _prepare;
        private readonly Action _commit;
        private readonly Func<ToolConfigurationAndExecutablePath> _obtainAndReturnExecutablePath;

        internal ObtainTransaction(
            Func<ToolConfigurationAndExecutablePath> ObtainAndReturnExecutablePath,
            Action Commit,
            Action<PreparingEnlistment> Prepare,
            Action Rollback)
        {
            _obtainAndReturnExecutablePath = ObtainAndReturnExecutablePath ??
                                             throw new ArgumentNullException(nameof(ObtainAndReturnExecutablePath));
            _commit = Commit ?? throw new ArgumentNullException(nameof(Commit));
            _prepare = Prepare ?? throw new ArgumentNullException(nameof(Prepare));
            _rollback = Rollback ?? throw new ArgumentNullException(nameof(Rollback));
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
