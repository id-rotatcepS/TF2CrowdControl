using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;

namespace ASPEN
{

	public class DialogInfo : AspenDialog
	{
		private AspenDialogTemplate formats;
		private object[] formatArgs;
		public TextWriter writer;
		public TextReader reader;

		public DialogInfo(object key, params object[] formatArgs)
		{
			try
			{
				this.formats = DefaultDialogUtility.GetTemplate(key);
			}
			catch { }

			this.formatArgs = formatArgs;

			this.writer = Console.Out;
			this.reader = Console.In;
		}

		public string Main
		{
			get
			{
				string format = this.formats?.MainFormat;
				return Formatted(format);
			}
		}

		public string Supplementary
		{
			get
			{
				string format = this.formats?.SupplementaryFormat;
				return Formatted(format);
			}
		}

		public Exception Exception { get; set; }
		public bool IsRisky { get; set; }
		public string OKYes
		{
			get
			{
				string format = this.formats?.OKYesFormat;
				return Formatted(format) ?? nameof(OKYes);
			}
		}

		public string CancelNo
		{
			get
			{
				string format = this.formats?.CancelNoFormat;
				return Formatted(format) ?? nameof(CancelNo);
			}
		}

		public string DontAskAgain
		{
			get
			{
				string format = this.formats?.DontAskAgainFormat;
				return Formatted(format);
			}
		}

		private Response Response { get; set; }
		public bool IsResponded
		{
			get
			{
				return (Response != Response.NO_RESPONSE);
			}
		}

		public bool IsAgreed
		{
			get
			{
				switch (Response)
				{
					case Response.OKYes:
					case Response.OKYesForever:
						return true;
					case Response.CancelNo:
					case Response.CancelNoForever:
						return false;
					case Response.NO_RESPONSE:
					default:
						throw new NotSupportedException("No Response Yet");
				}
			}
		}
		public bool IsResponseForever
		{
			get
			{
				switch (Response)
				{
					case Response.OKYesForever:
					case Response.CancelNoForever:
						return true;
					default:
						return false;
				}
			}
		}

		public void Ask()
		{
			PresentDetails();
			this.Response = GetAgreement();
		}

		public void AskToContinue(Action<AspenDialog> continuation)
		{
			Ask();
			if (IsAgreed)
				continuation.Invoke(this);
		}

		public void AskToContinueOrCancel(Action<AspenDialog> continuation, Action<AspenDialog> cancellation)
		{
			Ask();
			if (IsAgreed)
				continuation.Invoke(this);
			else
				cancellation.Invoke(this);
		}

		public void Alert()
		{
			PresentDetails();
		}

		public void Notify()
		{
			PresentDetails();
		}

		private string Formatted(string format)
		{
			if (format == null)
				return null;
			return string.Format(format, this.formatArgs);
		}

		public void PresentDetails()
		{
			ContextHeader();
			writer.WriteLine(Main);
			if (Supplementary != null)
				writer.WriteLine(Supplementary);
			if (Exception != null)
				writer.WriteLine(Exception.Message);
			writer.Flush();
		}

		public void ContextHeader()
		{
			AspenCommandContext context = Aspen.Track.GetCurrent();
			if (context != null)
				writer.Write(string.Format("{0}: ", context.Name));
		}

		private Response GetAgreement()
		{
			InstructionsForAgreement();

			Response answer = GetTranslatedAgreementResponse();

			return answer;
		}

		private void InstructionsForAgreement()
		{
			if (DontAskAgain != null)
				writer.WriteLine(string.Format("{0} (type out full word yes or no in response)", DontAskAgain));

			string format;
			if (IsRisky)
				format = "[y/N]: {0}/{1}";
			else
				format = "[Y/n]: {0}/{1}";
			writer.WriteLine(string.Format(format, this.OKYes, this.CancelNo));
			writer.Flush();
		}

		private Response GetTranslatedAgreementResponse()
		{
			string response = reader.ReadLine().ToUpper();

			Response answer;
			if (response.StartsWith("Y"))
				if (DontAskAgain != null && response == "YES")
					answer = Response.OKYesForever;
				else
					answer = Response.OKYes;
			else if (response.StartsWith("N"))
				if (DontAskAgain != null && response == "NO")
					answer = Response.CancelNoForever;
				else
					answer = Response.CancelNo;
			else if (IsRisky)
				answer = Response.CancelNo;
			else
				answer = Response.OKYes;
			return answer;
		}

		//public void TrackProgress(Action<AspenDialog, AspenProgress> progression, Action<AspenDialog> continuation, Action<AspenDialog, Exception> errorHandler)
		//{
		//	BackgroundWorker bg = new BackgroundWorker();
		//	AspenProgress progress = new ProgressInfo(this);
		//	bg.DoWork += (a, b) =>
		//	{
		//		progression.Invoke(this, progress);
		//		this.Response = Response.OKYes;
		//	};
		//	bg.RunWorkerCompleted += (a,b) =>
		//	{
		//		if (b.Error != null)
		//			errorHandler?.Invoke(this, b.Error);
		//		else
		//		{
		//			continuation?.Invoke(this);
		//			writer.WriteLine("[Completed]");
		//		}
		//	};
		//	bg.RunWorkerAsync();

		//	//try
		//	//{
		//	//	progression.Invoke(this, progress);
		//	//	if (!progress.Cancelled)
		//	//		writer.WriteLine("Completed");
		//	//	this.Response = Response.OKYes;
		//	//	continuation.Invoke(this);
		//	//}
		//	//catch (Exception exc)
		//	//{
		//	//	if (errorHandler != null)
		//	//		errorHandler.Invoke(this, exc);
		//	//}
		//}

		//public void TrackCancellableProgress(Action<AspenDialog, AspenProgress> progression, Action<AspenDialog, AspenProgress> cancelHandler, Action<AspenDialog> continuation, Action<AspenDialog, Exception> errorHandler)
		//{
		//	BackgroundWorker bg = new BackgroundWorker();
		//	bg.WorkerSupportsCancellation = true;

		//	AspenProgress progress = new ProgressInfo(this);
		//	bg.DoWork += (a, b) =>
		//	{
		//		writer.WriteLine("[Enter: " + this.CancelNo + "]");
		//		try
		//		{
		//			progression.Invoke(this, progress);
		//			this.Response = Response.OKYes;
		//		}
		//		catch (ProgressCancelledException)
		//		{
		//			(a as BackgroundWorker).CancelAsync();
		//			this.Response = Response.CancelNo;
		//		}
		//	};
		//	bg.RunWorkerCompleted += (a, b) =>
		//	{
		//		if (b.Cancelled)
		//		{
		//			cancelHandler?.Invoke(this, progress);
		//			writer.WriteLine("[Cancelled]");
		//		}
		//		if (b.Error != null)
		//			errorHandler?.Invoke(this, b.Error);
		//		else
		//		{
		//			continuation?.Invoke(this);
		//			writer.WriteLine("[Completed]");
		//		}
		//	};
		//	bg.RunWorkerAsync();

		//	//System.Threading.Tasks.Task<string> cancelWatch = reader.ReadLineAsync();
		//	//AspenProgress progress = new ProgressInfo(this, cancelWatch);
		//	//try
		//	//{
		//	//	progression.Invoke(this, progress);
		//	//	if (!progress.Cancelled)
		//	//		writer.WriteLine("Completed");
		//	//	else
		//	//		writer.WriteLine("[Cancelled]");
		//	//	this.Response = Response.OKYes;
		//	//	continuation.Invoke(this);
		//	//}
		//	//catch (CancelledException)
		//	//{
		//	//	if (cancelHandler != null)
		//	//		cancelHandler.Invoke(this, progress);
		//	//	writer.WriteLine("[Cancelled]");
		//	//	this.Response = Response.CancelNo;
		//	//}
		//	//catch (Exception exc)
		//	//{
		//	//	if (errorHandler != null)
		//	//		errorHandler.Invoke(this, exc);
		//	//}
		//}
		public void TrackProgress(Action<AspenDialog, AspenProgress> progression)
		{
			BackgroundWorker bg = new BackgroundWorker();

			AspenProgress progress = new ProgressInfo(this);
			bg.WorkerSupportsCancellation = true /*TODO progress.Cancellable*/;
			bg.DoWork += (a, b) =>
			{
				//writer.WriteLine("[Enter: " + this.CancelNo + "]");
				try
				{
					progression.Invoke(this, progress);
					this.Response = Response.OKYes;
				}
				catch (ProgressCancelledException)
				{
					(a as BackgroundWorker).CancelAsync();
					this.Response = Response.CancelNo;
				}
			};

			RunSync(bg);
		}

        private void RunSync(BackgroundWorker bg)
        {
			RunWorkerCompletedEventArgs bgresult = null;
			bg.RunWorkerCompleted += (a, b) =>
			{
                 bgresult = b;
            };
			bg.RunWorkerAsync();
			while (bgresult == null)
				Thread.Sleep(100);

            if (bgresult.Cancelled)
            {
                //cancelHandler?.Invoke(this, progress);
                writer.WriteLine("[Cancelled]");
                throw new ProgressCancelledException();
            }
			if (bgresult.Error != null)
				//errorHandler?.Invoke(this, b.Error);
				throw bgresult.Error;

            //continuation?.Invoke(this);
            writer.WriteLine("[Completed]");
        }
    }

	//TODO change to an object hierarchy? not much information to track.
	public enum Response
	{
		NO_RESPONSE,
		OKYes,
		CancelNo,
		OKYesForever,
		CancelNoForever
	}

	[Serializable]
	public class ProgressCancelledException : Exception
	{
		public ProgressCancelledException()
		{
		}

		public ProgressCancelledException(string message) : base(message)
		{
		}

		public ProgressCancelledException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected ProgressCancelledException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}


	public class ProgressInfo : AspenProgress
	{
		private DialogInfo dialog;
		private TextWriter writer;
		private TextReader reader;

		private System.Threading.Tasks.Task<string> cancelWatch;

		//TODO based on this idea, DialogInfo should require a Context object in its constructor
		public ProgressInfo(DialogInfo dialog)
		{
			this.dialog = dialog;
			this.writer = dialog.writer;
			this.reader = dialog.reader;
		}
		public ProgressInfo(DialogInfo dialog, System.Threading.Tasks.Task<string> cancelWatch)
			: this(dialog)
		{
			this.cancelWatch = cancelWatch;
		}

		//TODO maybe this should be a new subclass
		public ProgressInfo(ProgressInfo parentProgress, double endPercent)
		{
			//TODO this is probably creating a memory leak
			this.parent = parentProgress;
			this.startPercent = parentProgress.Percent;
			this.endPercent = endPercent;
			this.dialog = parentProgress.dialog;
			this.writer = parentProgress.writer;
			this.reader = parentProgress.reader;
			this.cancelWatch = parentProgress.cancelWatch;
		}

		private double _percent = 0.0;
		private ProgressInfo parent;
		private double startPercent;
		private double endPercent;
		private bool indeterminate;

		public double Percent
        {
            get => _percent;
            set
            {
                ExceptionIfCancelled();
                _percent = value;
                if (parent == null)
                    writer.WriteLine("{1}@{0:P0}", _percent, Aspen.Track?.GetCurrent()?.Name);
                else
                    parent.Percent = CalcParentPercent();
            }
        }

        private void ExceptionIfCancelled()
		{
			// TODO oversimplified, probably has to handle more booleans and not consider it cancelled if progress is complete.
			if (cancelWatch != null && cancelWatch.IsCompleted)
			{
				this.Cancelled = true;
			}

			if (Cancelled || (dialog.IsResponded && !dialog.IsAgreed))
			{
				this.Cancelled = true;
				throw new ProgressCancelledException();
			}
		}

		private double CalcParentPercent()
		{
			double range = endPercent - startPercent;
			return startPercent + range * _percent;
		}

		public bool Indeterminate
        {
            get => indeterminate;
            set
            {
                ExceptionIfCancelled();
                indeterminate = value;
            }
        }

        public bool Cancelled { get; private set; }
		public string Title { get; set; }
		public T Step<T>(string stepTitle, Func<AspenProgress, T> step, double percentAfterStep)
		{
			if (stepTitle != null) this.Title = stepTitle;
			AspenProgress subprogress = new ProgressInfo(this, percentAfterStep);
			T result = step.Invoke(subprogress);
			this.Percent = percentAfterStep;
			return result;
		}
		public void Step(string stepTitle, Action<AspenProgress> step, double percentAfterStep)
		{
			if (stepTitle != null) this.Title = stepTitle;
			AspenProgress subprogress = new ProgressInfo(this, percentAfterStep);
			step.Invoke(subprogress);
			this.Percent = percentAfterStep;
		}
		public T StepIndeterminately<T>(string stepTitle, Func<AspenProgress, T> step, double percentAfterStep)
		{
			this.Indeterminate = true;
			try
			{
				return Step(stepTitle, step, percentAfterStep);
			}
			finally
			{
				this.Indeterminate = false;
			}
		}
		public void StepIndeterminately(string stepTitle, Action<AspenProgress> step, double percentAfterStep)
		{
			this.Indeterminate = true;
			try
			{
				Step(stepTitle, step, percentAfterStep);
			}
			finally
			{
				this.Indeterminate = false;
			}
		}
		public void StepsForItems<T>(string itemsTitle, IEnumerable<T> items, Action<T, AspenProgress> itemStep, double percentAfterAll)
		{
			Step(itemsTitle,
				(itemsProgress) =>
				{
					double stepSize = 100.0 / items.Count();
					double percentAfterItem = 0;
					foreach (T item in items)
					{
						percentAfterItem += stepSize;

						itemsProgress.Step(null,
							(itemProgress) => itemStep.Invoke(item, itemProgress),
							percentAfterStep: percentAfterItem);
					}
				},
				percentAfterAll);
		}

		//public void ProgressToPercent(Action<AspenProgress> subprogression, Action<Exception> errorHandler, double percentEnd)
		//{
		//	try
		//	{
		//		Step(null, subprogression, percentEnd);
		//	}
		//	catch (Exception exc)
		//	{
		//		if (errorHandler != null)
		//			errorHandler.Invoke(exc);
		//		else
		//			throw;
		//	}
		//}
	}

}
