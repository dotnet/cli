using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Cli.Utils
{
	internal static class PAL
	{
		public static readonly string CompilerName;
		public static readonly string ExecutableNameFormatter;
		
		static PAL()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				// Temporarily use Mono
				CompilerName = "mcs";
				ExecutableNameFormatter = "{0}";
			}
			else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				// Temporarily use Mono
				CompilerName = "mcs";
				ExecutableNameFormatter = "{0}";
			}
			else if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				CompilerName = "csc";
				ExecutableNameFormatter = "{0}.exe";
			}
		}
		
		public static string ExeName(string baseName) 
		{
			return string.Format(ExecutableNameFormatter, baseName);
		}
	}
}