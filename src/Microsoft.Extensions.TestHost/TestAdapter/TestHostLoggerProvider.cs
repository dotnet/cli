// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.TestHost
{
    public class TestHostLoggerProvider : ILoggerProvider
    {
        private readonly ReportingChannel _channel;

        public TestHostLoggerProvider(ReportingChannel channel)
        {
            _channel = channel;
        }

        public ILogger CreateLogger(string name)
        {
            return new TestHostLogger(name, _channel);
        }

        public void Dispose()
        {
        }

        private class TestHostLogger : ILogger
        {
            private readonly string _name;
            private readonly ReportingChannel _channel;

            public TestHostLogger(string name, ReportingChannel channel)
            {
                _name = name;
                _channel = channel;
            }

            public IDisposable BeginScopeImpl(object state)
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                // Don't filter - send everything to the server.
                return true;
            }

            public void Log(
                LogLevel logLevel,
                int eventId,
                object state,
                Exception exception,
                Func<object, Exception, string> formatter)
            {
                string message = null;
                if (formatter != null)
                {
                    message = formatter(state, exception);
                }
                else if (state != null)
                {
                    message = state.ToString();
                    if (exception != null)
                    {
                        message += Environment.NewLine + exception.ToString();
                    }
                }
                else if (exception != null)
                {
                    message = exception.ToString();
                }

                if (string.IsNullOrEmpty(message))
                {
                    return;
                }

                _channel.Send(new Message()
                {
                    MessageType = "Log",
                    Payload = JToken.FromObject(new LogMessage()
                    {
                        Name = _name,
                        EventId = eventId,
                        Level = logLevel.ToString(),
                        Message = message,
                    }),
                });
            }
        }
    }
}
