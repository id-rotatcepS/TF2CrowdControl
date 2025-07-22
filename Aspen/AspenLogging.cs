using System;

namespace ASPEN
{
	/// <summary>
	/// All Software Projects Eventually Need Logging.
	/// Basic interface to chosen logging system.
	/// Expected to use AspenContextTracking to provide category-like contexts.
	/// </summary>
	public interface AspenLogging
	{
		void Error(string msg);
		void ErrorException(Exception exc, string msg);
		void Warning(string msg);
		void WarningException(Exception exc, string msg);
		void Info(string msg);
		void InfoException(Exception exc, string msg);
		void Trace(string msg);
	}
}
