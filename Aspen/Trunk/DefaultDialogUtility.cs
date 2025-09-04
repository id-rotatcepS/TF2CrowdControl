using System;
using System.Collections.Generic;
using System.Resources;

namespace ASPEN
{

	public class AspenDialogTemplate
	{
		//TODO maybe: string TitleFormat { get; }

		public string MainFormat { get; set; }
		public string SupplementaryFormat { get; set; }
		private string _ok;
		public string OKYesFormat //{ get; set; }
		{
			get
			{
				if (_ok == null) return "OK";
				else return _ok;
			}
			set { _ok = value; }
		}
		private string _cancel;
		public string CancelNoFormat //{ get; set; }
		{
			get
			{
				if (_cancel == null) return "Cancel";
				else return _cancel;
			}
			set { _cancel = value; }
		}
		//public bool? Risky;
		//public bool? WaitForUser;
		public string DontAskAgainFormat { get; internal set; }
	}

	/// <summary>
	/// Console output/input dialogs 
	/// uses Resources/Dialogs.resource with key (Summary's Format), 
	/// and optionally key.SupplementaryFormat, key.OKYesFormat, key.CancelNoFormat
	/// Alternatively use SetDialog to load formats individually.
	/// </summary>
	public class DefaultDialogUtility : AspenUserInteraction
	{
		public void SetDialog(object key, AspenDialogTemplate formattable)
		{
			if (!map.ContainsKey(key))
			{
				map.Add(key, formattable);
			}
		}

		public virtual AspenDialog NewDialogDetail(object key, object[] formatArgs)
		{
			return new DialogInfo(key, formatArgs);
		}
		public virtual AspenDialog NewExceptionDialogDetail(object key, Exception ex, object[] formatArgs)
		{
			return new DialogInfo(key, formatArgs) { Exception = ex };
		}

		//public DefaultDialogUtility()
		//{
		//	DialogDetails.KeyModel = new DefaultKeyModel();
		//}

		public void Alert(object key, 
			//string defaultSummaryFormat, 
			params object[] args)
		{
			LoadDialogFormatsForKey(key);//, defaultSummaryFormat);
			AspenDialog dialog = NewDialogDetail(key, args);
			dialog.Alert();
		}
		public void ExceptionAlert(object key, 
			//string defaultSummaryFormat, 
			Exception exc, params object[] args)
		{
			LoadDialogFormatsForKey(key);//, defaultSummaryFormat);
			AspenDialog dialog = NewExceptionDialogDetail(key, exc, args);
			dialog.Alert();
		}

		public void Notice(object key, 
			//string defaultSummaryFormat, 
			params object[] args)
		{
			LoadDialogFormatsForKey(key);//, defaultSummaryFormat);
			AspenDialog dialog = NewDialogDetail(key, args);
			dialog.Notify();
		}

		public void QuestionToContinue(object key, Action continuation, 
			//string defaultQuestionFormat, 
			params object[] args)
		{
			LoadDialogFormatsForKey(key);//, defaultSummaryFormat);
			AspenDialog dialog = NewDialogDetail(key, args);
			dialog.AskToContinue((info) => continuation.Invoke());
		}
		public void QuestionToContinueOrCancel(object key, Action continuation, Action cancellation, 
			//string defaultQuestionFormat, 
			params object[] args)
		{
			LoadDialogFormatsForKey(key);//, defaultSummaryFormat);
			AspenDialog dialog = NewDialogDetail(key, args);
			dialog.AskToContinueOrCancel((info) => continuation.Invoke(), (info) => cancellation.Invoke());
		}


		/// <summary>
		/// Load Resources for this key, or set up a simple one with this default
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultSummaryFormat"></param>
		private void LoadDialogFormatsForKey(object key, string defaultSummaryFormat = null)
		{
			if (!map.ContainsKey(key))
			{
				try
				{
					UseDefaultDialogResourceManager(key);
				}
				catch
				{
					try
					{
						UseDefaultAspenText(key);
					}
					catch
					{
						map.Add(key, new AspenDialogTemplate() { MainFormat = defaultSummaryFormat ?? (key + " (Dialog Resource Key)") });
					}
				}
			}
		}

		private static void UseDefaultAspenText(object key)
		{
			map.Add(key, new AspenDialogTemplate()
			{
				MainFormat = Aspen.Text.Translated(key + MainFormatSubKey),
				SupplementaryFormat = Aspen.Text.Translated(key + SupplementaryFormatSubKey),

				OKYesFormat = Aspen.Text.Translated(key + OKFormatSubKey),
				CancelNoFormat = Aspen.Text.Translated(key + CancelFormatSubKey),

				DontAskAgainFormat = Aspen.Text.Translated(key + ForeverFormatSubKey),
			});
		}

		private const string MainFormatSubKey = "";
		private const string SupplementaryFormatSubKey = ".SupplementaryFormat";
		private const string OKFormatSubKey = ".OKYesFormat";
		private const string CancelFormatSubKey = ".CancelNoFormat";
		private const string ForeverFormatSubKey = ".DontAskAgainFormat";
		private ResourceManager manager = null;
		private void UseDefaultDialogResourceManager(object key)
		{
			if (manager == null)
			{
				manager = ResourceManager.CreateFileBasedResourceManager("Dialogs", "Resources", null);
			}
			map.Add(key, new AspenDialogTemplate()
			{
				MainFormat = manager.GetString(key + MainFormatSubKey),
				SupplementaryFormat = manager.GetString(key + SupplementaryFormatSubKey),

				OKYesFormat = manager.GetString(key + OKFormatSubKey),
				CancelNoFormat = manager.GetString(key + CancelFormatSubKey),

				DontAskAgainFormat = manager.GetString(key + ForeverFormatSubKey),
			});
		}

		//private class DefaultKeyModel : DialogDetails.Model
		//{
		//	public Dictionary<object, DialogFormat> map = new Dictionary<object, DialogFormat>();
		//	public string GetSummaryFormat(object key) { return map[key].SummaryFormat; }
		//	public string GetSupplementaryFormat(object key) { return map[key].SupplementaryFormat; }
		//	public string GetCancelNoFormat(object key) { return map[key].CancelNoFormat; }
		//	public string GetOKYesFormat(object key) { return map[key].OKYesFormat; }
		//	public bool? GetRiskyDefault(object key) { return map[key].Risky; }
		//	public bool? GetWaitForUserDefault(object key) { return map[key].WaitForUser; }
		//}
		private static Dictionary<object, AspenDialogTemplate> map = new Dictionary<object, AspenDialogTemplate>();//{ get { return (DialogDetails.KeyModel as DefaultKeyModel).map; } }

		public static AspenDialogTemplate GetTemplate(object key)
		{
			return map[key];
		}


		public void Progress(object key,
			Action<AspenProgress> progression,
			Action continuation,
			Action<Exception> errorHandler,
			//string defaultProgressSubject,
			params object[] args)
		{
			LoadDialogFormatsForKey(key);//, defaultSummaryFormat);
			AspenDialog dialog = NewDialogDetail(key, args);
			dialog.Notify();
			try
			{
				dialog.TrackProgress(
					progression: (info, prog) => progression.Invoke(prog)
					//,
					//continuation: (info) => continuation?.Invoke(),
					//errorHandler: (info, exc) =>
					//{
					//	if (errorHandler != null)
					//		errorHandler.Invoke(exc);
					//	else
					//		throw exc;
					//}
					);

				continuation?.Invoke();
			}
			catch (Exception exc)
			{
				if (errorHandler != null)
					errorHandler.Invoke(exc);
				else
					throw;

			}
		}

		public void CancellableProgress(object key,
			Action<AspenProgress> progression,
			Action<AspenProgress> cancelHandler,
			Action continuation,
			Action<Exception> errorHandler,
			//string defaultProgressSubject, 
			params object[] args)
		{
			LoadDialogFormatsForKey(key);//, defaultSummaryFormat);
			DialogInfo dialog = new DialogInfo(key, args);
			dialog.PresentDetails();

			try
			{
				dialog.TrackProgress(
					(info, prog) =>
					{
						try
						{
							progression.Invoke(prog);
						}
						catch (ProgressCancelledException)
						{
							cancelHandler?.Invoke(prog);
						}
					}
					//,
					//cancelHandler: (info, prog) => cancelHandler?.Invoke(prog),
					//continuation: (info) => continuation?.Invoke(),
					//errorHandler: (info, exc) =>
					//{
					//	if (errorHandler != null) 
					//		errorHandler.Invoke(exc); 
					//	else 
					//		throw exc;
					//}
					);
				continuation?.Invoke(/*dialog*/);
			}
			//catch (ProgressCancelledException)
			//{
			//	cancelHandler?.Invoke(prog);
			//}
			catch (Exception exc)
			{
				if (errorHandler != null)
					errorHandler.Invoke(exc/*, dialog*/);
				else
					throw;
			}
		}


	}
}
