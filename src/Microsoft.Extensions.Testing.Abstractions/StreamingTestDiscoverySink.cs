using System;
using System.IO;
using Newtonsoft.Json.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.Extensions.Testing.Abstractions
{
    public class StreamingTestDiscoverySink : StreamingTestSink, ITestDiscoverySink
    {
        public StreamingTestDiscoverySink(Stream stream) : base(stream)
        {
        }

        public void SendTestFound(TestCase test)
        {
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }

            Stream.Send(new Message
            {
                MessageType = "TestDiscovery.TestFound",
                Payload = JToken.FromObject(test),
            });
        }
    }
}