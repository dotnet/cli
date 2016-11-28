using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Cli.SlnFile.Services
{
    public static class MSBuildProjectService
    {
        internal static bool FromMSBuildPath (string basePath, string relPath, out string resultPath)
		{
			resultPath = relPath;
			
			if (string.IsNullOrEmpty (relPath))
				return false;
			
			string path = UnescapePath (relPath);

			if (char.IsLetter (path [0]) && path.Length > 1 && path[1] == ':') {
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
					resultPath = path; // Return the escaped value
					return true;
				} else
					return false;
			}
			
			bool isRooted = Path.IsPathRooted (path);
			
			if (!isRooted && basePath != null) {
				path = Path.Combine (basePath, path);
				isRooted = Path.IsPathRooted (path);
			}
			
			// Return relative paths as-is, we can't do anything else with them
			if (!isRooted) {
				// resultPath = FileService.NormalizeRelativePath (path);
				return true;
			}
			
			// If we're on Windows, don't need to fix file casing.
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				// resultPath = FileService.GetFullPath (path);
				return true;
			}
			
			// If the path exists with the exact casing specified, then we're done
			if (System.IO.File.Exists (path) || System.IO.Directory.Exists (path)){
				resultPath = Path.GetFullPath (path);
				return true;
			}
			
			// Not on Windows, file doesn't exist. That could mean the project was brought from Windows
			// and the filename case in the project doesn't match the file on disk, because Windows is
			// case-insensitive. Since we have an absolute path, search the directory for the file with
			// the correct case.
			string[] names = path.Substring (1).Split ('/');
			string part = "/";
			
			for (int n=0; n<names.Length; n++) {
				string[] entries;

				if (names [n] == ".."){
					if (part == "/")
						return false; // Can go further back. It's not an existing file
					part = Path.GetFullPath (part + "/..");
					continue;
				}
				
				entries = Directory.GetFileSystemEntries (part);
				
				string fpath = null;
				foreach (string e in entries) {
					if (string.Compare (Path.GetFileName (e), names [n], StringComparison.OrdinalIgnoreCase) == 0) {
						fpath = e;
						break;
					}
				}
				if (fpath == null) {
					// Part of the path does not exist. Can't do any more checking.
					part = Path.GetFullPath (part);
					for (; n < names.Length; n++)
						part += "/" + names[n];
					resultPath = part;
					return true;
				}

				part = fpath;
			}
			resultPath = Path.GetFullPath (part);
			return true;
		}

		public static string FromMSBuildPath (string basePath, string relPath)
		{
			string res;
			FromMSBuildPath (basePath, relPath, out res);
			return res;
		}

		public static string ToMSBuildPath (string baseDirectory, string absPath, bool normalize = true)
		{
			if (string.IsNullOrEmpty (absPath))
				return absPath;
			if (baseDirectory != null) {
				absPath = FileService.AbsoluteToRelativePath (baseDirectory, absPath);
				if (normalize)
					// absPath = FileService.NormalizeRelativePath (absPath);
                    throw new NotImplementedException();
			}
			return EscapeString (absPath).Replace ('/', '\\');
		}

        public static string UnescapePath (string path)
		{
			if (string.IsNullOrEmpty (path))
				return path;
			
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				path = path.Replace ("\\", "/");
			
			return UnscapeString (path);
		}

		public static string UnscapeString (string str)
		{
			int i = str.IndexOf ('%');
			while (i != -1 && i < str.Length - 2) {
				int c;
				if (int.TryParse (str.Substring (i+1, 2), NumberStyles.HexNumber, null, out c))
					str = str.Substring (0, i) + (char) c + str.Substring (i + 3);
				i = str.IndexOf ('%', i + 1);
			}
			return str;
		}
        
        static char[] specialCharacters = new char [] {'%', '$', '@', '(', ')', '\'', ';', '?' };

        public static string EscapeString (string str)
		{
			int i = str.IndexOfAny (specialCharacters);
			while (i != -1) {
				str = str.Substring (0, i) + '%' + ((int) str [i]).ToString ("X") + str.Substring (i + 1);
				i = str.IndexOfAny (specialCharacters, i + 3);
			}
			return str;
		}

    }
}