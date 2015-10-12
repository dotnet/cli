using System;

namespace Microsoft.DotNet.Cli.Utils
{
    public class Log : ILog
    {
        public Log()
        {
            _level = LogLevel.Error;
        }

        public Log(int level)
        {
            if (level < LogLevel.Error) level = LogLevel.Error;
            if (level > LogLevel.Debug) level = LogLevel.Debug;
            _level = level;
        }

        private int _level;
        public int Level
        {
            get
            {
                return _level;

            }
            set
            {
                if (value < LogLevel.Error)
                {
                    _level = LogLevel.Error;
                }
                else if (value > LogLevel.Debug)
                {
                    _level = LogLevel.Debug;
                }
                else
                {
                    _level = value;
                }
            }
        }

        public void Debug(string text)
        {
            if (Level < LogLevel.Debug) return;
            Console.WriteLine(text);
        }

        public void Debug(string format, params object[] args)
        {
            if (Level < LogLevel.Debug) return;
            Console.WriteLine(format, args);
        }

        public void Info(string text)
        {
            if (Level < LogLevel.Info) return;
            Console.WriteLine(text);
        }

        public void Info(string format, params object[] args)
        {
            if (Level < LogLevel.Info) return;
            Console.WriteLine(format, args);
        }

        public void Warning(string text)
        {
            if (Level < LogLevel.Warning) return;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public void Warning(string format, params object[] args)
        {
            if (Level < LogLevel.Warning) return;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(format, args);
            Console.ResetColor();
        }

        public void Error(string text)
        {
            if (Level < LogLevel.Error) return;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public void Error(string format, params object[] args)
        {
            if (Level < LogLevel.Error) return;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, args);
            Console.ResetColor();
        }
    }
}
