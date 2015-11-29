using System.IO;
using Newtonsoft.Json;

namespace Microsoft.Extensions.Testing.Abstractions
{
    class LineDelimitedJsonStream
    {
        private readonly StreamWriter _stream;

        public LineDelimitedJsonStream(Stream stream)
        {
            _stream = new StreamWriter(stream);
        }

        public void Send(object @object)
        {
            _stream.WriteLine(JsonConvert.SerializeObject(@object));

            _stream.Flush();
        }
    }
}