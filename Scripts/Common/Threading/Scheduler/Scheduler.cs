using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Ceto.Common.Threading.Tasks;

namespace Ceto.Common.Threading.Scheduling
{
  /// <summary>
  ///   Scheduler.
  /// </summary>
  public class Scheduler : IScheduler
  {
    /// <summary>
    ///   Used to run coroutine tasks.
    /// </summary>
    private readonly ICoroutine m_coroutine;

    /// <summary>
    ///   The queue of tasks that have finished and need to be clean up.
    /// </summary>
    private readonly LinkedList<IThreadedTask> m_finishedTasks;

    /// <summary>
    ///   Temporary list hold tasks that have ran.
    /// </summary>
    private readonly LinkedList<IThreadedTask> m_haveRan;

    /// <summary>
    ///   Lock for functions that maybe accessed by task running on another thread.
    /// </summary>
    private readonly object m_lock = new();

    /// <summary>
    ///   The list of tasks currently running.
    /// </summary>
    private readonly LinkedList<IThreadedTask> m_runningTasks;

    /// <summary>
    ///   The queue of tasks that need to be run.
    /// </summary>
    private readonly LinkedList<IThreadedTask> m_scheduledTasks;

    /// <summary>
    ///   The list of task currently waiting.
    /// </summary>
    private readonly LinkedList<IThreadedTask> m_waitingTasks;

    /// <summary>
    ///   A a exception thrown by a running task to be rethrown by the schedular
    /// </summary>
    private Exception m_exception;

    private volatile bool m_shutingDown;

    public Scheduler()
    {
      MaxWaitTime = 1000.0f;
      MinWaitTime = 100.0f;

      m_coroutine = null;
      MaxTasksPerUpdate = 100;
      MaxFinishPerUpdate = 100;

      m_scheduledTasks = new LinkedList<IThreadedTask>();
      m_finishedTasks = new LinkedList<IThreadedTask>();
      m_runningTasks = new LinkedList<IThreadedTask>();
      m_waitingTasks = new LinkedList<IThreadedTask>();
      m_haveRan = new LinkedList<IThreadedTask>();
    }

    public Scheduler(int maxTasksPerUpdate, int maxFinishPerUpdate, ICoroutine coroutine)
    {
      MaxWaitTime = 1000.0f;
      MinWaitTime = 100.0f;

      m_coroutine = coroutine;
      MaxTasksPerUpdate = Math.Max(1, maxTasksPerUpdate);
      MaxFinishPerUpdate = Math.Max(1, maxFinishPerUpdate);

      m_scheduledTasks = new LinkedList<IThreadedTask>();
      m_finishedTasks = new LinkedList<IThreadedTask>();
      m_runningTasks = new LinkedList<IThreadedTask>();
      m_waitingTasks = new LinkedList<IThreadedTask>();
      m_haveRan = new LinkedList<IThreadedTask>();
    }

    /// <summary>
    ///   How many tasks were ran this update.
    /// </summary>
    public int TasksRanThisUpdate { get; private set; }

    /// <summary>
    ///   How many task were finished this update.
    /// </summary>
    public int TasksFinishedThisUpdate { get; private set; }

    /// <summary>
    ///   Max tasks that will be ran per update.
    /// </summary>
    public int MaxTasksPerUpdate { get; }

    /// <summary>
    ///   Max tasks to run per update.
    /// </summary>
    public int MaxFinishPerUpdate { get; }

    /// <summary>
    ///   The maximum time (ms) to wait when cancelling tasks.
    /// </summary>
    public float MaxWaitTime { get; set; }

    /// <summary>
    ///   The minimum time (ms) to wait when cancelling tasks.
    /// </summary>
    public float MinWaitTime { get; set; }

    /// <summary>
    ///   Disable multithreading.
    /// </summary>
    public bool DisableMultithreading { get; set; }

    /// <summary>
    ///   Is the scheduler shunting down.
    /// </summary>
    public bool ShutingDown
    {
      set => m_shutingDown = value;
    }

    /// <summary>
    ///   Returns true if the scheduler has
    ///   tasks to run or if there are tasks running.
    /// </summary>
    public bool HasTasks()
    {
      return ScheduledTasks() > 0 || RunningTasks() > 0 || FinishingTasks() > 0 || WaitingTasks() > 0;
    }

    /// <summary>
    ///   Cancel a task. Task will have its cancel function called
    ///   if it is cancelled. Tasks that are already running or
    ///   finishing can not be cancelled.
    /// </summary>
    public void Cancel(IThreadedTask task)
    {
      lock (m_lock)
      {
        if (m_scheduledTasks.Contains(task))
        {
          task.Cancel();
          m_scheduledTasks.Remove(task);
        }
        else if (m_waitingTasks.Contains(task))
        {
          task.Cancel();
          m_waitingTasks.Remove(task);
        }
      }
    }

    /// <summary>
    ///   Does the scheduler contain the task in any of
    ///   its queues.
    /// </summary>
    public bool Contains(IThreadedTask task)
    {
      if (IsScheduled(task)) return true;
      if (IsWaiting(task)) return true;
      if (IsRunning(task)) return true;
      if (IsFinishing(task)) return true;

      return false;
    }

    /// <summary>
    ///   Returns > 0 if the scheduler has tasks scheduled.
    /// </summary>
    public int ScheduledTasks()
    {
      var count = 0;
      lock (m_lock)
      {
        count = m_scheduledTasks.Count;
      }

      return count;
    }

    /// <summary>
    ///   Returns > 0 if the scheduler has tasks running.
    /// </summary>
    public int RunningTasks()
    {
      var count = 0;
      lock (m_lock)
      {
        count = m_runningTasks.Count;
      }

      return count;
    }

    /// <summary>
    ///   Returns > 0 if the scheduler has tasks waiting.
    /// </summary>
    public int WaitingTasks()
    {
      var count = 0;
      lock (m_lock)
      {
        count = m_waitingTasks.Count;
      }

      return count;
    }

    /// <summary>
    ///   Returns > 0 if the scheduler has tasks finishing.
    /// </summary>
    public int FinishingTasks()
    {
      var count = 0;
      lock (m_lock)
      {
        count = m_finishedTasks.Count;
      }

      return count;
    }

    /// <summary>
    ///   Returns > 0 if the scheduler has tasks.
    /// </summary>
    public int Tasks()
    {
      return RunningTasks() + ScheduledTasks() + WaitingTasks() + FinishingTasks();
    }

    /// <summary>
    ///   Returns true if the tasks has been added to
    ///   the scheduler and has not been run.
    /// </summary>
    public bool IsScheduled(IThreadedTask task)
    {
      bool b;
      lock (m_lock)
      {
        b = m_scheduledTasks.Contains(task);
      }

      return b;
    }

    /// <summary>
    ///   Returns true if the tasks has been added to
    ///   the scheduler and is running.
    /// </summary>
    public bool IsRunning(IThreadedTask task)
    {
      bool b;
      lock (m_lock)
      {
        b = m_runningTasks.Contains(task);
      }

      return b;
    }

    /// <summary>
    ///   Returns true if the tasks has been added to
    ///   the scheduler and is waiting.
    /// </summary>
    public bool IsWaiting(IThreadedTask task)
    {
      bool b;
      lock (m_lock)
      {
        b = m_waitingTasks.Contains(task);
      }

      return b;
    }

    /// <summary>
    ///   Returns true if the tasks has been added to
    ///   the scheduler and is finishing.
    /// </summary>
    public bool IsFinishing(IThreadedTask task)
    {
      bool b;
      lock (m_lock)
      {
        b = m_finishedTasks.Contains(task);
      }

      return b;
    }

    /// <summary>
    ///   Add a task to scheduler. The task will be queued and
    ///   will be run when it reaches the front of queue.
    /// </summary>
    public void Add(IThreadedTask task)
    {
      lock (m_lock)
      {
        if (m_shutingDown) return;

#if CETO_DEBUG_SCHEDULER
				if (Contains(task))
					throw new ArgumentException("Scheduler already contains task.");
				
				if (task.Started)
					throw new ArgumentException("Task has already been started.");

				if (task.Ran)
					throw new ArgumentException("Task has already been ran.");
				
				if (task.Done)
					throw new ArgumentException("Task has already been done.");

                if (task.Cancelled)
					throw new ArgumentException("Task has been cancelled.");
#endif

        task.Scheduler = this;
        m_scheduledTasks.AddLast(task);
      }
    }

    /// <summary>
    ///   Add a task to scheduler and run immediately.
    /// </summary>
    public void Run(IThreadedTask task)
    {
      lock (m_lock)
      {
        if (m_shutingDown) return;

#if CETO_DEBUG_SCHEDULER
				if (Contains(task))
					throw new ArgumentException("Scheduler already contains task.");
				
				if (task.Started)
					throw new ArgumentException("Task has already been started.");

				if (task.Ran)
					throw new ArgumentException("Task has already been ran.");
				
				if (task.Done)
					throw new ArgumentException("Task has already been done.");

                if (task.Cancelled)
					throw new ArgumentException("Task has been cancelled.");
#endif

        task.Scheduler = this;

        if (TasksRanThisUpdate >= MaxTasksPerUpdate)
          Add(task);
        else
          RunTask(task);
      }
    }

    /// <summary>
    ///   Adds a task to the waiting queue. A task will stop
    ///   waiting on some predefined event like another task
    ///   finishing.
    /// </summary>
    public void AddWaiting(IThreadedTask task)
    {
      lock (m_lock)
      {
        if (m_shutingDown) return;

#if CETO_DEBUG_SCHEDULER
				if (Contains(task))
					throw new ArgumentException("Scheduler already contains task.");
				
				if (task.Started)
					throw new ArgumentException("Task has already been started.");

				if (task.Ran)
					throw new ArgumentException("Task has already been ran.");
				
				if (task.Done)
					throw new ArgumentException("Task has already been done.");

               if (task.Cancelled)
					throw new ArgumentException("Task has been cancelled.");
#endif
        task.Scheduler = this;
        m_waitingTasks.AddLast(task);
      }
    }

    /// <summary>
    ///   Removes a task from the waiting queue and
    ///   adds it to the scheduled queue were it will be run.
    /// </summary>
    public void StopWaiting(IThreadedTask task, bool run)
    {
      lock (m_lock)
      {
        if (m_shutingDown) return;

#if CETO_DEBUG_SCHEDULER
				if (IsScheduled(task))
					throw new ArgumentException("Task has already been scheduled.");
				
				if (IsRunning(task))
					throw new ArgumentException("Task is currently running.");
				
				if (IsFinishing(task))
					throw new ArgumentException("Task is currently finishing.");
				
				if (task.Started)
					throw new ArgumentException("Task has already been started.");

				if (task.Ran)
					throw new ArgumentException("Task has already been ran.");
				
				if (task.Done)
					throw new ArgumentException("Task has already been done.");

                if (task.Cancelled)
					throw new ArgumentException("Task has been cancelled.");
#endif

        m_waitingTasks.Remove(task);

        if (run)
          RunTask(task);
        else
          m_scheduledTasks.AddLast(task);
      }
    }

    /// <summary>
    ///   Update Scheduler.
    /// </summary>
    public void Update()
    {
      //Dont forget any multithreaded or coroutine tasks must 
      //tell the scheduler when they are finished from their Run 
      //function by calling FinishedRunning().

      TasksRanThisUpdate = 0;
      TasksFinishedThisUpdate = 0;

      //clean up any tasks that have finished since last time
      //scheduler was updated.
      FinishTasks();

      while (TasksRanThisUpdate < MaxTasksPerUpdate)
      {
        if (ScheduledTasks() > 0)
        {
          var task = m_scheduledTasks.First.Value;
          m_scheduledTasks.RemoveFirst();
          RunTask(task);
        }

        //If a task running on another thread or in a 
        //coroutine has thrown a exception rethrow it here.
        CheckForException();

        if (ScheduledTasks() == 0) break;
      }

      FinishTasks();
    }

    /// <summary>
    ///   Runs next task.
    /// </summary>
    private void RunTask(IThreadedTask task)
    {
      lock (m_lock)
      {
#if CETO_DEBUG_SCHEDULER
				if (Contains(task))
					throw new ArgumentException("Scheduler already contains task.");
				
				if (task.Started)
					throw new ArgumentException("Task has already been started.");

				if (task.Ran)
					throw new ArgumentException("Task has already been ran.");
				
				if (task.Done)
					throw new ArgumentException("Task has already been done.");

                if (task.Cancelled)
					throw new ArgumentException("Task has been cancelled.");
#endif

        TasksRanThisUpdate++;

        if (!task.IsThreaded || DisableMultithreading)
        {
          //Start task.
          task.Start();
          //Run task
          var e = task.Run();

          //If task returned a enumerator 
          //the task is a coroutine.
          if (e != null)
          {
            if (m_coroutine == null)
              throw new InvalidOperationException(
                "Scheduler trying to run a coroutine task when coroutine interface is null");

            //Start coroutine and add to running queue if it has 
            //not been added to the finishing queue.
            //If a task has ran quickly it may have already
            //been added to the finished queue.
            m_coroutine.RunCoroutine(e);
            if (!IsFinishing(task)) m_runningTasks.AddLast(task);
          }
          else
          {
            //Clean up task
            task.End();
          }
        }
        else
        {
          //Start task
          task.Start();
          //Run task on separate thread
          //and add to running tasks list.
          m_runningTasks.AddLast(task);
          ThreadPool.QueueUserWorkItem(RunThreaded, task);
        }
      }
    }

    /// <summary>
    ///   Runs a threaded task
    /// </summary>
    private void RunThreaded(object o)
    {
      var task = o as IThreadedTask;

      if (task == null)
        Throw(new InvalidCastException("Object is not a ITask or is null"));
      else
        try
        {
          task.Run();
        }
        catch (Exception e)
        {
          Throw(e);
        }
    }

    /// <summary>
    ///   Finish all tasks in the finished list
    ///   by calling there end function and removing
    ///   the from the running list.
    /// </summary>
    public void FinishTasks()
    {
      if (TasksFinishedThisUpdate >= MaxFinishPerUpdate) return;

      lock (m_lock)
      {
        m_haveRan.Clear();

        //Get a list of all tasks that have ran.
        var e1 = m_runningTasks.GetEnumerator();
        while (e1.MoveNext())
        {
          var task = e1.Current;

          if (task.Ran)
            m_haveRan.AddLast(task);
        }

        //Remove from running list and add to finished list.
        var e2 = m_haveRan.GetEnumerator();
        while (e2.MoveNext()) Finished(e2.Current);

        if (m_finishedTasks.Count == 0) return;

        //Get task at start of queue
        var finished = m_finishedTasks.First.Value;
        m_finishedTasks.RemoveFirst();

        while (finished != null)
        {
          //Clean up task.
          finished.End();
          TasksFinishedThisUpdate++;

          if (m_finishedTasks.Count == 0 || TasksFinishedThisUpdate >= MaxFinishPerUpdate)
          {
            finished = null;
          }
          else
          {
            finished = m_finishedTasks.First.Value;
            m_finishedTasks.RemoveFirst();
          }
        }

        m_haveRan.Clear();
      }
    }

    /// <summary>
    ///   Cancels all the tasks in the scheduler.
    ///   Running task have there cancel function called.
    /// </summary>
    public void CancelAllTasks()
    {
      lock (m_lock)
      {
        m_scheduledTasks.Clear();
        m_waitingTasks.Clear();

        foreach (var task in m_runningTasks)
          task.Cancel();

        var timer = new Stopwatch();
        timer.Start();

        //Try and wait for all threaded tasks to finish running.
        //Tasks should abort early when cancelled.
        //If takes longer than max wait time then give up.
        var allRan = false;
        while (!allRan && timer.ElapsedMilliseconds < MaxWaitTime)
        {
          allRan = true;
          foreach (var task in m_runningTasks)
            if (!task.Ran && task.IsThreaded)
            {
              allRan = false;
              break;
            }
        }

        while (timer.ElapsedMilliseconds < MinWaitTime)
        {
        }

        m_runningTasks.Clear();
        m_finishedTasks.Clear();
      }
    }

    /// <summary>
    ///   If a task is not running on the main thread or uses a
    ///   coroutine it needs to tell the scheduler when it is finished.
    ///   The task will then be cleaned up and have its end function called.
    /// </summary>
    public void Finished(IThreadedTask task)
    {
      lock (m_lock)
      {
#if CETO_DEBUG_SCHEDULER
				if (IsFinishing(task))
					throw new ArgumentException("Task has already been added to finished list.");
				
				if (IsScheduled(task))
					throw new ArgumentException("Task is currently scheduled.");
				
				if (IsWaiting(task))
					throw new ArgumentException("Task is currently waiting.");
#endif

        m_runningTasks.Remove(task);

        if (!m_shutingDown && !task.NoFinish && !task.Cancelled)
          m_finishedTasks.AddLast(task);
      }
    }

    /// <summary>
    ///   Checks to see if a tasks has thrown a exception.
    /// </summary>
    public void CheckForException()
    {
      lock (m_lock)
      {
        if (m_exception != null)
        {
          var e = m_exception;
          m_exception = null;
          throw e;
        }
      }
    }

    /// <summary>
    ///   If a task running on another thread or in a
    ///   coroutine has thrown a exception use this
    ///   function to throw it to the scheduler which
    ///   will then rethrow it from the main thread.
    /// </summary>
    public void Throw(Exception e)
    {
      lock (m_lock)
      {
        m_exception = e;
      }
    }

    /// <summary>
    ///   Clear scheduler of all tasks. Scheduler must
    ///   have no running tasks for it to be cleared.
    /// </summary>
    public void Clear()
    {
      if (RunningTasks() > 0)
        throw new InvalidOperationException("Can not clear the scheduler when there are running tasks.");

      m_scheduledTasks.Clear();
      m_runningTasks.Clear();
      m_finishedTasks.Clear();
      m_waitingTasks.Clear();
      m_exception = null;
    }
  }
}