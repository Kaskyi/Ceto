namespace Ceto.Common.Threading.Tasks
{
  /// <summary>
  ///   Allows a task to listen on for when
  ///   other tasks finish. When all tasks
  ///   this task is listening to are finished
  ///   the tasks StopWaiting function is called.
  /// </summary>
  public class TaskListener
  {
    private volatile int m_waiting;

    /// <summary>
    ///   Create a new listener.
    /// </summary>
    /// <param name="task">The task that is listening.</param>
    public TaskListener(ThreadedTask task)
    {
      ListeningTask = task;
    }

    /// <summary>
    ///   The task that is listening.
    /// </summary>
    public ThreadedTask ListeningTask { get; }

    /// <summary>
    ///   How many tasks the task is waiting on.
    /// </summary>
    public int Waiting
    {
      get => m_waiting;
      set => m_waiting = value;
    }

    /// <summary>
    ///   Called when any of the tasks this task is listening
    ///   on have finished. Once waiting reaches 0 the task
    ///   stops waiting.
    /// </summary>
    public void OnFinish()
    {
      m_waiting--;

      if (m_waiting == 0 && !ListeningTask.Cancelled)
        ListeningTask.StopWaiting();
    }
  }
}