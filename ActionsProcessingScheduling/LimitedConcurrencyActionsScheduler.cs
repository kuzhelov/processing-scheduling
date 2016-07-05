using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ActionsProcessingScheduling
{
	// the lack of .NET >= 4.5 features (such as awaitable calls) is intentional
	public class LimitedConcurrencyActionsScheduler
	{
		public LimitedConcurrencyActionsScheduler(int maxConcurrencyLevel)
		{
			Debug.Assert(maxConcurrencyLevel > 0);

			MaxConcurrencyLevel = maxConcurrencyLevel;
		}

		// has a promise-return semantics that provides an ability to 
		// keep track of action execution progress and its result (in a sence of absence of errors)
		public Task ScheduleAction(Action action, bool withHighestPriority = false)
		{
			var newActionDescriptor = new Tuple<Action, TaskCompletionSource<int>>(
				item1: action,
				item2: new TaskCompletionSource<int>());

			lock (_schedulingSync)
			{
				if (withHighestPriority)
				{
					_pendingTasks.Insert(
						index: 0,
						item: newActionDescriptor);
				}
				else
				{
					_pendingTasks.Add(newActionDescriptor);
				}

				if (_currentlyProcessingJobsCount < MaxConcurrencyLevel)
				{
					++_currentlyProcessingJobsCount;
					StartExecutionAsync(ofAction: action);
				}
			}

			return newActionDescriptor.Item2.Task;
		}

		public int MaxConcurrencyLevel { get; private set; }

		private void StartExecutionAsync(Action ofAction)
		{
			_taskFactory
				.StartNew(() =>
				{
					// iterative approach is preferred to the recursive one
					var shouldProcessNextTaskFromQueue = true;
					while (shouldProcessNextTaskFromQueue)
					{
						shouldProcessNextTaskFromQueue = ProcessNextTaskFromQueue();
					}
				});
		}

		private bool ProcessNextTaskFromQueue()
		{
			Tuple<Action, TaskCompletionSource<int>> nextTaskDescriptor = null;

			lock (_schedulingSync)
			{
				if (_pendingTasks.Any())
				{
					nextTaskDescriptor = _pendingTasks[0];
					_pendingTasks.RemoveAt(0);
				}
				else
				{
					_currentlyProcessingJobsCount--;
				}
			}

			if (nextTaskDescriptor != null)
			{
				var actionToProcess = nextTaskDescriptor.Item1;
				var actionProcessingCompletionSource = nextTaskDescriptor.Item2;

				try
				{
					actionToProcess();
					actionProcessingCompletionSource.SetResult(0);
				}
				// ReSharper disable once EmptyGeneralCatchClause in order to posses consistency of actions processing (exceptions are swallowed)
				catch (Exception exception)
				{
					actionProcessingCompletionSource.SetException(exception);
				}

				// recursive call here could be considered as an alternative, 
				// but it is a bit more efficient and a way more safer to prefer the iterative approach instead
				return true; 
			}

			return false;
		}

		private readonly TaskFactory _taskFactory = new TaskFactory();
		private readonly List<Tuple<Action, TaskCompletionSource<int>>> _pendingTasks = 
			new List<Tuple<Action, TaskCompletionSource<int>>>();

		private readonly object _schedulingSync = new object();
		private int _currentlyProcessingJobsCount;
	}
}