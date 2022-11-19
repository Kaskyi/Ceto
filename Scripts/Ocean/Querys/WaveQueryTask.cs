using System;
using System.Collections.Generic;
using Razomy.Unity.Scripts.Common.Interpolation;
using Razomy.Unity.Scripts.Common.Threading.Tasks;
using Razomy.Unity.Scripts.Ocean.Overlays;
using Razomy.Unity.Scripts.Spectrum;
using UnityEngine;

namespace Razomy.Unity.Scripts.Ocean.Querys
{
  /// <summary>
  ///   This is the base class for the threaded and coroutine query tasks.
  /// </summary>
  public abstract class WaveQueryTask : ThreadedTask
  {
    protected IList<InterpolatedArray2f> Displacements;

    public WaveQueryTask(IEnumerable<WaveQuery> querys, Action<IEnumerable<WaveQuery>> callback, bool isThreaded)
      : base(isThreaded)
    {
      Querys = querys;
      CallBack = callback;

      Scaling = new QueryGridScaling();
    }

    /// <summary>
    ///   Can the task sample the overlays.
    ///   Only supported if running on main thread.
    /// </summary>
    public abstract bool SupportsOverlays { get; }

    /// <summary>
    ///   A handle to the overlay manager to query overlays.
    /// </summary>
    public IOverlaySampler OverlaySampler { get; set; }

    /// <summary>
    ///   Has the task been added to the scheduler.
    ///   Used to make sure a task is not added twice.
    /// </summary>
    public bool IsScheduled { get; set; }

    /// <summary>
    ///   This will be called once the task is done.
    /// </summary>
    public Action<IEnumerable<WaveQuery>> CallBack { get; }

    /// <summary>
    ///   The querys to run.
    /// </summary>
    public IEnumerable<WaveQuery> Querys { get; }

    protected float OceanLevel { get; private set; }

    protected int EnabledBuffers { get; private set; }

    protected QueryGridScaling Scaling { get; }

    protected int BufferSize { get; private set; }

    /// <summary>
    ///   The task needs to be reset before being scheduled.
    ///   This will update the settings in case wave conditions have changed.
    /// </summary>
    public void Reset(WaveSpectrum spectrum, Vector3 offset, float level)
    {
      //Dont forget to reset base.
      base.Reset();

      IsScheduled = false;

      //If the spectrum component is added and enabled then take a copy of the 
      //displacement data and update scaling settings for the waves.
      if (spectrum != null && spectrum.DisplacementBuffer != null)
      {
        var buffer = spectrum.DisplacementBuffer;

        if (Displacements == null || BufferSize != buffer.Size)
        {
          buffer.CopyAndCreateDisplacements(out Displacements);
          BufferSize = buffer.Size;
        }
        else
        {
          buffer.CopyDisplacements(Displacements);
        }

        EnabledBuffers = buffer.EnabledBuffers();

        var invGridSizes = new Vector4();
        invGridSizes.x = 1.0f / (spectrum.GridSizes.x * spectrum.gridScale);
        invGridSizes.y = 1.0f / (spectrum.GridSizes.y * spectrum.gridScale);
        invGridSizes.z = 1.0f / (spectrum.GridSizes.z * spectrum.gridScale);
        invGridSizes.w = 1.0f / (spectrum.GridSizes.w * spectrum.gridScale);

        Scaling.invGridSizes = invGridSizes;
        Scaling.choppyness = spectrum.Choppyness * spectrum.gridScale;
        Scaling.scaleY = spectrum.gridScale;
        Scaling.offset = offset;
        Scaling.numGrids = spectrum.numberOfGrids;
      }
    }

    public override void End()
    {
      base.End();

      CallBack(Querys);
    }
  }
}