using System;
using Xunit.Abstractions;

namespace Xunit.Runner.Dnx
{
    public class DiagnosticMessageVisitor : TestMessageVisitor
    {
        readonly string assemblyDisplayName;
        readonly object consoleLock;
        readonly bool noColor;
        readonly bool showDiagnostics;

        public DiagnosticMessageVisitor(object consoleLock, string assemblyDisplayName, bool showDiagnostics, bool noColor)
        {
            this.noColor = noColor;
            this.consoleLock = consoleLock;
            this.assemblyDisplayName = assemblyDisplayName;
            this.showDiagnostics = showDiagnostics;
        }

        protected override bool Visit(IDiagnosticMessage diagnosticMessage)
        {
            if (showDiagnostics)
                lock (consoleLock)
                {
                    if (!noColor)
                        Console.ForegroundColor = ConsoleColor.Yellow;

                    Console.WriteLine("   {0}: {1}", assemblyDisplayName, diagnosticMessage.Message);

                    if (!noColor)
                        Console.ForegroundColor = ConsoleColor.Gray;
                }

            return base.Visit(diagnosticMessage);
        }
    }
}
