using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Xunit.Abstractions;
using VsTestCase = Microsoft.Extensions.Testing.Abstractions.Test;

namespace Xunit.Runner.Dnx
{
    public static class DesignTimeTestConverter
    {
#if DNXCORE50
        private readonly static HashAlgorithm _hash = SHA1.Create();
#else
        private readonly static HashAlgorithm _hash = new SHA1Managed();
#endif
        public static IDictionary<ITestCase, VsTestCase> Convert(IEnumerable<ITestCase> testcases)
        {
            // When tests have the same class name and method name, generate unique names for display
            var groups = testcases
                .Select(tc => new
                {
                    testcase = tc,
                    shortName = GetShortName(tc),
                    fullyQualifiedName = string.Format("{0}.{1}", tc.TestMethod.TestClass.Class.Name, tc.TestMethod.Method.Name)
                })
                .GroupBy(tc => tc.fullyQualifiedName);

            var results = new Dictionary<ITestCase, VsTestCase>();
            foreach (var group in groups)
            {
                var uniquifyNames = group.Count() > 1;
                foreach (var testcase in group)
                {
                    results.Add(
                        testcase.testcase,
                        Convert(
                            testcase.testcase,
                            testcase.shortName,
                            testcase.fullyQualifiedName,
                            uniquifyNames));
                }
            }

            return results;
        }

        private static string GetShortName(ITestCase tc)
        {
            var shortName = new StringBuilder();

            var classFullName = tc.TestMethod.TestClass.Class.Name;
            var dotIndex = classFullName.LastIndexOf('.');
            if (dotIndex >= 0)
                shortName.Append(classFullName.Substring(dotIndex + 1));
            else
                shortName.Append(classFullName);

            shortName.Append(".");
            shortName.Append(tc.TestMethod.Method.Name);

            // We need to shorten the arguments list if it's long. Let's arbitrarily pick 50 characters.
            var argumentsIndex = tc.DisplayName.IndexOf('(');
            if (argumentsIndex >= 0 && tc.DisplayName.Length - argumentsIndex > 50)
            {
                shortName.Append(tc.DisplayName.Substring(argumentsIndex, 46));
                shortName.Append("...");
                shortName.Append(")");
            }
            else if (argumentsIndex >= 0)
                shortName.Append(tc.DisplayName.Substring(argumentsIndex));

            return shortName.ToString();
        }

        private static VsTestCase Convert(
            ITestCase testcase,
            string shortName,
            string fullyQualifiedName,
            bool uniquifyNames)
        {
            string uniqueName;
            if (uniquifyNames)
                uniqueName = string.Format("{0}({1})", fullyQualifiedName, testcase.UniqueID);
            else
                uniqueName = fullyQualifiedName;

            var result = new VsTestCase();
            result.DisplayName = shortName;
            result.FullyQualifiedName = uniqueName;

            result.Id = GuidFromString(testcase.UniqueID);

            if (testcase.SourceInformation != null)
            {
                result.CodeFilePath = testcase.SourceInformation.FileName;
                result.LineNumber = testcase.SourceInformation.LineNumber;
            }

            return result;
        }

        private static Guid GuidFromString(string data)
        {
            var hash = _hash.ComputeHash(Encoding.Unicode.GetBytes(data));
            var b = new byte[16];
            Array.Copy((Array)hash, (Array)b, 16);
            return new Guid(b);
        }
    }
}