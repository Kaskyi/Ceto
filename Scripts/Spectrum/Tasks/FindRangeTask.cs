using System.Collections;
using System.Collections.Generic;
using Razomy.Unity.Scripts.Common.Interpolation;
using Razomy.Unity.Scripts.Common.Threading.Tasks;
using Razomy.Unity.Scripts.Spectrum.Buffers;
using UnityEngine;

namespace Razomy.Unity.Scripts.Spectrum.Tasks
{
  public class FindRangeTask : ThreadedTask
  {
    private readonly IList<InterpolatedArray2f> m_displacements;

    private readonly WaveSpectrum m_spectrum;
    private Vector4 m_choppyness;

    private Vector2 m_gridScale;

    private Vector4 m_max;

    public FindRangeTask(WaveSpectrum spectrum) : base(true)
    {
      m_spectrum = spectrum;
      m_choppyness = spectrum.Choppyness;
      m_gridScale = new Vector2(spectrum.gridScale, spectrum.gridScale);

      var buffer = spectrum.DisplacementBuffer;
      buffer.CopyAndCreateDisplacements(out m_displacements);
    }

    public override void Reset()
    {
      base.Reset();

      m_choppyness = m_spectrum.Choppyness;
      m_gridScale = new Vector2(m_spectrum.gridScale, m_spectrum.gridScale);

      var buffer = m_spectrum.DisplacementBuffer;
      buffer.CopyDisplacements(m_displacements);
    }

    public override IEnumerator Run()
    {
      m_max = QueryDisplacements.MaxRange(m_displacements, m_choppyness, m_gridScale, this);

      FinishedRunning();
      return null;
    }

    public override void End()
    {
      m_spectrum.MaxDisplacement = new Vector2(Mathf.Max(m_max.x, m_max.z), m_max.y);

      base.End();
    }
  }
}