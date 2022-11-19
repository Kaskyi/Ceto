using System;
using System.Collections;
using System.Collections.Generic;
using Razomy.Unity.Scripts.Spectrum.Buffers;

namespace Razomy.Unity.Scripts.Ocean.Querys
{
  /// <summary>
  ///   This task will run a batch of querys on another thread.
  /// </summary>
  public class ThreadedWaveQueryTask : WaveQueryTask
  {
    public ThreadedWaveQueryTask(IEnumerable<WaveQuery> querys, Action<IEnumerable<WaveQuery>> callBack)
      : base(querys, callBack, true)
    {
    }

    /// <summary>
    ///   Threaded sampling of the overlays is not supported.
    /// </summary>
    public override bool SupportsOverlays => false;

    /// <summary>
    ///   Run the task.
    ///   Warning - this will not run on the main thread.
    /// </summary>
    public override IEnumerator Run()
    {
      var e = Querys.GetEnumerator();
      while (e.MoveNext())
      {
        //Task has been cancelled. Stop and return.
        if (Cancelled) break;

        var query = e.Current;

        query.result.Clear();

        //Sample the spectrum waves.
        if (Displacements != null && query.SamplesSpectrum)
          QueryDisplacements.QueryWaves(query, EnabledBuffers, Displacements, Scaling);

        query.result.height += OceanLevel;
      }

      FinishedRunning();
      return null;
    }
  }
}