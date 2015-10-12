using System;

namespace Microsoft.DotNet.Cli.Utils
{
	public static class Csc
	{
		public static string Path 
		{
			get
			{
				string env = Environment.GetEnvironmentVariable("DOTNET_CSC");
				if (string.IsNullOrEmpty(env))
				{
					return PAL.CompilerName;
				}
				else
				{
					return env;
				}
			}
		}
	}
}