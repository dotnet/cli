using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.DotNet.Cli.SlnFile.FileManipulation;
using Microsoft.DotNet.Cli.SlnFile.Services;

namespace Microsoft.DotNet.Cli.SlnFile
{
    	public class SlnPropertySet: IDictionary<string,string>
	{
		OrderedDictionary values = new OrderedDictionary ();
		bool isMetadata;

		internal bool Processed { get; set; }

		public SlnFile ParentFile {
			get { return ParentSection != null ? ParentSection.ParentFile : null; }
		}

		public SlnSection ParentSection { get; set; }

		/// <summary>
		/// Text file line of this section in the original file
		/// </summary>
		/// <value>The line.</value>
		public int Line { get; private set; }

		internal SlnPropertySet ()
		{
		}

		/// <summary>
		/// Creates a new property set with the specified ID
		/// </summary>
		/// <param name="id">Identifier.</param>
		public SlnPropertySet (string id)
		{
			Id = id;
		}

		internal SlnPropertySet (bool isMetadata)
		{
			this.isMetadata = isMetadata;
		}

		/// <summary>
		/// Gets a value indicating whether this property set is empty.
		/// </summary>
		/// <value><c>true</c> if this instance is empty; otherwise, <c>false</c>.</value>
		public bool IsEmpty {
			get {
				return values.Count == 0;
			}
		}

		internal void ReadLine (string line, int currentLine)
		{
			if (Line == 0)
				Line = currentLine;
			line = line.Trim ();
			int k = line.IndexOf ('=');
			if (k != -1) {
				var name = line.Substring (0, k).Trim ();
				var val = line.Substring (k + 1).Trim ();
				values [name] = val;
			} else {
				values.Add (line, null);
			}
		}

		internal void Write (TextWriter writer)
		{
			foreach (DictionaryEntry e in values) {
				if (!isMetadata)
					writer.Write ("\t\t");
				if (Id != null)
					writer.Write (Id + ".");
				writer.WriteLine (e.Key + " = " + e.Value);
			}
		}

		/// <summary>
		/// Gets the identifier of the property set
		/// </summary>
		/// <value>The identifier.</value>
		public string Id { get; private set; }

		public string GetValue (string name, string defaultValue = null)
		{
			string res;
			if (TryGetValue (name, out res))
				return res;
			else
				return defaultValue;
		}

		public FilePath GetPathValue (string name, FilePath defaultValue = default(FilePath), bool relativeToSolution = true, FilePath relativeToPath = default(FilePath))
		{
			string val;
			if (TryGetValue (name, out val)) {
				string baseDir = null;
				if (relativeToPath != null) {
					baseDir = relativeToPath;
				} else if (relativeToSolution && ParentFile != null && ParentFile.FileName != null) {
					baseDir = ParentFile.FileName.ParentDirectory;
				}
				return MSBuildProjectService.FromMSBuildPath (baseDir, val);
			}
			else
				return defaultValue;
		}

		public bool TryGetPathValue (string name, out FilePath value, FilePath defaultValue = default(FilePath), bool relativeToSolution = true, FilePath relativeToPath = default(FilePath))
		{
			string val;
			if (TryGetValue (name, out val)) {
				string baseDir = null;

				if (relativeToPath != null) {
					baseDir = relativeToPath;
				} else if (relativeToSolution && ParentFile != null && ParentFile.FileName != null) {
					baseDir = ParentFile.FileName.ParentDirectory;
				}
				string path;
				var res = MSBuildProjectService.FromMSBuildPath (baseDir, val, out path);
				value = path;
				return res;
			}
			else {
				value = defaultValue;
				return value != default(FilePath);
			}
		}

		public T GetValue<T> (string name)
		{
			return (T) GetValue (name, typeof(T), default(T));
		}

		public T GetValue<T> (string name, T defaultValue)
		{
			return (T) GetValue (name, typeof(T), defaultValue);
		}

		public object GetValue (string name, Type t, object defaultValue)
		{
			string val;
			if (TryGetValue (name, out val)) {
				if (t == typeof(bool))
					return (object) val.Equals ("true", StringComparison.OrdinalIgnoreCase);
				if (t.GetTypeInfo().IsEnum)
					return Enum.Parse (t, val, true);
				if (t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition () == typeof(Nullable<>)) {
					var at = t.GetTypeInfo().GetGenericArguments () [0];
					if (string.IsNullOrEmpty (val))
						return null;
					return Convert.ChangeType (val, at, CultureInfo.InvariantCulture);

				}
				return Convert.ChangeType (val, t, CultureInfo.InvariantCulture);
			}
			else
				return defaultValue;
		}

		public void SetValue (string name, string value, string defaultValue = null, bool preserveExistingCase = false)
		{
			if (value == null && defaultValue == "")
				value = "";
			if (value == defaultValue) {
				// if the value is default, only remove the property if it was not already the default
				// to avoid unnecessary project file churn
				string res;
				if (TryGetValue (name, out res) && !string.Equals (defaultValue ?? "", res, preserveExistingCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
					Remove (name);
				return;
			}
			string currentValue;
			if (preserveExistingCase && TryGetValue (name, out currentValue) && string.Equals (value, currentValue, StringComparison.OrdinalIgnoreCase))
				return;
			values [name] = value;
		}

		public void SetValue (string name, FilePath value, FilePath defaultValue = default(FilePath), bool relativeToSolution = true, FilePath relativeToPath = default(FilePath))
		{
			var isDefault = value.CanonicalPath == defaultValue.CanonicalPath;
			if (isDefault) {
				// if the value is default, only remove the property if it was not already the default
				// to avoid unnecessary project file churn
				if (ContainsKey (name) && (defaultValue == null || defaultValue != GetPathValue (name, relativeToSolution:relativeToSolution, relativeToPath:relativeToPath)))
					Remove (name);
				return;
			}
			string baseDir = null;
			if (relativeToPath != null) {
				baseDir = relativeToPath;
			} else if (relativeToSolution && ParentFile != null && ParentFile.FileName != null) {
				baseDir = ParentFile.FileName.ParentDirectory;
			}
			values [name] = MSBuildProjectService.ToMSBuildPath (baseDir, value, false);
		}

		public void SetValue (string name, object value, object defaultValue = null)
		{
			var isDefault = object.Equals (value, defaultValue);
			if (isDefault) {
				// if the value is default, only remove the property if it was not already the default
				// to avoid unnecessary project file churn
				if (ContainsKey (name) && (defaultValue == null || !object.Equals (defaultValue, GetValue (name, defaultValue.GetType (), null))))
					Remove (name);
				return;
			}

			if (value is bool)
				values [name] = (bool)value ? "TRUE" : "FALSE";
			else
				values [name] = Convert.ToString (value, CultureInfo.InvariantCulture);
		}

		void IDictionary<string,string>.Add (string key, string value)
		{
			SetValue (key, value);
		}

		/// <summary>
		/// Determines whether the current instance contains an entry with the specified key
		/// </summary>
		/// <returns><c>true</c>, if key was containsed, <c>false</c> otherwise.</returns>
		/// <param name="key">Key.</param>
		public bool ContainsKey (string key)
		{
			return values.Contains (key);
		}

		/// <summary>
		/// Removes a property
		/// </summary>
		/// <param name="key">Property name</param>
		public bool Remove (string key)
		{
			var wasThere = values.Contains (key);
			values.Remove (key);
			return wasThere;
		}

		/// <summary>
		/// Tries to get the value of a property
		/// </summary>
		/// <returns><c>true</c>, if the property exists, <c>false</c> otherwise.</returns>
		/// <param name="key">Property name</param>
		/// <param name="value">Value.</param>
		public bool TryGetValue (string key, out string value)
		{
			value = (string) values [key];
			return value != null;
		}

		/// <summary>
		/// Gets or sets the value of a property
		/// </summary>
		/// <param name="index">Index.</param>
		public string this [string index] {
			get {
				return (string) values [index];
			}
			set {
				values [index] = value;
			}
		}

		public ICollection<string> Values {
			get {
				return values.Values.Cast<string>().ToList ();
			}
		}

		public ICollection<string> Keys {
			get { return values.Keys.Cast<string> ().ToList (); }
		}

		void ICollection<KeyValuePair<string, string>>.Add (KeyValuePair<string, string> item)
		{
			SetValue (item.Key, item.Value);
		}

		public void Clear ()
		{
			values.Clear ();
		}

		internal void ClearExcept (HashSet<string> keys)
		{
			foreach (var k in values.Keys.Cast<string>().Except (keys).ToArray ())
				values.Remove (k);
		}

		bool ICollection<KeyValuePair<string, string>>.Contains (KeyValuePair<string, string> item)
		{
			var val = GetValue (item.Key);
			return val == item.Value;
		}

		public void CopyTo (KeyValuePair<string, string>[] array, int arrayIndex)
		{
			foreach (DictionaryEntry de in values)
				array [arrayIndex++] = new KeyValuePair<string, string> ((string)de.Key, (string)de.Value);
		}

		bool ICollection<KeyValuePair<string, string>>.Remove (KeyValuePair<string, string> item)
		{
			if (((ICollection<KeyValuePair<string, string>>)this).Contains (item)) {
				Remove (item.Key);
				return true;
			} else
				return false;
		}

		public int Count {
			get {
				return values.Count;
			}
		}

		internal void SetLines (IEnumerable<KeyValuePair<string,string>> lines)
		{
			values.Clear ();
			foreach (var line in lines)
				values [line.Key] = line.Value;
		}

		bool ICollection<KeyValuePair<string, string>>.IsReadOnly {
			get {
				return false;
			}
		}

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator ()
		{
			foreach (DictionaryEntry de in values)
				yield return new KeyValuePair<string,string> ((string)de.Key, (string)de.Value);
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			foreach (DictionaryEntry de in values)
				yield return new KeyValuePair<string,string> ((string)de.Key, (string)de.Value);
		}
	}

}