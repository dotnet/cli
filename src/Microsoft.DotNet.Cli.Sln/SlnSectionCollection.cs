using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.DotNet.Cli.SlnFile
{
    	public class SlnSectionCollection: Collection<SlnSection>
	{
		SlnFile parentFile;

		internal SlnFile ParentFile {
			get {
				return parentFile;
			}
			set {
				parentFile = value;
				foreach (var it in this)
					it.ParentFile = parentFile;
			}
		}

		public SlnSection GetSection (string id)
		{
			return this.FirstOrDefault (s => s.Id == id);
		}

		public SlnSection GetSection (string id, SlnSectionType sectionType)
		{
			return this.FirstOrDefault (s => s.Id == id && s.SectionType == sectionType);
		}

		public SlnSection GetOrCreateSection (string id, SlnSectionType sectionType)
		{
			if (id == null)
				throw new ArgumentNullException ("id");
			var sec = this.FirstOrDefault (s => s.Id == id);
			if (sec == null) {
				sec = new SlnSection { Id = id };
				sec.SectionType = sectionType;
				Add (sec);
			}
			return sec;
		}

		public void RemoveSection (string id)
		{
			if (id == null)
				throw new ArgumentNullException ("id");
			var s = GetSection (id);
			if (s != null)
				Remove (s);
		}

		protected override void InsertItem (int index, SlnSection item)
		{
			base.InsertItem (index, item);
			item.ParentFile = ParentFile;
		}

		protected override void SetItem (int index, SlnSection item)
		{
			base.SetItem (index, item);
			item.ParentFile = ParentFile;
		}

		protected override void RemoveItem (int index)
		{
			var it = this [index];
			it.ParentFile = null;
			base.RemoveItem (index);
		}

		protected override void ClearItems ()
		{
			foreach (var it in this)
				it.ParentFile = null;
			base.ClearItems ();
		}
	}

}