using System;
using System.Collections.Generic;

namespace ASPEN
{
	/// <summary>
	/// All Software Projects Eventually Need a way to track dialog progress
	/// </summary>
	public interface AspenProgress
	{
		string Title { get; set; }

		double Percent { get; set; }

		bool Indeterminate { get; set; }

		bool Cancelled { get; }
		//TODO StepUncancellable but with a better name, add bool Cancellable {get;set;} that prevents automatic exception when Cancelled
		T Step<T>(string stepTitle, Func<AspenProgress, T> step, double percentAfterStep);
		void Step(string stepTitle, Action<AspenProgress> step, double percentAfterStep);
		
		//TODO except for cancellation, should this just not pass an AspenProgress to differentiate how it works?
		T StepIndeterminately<T>(string stepTitle, Func<AspenProgress, T> step, double percentAfterStep);
		void StepIndeterminately(string stepTitle, Action<AspenProgress> step, double percentAfterStep);

		void StepsForItems<T>(string itemsTitle, IEnumerable<T> items, Action<T, AspenProgress> itemStep, double percentAfterAll);
	}
}
