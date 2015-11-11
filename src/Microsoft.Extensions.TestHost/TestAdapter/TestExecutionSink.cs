// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Testing.Abstractions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.TestHost
{
    public class TestExecutionSink : ITestExecutionSink
    {
        private readonly ReportingChannel _channel;
        private readonly ConcurrentDictionary<string, TestState> _runningTests;

        public TestExecutionSink(ReportingChannel channel)
        {
            _channel = channel;

            _runningTests = new ConcurrentDictionary<string, TestState>();
        }

        public void RecordStart(Test test)
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

            _channel.Send(new Message
            {
                MessageType = "TestExecution.TestStarted",
                Payload = JToken.FromObject(test),
            });
        }

        public void RecordResult(TestResult testResult)
        {
            if (testResult == null)
            {
                throw new ArgumentNullException(nameof(testResult));
            }

            if (testResult.StartTime == default(DateTimeOffset) && testResult.Test.FullyQualifiedName != null)
            {
                TestState state;
                _runningTests.TryRemove(testResult.Test.FullyQualifiedName, out state);

                testResult.StartTime = state.StartTime;
            }

            if (testResult.EndTime == default(DateTimeOffset))
            {
                testResult.EndTime = DateTimeOffset.Now;
            }

            _channel.Send(new Message
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