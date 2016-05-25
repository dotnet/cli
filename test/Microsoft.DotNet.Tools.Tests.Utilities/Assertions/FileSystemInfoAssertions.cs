// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public abstract class FileSystemInfoAssertions<TSubject, TAssertions>
        where TSubject : FileSystemInfo
        where TAssertions : FileSystemInfoAssertions<TSubject, TAssertions>
    {
        public TSubject Subject { get; protected set; }

        public AndConstraint<TAssertions> Exist(
            string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .ForCondition(Subject.Exists)
                .BecauseOf(because, becauseArgs)
                .FailWith($"Expected '{Subject.FullName}' to exist{{reason}}, but it does not.");

            return new AndConstraint<TAssertions>((TAssertions)this);
        }

        public AndConstraint<TAssertions> NotExist(
            string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .ForCondition(!Subject.Exists)
                .BecauseOf(because, becauseArgs)
                .FailWith($"Expected '{Subject.FullName}' not to exist{{reason}}, but it does.");

            return new AndConstraint<TAssertions>((TAssertions)this);
        }
    }
}