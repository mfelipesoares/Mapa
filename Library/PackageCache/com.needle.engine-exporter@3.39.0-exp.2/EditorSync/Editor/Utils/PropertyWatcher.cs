using System.Collections.Generic;

namespace Needle.Engine.EditorSync.Utils
{
	internal class PropertyWatcher
	{
		public static bool Update(IList<PropertyWatcher> watchers, int index, object value)
		{
			if (watchers.Count <= index)
			{
				for (var i = watchers.Count; i <= index; i++)
					watchers.Add(null);
			}
			var watcher = watchers[index];
			if (watcher == null)
			{
				watchers[index] = new PropertyWatcher(value);
				return false;
			}
			return watcher.Update(value);
		}
		
		
		private object _value;
		
		public PropertyWatcher(object value)
		{
			_value = value;
		}
		
		public bool Update(object newValue)
		{
			var changed = !Equals(_value, newValue);
			_value = newValue;
			return changed;
		}
	}
}