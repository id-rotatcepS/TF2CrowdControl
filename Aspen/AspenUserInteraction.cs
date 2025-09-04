using System;

namespace ASPEN
{
    /// <summary>
    /// All Software Projects Eventually Need User Interaction dialogs.
    /// Expected to use <see cref="AspenDialog"/>.
    /// Expected to use <see cref="AspenContextTracking"/> to provide title-like contexts.
    /// Might use <see cref="AspenUserMessages"/> to provide dialog texts or could use a separate resource.
    /// </summary>
    public interface AspenUserInteraction
	{
		//void Alert(object key, string defaultSummaryFormat, params object[] args);
		//void ExceptionAlert(object key, string defaultSummaryFormat, Exception exc, params object[] args);
		/// <summary>
		/// <see cref="Notice(object, object[])"/> with emphasis
		/// </summary>
		/// <param name="key"></param>
		/// <param name="args"></param>
		void Alert(object key, params object[] args);
		/// <summary>
		/// <see cref="Notice(object, object[])"/> with emphasis and error details
		/// </summary>
		/// <param name="key"></param>
		/// <param name="exc"></param>
		/// <param name="args"></param>
		void ExceptionAlert(object key, Exception exc, params object[] args);

		//void Alert(AspenDialog errorDetails);
		//AspenDialog.Alert()
		//void ExceptionError(AspenDialog errorDetails, Exception exc);

		//void Notice(object key, string defaultSummaryFormat, params object[] args);
		/// <summary>
		/// Basic user notification, possibly even not modal.
		/// Expected to use <see cref="AspenUserMessages"/> to populate the user-facing dialog text and labels and <see cref="AspenContextTracking"/> for headings.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="args"></param>
		void Notice(object key, params object[] args);

		//void Notice(AspenDialog notificationDetails);
		//AspenDialog.Notify()
		//void ExceptionNotice(AspenDialog notificationDetails, Exception exc);
		//void ExceptionNotice(object key, string defaultSummaryFormat, Exception exc, params object[] args);

		//void QuestionToContinue(object key, Action continuation, string defaultQuestionFormat, params object[] args);
		/// <summary>
		/// Dialog to take an optional additional action or not.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="continuation">the action taken if the user agrees</param>
		/// <param name="args"></param>
		void QuestionToContinue(object key, Action continuation, params object[] args);
		/// <summary>
		/// Ask the user a question and execute one of two paths depending on response.
		/// </summary>
		/// <param name="key">dialog key that points to the internationalized question, details, responses, etc.</param>
		/// <param name="continuation">Action executed upon an OK/Yes response</param>
		/// <param name="cancellation">Action executed upon a Cancel/No response</param>
		/// if key is defined this is documentation for what the key should ask and how format args are used</param>
		/// <param name="args">args used in formatting the question and any other formats identified by the key</param>
		// <param name="defaultQuestionFormat">if key is not defined, this format is used for a simple question,
		//void QuestionToContinueOrCancel(object key, Action continuation, Action cancellation, string defaultQuestionFormat, params object[] args);
		void QuestionToContinueOrCancel(object key, Action continuation, Action cancellation, params object[] args);

		// <summary>
		// Ask a question, storing the response in the AspenDialog instance.
		// </summary>
		// <param name="questionDetails"></param>
		//void Question(AspenDialog questionDetails);
		//AspenDialog.Ask()

		////void ExceptionQuestion(AspenDialog questionDetails, Exception exc);
		//void QuestionToContinue(object key, string defaultQuestionFormat, Action<AspenDialog> continuation, params object[] args);

		//void QuestionToContinue(AspenDialog questionDetails, Action<AspenDialog> continuation);
		//AspenDialog.AskToContinue(Action<AspenDialog> continuation)
		//void QuestionToContinue(AspenDialog questionDetails, Action continuation);
		//void QuestionToContinueOrCancel(object key, string defaultQuestionFormat, Action<AspenDialog> continuation, Action<AspenDialog> cancellation, params object[] args);

		//void QuestionToContinueOrCancel(AspenDialog questionDetails, Action<AspenDialog> continuation, Action<AspenDialog> cancellation);
		//AspenDialog.AskToContinueOrCancel(Action<AspenDialog> continuation, Action<AspenDialog> cancellation)
		//void QuestionToContinueOrCancel(AspenDialog questionDetails, Action continuation, Action cancellation);

		//void Progress(object key,
		//	Action<AspenProgress> progression,
		//	Action<Exception> errorHandler,
		//	string defaultProgressSubject, params object[] args);
		/// <summary>
		/// Display a user dialog that will report 100% of the <see cref="AspenProgress"/> instance by the time it completes.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="progression"></param>
		/// <param name="completed"></param>
		/// <param name="errorHandler"></param>
		/// <param name="args"></param>
		void Progress(object key,
			Action<AspenProgress> progression,
			Action completed,
			Action<Exception> errorHandler,
			params object[] args);

		//void CancellableProgress(object key, 
		//	Action cancelHandler, 
		//	Action<AspenProgress> progression, 
		//	Action<Exception> errorHandler, 
		//	string defaultProgressSubject, params object[] args);
		void CancellableProgress(object key,
			Action<AspenProgress> progression,
			Action<AspenProgress> cancelHandler,
			Action completed,
			Action<Exception> errorHandler,
			params object[] args);

		////bool Invoked(IDialogForm form, string ok, string cancel);//, [context]);
		////bool WizardFinished(IDialogWizard wizard, string finish, string cancel);//, [context]);
		////bool ProgressCompleted(IDialogProgress progress, string close, string cancel);//, [context]);

		////void setDialogContext(IDialogContext context);
		////void setDialogHandler(IDialogProvider provider);
	}
}
