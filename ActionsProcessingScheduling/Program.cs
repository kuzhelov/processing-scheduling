using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ActionsProcessingScheduling
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			// that's just the simple example of the way to guarantee that there is at least one processor 
			// that stays free of processing even under the high load
			var maxConcurrencyLevel = Math.Max(1, Environment.ProcessorCount - 1);

			var scheduler = new LimitedConcurrencyActionsScheduler(
				maxConcurrencyLevel: maxConcurrencyLevel);

			// modern syntax constructs that are responsible for async calls are used for the sake of simplicity
			// scheduler's implementation is completely unaware of these features intentionally
			var successfulActions = new[]
			{
				CreateProcessingAction(completesSuccessfully: true, processingTime: TimeSpan.FromSeconds(3)),
				CreateProcessingAction(completesSuccessfully: true, processingTime: TimeSpan.FromSeconds(1)),
				CreateProcessingAction(completesSuccessfully: true, processingTime: TimeSpan.FromSeconds(5))
			};

			var failedActions = new[]
			{
				CreateProcessingAction(completesSuccessfully: false, processingTime: TimeSpan.FromSeconds(8)),
				CreateProcessingAction(completesSuccessfully: false, processingTime: TimeSpan.FromSeconds(1))
			};

			Console.WriteLine($"Scheduling tasks with max concurrency level of: {maxConcurrencyLevel}");

			var actionIdCounter = 0;

			var tasksToWait = successfulActions.Concat(failedActions)
				.Select(actionToProcess =>
				{
					var currentActionId = actionIdCounter++;

					return scheduler.ScheduleAction(actionToProcess)
						.ContinueWith(completedProcessingTask =>
						{
							Debug.Assert(completedProcessingTask.IsCompleted);
							Console.WriteLine($"Action [{currentActionId}] has completed " +
								(completedProcessingTask.IsFaulted ? "with error" : "successfully"));
						});
				});

			Task.WaitAll(tasksToWait.ToArray());
		}

		private static Action CreateProcessingAction(
			bool completesSuccessfully,
			TimeSpan processingTime)
		{
			return () =>
			{
				Console.WriteLine("Start action processing..");

				Thread.Sleep(processingTime);
				if (!completesSuccessfully)
				{
					throw new ApplicationException("task processing exception");
				}
			};
		}
	}
}