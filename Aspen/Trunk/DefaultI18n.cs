using System;
using System.Resources;

namespace ASPEN
{
	internal class DefaultI18n : AspenUserMessages
	{
		private string resourceName;
		private string resourceDir = "Resources";

		/// <summary>
		/// uses Resources/UserMessages.resource by default (including when argument is null), Resource/{name}.resource
		/// </summary>
		/// <param name="configuredResourceName"></param>
		public DefaultI18n(string configuredResourceName = null)
		{
			if(configuredResourceName == null)
			{
				configuredResourceName = "UserMessages";
			}
			this.resourceName = configuredResourceName;
		}

		private ResourceManager _manager;
		private ResourceManager Resource
		{
			get
			{
				if (_manager == null)
				{
					_manager = ResourceManager.CreateFileBasedResourceManager(resourceName, resourceDir, null);
				}
				return _manager;
			}
		}

		/// Processor for user-facing text of any kind - indirection of exact wording that also enables internationalization (i18n) of the application.
		/// also needed by view.
		/// object key allows support for less error-prone keys than strings.
		/// Typical C# implementation is equivalent to string.Format(resourceManager.GetString(key), args)
		///shortcut for I18n.Text
		public string Formatted(object key, params object[] args)
		{
			try
			{
				return string.Format(Translated(key), args);
			}
			catch
			{
				return key.ToString();
			}
		}

		public string Translated(object key)
		{
			if (key == null) throw new ArgumentNullException();

			string keyString = key.ToString();
			return this.Resource.GetString(keyString);
		}

	}
}
