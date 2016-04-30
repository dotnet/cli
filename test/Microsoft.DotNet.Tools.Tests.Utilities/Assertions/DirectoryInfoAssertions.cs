using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public class DirectoryInfoAssertions : FileSystemInfoAssertions<DirectoryInfo, DirectoryInfoAssertions>
    {
        public DirectoryInfoAssertions(DirectoryInfo subject)
        {
            Subject = subject;
        }

        public AndConstraint<DirectoryInfoAssertions> HaveFile(string expectedFile, string because = "", params object[] becauseArgs)
        {
            var immediateChildFiles = Subject.EnumerateFiles(expectedFile, SearchOption.TopDirectoryOnly);

            Execute.Assertion
                .ForCondition(immediateChildFiles.Any())
                .BecauseOf(because, becauseArgs)
                .FailWith($"Expected '{Subject.FullName}' to have file {expectedFile}{{reason}}, but it does not.");

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> NotHaveFile(string expectedFile, string because = "", params object[] becauseArgs)
        {
            var immediateChildFiles = Subject.EnumerateFiles(expectedFile, SearchOption.TopDirectoryOnly);

            Execute.Assertion
                .ForCondition(!immediateChildFiles.Any())
                .BecauseOf(because, becauseArgs)
                .FailWith($"Expected '{Subject.FullName}' not to have file {expectedFile}{{reason}}, but it does.");

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> HaveDirectory(string expectedDir, string because = "", params object[] becauseArgs)
        {
            var immediateChildDirs = Subject.EnumerateDirectories(expectedDir, SearchOption.TopDirectoryOnly);

            Execute.Assertion
                .ForCondition(immediateChildDirs.Any())
                .BecauseOf(because, becauseArgs)
                .FailWith($"Expected '{Subject.FullName}' to have directory {expectedDir}{{reason}}, but it does not.");

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> NotHaveDirectory(string expectedDir, string because = "", params object[] becauseArgs)
        {
            var immediateChildDirs = Subject.EnumerateDirectories(expectedDir, SearchOption.TopDirectoryOnly);

            Execute.Assertion
                .ForCondition(!immediateChildDirs.Any())
                .BecauseOf(because, becauseArgs)
                .FailWith($"Expected '{Subject.FullName}' to not have directory {expectedDir}{{reason}}, but it does.");

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> HaveFiles(IEnumerable<string> expectedFiles, string because = "", params object[] becauseArgs)
        {
            var actualFiles = Subject.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Select(f => f.Name);
            var missingFiles = Enumerable.Except(expectedFiles, actualFiles);
            var nl = Environment.NewLine;

            Execute.Assertion
                .ForCondition(!missingFiles.Any())
                .FailWith($"Expected '{Subject.FullName}' to have:{nl}{string.Join(nl, expectedFiles)} {nl}{{reason}}, but the following files are missing:{nl}{string.Join(nl, missingFiles)}");

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> NotHaveFiles(IEnumerable<string> files, string because = "", params object[] becauseArgs)
        {
            var actualFiles = Subject.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Select(f => f.Name);
            var presentFiles = Enumerable.Union(actualFiles, files);
            var nl = Environment.NewLine;

            Execute.Assertion
                .ForCondition(!presentFiles.Any())
                .FailWith($"Expected '{Subject.FullName}' to not have: {nl}{string.Join(nl, files)} {nl}{{reason}}, but the following extra files were found:{nl}{string.Join(nl, presentFiles)}");

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> OnlyHaveFiles(IEnumerable<string> expectedFiles, string because = "", params object[] becauseArgs)
        {
            var actualFiles = Subject.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Select(f => f.Name);
            var missingFiles = Enumerable.Except(expectedFiles, actualFiles);
            var extraFiles = Enumerable.Except(actualFiles, expectedFiles);
            var nl = Environment.NewLine;

            Execute.Assertion
                .ForCondition(!missingFiles.Any())
                .FailWith($"Expected '{Subject.FullName}' to have: {nl}{string.Join(nl, expectedFiles)} {nl}{{reason}}, but the following files are missing:{nl}{string.Join(nl, missingFiles)}");

            Execute.Assertion
                .ForCondition(!extraFiles.Any())
                .FailWith($"Expected '{Subject.FullName}' to only have: {nl}{string.Join(nl, expectedFiles)} {nl}{{reason}}, but the following extra files were found:{nl}{string.Join(nl, extraFiles)}");

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }
    }
}