using System;
using System.IO;

namespace ASPEN
{
    /// Console Error output log
    public class DefaultLogUtility : AspenLogging
	{
		public TextWriter LogOutput;

		public DefaultLogUtility()
		{
			LogOutput = Console.Error;
		}
		public DefaultLogUtility(TextWriter output)
		{
			LogOutput = output;
		}

		private void Log(string prefix, string msg)
		{
			ContextHeader();
			LogOutput.WriteLine(msg);
		}
		private void LogException(string prefix, Exception exc, string msg)
		{
			ContextHeader();
			LogOutput.WriteLine(msg);
			LogOutput.WriteLine(exc?.Message);
		}
		private void ContextHeader()
		{
			AspenCommandContext context = Aspen.Track.GetCurrent();
			if (context != null)
			{
				string title = context.Name;
				LogOutput.Write(string.Format("{0} - ", title));
			}
		}

		public void Error(string msg) { Log("Error: ", msg); }
		public void ErrorException(Exception exc, string msg) { LogException("Error: ", exc, msg); }
		public void Warning(string msg) { Log("Warning: ", msg); }
		public void WarningException(Exception exc, string msg) { LogException("Warning: ", exc, msg); }
		public void Info(string msg) { Log("Log: ", msg); }
		public void InfoException(Exception exc, string msg) { LogException("Log: ", exc, msg); }
		public void Trace(string msg) { Log("Debug: ", msg); }
	}
}
