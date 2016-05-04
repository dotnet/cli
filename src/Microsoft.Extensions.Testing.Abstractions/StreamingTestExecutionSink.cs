// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.Extensions.Testing.Abstractions
{
    public class StreamingTestExecutionSink : StreamingTestSink, ITestExecutionSink
    {
        private readonly ConcurrentDictionary<string, TestState> _runningTests;

        public StreamingTestExecutionSink(Stream stream) : base(stream)
        {
            _runningTests = new ConcurrentDictionary<string, TestState>();
        }

        public void SendTestStarted(TestCase test)
        {
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }

            if (test.FullyQualifiedName != null)
            {
                var state = new TestState() { StartTime = DateTimeOffset.Now, };
                _runningTests.TryAdd(test.FullyQualifiedName, state);
            }

            Stream.Send(new Message
            {
                MessageType = "TestExecution.TestStarted",
                Payload = JToken.FromObject(test),
            });
        }

        public void SendTestResult(TestResult testResult)
        {
            if (testResult == null)
            {
                throw new ArgumentNullException(nameof(testResult));
            }

            if (testResult.StartTime == default(DateTimeOffset) && testResult.TestCase.FullyQualifiedName != null)
            {
                TestState state;
                _runningTests.TryRemove(testResult.TestCase.FullyQualifiedName, out state);

                testResult.StartTime = state.StartTime;
            }

            if (testResult.EndTime == default(DateTimeOffset))
            {
                testResult.EndTime = DateTimeOffset.Now;
            }

            Stream.Send(new Message
            {
                MessageType = "TestExecution.TestResult",
                Payload = JToken.FromObject(testResult),
            });
        }

        private class TestState
        {
            public DateTimeOffset StartTime { get; set; }
        }
    }
}