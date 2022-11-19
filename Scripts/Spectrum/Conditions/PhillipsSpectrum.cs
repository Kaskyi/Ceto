using Razomy.Unity.Scripts.Spectrum.Tasks;
using UnityEngine;

namespace Razomy.Unity.Scripts.Spectrum.Conditions
{
  /// <summary>
  /// </summary>
  public class PhillipsSpectrum : ISpectrum
  {
    private readonly float AMP = 0.02f;

    private readonly float GRAVITY = SpectrumTask.GRAVITY;

    private readonly float length2, dampedLength2;

    private readonly Vector2 WindDir;

    private readonly float WindSpeed;

    public PhillipsSpectrum(float windSpeed, float windDir)
    {
      WindSpeed = windSpeed;

      var theta = windDir * Mathf.PI / 180.0f;
      WindDir = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));

      var L = WindSpeed * WindSpeed / GRAVITY;
      length2 = L * L;

      var damping = 0.001f;
      dampedLength2 = length2 * damping * damping;
    }

    public float Spectrum(float kx, float kz)
    {
      var u = kx * WindDir.x - kz * WindDir.y;
      var v = kx * WindDir.y + kz * WindDir.x;

      kx = u;
      kz = v;

      var k_length = Mathf.Sqrt(kx * kx + kz * kz);
      if (k_length < 0.000001f) return 0.0f;

      var k_length2 = k_length * k_length;
      var k_length4 = k_length2 * k_length2;

      kx /= k_length;
      kz /= k_length;

      var k_dot_w = kx * 1.0f + kz * 0.0f;
      var k_dot_w2 = k_dot_w * k_dot_w * k_dot_w * k_dot_w * k_dot_w * k_dot_w;

      return AMP * Mathf.Exp(-1.0f / (k_length2 * length2)) / k_length4 * k_dot_w2 *
             Mathf.Exp(-k_length2 * dampedLength2);
    }
  }
}