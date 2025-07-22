using System;

namespace ASPEN
{
	/// <summary>
	/// All Software Projects Eventually Need a Dialog instance & response object.
	/// This is the actual instance, with any case-specific formatting arguments already applied to the Summary/etc.
	/// </summary>
	public interface AspenDialog
	{
		//TODO should this instance have its title/context string/object?
		//AspenCommandContext Context { get; }

		/// <summary>
		/// Main question or notification or progress subject
		/// </summary>
		string Main { get; }
		/// <summary>
		/// Additional details to help decide the question, or understand the notification or progression intent
		/// </summary>
		string Supplementary { get; }
		/// <summary>
		/// This dialog is more important and consequential than others.  
		/// Question default response is changed to No.
		/// </summary>
		bool IsRisky { get; set; }
		/// <summary>
		/// The default response option label to indicate agreement
		/// </summary>
		string OKYes { get; }
		/// <summary>
		/// The alternate response option label that indicates disagreement.
		/// </summary>
		string CancelNo { get; }
		/// <summary>
		/// The text displayed to indicate the user may allow the dialog to auto-respond in the future with the response that is about to be given.
		/// Null if auto-response is not an option.  
		/// For Progress this is text displayed to indicate the user may allow the dialog to automatically dismiss the progress details upon completion.
		/// </summary>
		string DontAskAgain { get; }

		/// <summary>
		/// Exception that caused this dialog to appear
		/// </summary>
		Exception Exception { get; }

		/// <summary>
		/// Whether this dialog has been responded to
		/// </summary>
		bool IsResponded { get; }
		/// <summary>
		/// Whether the dialog response was positive (OKYes).
		/// </summary>
		bool IsAgreed { get; }
		/// <summary>
		/// Whether the dialog response included the option to "DontAskAgain" and should be remembered for the next attempt.  
		/// Progress uses this to remember autodismissal upon completion.
		/// </summary>
		bool IsResponseForever { get; }

		/// <summary>
		/// Priority Notification (probably equivalent to Notify with a Risky dialog)
		/// </summary>
		void Alert();
		/// <summary>
		/// Basic Notification
		/// </summary>
		void Notify();
		/// <summary>
		/// Basic Question
		/// </summary>
		void Ask();
		/// <summary>
		/// Basic Question and action to perform if agreed
		/// </summary>
		/// <param name="continuation"></param>
		void AskToContinue(Action<AspenDialog> continuation);
		/// <summary>
		/// Basic question and action to perform for either response.
		/// </summary>
		/// <param name="continuation"></param>
		/// <param name="cancellation"></param>
		void AskToContinueOrCancel(Action<AspenDialog> continuation, Action<AspenDialog> cancellation);

		///// <summary>
		///// Notification that indicates progress during an action
		///// </summary>
		///// <param name="progression">the progress action, provided current state of this dialog and a progress-reporting object</param>
		///// <param name="continuation">the action to take when progress ended</param>
		///// <param name="errorHandler">the action to take if an exception happens during progression</param>
		//void TrackProgress(Action<AspenDialog, AspenProgress> progression, Action<AspenDialog> continuation, Action<AspenDialog, Exception> errorHandler);
		///// <summary>
		///// Notification that indicates progress during an action and includes the ability to cancel it
		///// </summary>
		///// <param name="progression">the progress action, provided current state of this dialog and a progress-reporting object</param>
		///// <param name="cancelHandler">the action to take if the user requests a cancellation of the progression, AspenDialog's response is now that of "CancelNo": IsResponded && !IsAgreed.</param>
		///// <param name="continuation">the action to take when progress ended</param>
		///// <param name="errorHandler">the action to take if an exception happens during progression</param>
		//void TrackCancellableProgress(Action<AspenDialog, AspenProgress> progression, Action<AspenDialog, AspenProgress> cancelHandler, Action<AspenDialog> continuation, Action<AspenDialog, Exception> errorHandler);
		/// <summary>
		/// Notification that indicates progress during an action
		/// </summary>
		/// <param name="progression">the progress action, provided current state of this dialog and a progress-reporting object</param>
		/// <exception cref="ProgressCancelledException">if Progress got marked as cancelled (outside of a protected step)</exception>
		void TrackProgress(Action<AspenDialog, AspenProgress> progression);
	}

}
