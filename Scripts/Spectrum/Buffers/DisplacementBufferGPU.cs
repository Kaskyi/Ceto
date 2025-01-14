﻿using System.Collections.Generic;
using Razomy.Unity.Scripts.Common.Interpolation;
using Razomy.Unity.Scripts.Ocean;
using Razomy.Unity.Scripts.Ocean.Querys;
using UnityEngine;

namespace Razomy.Unity.Scripts.Spectrum.Buffers
{
  public class DisplacementBufferGPU : WaveSpectrumBufferGPU, IDisplacementBuffer
  {
    private static readonly int NUM_BUFFERS = 3;

    private readonly InterpolatedArray2f[] m_displacements;

    public DisplacementBufferGPU(int size, Shader fourierSdr)
      : base(size, fourierSdr, NUM_BUFFERS)
    {
      var GRIDS = QueryDisplacements.GRIDS;
      var CHANNELS = QueryDisplacements.CHANNELS;

      m_displacements = new InterpolatedArray2f[GRIDS];

      for (var i = 0; i < GRIDS; i++) m_displacements[i] = new InterpolatedArray2f(size, size, CHANNELS, true);
    }

    public InterpolatedArray2f[] GetReadDisplacements()
    {
      return m_displacements;
    }

    public void CopyAndCreateDisplacements(out IList<InterpolatedArray2f> displacements)
    {
      QueryDisplacements.CopyAndCreateDisplacements(m_displacements, out displacements);
    }

    public void CopyDisplacements(IList<InterpolatedArray2f> displacements)
    {
      QueryDisplacements.CopyDisplacements(m_displacements, displacements);
    }

    public Vector4 MaxRange(Vector4 choppyness, Vector2 gridScale)
    {
      return QueryDisplacements.MaxRange(m_displacements, choppyness, gridScale, null);
    }

    public void QueryWaves(WaveQuery query, QueryGridScaling scaling)
    {
      var enabled = EnabledBuffers();

      //If no buffers are enabled there is nothing to sample.
      if (enabled == 0) return;

      QueryDisplacements.QueryWaves(query, enabled, m_displacements, scaling);
    }
  }
}