using System;
using System.Collections;
using System.Collections.Generic;
using Razomy.Unity.Scripts.Common.Threading.Scheduler;

namespace Razomy.Unity.Scripts.Common.Threading.Tasks
{
  /// <summary>
  ///   A abstract task that implements the default behaviour of a task
  /// </summary>
  public abstract class ThreadedTask : IThreadedTask, ICancelToken
  {
    /// <summary>
    ///   Lock for functions that maybe accessed by task running on another thread.
    /// </summary>
    private readonly object m_lock = new();

    private volatile bool m_cancelled;
    private volatile bool m_done;
    private volatile bool m_noFinish;
    private volatile bool m_ran;
    private volatile bool m_runOnStopWaiting;
    protected IScheduler m_scheduler;
    private volatile bool m_started;

    /// <summary>
    ///   Create a task.
    /// </summary>
    /// <param name="mainThread"> Can the task only be run on the main thread</param>
    /// <param name="key">The key to identify the task. Can be null</param>
    protected ThreadedTask(bool isThreaded)
    {
      m_scheduler = null;
      IsThreaded = isThreaded;
      Listeners = new LinkedList<TaskListener>();
      Listener = new TaskListener(this);
    }

    /// <summary>
    ///   A list of task listeners that are waiting
    ///   on this task to finish running.
    /// </summary>
    protected LinkedList<TaskListener> Listeners { get; }

    /// <summary>
    ///   The listener for this task that can listen on another
    ///   task to stop running.
    /// </summary>
    protected TaskListener Listener { get; }

    /// <summary>
    ///   How long the task took to run in milliseconds.
    /// </summary>
    public float RunTime { get; set; }

    /// <summary>
    ///   True if this task must be run on the main thread.
    /// </summary>
    public bool IsThreaded { get; }

    /// <summary>
    ///   True if the task has ran.
    ///   Should be set to true in the tasks end function.
    /// </summary>
    public bool Done => m_done;

    /// <summary>
    ///   True if the task is finished.
    ///   Should be set to true in the tasks run function.
    /// </summary>
    public bool Ran => m_ran;

    /// <summary>
    ///   Set to true to skip the end function.
    ///   This will immediately trigger any tasks
    ///   waiting on this one to stop waiting.
    /// </summary>
    public bool NoFinish
    {
      get => m_noFinish;
      set => m_noFinish = value;
    }

    /// <summary>
    ///   Is the task waiting on another task to finish.
    /// </summary>
    public bool Waiting => Listener.Waiting > 0;

    /// <summary>
    ///   True if the tasks runs immediately after stop wait
    ///   or gets queued as a scheduled task.
    /// </summary>
    public bool RunOnStopWaiting
    {
      get => m_runOnStopWaiting;
      set => m_runOnStopWaiting = value;
    }

    /// <summary>
    ///   True if the task has started.
    ///   Should be set to true in the tasks start function
    /// </summary>
    public bool Started => m_started;

    /// <summary>
    ///   True if the task has been cancelled.
    /// </summary>
    public bool Cancelled => m_cancelled;

    /// <summary>
    ///   The scheduler used to run this task
    /// </summary>
    public IScheduler Scheduler
    {
      set => m_scheduler = value;
    }

    /// <summary>
    ///   Starts the task. Used to initialize anything
    ///   that maybe needed before the task is run.
    ///   Is always called from the main thread.
    /// </summary>
    public virtual void Start()
    {
      m_started = true;
    }

    /// <summary>
    ///   Reset task to its starting conditions.
    /// </summary>
    public virtual void Reset()
    {
      lock (m_lock)
      {
        Listeners.Clear();
        Listener.Waiting = 0;
        m_ran = false;
        m_done = false;
        m_cancelled = false;
        m_started = false;
        RunTime = 0.0f;
      }
    }

    /// <summary>
    ///   Runs the task. If mainThread is true this will
    ///   only be called from the main thread. If it is false the
    ///   task will be run on any available thread.
    /// </summary>
    public abstract IEnumerator Run();

    /// <summary>
    ///   Ends the task. Used to do any clean up when the task is
    ///   finished. Is always called from the main thread.
    /// </summary>
    public virtual void End()
    {
      m_done = true;

      lock (m_lock)
      {
        if (!m_cancelled)
        {
          //Inform tasks waiting on this task to finish that it has.
          var e = Listeners.GetEnumerator();
          while (e.MoveNext()) e.Current.OnFinish();
        }

        Listeners.Clear();
      }
    }

    /// <summary>
    ///   This function gets called on task if
    ///   scheduler cancels tasks.
    /// </summary>
    public virtual void Cancel()
    {
      lock (m_lock)
      {
        m_cancelled = true;
        Listeners.Clear();
      }
    }

    /// <summary>
    ///   Wait on task to finish before running.
    ///   This task will be added to the scheduler waiting queue
    ///   and will be added to the schedule queue when all tasks
    ///   it is waiting on have finished.
    /// </summary>
    public virtual void WaitOn(ThreadedTask task)
    {
      lock (m_lock)
      {
        if (task.Cancelled)
          throw new InvalidOperationException("Can not wait on a task that is cancelled");

        if (task.Done)
          throw new InvalidOperationException("Can not wait on a task that is already done");

        if (task.IsThreaded && task.NoFinish && !IsThreaded)
          throw new InvalidOperationException("A non-threaded task cant wait on a threaded task with no finish");

        Listener.Waiting++;
        task.Listeners.AddLast(Listener);
      }
    }

    /// <summary>
    ///   Must be called at the end of the run function
    ///   to notify the scheduler that the task has finished.
    /// </summary>
    protected virtual void FinishedRunning()
    {
      m_ran = true;

      if (m_noFinish)
        m_done = true;

      lock (m_lock)
      {
        if (m_noFinish && !m_cancelled)
        {
          //Inform tasks waiting on this task to finish that it has.
          var e = Listeners.GetEnumerator();
          while (e.MoveNext()) e.Current.OnFinish();

          Listeners.Clear();
        }
      }
    }

    /// <summary>
    ///   The tasks that this task was waiting on to finish have
    ///   now finished and it will now be run by the scheduler.
    /// </summary>
    public virtual void StopWaiting()
    {
      lock (m_lock)
      {
        if (m_scheduler == null || m_cancelled) return;

        m_scheduler.StopWaiting(this, m_runOnStopWaiting);
      }
    }

    /// <summary>
    ///   The task as a string.
    /// </summary>
    public override string ToString()
    {
      return string.Format("[Task: isThreaded={0}, started={1}, ran={2}, done={3}, cancelled={4}]", IsThreaded,
        m_started, m_ran, m_done, m_cancelled);
    }
  }
}