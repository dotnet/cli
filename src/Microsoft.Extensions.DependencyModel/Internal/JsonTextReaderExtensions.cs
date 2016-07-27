using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyModel.Internal
{
    internal static class JsonTextReaderExtensions
    {
        internal static bool TryReadStringProperty(this JsonTextReader reader, out string name, out string value)
        {
            name = null;
            value = null;

            if (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                name = (string)reader.Value;
                value = reader.ReadAsString();
                return true;
            }

            return false;
        }



        internal static void ReadStartObject(this JsonTextReader reader)
        {
            reader.Read();
            CheckStartObject(reader);
        }



        internal static void CheckStartObject(this JsonTextReader reader)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new FormatException($"Excepted '{{' line {reader.LineNumber}");
            }
        }


        internal static void CheckEndObject(this JsonTextReader reader)
        {
            if (reader.TokenType != JsonToken.EndObject)
            {
                throw new FormatException($"Excepted '}}' line {reader.LineNumber}");
            }
        }



        internal static string[] ReadStringArray(this JsonTextReader reader)
        {
            reader.Read();
            if (reader.TokenType != JsonToken.StartArray)
            {
                throw new FormatException($"Excepted '[' line {reader.LineNumber}");
            }

            var l = new List<string>();

            while (reader.Read() && reader.TokenType == JsonToken.String)
            {
                l.Add((string)reader.Value);
            }

            if (reader.TokenType != JsonToken.EndArray)
            {
                throw new FormatException($"Excepted ']' line {reader.LineNumber}");
            }

            return l.ToArray();
        }



        internal static Dictionary<string, string> ReadStringDictionary(this JsonTextReader reader)
        {
            reader.ReadStartObject();

            Dictionary<string, string> d = null;

            string name = null;
            string value = null;
            while (reader.TryReadStringProperty(out name, out value))
            {
                if (d == null)
                {
                    d = new Dictionary<string, string>();
                }
                d.Add(name, value);
            }

            CheckEndObject(reader);

            return d;
        }



        internal static void IgnoreStringDictionary(this JsonTextReader reader)
        {
            reader.ReadStartObject();

            string name = null;
            string value = null;
            while (reader.TryReadStringProperty(out name, out value))
            {
            }

            CheckEndObject(reader);
        }
    }
}
