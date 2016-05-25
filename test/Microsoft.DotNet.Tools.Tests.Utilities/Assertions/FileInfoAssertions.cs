// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public class FileInfoAssertions : FileSystemInfoAssertions<FileInfo, FileInfoAssertions>
    {
        public FileInfoAssertions(FileInfo subject)
        {
            Subject = subject;
        }

        public AndConstraint<FileInfoAssertions> ContainText(string expectedText, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .ForCondition(SubjectContainsText(expectedText))
                .BecauseOf(because, becauseArgs)
                .FailWith($"Expected '{Subject.FullName}' to contain text '{expectedText}'{{reason}}, but it does not.");

            return new AndConstraint<FileInfoAssertions>(this);
        }

        public AndConstraint<FileInfoAssertions> NotContainText(string expectedText, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .ForCondition(!SubjectContainsText(expectedText))
                .BecauseOf(because, becauseArgs)
                .FailWith($"Expected '{Subject.FullName}' to not contain text '{expectedText}'{{reason}}, but it does.");

            return new AndConstraint<FileInfoAssertions>(this);
        }
        
        private bool SubjectContainsText(string text)
        {
            return Subject.OpenText().ReadToEnd().Contains(text);
        }
    }
}