using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Cli.SlnFile
{
    	public class SlnSection
	{
		SlnPropertySetCollection nestedPropertySets;
		SlnPropertySet properties;
		List<string> sectionLines;
		int baseIndex;

		public string Id { get; set; }
		public int Line { get; private set; }

		internal bool Processed { get; set; }

		public SlnFile ParentFile { get; internal set; }

		public bool IsEmpty {
			get {
				return (properties == null || properties.Count == 0) && (nestedPropertySets == null || nestedPropertySets.All (t => t.IsEmpty)) && (sectionLines == null || sectionLines.Count == 0);
			}
		}

		/// <summary>
		/// If true, this section won't be written to the file if it is empty
		/// </summary>
		/// <value><c>true</c> if skip if empty; otherwise, <c>false</c>.</value>
		public bool SkipIfEmpty { get; set; }

		public void Clear ()
		{
			properties = null;
			nestedPropertySets = null;
			sectionLines = null;
		}

		public SlnPropertySet Properties {
			get {
				if (properties == null) {
					properties = new SlnPropertySet ();
					properties.ParentSection = this;
					if (sectionLines != null) {
						foreach (var line in sectionLines)
							properties.ReadLine (line, Line);
						sectionLines = null;
					}
				}
				return properties;
			}
		}

		public SlnPropertySetCollection NestedPropertySets {
			get {
				if (nestedPropertySets == null) {
					nestedPropertySets = new SlnPropertySetCollection (this);
					if (sectionLines != null)
						LoadPropertySets ();
				}
				return nestedPropertySets;
			}
		}

		public void SetContent (IEnumerable<KeyValuePair<string,string>> lines)
		{
			sectionLines = new List<string> (lines.Select (p => p.Key + " = " + p.Value));
			properties = null;
			nestedPropertySets = null;
		}

		public IEnumerable<KeyValuePair<string,string>> GetContent ()
		{
			if (sectionLines != null)
				return sectionLines.Select (li => {
					int i = li.IndexOf ('=');
					if (i != -1)
						return new KeyValuePair<string,string> (li.Substring (0, i).Trim(), li.Substring (i + 1).Trim());
					else
						return new KeyValuePair<string,string> (li.Trim (), "");
				});
			else
				return new KeyValuePair<string,string> [0];
		}

		public SlnSectionType SectionType { get; set; }

		SlnSectionType ToSectionType (int curLineNum, string s)
		{
			if (s == "preSolution" || s == "preProject")
				return SlnSectionType.PreProcess;
			if (s == "postSolution" || s == "postProject")
				return SlnSectionType.PostProcess;
			throw new InvalidSolutionFormatException (curLineNum, "Invalid section type: " + s);
		}

		string FromSectionType (bool isProjectSection, SlnSectionType type)
		{
			if (type == SlnSectionType.PreProcess)
				return isProjectSection ? "preProject" : "preSolution";
			else
				return isProjectSection ? "postProject" : "postSolution";
		}

		internal void Read (TextReader reader, string line, ref int curLineNum)
		{
			Line = curLineNum;
			int k = line.IndexOf ('(');
			if (k == -1)
				throw new InvalidSolutionFormatException (curLineNum, "Section id missing");
			var tag = line.Substring (0, k).Trim ();
			var k2 = line.IndexOf (')', k);
			if (k2 == -1)
				throw new InvalidSolutionFormatException (curLineNum);
			Id = line.Substring (k + 1, k2 - k - 1);

			k = line.IndexOf ('=', k2);
			SectionType = ToSectionType (curLineNum, line.Substring (k + 1).Trim ());

			var endTag = "End" + tag;

			sectionLines = new List<string> ();
			baseIndex = ++curLineNum;
			while ((line = reader.ReadLine()) != null) {
				curLineNum++;
				line = line.Trim ();
				if (line == endTag)
					break;
				sectionLines.Add (line);
			}
			if (line == null)
				throw new InvalidSolutionFormatException (curLineNum, "Closing section tag not found");
		}

		void LoadPropertySets ()
		{
			if (sectionLines != null) {
				SlnPropertySet curSet = null;
				for (int n = 0; n < sectionLines.Count; n++) {
					var line = sectionLines [n];
					if (string.IsNullOrEmpty (line.Trim ()))
						continue;
					var i = line.IndexOf ('.');
					if (i == -1)
						throw new InvalidSolutionFormatException (baseIndex + n);
					var id = line.Substring (0, i);
					if (curSet == null || id != curSet.Id) {
						curSet = new SlnPropertySet (id);
						nestedPropertySets.Add (curSet);
					}
					curSet.ReadLine (line.Substring (i + 1), baseIndex + n);
				}
				sectionLines = null;
			}
		}

		internal void Write (TextWriter writer, string sectionTag)
		{
			if (SkipIfEmpty && IsEmpty)
				return;

			writer.Write ("\t");
			writer.Write (sectionTag);
			writer.Write ('(');
			writer.Write (Id);
			writer.Write (") = ");
			writer.WriteLine (FromSectionType (sectionTag == "ProjectSection", SectionType));
			if (sectionLines != null) {
				foreach (var l in sectionLines)
					writer.WriteLine ("\t\t" + l);
			} else if (properties != null)
				properties.Write (writer);
			else if (nestedPropertySets != null) {
				foreach (var ps in nestedPropertySets)
					ps.Write (writer);
			}
			writer.WriteLine ("\tEnd" + sectionTag);
		}
	}

}