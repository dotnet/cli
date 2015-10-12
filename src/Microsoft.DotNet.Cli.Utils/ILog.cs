namespace Microsoft.DotNet.Cli.Utils
{
    interface ILog
    {
        int Level { get; set; }
        void Debug(string text);
        void Debug(string format, params object[] args);
        void Info(string text);
        void Info(string format, params object[] args);
        void Warning(string text);
        void Warning(string format, params object[] args);
        void Error(string text);
        void Error(string format, params object[] args);
    }
}
