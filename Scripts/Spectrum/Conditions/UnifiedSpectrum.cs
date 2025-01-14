﻿using System;
using Razomy.Unity.Scripts.Spectrum.Tasks;
using UnityEngine;

namespace Razomy.Unity.Scripts.Spectrum.Conditions
{
  /// <summary>
  ///   Generates a wave spectrum using the formula in the follow research paper.
  ///   WAVES SPECTRUM
  ///   using "A unified directional spectrum for long and short wind-driven waves"
  ///   T. Elfouhaily, B. Chapron, K. Katsaros, D. Vandemark
  ///   Journal of Geophysical Research vol 102, p781-796, 1997
  /// </summary>
  public class UnifiedSpectrum : ISpectrum
  {
    private readonly float ALPHA_P;
    private readonly float G_SQ_OMEGA_U10;

    private readonly float GRAVITY = SpectrumTask.GRAVITY;
    private readonly float kp, cp, z0, u_star, gamma, HALF_ALPHA_P_CP, alpham, HALF_ALPHAM_WAVE_CM, am;
    private readonly float LOG_2_4;
    private readonly float LOG_OMEGA_6;
    private readonly float PI_2;
    private readonly float SIGMA;
    private readonly float SQ_SIGMA_2;
    private readonly float SQRT_10;

    private readonly float U10;
    private readonly float WAVE_CM = SpectrumTask.WAVE_CM;
    private readonly float WAVE_KM = SpectrumTask.WAVE_KM;

    private readonly Vector2 WindDir;

    private readonly float WindSpeed, WaveAge;
    private readonly float Z_SQ_U10_G;

    public UnifiedSpectrum(float windSpeed, float windDir, float waveAge)
    {
      WindSpeed = windSpeed;
      WaveAge = waveAge;

      var theta = windDir * Mathf.PI / 180.0f;
      WindDir = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));

      //TODO - clean this mess up.

      U10 = WindSpeed;

      PI_2 = Mathf.PI * 2.0f;

      SQRT_10 = Mathf.Sqrt(10);

      G_SQ_OMEGA_U10 = GRAVITY * sqr(WaveAge / U10);

      Z_SQ_U10_G = 3.7e-5f * sqr(U10) / 9.81f;

      LOG_OMEGA_6 = Mathf.Log(WaveAge) * 6f;

      SIGMA = 0.08f * (1.0f + 4.0f / Mathf.Pow(WaveAge, 3.0f));

      SQ_SIGMA_2 = sqr(SIGMA) * 2.0f;

      ALPHA_P = 0.006f * Mathf.Sqrt(WaveAge); // Eq 34

      LOG_2_4 = Mathf.Log(2.0f) / 4.0f;

      kp = G_SQ_OMEGA_U10; // after Eq 3
      cp = omega(kp) / kp;

      // friction velocity
      z0 = Z_SQ_U10_G * Mathf.Pow(U10 / cp, 0.9f); // Eq 66
      u_star = 0.41f * U10 / Mathf.Log(10.0f / z0); // Eq 60

      gamma = WaveAge < 1.0f ? 1.7f : 1.7f + LOG_OMEGA_6; // after Eq 3 // log10 or log?

      HALF_ALPHA_P_CP = 0.5f * ALPHA_P * cp;

      alpham = 0.01f * (u_star < WAVE_CM
        ? 1.0f + Mathf.Log(u_star / WAVE_CM)
        : 1.0f + 3.0f * Mathf.Log(u_star / WAVE_CM)); // Eq 44

      HALF_ALPHAM_WAVE_CM = 0.5f * alpham * WAVE_CM;

      am = 0.13f * u_star / WAVE_CM; // Eq 59
    }

    public float Spectrum(float kx, float ky)
    {
      var u = kx * WindDir.x - ky * WindDir.y;
      var v = kx * WindDir.y + ky * WindDir.x;

      kx = u;
      ky = v;

      // phase speed
      var k = Mathf.Sqrt(kx * kx + ky * ky);
      var c = omega(k) / k;

      // spectral peak
      //float kp = G_SQ_OMEGA_U10; // after Eq 3
      //float cp = omega(kp) / kp;

      // friction velocity
      //float z0 = Z_SQ_U10_G * Mathf.Pow(U10 / cp, 0.9f); // Eq 66
      //float u_star = 0.41f * U10 / Mathf.Log(10.0f / z0); // Eq 60

      var Lpm = Mathf.Exp(-5.0f / 4.0f * sqr(kp / k)); // after Eq 3
      //float gamma = (m_omega < 1.0f) ? 1.7f : 1.7f + LOG_OMEGA_6; // after Eq 3 // log10 or log?
      var Gamma = Mathf.Exp(-1.0f / SQ_SIGMA_2 * sqr(Mathf.Sqrt(k / kp) - 1.0f));
      var Jp = Mathf.Pow(gamma, Gamma); // Eq 3
      var Fp = Lpm * Jp * Mathf.Exp(-WaveAge / SQRT_10 * (Mathf.Sqrt(k / kp) - 1.0f)); // Eq 32
      //float alphap = ALPHA_P; // Eq 34
      var Bl = HALF_ALPHA_P_CP / c * Fp; // Eq 31

      //float alpham = 0.01f * (u_star < WAVE_CM ? 1.0f + Mathf.Log(u_star / WAVE_CM) : 1.0f + 3.0f * Mathf.Log(u_star / WAVE_CM)); // Eq 44
      var Fm = Mathf.Exp(-0.25f * sqr(k / WAVE_KM - 1.0f)); // Eq 41
      var Bh = HALF_ALPHAM_WAVE_CM / c * Fm * Lpm; // Eq 40 (fixed)

      //float a0 = LOG_2_4;
      //float ap = 4.0f;
      //float am = 0.13f * u_star / WAVE_CM; // Eq 59
      var Delta = (float)Math.Tanh(LOG_2_4 + 4.0f * Mathf.Pow(c / cp, 2.5f) +
                                   am * Mathf.Pow(WAVE_CM / c, 2.5f)); // Eq 57

      var phi = Mathf.Atan2(ky, kx);

      if (kx < 0.0f) return 0.0f;

      Bl *= 2.0f;
      Bh *= 2.0f;

      // remove waves perpendicular to wind dir
      var tweak = Mathf.Sqrt(Mathf.Max(kx / k, 0.0f));
      //tweak = 1.0;

      return (Bl + Bh) * (1.0f + Delta * Mathf.Cos(2.0f * phi)) / (PI_2 * k * k * k * k) * tweak; // Eq 677
    }

    private float sqr(float x)
    {
      return x * x;
    }

    private float omega(float k)
    {
      return Mathf.Sqrt(GRAVITY * k * (1.0f + sqr(k / WAVE_KM)));
    } // Eq 24
  }
}