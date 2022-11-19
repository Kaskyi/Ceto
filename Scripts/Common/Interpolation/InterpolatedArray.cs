using System;

namespace Razomy.Unity.Scripts.Common.Interpolation
{
  /// <summary>
  ///   Abstract class providing common functions for Interpolated array.
  ///   A filtered array allows the bilinear filtering of its contained data.
  /// </summary>
  public abstract class InterpolatedArray
  {
    public InterpolatedArray(bool wrap)
    {
      Wrap = wrap;
      HalfPixelOffset = true;
    }

    /// <summary>
    ///   Should the sampling of the array be wrapped or clamped.
    /// </summary>
    public bool Wrap { get; set; }

    /// <summary>
    ///   Should the interpolation be done with a
    ///   half pixel offset.
    /// </summary>
    public bool HalfPixelOffset { get; set; }

    /// <summary>
    ///   Get the index that needs to be sampled for point filtering.
    /// </summary>
    public void Index(ref int x, int sx)
    {
      if (Wrap)
      {
        if (x >= sx || x <= -sx) x = x % sx;
        if (x < 0) x = sx - -x;
      }
      else
      {
        if (x < 0) x = 0;
        else if (x >= sx) x = sx - 1;
      }
    }

    /// <summary>
    ///   Get the two indices that need to be sampled for bilinear filtering.
    /// </summary>
    public void Index(double x, int sx, out int ix0, out int ix1)
    {
      ix0 = (int)x;
      ix1 = (int)x + Math.Sign(x);

      if (Wrap)
      {
        if (ix0 >= sx || ix0 <= -sx) ix0 = ix0 % sx;
        if (ix0 < 0) ix0 = sx - -ix0;

        if (ix1 >= sx || ix1 <= -sx) ix1 = ix1 % sx;
        if (ix1 < 0) ix1 = sx - -ix1;
      }
      else
      {
        if (ix0 < 0) ix0 = 0;
        else if (ix0 >= sx) ix0 = sx - 1;

        if (ix1 < 0) ix1 = 0;
        else if (ix1 >= sx) ix1 = sx - 1;
      }
    }
  }
}