// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Cli.Utils
{
    public class LineNotificationTextWriterTests : TestBase
    {
        private static IEnumerable<object[]> TestStrings()
        {
            yield return new object[] { "123", 0 };
            yield return new object[] { "123\n", 1 };
            yield return new object[] { "123\r\n", 1 };
            yield return new object[] { "\n", 1 };
            yield return new object[] { "\n\n", 2 };
            yield return new object[] { "\n\r\n", 2 };
            yield return new object[] { "\n\r\n", 2 };
            yield return new object[] { "first\nsecond\nthird\nlast", 3 };
        }

        /// <summary>
        /// Tests the LineNotificationTextWriter.Write method raises OnWriteLine
        /// the correct number of times and always with a '\n' character at the end of the line.
        /// </summary>
        [Theory]
        [MemberData(nameof(TestStrings))]
        public void TestWrite(string inputStr, int newLineCount)
        {
            int count = 0;
            var writer = new LineNotificationTextWriter(null, null)
                .OnWriteLine(line =>
                {
                    count++;
                    Assert.Equal('\n', line[line.Length - 1]);
                });

            writer.Write(inputStr);

            Assert.Equal(newLineCount, count);
        }

        /// <summary>
        /// Tests the LineNotificationTextWriter.WriteLine method raises OnWriteLine
        /// the correct number of times and always with a '\n' character at the end of the line.
        /// </summary>
        [Theory]
        [MemberData(nameof(TestStrings))]
        public void TestWriteLine(string inputStr, int newLineCount)
        {
            int count = 0;
            var writer = new LineNotificationTextWriter(null, null)
                .OnWriteLine(line =>
                {
                    count++;
                    Assert.Equal('\n', line[line.Length - 1]);
                });

            writer.WriteLine(inputStr);

            Assert.Equal(newLineCount + 1, count);
        }

        /// <summary>
        /// Tests the LineNotificationTextWriter.Write(char) method raises OnWriteLine
        /// the correct number of times and always with a '\n' character at the end of the line.
        /// </summary>
        [Fact]
        public void TestWriteChar()
        {
            int count = 0;
            var writer = new LineNotificationTextWriter(null, null)
                .OnWriteLine(line =>
                {
                    count++;
                    Assert.Equal('\n', line[line.Length - 1]);
                });

            writer.Write('a');
            writer.Write('b');
            writer.Write('c');
            writer.Write('\r');
            writer.Write('\n');
            writer.Write('\n');
            writer.Write('c');
            writer.Write('b');
            writer.Write('a');
            writer.Write('\n');

            Assert.Equal(3, count);
        }
    }
}
