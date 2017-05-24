using System;
using System.IO;

namespace Microsoft.DotNet.Cli.SlnFile
{
    	public class SlnProject
	{
		SlnSectionCollection sections = new SlnSectionCollection ();

		SlnFile parentFile;

		public SlnFile ParentFile {
			get {
				return parentFile;
			}
			internal set {
				parentFile = value;
				sections.ParentFile = parentFile;
			}
		}

		public string Id { get; set; }
		public string TypeGuid { get; set; }
		public string Name { get; set; }
		public string FilePath { get; set; }
		public int Line { get; private set; }
		internal bool Processed { get; set; }

		public SlnSectionCollection Sections {
			get { return sections; }
		}

		internal void Read (TextReader reader, string line, ref int curLineNum)
		{
			Line = curLineNum;

			int n = 0;
			FindNext (curLineNum, line, ref n, '(');
			n++;
			FindNext (curLineNum, line, ref n, '"');
			int n2 = n + 1;
			FindNext (curLineNum, line, ref n2, '"');
			TypeGuid = line.Substring (n + 1, n2 - n - 1);

			n = n2 + 1;
			FindNext (curLineNum, line, ref n, ')');
			FindNext (curLineNum, line, ref n, '=');

			FindNext (curLineNum, line, ref n, '"');
			n2 = n + 1;
			FindNext (curLineNum, line, ref n2, '"');
			Name = line.Substring (n + 1, n2 - n - 1);

			n = n2 + 1;
			FindNext (curLineNum, line, ref n, ',');
			FindNext (curLineNum, line, ref n, '"');
			n2 = n + 1;
			FindNext (curLineNum, line, ref n2, '"');
			FilePath = line.Substring (n + 1, n2 - n - 1);

			n = n2 + 1;
			FindNext (curLineNum, line, ref n, ',');
			FindNext (curLineNum, line, ref n, '"');
			n2 = n + 1;
			FindNext (curLineNum, line, ref n2, '"');
			Id = line.Substring (n + 1, n2 - n - 1);

			while ((line = reader.ReadLine ()) != null) {
				curLineNum++;
				line = line.Trim ();
				if (line == "EndProject") {
					return;
				}
				if (line.StartsWith ("ProjectSection", StringComparison.Ordinal)) {
					if (sections == null)
						sections = new SlnSectionCollection ();
					var sec = new SlnSection ();
					sections.Add (sec);
					sec.Read (reader, line, ref curLineNum);
				}
			}

			throw new InvalidSolutionFormatException (curLineNum, "Project section not closed");
		}

		void FindNext (int ln, string line, ref int i, char c)
		{
			i = line.IndexOf (c, i);
			if (i == -1)
				throw new InvalidSolutionFormatException (ln);
		}

		public void Write (TextWriter writer)
		{
			writer.Write ("Project(\"");
			writer.Write (TypeGuid);
			writer.Write ("\") = \"");
			writer.Write (Name);
			writer.Write ("\", \"");
			writer.Write (FilePath);
			writer.Write ("\", \"");
			writer.Write (Id);
			writer.WriteLine ("\"");
			if (sections != null) {
				foreach (SlnSection s in sections)
					s.Write (writer, "ProjectSection");
			}
			writer.WriteLine ("EndProject");
		}
	}

}