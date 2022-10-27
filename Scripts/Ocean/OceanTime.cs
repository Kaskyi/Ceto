using UnityEngine;

namespace Ceto
{
  /// <summary>
  ///   The default implementation.
  ///   Just uses Unitys time.
  /// </summary>
  public class OceanTime : IOceanTime
  {
    /// <summary>
    ///   The current time in seconds.
    /// </summary>
    public float Now => Time.time;
  }
}