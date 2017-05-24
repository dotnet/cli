using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.DotNet.Cli.SlnFile
{
    	public class SlnProjectCollection: Collection<SlnProject>
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

		public SlnProject GetProject (string id)
		{
			return this.FirstOrDefault (s => s.Id == id);
		}

		public SlnProject GetOrCreateProject (string id)
		{
			var p = this.FirstOrDefault (s => s.Id.Equals (id, StringComparison.OrdinalIgnoreCase));
			if (p == null) {
				p = new SlnProject { Id = id };
				Add (p);
			}
			return p;
		}

		protected override void InsertItem (int index, SlnProject item)
		{
			base.InsertItem (index, item);
			item.ParentFile = ParentFile;
		}

		protected override void SetItem (int index, SlnProject item)
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