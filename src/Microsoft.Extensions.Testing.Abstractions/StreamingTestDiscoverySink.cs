using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.Testing.Abstractions
{
    public class StreamingTestDiscoverySink : ITestDiscoverySink
    {
        private readonly LineDelimitedJsonStream _stream;

        public StreamingTestDiscoverySink(Stream stream)
        {
            _stream = new LineDelimitedJsonStream(stream);
        }

        public void SendTest(Test test)
        {
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }

            _stream.Send(new Message
            {
                MessageType = "TestDiscovery.TestFound",
                Payload = JToken.FromObject(test),
            });
        }
    }
}