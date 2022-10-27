using System;

namespace Ceto
{
  public abstract class WaveSpectrumConditionKey : IEquatable<WaveSpectrumConditionKey>
  {
    public WaveSpectrumConditionKey(int size, float windDir, SPECTRUM_TYPE spectrumType, int numGrids)
    {
      Size = size;
      NumGrids = numGrids;
      WindDir = windDir;
      SpectrumType = spectrumType;
    }

    public int Size { get; }

    public int NumGrids { get; }

    public float WindDir { get; }

    public SPECTRUM_TYPE SpectrumType { get; }

    /// <summary>
    ///   Is the key equal to another key.
    /// </summary>
    public bool Equals(WaveSpectrumConditionKey k)
    {
      return k == this;
    }

    /// <summary>
    ///   Allows the parent class to determine if these keys are equal.
    /// </summary>
    protected abstract bool Matches(WaveSpectrumConditionKey k);

    /// <summary>
    ///   Allows the parent class to add to the hash code.
    /// </summary>
    protected abstract int AddToHashCode(int hashcode);

    /// <summary>
    ///   Are these keys equal.
    /// </summary>
    public static bool operator ==(WaveSpectrumConditionKey k1, WaveSpectrumConditionKey k2)
    {
      // If both are null, or both are same instance, return true.
      if (ReferenceEquals(k1, k2)) return true;

      // If one is null, but not both, return false.
      if ((object)k1 == null || (object)k2 == null) return false;

      if (k1.Size != k2.Size) return false;
      if (k1.NumGrids != k2.NumGrids) return false;
      if (k1.WindDir != k2.WindDir) return false;
      if (k1.SpectrumType != k2.SpectrumType) return false;

      return k1.Matches(k2);
    }

    /// <summary>
    ///   Are these keys not equal.
    /// </summary>
    public static bool operator !=(WaveSpectrumConditionKey k1, WaveSpectrumConditionKey k2)
    {
      return !(k1 == k2);
    }

    /// <summary>
    ///   Is the key equal to another key.
    /// </summary>
    public override bool Equals(object o)
    {
      var k = o as WaveSpectrumConditionKey;

      if (k == null) return false;

      return k == this;
    }

    /// <summary>
    ///   The keys hash code.
    /// </summary>
    public override int GetHashCode()
    {
      var hashcode = 23;

      hashcode = hashcode * 37 + Size.GetHashCode();
      hashcode = hashcode * 37 + NumGrids.GetHashCode();
      hashcode = hashcode * 37 + WindDir.GetHashCode();
      hashcode = hashcode * 37 + SpectrumType.GetHashCode();

      return AddToHashCode(hashcode);
    }
  }
}