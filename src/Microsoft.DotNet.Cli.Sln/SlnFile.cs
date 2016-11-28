//
// SlnFile.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Collections;
//using MonoDevelop.Core;
//using MonoDevelop.Projects.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection;
using Microsoft.DotNet.Cli.SlnFile.FileManipulation;
using Microsoft.DotNet.Cli.SlnFile.Services;

namespace Microsoft.DotNet.Cli.SlnFile
{
	public class SlnFile
	{
		SlnProjectCollection projects = new SlnProjectCollection ();
		SlnSectionCollection sections = new SlnSectionCollection ();
		SlnPropertySet metadata = new SlnPropertySet (true);
		int prefixBlankLines = 1;
		TextFormatInfo format = new TextFormatInfo { NewLine = "\r\n" };

		public string FormatVersion { get; set; }
		public string ProductDescription { get; set; }

		public string VisualStudioVersion {
			get { return metadata.GetValue ("VisualStudioVersion"); }
			set { metadata.SetValue ("VisualStudioVersion", value); }
		}

		public string MinimumVisualStudioVersion {
			get { return metadata.GetValue ("MinimumVisualStudioVersion"); }
			set { metadata.SetValue ("MinimumVisualStudioVersion", value); }
		}

		public SlnFile ()
		{
			projects.ParentFile = this;
			sections.ParentFile = this;
		}

		/// <summary>
		/// Gets the sln format version of the provided solution file
		/// </summary>
		/// <returns>The file version.</returns>
		/// <param name="file">File.</param>
		public static string GetFileVersion (string file)
		{
			string strVersion;
			using (var reader = new StreamReader (new FileStream(file, FileMode.Open))) {
				var strInput = reader.ReadLine();
				if (strInput == null)
					return null;

				var match = slnVersionRegex.Match (strInput);
				if (!match.Success) {
					strInput = reader.ReadLine();
					if (strInput == null)
						return null;
					match = slnVersionRegex.Match (strInput);
					if (!match.Success)
						return null;
				}

				strVersion = match.Groups[1].Value;
				return strVersion;
			}
		}

		static Regex slnVersionRegex = new Regex (@"Microsoft Visual Studio Solution File, Format Version (\d?\d.\d\d)");

		/// <summary>
		/// The directory to be used as base for converting absolute paths to relative
		/// </summary>
		public FilePath BaseDirectory {
			get { return FileName.ParentDirectory; }
		}

		/// <summary>
		/// Gets the solution configurations section.
		/// </summary>
		/// <value>The solution configurations section.</value>
		public SlnPropertySet SolutionConfigurationsSection {
			get { return sections.GetOrCreateSection ("SolutionConfigurationPlatforms", SlnSectionType.PreProcess).Properties; }
		}

		/// <summary>
		/// Gets the project configurations section.
		/// </summary>
		/// <value>The project configurations section.</value>
		public SlnPropertySetCollection ProjectConfigurationsSection {
			get { return sections.GetOrCreateSection ("ProjectConfigurationPlatforms", SlnSectionType.PostProcess).NestedPropertySets; }
		}

		public SlnSectionCollection Sections {
			get { return sections; }
		}

		public SlnProjectCollection Projects {
			get { return projects; }
		}

		public FilePath FileName { get; set; }

		public void Read (string file)
		{
			FileName = file;
			format = FileUtil.GetTextFormatInfo (file);

			using (var sr = new StreamReader (new FileStream(file, FileMode.Open)))
				Read (sr);
		}

		public void Read (TextReader reader)
		{
			string line;
			int curLineNum = 0;
			bool globalFound = false;
			bool productRead = false;

			while ((line = reader.ReadLine ()) != null) {
				curLineNum++;
				line = line.Trim ();
				if (line.StartsWith ("Microsoft Visual Studio Solution File", StringComparison.Ordinal)) {
					int i = line.LastIndexOf (' ');
					if (i == -1)
						throw new InvalidSolutionFormatException (curLineNum);
					FormatVersion = line.Substring (i + 1);
					prefixBlankLines = curLineNum - 1;
				}
				if (line.StartsWith ("# ", StringComparison.Ordinal)) {
					if (!productRead) {
						productRead = true;
						ProductDescription = line.Substring (2);
					}
				} else if (line.StartsWith ("Project", StringComparison.Ordinal)) {
					SlnProject p = new SlnProject ();
					p.Read (reader, line, ref curLineNum);
					projects.Add (p);
				} else if (line == "Global") {
					if (globalFound)
						throw new InvalidSolutionFormatException (curLineNum, "Global section specified more than once");
					globalFound = true;
					while ((line = reader.ReadLine ()) != null) {
						curLineNum++;
						line = line.Trim ();
						if (line == "EndGlobal") {
							break;
						} else if (line.StartsWith ("GlobalSection", StringComparison.Ordinal)) {
							var sec = new SlnSection ();
							sec.Read (reader, line, ref curLineNum);
							sections.Add (sec);
						} else
							throw new InvalidSolutionFormatException (curLineNum);
					}
					if (line == null)
						throw new InvalidSolutionFormatException (curLineNum, "Global section not closed");
				} else if (line.IndexOf ('=') != -1) {
					metadata.ReadLine (line, curLineNum);
				}
			}
			if (FormatVersion == null)
				throw new InvalidSolutionFormatException (curLineNum, "File header is missing");
		}

		public void Write (string file)
		{
			FileName = file;
			var sw = new StringWriter ();
			Write (sw);
			TextFile.WriteFile (file, sw.ToString(), format.ByteOrderMark, true);
		}

		public void Write (TextWriter writer)
		{
			writer.NewLine = format.NewLine;
			for (int n=0; n<prefixBlankLines; n++)
				writer.WriteLine ();
			writer.WriteLine ("Microsoft Visual Studio Solution File, Format Version " + FormatVersion);
			writer.WriteLine ("# " + ProductDescription);

			metadata.Write (writer);

			foreach (var p in projects)
				p.Write (writer);

			writer.WriteLine ("Global");
			foreach (SlnSection s in sections)
				s.Write (writer, "GlobalSection");
			writer.WriteLine ("EndGlobal");
		}
	}



	/// <summary>
	/// A collection of properties
	/// </summary>





	public enum SlnSectionType
	{
		PreProcess,
		PostProcess
	}
}

