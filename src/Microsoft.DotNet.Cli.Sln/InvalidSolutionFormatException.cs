using System;

namespace Microsoft.DotNet.Cli.SlnFile
{
    	class InvalidSolutionFormatException: Exception
	{
		public InvalidSolutionFormatException (int line): base ("Invalid format in line " + line)
		{
		}

		public InvalidSolutionFormatException (int line, string msg): base ("Invalid format in line " + line + ": " + msg)
		{
			
		}
	}

}