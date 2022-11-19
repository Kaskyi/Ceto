using Razomy.Unity.Scripts.Ocean;
using Razomy.Unity.Scripts.Spectrum.Conditions;
using UnityEngine;

namespace Razomy.Unity.Scripts.Spectrum
{
  //See the CustomWaveSpectrumExample script for a example of how to implement this.
  public interface ICustomWaveSpectrum
  {
    bool MultiThreadTask { get; }

    WaveSpectrumConditionKey CreateKey(int size, float windDir, SPECTRUM_TYPE spectrumType, int numGrids);

    ISpectrum CreateSpectrum(WaveSpectrumConditionKey key);

    Vector4 GetGridSizes(int numGrids);

    Vector4 GetChoppyness(int numGrids);

    Vector4 GetWaveAmps(int numGrids);
  }
}