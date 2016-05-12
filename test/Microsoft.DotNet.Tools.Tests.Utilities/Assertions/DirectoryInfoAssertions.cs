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
            Execute.Assertion
                .ForCondition(SubjectHasFile(expectedFile))
                .BecauseOf(because, becauseArgs)
                .FailWith($"Expected '{Subject.FullName}' to have file {expectedFile}{{reason}}, but it does not.");

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> NotHaveFile(string expectedFile, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .ForCondition(!SubjectHasFile(expectedFile))
                .BecauseOf(because, becauseArgs)
                .FailWith($"Expected '{Subject.FullName}' not to have file {expectedFile}{{reason}}, but it does.");

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> HaveDirectory(string expectedDir, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .ForCondition(SubjectHasDirectory(expectedDir))
                .BecauseOf(because, becauseArgs)
                .FailWith($"Expected '{Subject.FullName}' to have directory {expectedDir}{{reason}}, but it does not.");

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> NotHaveDirectory(string expectedDir, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .ForCondition(!SubjectHasDirectory(expectedDir))
                .BecauseOf(because, becauseArgs)
                .FailWith($"Expected '{Subject.FullName}' to not have directory {expectedDir}{{reason}}, but it does.");

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> HaveFiles(IEnumerable<string> expectedFiles, string because = "", params object[] becauseArgs)
        {
            var missingFiles = Enumerable.Except(expectedFiles, SubjectChildFiles);
            var nl = Environment.NewLine;

            Execute.Assertion
                .ForCondition(!missingFiles.Any())
                .BecauseOf(because, becauseArgs)
                .FailWith($"Expected '{Subject.FullName}' to have:{nl}{string.Join(nl, expectedFiles)} {nl}{{reason}}, but the following files are missing:{nl}{string.Join(nl, missingFiles)}");

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> NotHaveFiles(IEnumerable<string> files, string because = "", params object[] becauseArgs)
        {
            var presentFiles = Enumerable.Intersect(SubjectChildFiles, files);
            var nl = Environment.NewLine;

            Execute.Assertion
                .ForCondition(!presentFiles.Any())
                .BecauseOf(because, becauseArgs)
                .FailWith($"Expected '{Subject.FullName}' to not have: {nl}{string.Join(nl, files)} {nl}{{reason}}, but the following extra files were found:{nl}{string.Join(nl, presentFiles)}");

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> OnlyHaveFiles(IEnumerable<string> expectedFiles, string because = "", params object[] becauseArgs)
        {
            var missingFiles = Enumerable.Except(expectedFiles, SubjectChildFiles);
            var extraFiles = Enumerable.Except(SubjectChildFiles, expectedFiles);
            var nl = Environment.NewLine;

            Execute.Assertion
                .ForCondition(!missingFiles.Any())
                .BecauseOf(because, becauseArgs)
                .FailWith($"Expected '{Subject.FullName}' to have: {nl}{string.Join(nl, expectedFiles)} {nl}{{reason}}, but the following files are missing:{nl}{string.Join(nl, missingFiles)}");

            Execute.Assertion
                .ForCondition(!extraFiles.Any())
                .BecauseOf(because, becauseArgs)
                .FailWith($"Expected '{Subject.FullName}' to only have: {nl}{string.Join(nl, expectedFiles)} {nl}{{reason}}, but the following extra files were found:{nl}{string.Join(nl, extraFiles)}");

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }
        
        private IEnumerable<string> SubjectChildFiles
        {
            get
            {
                return Subject.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Select(f => f.Name);
            }
        }
        
        private bool SubjectHasDirectory(string expectedDir)
        {
            return new DirectoryInfo(Path.Combine(Subject.FullName, expectedDir)).Exists;
        }
        
        private bool SubjectHasFile(string expectedFile)
        {
            return new FileInfo(Path.Combine(Subject.FullName, expectedFile)).Exists;
        }
    }
}