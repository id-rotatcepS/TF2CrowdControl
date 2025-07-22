using System;

namespace ASPEN
{
	/// <summary>
	/// All Software Projects Eventually Need...
	/// </summary>
	public class Aspen
	{
		/// <summary>
		/// Model for i18n string formatting
		/// </summary>
		public static AspenUserMessages Text { get; set; }
		/// <summary>
		/// shorthand for <see cref="Text"/>, <see cref="AspenUserMessages.Formatted(object, object[])"/>
		/// </summary>
		/// <param name="key"></param>
		/// <param name="formatArgs"></param>
		/// <returns></returns>
		public static string Format(object key, params object[] formatArgs)
		{
			return Text.Formatted(key, formatArgs);
		}

        /// <summary>
        /// Business-driven generally-hidden-from-user tracking of errors, warnings, application usage, and other progress.
        /// </summary>
        public static AspenLogging Log { get; set; }
        
		/// <summary>
        /// Model for user settings e.g. content limitation, algorithm choice, confirmation bypass
        /// Usage: e.g. settings dialog
        /// View potentially uses this with view-specific keys as storage for preferences like window layout, sort order, visual theme customization
        /// Usage: possibly implicit (window layout), else settings dialog
        /// </summary>
        public static AspenUserSettings Option { get; set; }
        
		/// <summary>
        /// Model for Read-only, externally defined configuration for e.g. application directory, server address, application mode
        /// Can load from combination of launch args, .ini, config file, etc.  Ultimate default is hard-coded.
        /// </summary>
        public static AspenConfiguration Config { get; set; }
		/// <summary>
		/// shorthand for <see cref="Config"/>, <see cref="AspenConfiguration.Get{T}(object)"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <returns></returns>
		public static T Get<T>(object key)
		{
			return Config.Get<T>(key);
		}

        /// <summary>
		/// Usecase-driven focused user notifications, questions, and inputs required as a step in a process or event.
		/// implemented by view, could do logging as a side effect, 
        /// </summary>
		public static AspenUserInteraction Show { get; set; }

        /// <summary>
		/// Tracks context of active (hierarchy of) application processes, steps, and events.
		/// Not an obvious ASPEN concept, but valuable to automatically inform logging categories and dialog headings as well as command usage history.
        /// </summary>
		public static AspenContextTracking Track { get; set; }
        /// <summary>
        /// shorthand for <see cref="Track"/>, <see cref="AspenContextTracking.ForCommand(string, Action, Action{Exception})"/>
        /// </summary>
        /// <param name="contextName"></param>
        /// <param name="command"></param>
        /// <param name="exceptionHandler"></param>
        public static void Do(string contextName, Action command, Action<Exception> exceptionHandler)
		{
			Track.ForCommand(contextName, command, exceptionHandler);
		}

	}


}
