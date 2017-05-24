using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Cli.SlnFile.Services
{
    public static class FileService
    {
        static readonly string wildcardMarker = "_" + Guid.NewGuid ().ToString () + "_";

    	public unsafe static string AbsoluteToRelativePath (string baseDirectoryPath, string absPath)
		{
			if (!Path.IsPathRooted (absPath) || string.IsNullOrEmpty (baseDirectoryPath))
				return absPath;

			absPath = GetFullPath (absPath);
			baseDirectoryPath = GetFullPath (baseDirectoryPath).TrimEnd (Path.DirectorySeparatorChar);

			fixed (char* bPtr = baseDirectoryPath, aPtr = absPath) {
				var bEnd = bPtr + baseDirectoryPath.Length;
				var aEnd = aPtr + absPath.Length;
				char* lastStartA = aEnd;
				char* lastStartB = bEnd;

				int indx = 0;
				// search common base path
				var a = aPtr;
				var b = bPtr;
				while (a < aEnd) {
					if (!PathCharsAreEqual (*a, *b))
						break;
					if (IsSeparator (*a)) {
						indx++;
						lastStartA = a + 1;
						lastStartB = b;
					}
					a++;
					b++;
					if (b >= bEnd) {
						if (a >= aEnd || IsSeparator (*a)) {
							indx++;
							lastStartA = a + 1;
							lastStartB = b;
						}
						break;
					}
				}
				if (indx == 0)
					return absPath;

				if (lastStartA >= aEnd)
					return ".";

				// handle case a: some/path b: some/path/deeper...
				if (a >= aEnd) {
					if (IsSeparator (*b)) {
						lastStartA = a + 1;
						lastStartB = b;
					}
				}

				// look how many levels to go up into the base path
				int goUpCount = 0;
				while (lastStartB < bEnd) {
					if (IsSeparator (*lastStartB))
						goUpCount++;
					lastStartB++;
				}
				var size = goUpCount * 2 + goUpCount + aEnd - lastStartA;
				var result = new char [size];
				fixed (char* rPtr = result) {
					// go paths up
					var r = rPtr;
					for (int i = 0; i < goUpCount; i++) {
						*(r++) = '.';
						*(r++) = '.';
						*(r++) = Path.DirectorySeparatorChar;
					}
					// copy the remaining absulute path
					while (lastStartA < aEnd)
						*(r++) = *(lastStartA++);
				}
				return new string (result);
			}
		}
        public static string GetFullPath (string path)
		{
			if (path == null)
				throw new ArgumentNullException ("path");
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || path.IndexOf ('*') == -1)
				return Path.GetFullPath (path);
			else {
				// On Windows, GetFullPath doesn't work if the path contains wildcards.
				path = path.Replace ("*", wildcardMarker);
				path = Path.GetFullPath (path);
				return path.Replace (wildcardMarker, "*");
			}
		}

        public static bool PathCharsAreEqual(char a, char b)
        {
            // throw new NotImplementedException();
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				return PathCharsAreEqualCaseInsensitive(a, b);
			}
			else
			{
				return PathCharsAreEqualCaseSensitive(a, b);
			}
        }

		static char ToOrdinalIgnoreCase (char c)
		{
			return (((uint) c - 'a') <= ((uint) 'z' - 'a')) ? (char) (c - 0x20) : c;
		}
		static bool PathCharsAreEqualCaseInsensitive (char a, char b)
		{
			a = ToOrdinalIgnoreCase (a);
			b = ToOrdinalIgnoreCase (b);

			return a == b;
		}

		static bool PathCharsAreEqualCaseSensitive (char a, char b)
		{
			return a == b;
		}


		static bool IsSeparator (char ch)
		{
			return ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar || ch == Path.VolumeSeparatorChar;
		}
        

    }
}