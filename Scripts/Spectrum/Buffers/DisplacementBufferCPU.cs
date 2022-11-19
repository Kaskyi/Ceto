using System.Collections.Generic;
using Razomy.Unity.Scripts.Common.Interpolation;
using Razomy.Unity.Scripts.Common.Threading.Scheduler;
using Razomy.Unity.Scripts.Ocean;
using Razomy.Unity.Scripts.Ocean.Querys;
using Razomy.Unity.Scripts.Spectrum.Conditions;
using UnityEngine;

namespace Razomy.Unity.Scripts.Spectrum.Buffers
{
  public class DisplacementBufferCPU : WaveSpectrumBufferCPU, IDisplacementBuffer
  {
    private const int NUM_BUFFERS = 3;

    private readonly IList<InterpolatedArray2f[]> m_displacements;

    public DisplacementBufferCPU(int size, Scheduler scheduler) : base(size, NUM_BUFFERS, scheduler)
    {
      var GRIDS = QueryDisplacements.GRIDS;
      var CHANNELS = QueryDisplacements.CHANNELS;

      m_displacements = new List<InterpolatedArray2f[]>(2);

      m_displacements.Add(new InterpolatedArray2f[GRIDS]);
      m_displacements.Add(new InterpolatedArray2f[GRIDS]);

      for (var i = 0; i < GRIDS; i++)
      {
        m_displacements[0][i] = new InterpolatedArray2f(size, size, CHANNELS, true);
        m_displacements[1][i] = new InterpolatedArray2f(size, size, CHANNELS, true);
      }
    }

    public InterpolatedArray2f[] GetReadDisplacements()
    {
      return m_displacements[READ];
    }

    public void CopyAndCreateDisplacements(out IList<InterpolatedArray2f> displacements)
    {
      //Debug.Log("Copy and create");

      var source = GetReadDisplacements();
      QueryDisplacements.CopyAndCreateDisplacements(source, out displacements);
    }

    public void CopyDisplacements(IList<InterpolatedArray2f> displacements)
    {
      var source = GetReadDisplacements();
      QueryDisplacements.CopyDisplacements(source, displacements);
    }

    public Vector4 MaxRange(Vector4 choppyness, Vector2 gridScale)
    {
      var displacements = GetReadDisplacements();

      return QueryDisplacements.MaxRange(displacements, choppyness, gridScale, null);
    }

    public void QueryWaves(WaveQuery query, QueryGridScaling scaling)
    {
      var enabled = EnabledBuffers();

      //If no buffers are enabled there is nothing to sample.
      if (enabled == 0) return;

      var displacements = GetReadDisplacements();

      QueryDisplacements.QueryWaves(query, enabled, displacements, scaling);
    }

    protected override void Initilize(WaveSpectrumCondition condition, float time)
    {
      var displacements = GetWriteDisplacements();

      displacements[0].Clear();
      displacements[1].Clear();
      displacements[2].Clear();
      displacements[3].Clear();

      if (m_initTask == null)
        m_initTask = condition.GetInitSpectrumDisplacementsTask(this, time);
      else if (m_initTask.SpectrumType != condition.Key.SpectrumType || m_initTask.NumGrids != condition.Key.NumGrids)
        m_initTask = condition.GetInitSpectrumDisplacementsTask(this, time);
      else
        m_initTask.Reset(condition, time);
    }

    public InterpolatedArray2f[] GetWriteDisplacements()
    {
      return m_displacements[WRITE];
    }

    public override void Run(WaveSpectrumCondition condition, float time)
    {
      SwapDisplacements();
      base.Run(condition, time);
    }

    private void SwapDisplacements()
    {
      var tmp = m_displacements[0];
      m_displacements[0] = m_displacements[1];
      m_displacements[1] = tmp;
    }
  }
}