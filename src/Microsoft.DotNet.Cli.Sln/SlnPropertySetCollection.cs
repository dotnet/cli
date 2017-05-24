using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.DotNet.Cli.SlnFile
{
    	public class SlnPropertySetCollection: Collection<SlnPropertySet>
	{
		SlnSection parentSection;

		internal SlnPropertySetCollection (SlnSection parentSection)
		{
			this.parentSection = parentSection;
		}

		public SlnPropertySet GetPropertySet (string id, bool ignoreCase = false)
		{
			var sc = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			return this.FirstOrDefault (s => s.Id.Equals (id, sc));
		}

		public SlnPropertySet GetOrCreatePropertySet (string id, bool ignoreCase = false)
		{
			var ps = GetPropertySet (id, ignoreCase);
			if (ps == null) {
				ps = new SlnPropertySet (id);
				Add (ps);
			}
			return ps;
		}

		protected override void InsertItem (int index, SlnPropertySet item)
		{
			base.InsertItem (index, item);
			item.ParentSection = parentSection;
		}

		protected override void SetItem (int index, SlnPropertySet item)
		{
			base.SetItem (index, item);
			item.ParentSection = parentSection;
		}

		protected override void RemoveItem (int index)
		{
			var it = this [index];
			it.ParentSection = null;
			base.RemoveItem (index);
		}

		protected override void ClearItems ()
		{
			foreach (var it in this)
				it.ParentSection = null;
			base.ClearItems ();
		}
	}

}