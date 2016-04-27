using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.DotNet.Cli.Utils
{
    public class PerfTraceEvent
    {
        public string Type { get; }
        public string Instance { get; }
        public DateTime StartUtc { get; }
        public TimeSpan Duration { get; }
        public IList<PerfTraceEvent> Children { get; }

        public PerfTraceEvent(string type, string instance, IEnumerable<PerfTraceEvent> children, DateTime startUtc, TimeSpan duration)
        {
            Type = type;
            Instance = instance;
            StartUtc = startUtc;
            Duration = duration;
            Children = children.OrderBy(e => e.StartUtc).ToList();
        }
    }

    public static class PerfTrace
    {
        private static ConcurrentBag<PerfTraceThreadContext> _threads = new ConcurrentBag<PerfTraceThreadContext>();

        [ThreadStatic]
        private static PerfTraceThreadContext _current;

        public static bool Enabled { get; set; }

        public static PerfTraceThreadContext Current => _current ?? (_current = InitializeCurrent());

        private static PerfTraceThreadContext InitializeCurrent()
        {
            var context = new PerfTraceThreadContext(Thread.CurrentThread.ManagedThreadId);
            _threads.Add(context);
            return context;
        }

        public static IEnumerable<PerfTraceThreadContext> GetEvents()
        {
            return _threads;
        }
    }

    public class PerfTraceThreadContext
    {
        private readonly int _threadId;

        private TimerDisposable _activeEvent;

        public PerfTraceEvent Root => _activeEvent.CreateEvent();

        public PerfTraceThreadContext(int threadId)
        {
            _activeEvent = new TimerDisposable(this, "Thread", $"{threadId.ToString()}");
            _threadId = threadId;
        }

        public IDisposable CaptureTiming(string instance = "", [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            if(!PerfTrace.Enabled)
            {
                return null;
            }

            var newTimer = new TimerDisposable(this, $"{Path.GetFileNameWithoutExtension(filePath)}:{memberName}", instance);
            var previousTimer = Interlocked.Exchange(ref _activeEvent, newTimer);
            newTimer.Parent = previousTimer;
            return newTimer;
        }

        private void RecordTiming(PerfTraceEvent newEvent, TimerDisposable parent)
        {
            Interlocked.Exchange(ref _activeEvent, parent);
            _activeEvent.Children.Add(newEvent);
        }

        private class TimerDisposable : IDisposable
        {
            private readonly PerfTraceThreadContext _context;
            private string _eventType;
            private string _instance;
            private DateTime _startUtc;
            private Stopwatch _stopwatch = Stopwatch.StartNew();

            public TimerDisposable Parent { get; set; }

            public ConcurrentBag<PerfTraceEvent> Children { get; set; } = new ConcurrentBag<PerfTraceEvent>();

            public TimerDisposable(PerfTraceThreadContext context, string eventType, string instance)
            {
                _context = context;
                _eventType = eventType;
                _instance = instance;
                _startUtc = DateTime.UtcNow;
            }

            public void Dispose()
            {
                _stopwatch.Stop();

                _context.RecordTiming(CreateEvent(), Parent);
            }

            public PerfTraceEvent CreateEvent() => new PerfTraceEvent(_eventType, _instance, Children, _startUtc, _stopwatch.Elapsed);
        }
    }
}