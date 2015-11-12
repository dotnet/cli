using System;
using System.Collections.Generic;
using Microsoft.Extensions.Testing.Abstractions;
using Xunit.Abstractions;
using VsTestCase = Microsoft.Extensions.Testing.Abstractions.Test;

namespace Xunit.Runner.Dnx
{
    public class DesignTimeExecutionVisitor : TestMessageVisitor<ITestAssemblyFinished>, IExecutionVisitor
    {
        private readonly ITestExecutionSink _sink;
        private readonly IDictionary<ITestCase, VsTestCase> _conversions;
        private readonly IMessageSink _next;

        public DesignTimeExecutionVisitor(ITestExecutionSink sink, IDictionary<ITestCase, VsTestCase> conversions, IMessageSink next)
        {
            _sink = sink;
            _conversions = conversions;
            _next = next;

            ExecutionSummary = new ExecutionSummary();
        }

        public ExecutionSummary ExecutionSummary { get; private set; }

        protected override bool Visit(ITestStarting testStarting)
        {
            var test = _conversions[testStarting.TestCase];

            if (_sink != null)
                _sink.RecordStart(test);

            return true;
        }

        protected override bool Visit(ITestSkipped testSkipped)
        {
            var test = _conversions[testSkipped.TestCase];

            if (_sink != null)
                _sink.RecordResult(new TestResult(test) { Outcome = TestOutcome.Skipped });

            return true;
        }

        protected override bool Visit(ITestFailed testFailed)
        {
            var test = _conversions[testFailed.TestCase];
            var result = new TestResult(test)
            {
                Outcome = TestOutcome.Failed,
                Duration = TimeSpan.FromSeconds((double)testFailed.ExecutionTime),
                ErrorMessage = string.Join(Environment.NewLine, testFailed.Messages),
                ErrorStackTrace = string.Join(Environment.NewLine, testFailed.StackTraces),
            };

            result.Messages.Add(testFailed.Output);

            if (_sink != null)
                _sink.RecordResult(result);

            return true;
        }

        protected override bool Visit(ITestPassed testPassed)
        {
            var test = _conversions[testPassed.TestCase];

            if (_sink != null)
            {
                _sink.RecordResult(new TestResult(test)
                {
                    Outcome = TestOutcome.Passed,
                    Duration = TimeSpan.FromSeconds((double)testPassed.ExecutionTime),
                });
            }

            return true;
        }

        protected override bool Visit(ITestAssemblyFinished assemblyFinished)
        {
            var result = base.Visit(assemblyFinished);

            ExecutionSummary = new ExecutionSummary
            {
                Failed = assemblyFinished.TestsFailed,
                Skipped = assemblyFinished.TestsSkipped,
                Time = assemblyFinished.ExecutionTime,
                Total = assemblyFinished.TestsRun
            };

            return result;
        }

        public override bool OnMessage(IMessageSinkMessage message)
        {
            return
                base.OnMessage(message) &&
                _next.OnMessage(message);
        }
    }
}
