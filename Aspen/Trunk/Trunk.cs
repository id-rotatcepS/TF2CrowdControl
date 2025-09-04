using System;
using System.IO;
using System.Reflection;

namespace ASPEN
{
	public class Trunk
    {
		/// BaseConfiguration loaded from mainArgs pairs, basic ??unpopulated!?? Dictionary for i18n format strings, 
		/// DefaultUserSettingUtility, DefaultLogUtility, DefaultDialogUtility, and DefaultCommandContextUtility.
		public static void Defaults(string[] mainArgs)
		{
			DefaultConfig(mainArgs);
			DefaultUserSetting();
			DefaultLog();
			DefaultDialog();
			DefaultCommandContext();
			DefaultI18n();
		}

		public static void DefaultI18n()
		{
			Aspen.Text = new DefaultI18n(Aspen.Config.Get<string>("I18nResource"));
		}

		public static void DefaultConfig(string[] mainArgs)
		{
			DefaultUserSettingUtility config = new DefaultUserSettingUtility();
			if (mainArgs != null)
			{
				for (int i = 0; i < mainArgs.Length - 1; i += 2)
				{
					config.SetDefaultOnce<string>(mainArgs[i], mainArgs[i + 1]);
				}
			}
			Aspen.Config = config;
		}
		public static void DefaultUserSetting()
		{
			Aspen.Option = new DefaultUserSettingUtility();
		}
		public static void DefaultLog()
		{
			TextWriter output = Aspen.Config.Get<TextWriter>("LoggingWriter");
			if (output == null) output = Console.Error;
			Aspen.Log = new DefaultLogUtility(output);
		}
		public static void DefaultDialog()
		{
			Aspen.Show = new DefaultDialogUtility();
		}
		public static void DefaultCommandContext()
		{
			Aspen.Track = new DefaultCommandContextUtility();
			// add entry assembly's simple name as a starting point
			string commandName = Assembly.GetEntryAssembly()?.GetName()?.Name ?? "Application";
			Aspen.Track.Start(new CommandContext(commandName, null, null));
		}
	}



}
