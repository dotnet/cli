// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.DotNet.Cli.Utils
{
    public class PerfTraceOutput
    {
        public static void Print(Reporter reporter, IEnumerable<PerfTraceThreadContext> contexts)
        {
            Print(reporter, contexts.Select(c => c.Root), null);
        }

        private static void Print(Reporter reporter, IEnumerable<PerfTraceEvent> events, PerfTraceEvent parent, int padding = 0)
        {
            foreach (var e in events)
            {
                reporter.Write(new string(' ', padding));
                reporter.WriteLine(FormatEvent(e, parent));
                Print(reporter, e.Children, e, padding + 2);
            }
        }

        private static string FormatEvent(PerfTraceEvent e, PerfTraceEvent parent)
        {
            var builder = new StringBuilder();
            FormatEventTimeStat(builder, e, parent);
            builder.Append($" {e.Type.Bold()} {e.Instance}");
            return builder.ToString();
        }

        private static void FormatEventTimeStat(StringBuilder builder, PerfTraceEvent e, PerfTraceEvent parent)
        {
            builder.Append("[");
            var percent = e.Duration.TotalSeconds / parent?.Duration.TotalSeconds;
            if (percent != null)
            {
                var formattedPercent = $"{percent * 100:00\\.00%}";
                if (percent > 0.5)
                {
                    builder.Append(formattedPercent.Red());
                }
                else if (percent > 0.25)
                {
                    builder.Append(formattedPercent.Yellow());
                }
                else if (percent < 0.1)
                {
                    builder.Append(formattedPercent.White());
                }
                else
                {
                    builder.Append(formattedPercent);
                }
                builder.Append(":");
            }
            builder.Append($"{e.Duration.ToString("ss\\.fff").Blue()}]");
        }
    }
}