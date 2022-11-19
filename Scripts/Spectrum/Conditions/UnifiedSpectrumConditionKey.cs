using Razomy.Unity.Scripts.Ocean;

namespace Razomy.Unity.Scripts.Spectrum.Conditions
{
  public class UnifiedSpectrumConditionKey : WaveSpectrumConditionKey
  {
    public UnifiedSpectrumConditionKey(float windSpeed, float waveAge, int size, float windDir,
      SPECTRUM_TYPE spectrumType, int numGrids)
      : base(size, windDir, spectrumType, numGrids)
    {
      WindSpeed = windSpeed;
      WaveAge = waveAge;
    }


    public float WindSpeed { get; }

    public float WaveAge { get; }

    protected override bool Matches(WaveSpectrumConditionKey k)
    {
      var key = k as UnifiedSpectrumConditionKey;

      if (key == null) return false;
      if (WindSpeed != key.WindSpeed) return false;
      if (WaveAge != key.WaveAge) return false;

      return true;
    }

    protected override int AddToHashCode(int hashcode)
    {
      hashcode = hashcode * 37 + WindSpeed.GetHashCode();
      hashcode = hashcode * 37 + WaveAge.GetHashCode();

      return hashcode;
    }

    public override string ToString()
    {
      return string.Format(
        "[UnifiedSpectrumConditionKey WindSpeed={0}, WaveAge={1}, Size={2}, WindDir={3}, Type={4}, NumGrids={5}",
        WindSpeed, WaveAge, Size, WindDir, SpectrumType, NumGrids);
    }
  }
}