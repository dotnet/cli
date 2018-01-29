// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Transactions;

namespace Microsoft.DotNet.Cli
{
    public class TransactionResourceManager : IEnlistmentNotification
    {
        private readonly Action _commit;
        private readonly Action<PreparingEnlistment> _prepare;
        private readonly Action _rollback;

        public TransactionResourceManager(
            Action commit = null,
            Action<PreparingEnlistment> prepare = null,
            Action rollback = null)
        {
            _prepare = prepare ?? (enlistment => { });
            _commit = commit ?? (() => { });
            _rollback = rollback ?? (() => { });
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
