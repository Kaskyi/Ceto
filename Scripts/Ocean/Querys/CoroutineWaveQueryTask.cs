using System;
using System.Collections;
using System.Collections.Generic;

namespace Ceto
{
  /// <summary>
  ///   This task will run a batch of querys on the main thread but
  ///   will use a coroutine to spread the work over a number of frames.
  /// </summary>
  public class CoroutineWaveQueryTask : WaveQueryTask
  {
    public CoroutineWaveQueryTask(IEnumerable<WaveQuery> querys, Action<IEnumerable<WaveQuery>> callBack,
      int querysPerIteration = 16)
      : base(querys, callBack, false)
    {
      QuerysPerIteration = querysPerIteration;
    }

    /// <summary>
    ///   Threaded sampling of the overlays is supported.
    /// </summary>
    public override bool SupportsOverlays => true;

    /// <summary>
    ///   The number of querys to run on each iteration of the coroutine.
    /// </summary>
    public int QuerysPerIteration { get; set; }

    /// <summary>
    ///   Run the task.
    ///   This will run on the main thread.
    /// </summary>
    public override IEnumerator Run()
    {
      var count = 0;
      var querysPerIteration = Math.Max(1, QuerysPerIteration);

      var e = Querys.GetEnumerator();
      while (e.MoveNext())
      {
        //Task has been cancelled. Stop and return.
        if (Cancelled) yield break;

        var query = e.Current;

        query.result.Clear();

        //Sample the spectrum waves.
        if (Displacements != null && query.SamplesSpectrum)
          QueryDisplacements.QueryWaves(query, EnabledBuffers, Displacements, Scaling);

        //Sample the overlay waves.
        if (OverlaySampler != null)
          OverlaySampler.QueryWaves(query);

        query.result.height += OceanLevel;

        //If count has reached the number of querys to perform
        //each iteration the yield and come back next frame.
        if (count % querysPerIteration == querysPerIteration - 1) yield return null;

        count++;
      }

      FinishedRunning();
    }
  }
}