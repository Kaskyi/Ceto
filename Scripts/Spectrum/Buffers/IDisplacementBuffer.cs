using System.Collections.Generic;
using Razomy.Unity.Scripts.Common.Interpolation;
using Razomy.Unity.Scripts.Ocean;
using Razomy.Unity.Scripts.Ocean.Querys;
using UnityEngine;

namespace Razomy.Unity.Scripts.Spectrum.Buffers
{
  public interface IDisplacementBuffer
  {
    bool IsGPU { get; }

    int Size { get; }

    InterpolatedArray2f[] GetReadDisplacements();

    void CopyAndCreateDisplacements(out IList<InterpolatedArray2f> displacements);

    void CopyDisplacements(IList<InterpolatedArray2f> des);

    Vector4 MaxRange(Vector4 choppyness, Vector2 gridScale);

    void QueryWaves(WaveQuery query, QueryGridScaling scaling);

    int EnabledBuffers();
  }
}