using System;

namespace Microsoft.DotNet.Cli.Utils
{
    internal class CommandUnknownException : Exception
    {
        public CommandUnknownException()
        {
        }

        public CommandUnknownException(string message) : base(message)
        {
        }

        public CommandUnknownException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}