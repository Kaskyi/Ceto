namespace Ceto
{
  public class PhillipsSpectrumConditionKey : WaveSpectrumConditionKey
  {
    public PhillipsSpectrumConditionKey(float windSpeed, int size, float windDir, SPECTRUM_TYPE spectrumType,
      int numGrids)
      : base(size, windDir, spectrumType, numGrids)
    {
      WindSpeed = windSpeed;
    }


    public float WindSpeed { get; }

    protected override bool Matches(WaveSpectrumConditionKey k)
    {
      var key = k as PhillipsSpectrumConditionKey;

      if (key == null) return false;
      if (WindSpeed != key.WindSpeed) return false;

      return true;
    }

    protected override int AddToHashCode(int hashcode)
    {
      hashcode = hashcode * 37 + WindSpeed.GetHashCode();

      return hashcode;
    }

    public override string ToString()
    {
      return string.Format("[PhillipsSpectrumConditionKey WindSpeed={0}, Size={1}, WindDir={2}, Type={3}, NumGrids={4}",
        WindSpeed, Size, WindDir, SpectrumType, NumGrids);
    }
  }
}