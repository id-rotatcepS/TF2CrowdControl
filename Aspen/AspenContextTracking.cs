using System;

namespace ASPEN
{
	/// <summary>
	/// All Software Projects Eventually Need a way to identify current command context to 
	/// track usage and command history and to organize dialog or logging messages.
	/// Also for context of dialog parenting and Modal blocking level (normally all app or parent top level window).
	/// While not required, a stack of context is expected so context always falls back to the initial application level information.
	/// Ideally "using(new CommandContext)" gives the best automated control over this concept for temporary contexts.
	/// extreme example:
	/// App "MyApp" ; Icon *Modality 
	/// > Main Window ; WindowIcon "MyApp (mode)" 
	///  > 2ndary Window ; WindowIcon "Report" *Modality 
	///   > Docked tool ; ToolIcon "Settings" 
	///   > group view "footer settings" 
	///   > button "select image" 
	///    > (command ; CommandIcon "image finder") 
	///     > popup progress dialog window ; (window w/ CommandIcon) "searching images" *Modality 
	///      > (step "step title")
	///       > error notification dialog window ; ErrorIcon? "network error" *Modality 
	///       > detail view "exception view"
	/// </summary>
	public interface AspenContextTracking
	{
		void Start(AspenCommandContext context);
		void End(AspenCommandContext context);
		AspenCommandContext GetCurrent();

		/// <summary>
		/// Typically extracts resources based on context name to populate all data in an app-specific implementation.
		/// </summary>
		/// <param name="contextName">the context key, likely the AspenCommandContext.Name's value</param>
		/// <returns></returns>
		AspenCommandContext CreateContextFor(string contextName);

		/// <summary>
		/// Creates & starts context, runs command, and ends context after possibly handling exceptions.
		/// Shorthand for e.g. using(new CommandContext(contextName)) try { command(); } catch(Exception e) { exceptionHandler(e); }
		/// </summary>
		/// <param name="contextName"></param>
		/// <param name="command"></param>
		/// <param name="exceptionHandler"></param>
		void ForCommand(string contextName, Action command, Action<Exception> exceptionHandler);

	}

	/// <summary>
	/// Minimal context data for a command.  Implementations should add app-relevant details like icons & descriptions.
	/// </summary>
	public interface AspenCommandContext
	{
		string Name { get; }
	}

	/// often may be a derivation from a view/i18n configuration for an action button, toolbar button, window, etc..
	public class CommandContext : AspenCommandContext
	{
		public CommandContext(string ContextTitle, object view, object icon)
		{
			this.Name = ContextTitle;
			this.View = view;
			this.Icon = icon;
		}
		public string Name { get; set; }
		public object Icon { get; set; }
		public object View { get; set; }
	}

	/// <summary>
	/// CommandContext meant to start and ends with a using(){} block
	/// </summary>
	public class UsingCommandContext : CommandContext, System.IDisposable
	{
		public UsingCommandContext(string ContextTitle, object view, object icon)
			: base(ContextTitle, view, icon)
		{
			Aspen.Track.Start(this);
		}
		public UsingCommandContext(string ContextTitle)
			: base(ContextTitle, null, null)
		{
			Aspen.Track.Start(this);
		}

		public void Dispose()
		{
			Aspen.Track.End(this);
		}
	}
}
