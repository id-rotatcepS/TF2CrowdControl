using System;
using System.Collections.Generic;

namespace ASPEN
{
    /// <summary>
	/// Just holds a stack of command contexts with information used by the default log and dialog utilities
	/// </summary>
    public class DefaultCommandContextUtility : AspenContextTracking
	{
		private Stack<AspenCommandContext> current = new Stack<AspenCommandContext>();
		public AspenCommandContext CreateContextFor(string commandContextName)
		{
			return new CommandContext(commandContextName, null, null);
		}
		public void Start(AspenCommandContext context)
		{
			current.Push(context);
		}
		public void End(AspenCommandContext context)
		{
			if (current.Peek() == context) current.Pop();
		}
		public AspenCommandContext GetCurrent()
		{
			return current.Peek();
		}

		/// shorthand for a using(new CommandContext)try{command}catch(){handle}  snippet
		public void ForCommand(string commandContextName, Action command, Action<Exception> exceptionHandler)
		{
			//			using (new UsingCommandContext(commandContextName))
			AspenCommandContext context = CreateContextFor(commandContextName);
			Aspen.Track.Start(context);

			try
			{
				command.Invoke();
			}
			catch (Exception ex)
			{
				exceptionHandler.Invoke(ex);
			}

			finally
			{
				Aspen.Track.End(context);
			}
		}

	}



}
