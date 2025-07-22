using System;
using System.Collections.Generic;

namespace ASPEN
{
    /// basic Dictionary storage and rudimentary default setting
    public class DefaultUserSettingUtility : AspenUserSettings
	{
		private Dictionary<object, object> vals = new Dictionary<object, object>();
		/// for ad-hoc settings and in general configuring settings
		public void SetDefaultOnce<T>(object key, T defult)
		{
			if (vals.ContainsKey(key)) throw new Exception("can only set once");
			vals[key] = defult;
		}
		public void Set<T>(object key, T val)
		{
			vals[key] = val;
		}
		public T Get<T>(object key)
		{
			try
			{
				return (T)vals[key];
			}
			catch
			{
				return default(T);
			}
		}
	}
}
