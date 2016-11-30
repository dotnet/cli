//
// FileUtil.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using MonoDevelop.Projects.Utility;

namespace MonoDevelop.Projects.MSBuild
{
	static class FileUtil
	{
		public static TextFormatInfo GetTextFormatInfo (string file)
		{
			var info = new TextFormatInfo ();

			string newLine = null;
			ByteOrderMark bom;

			using (FileStream fs = File.OpenRead (file)) {
				byte[] buf = new byte [1024];
				int nread, i;

				if ((nread = fs.Read (buf, 0, buf.Length)) <= 0)
					return info;

				if (ByteOrderMark.TryParse (buf, nread, out bom))
					i = bom.Length;
				else
					i = 0;

				do {
					// Read to the first newline to figure out which line endings this file is using
					while (i < nread) {
						if (buf[i] == '\r') {
							newLine = "\r\n";
							break;
						} else if (buf[i] == '\n') {
							newLine = "\n";
							break;
						}

						i++;
					}

					if (newLine == null) {
						if ((nread = fs.Read (buf, 0, buf.Length)) <= 0) {
							newLine = "\n";
							break;
						}

						i = 0;
					}
				} while (newLine == null);

				// Check for a blank line at the end
				info.EndsWithEmptyLine = fs.Seek (-1, SeekOrigin.End) > 0 && fs.ReadByte () == (int) '\n';
				info.NewLine = newLine;
				info.ByteOrderMark = bom;
				return info;
			}
		}
	}

	class TextFormatInfo
	{
		public TextFormatInfo ()
		{
			NewLine = Environment.NewLine;
		}

		public string NewLine { get; set; }
		public ByteOrderMark ByteOrderMark { get; set; }
		public bool EndsWithEmptyLine { get; set; }
	}
}

