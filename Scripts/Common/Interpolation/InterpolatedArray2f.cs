using System;

namespace Ceto.Common.Containers.Interpolation
{
  /// <summary>
  ///   A Interpolated 2 dimensional array.
  ///   The array can be sampled using a float and the sampling
  ///   will be performed using bilinear filtering.
  /// </summary>
  public class InterpolatedArray2f : InterpolatedArray
  {
    public InterpolatedArray2f(int sx, int sy, int c, bool wrap) : base(wrap)
    {
      SX = sx;
      SY = sy;
      Channels = c;

      Data = new float[SX * SY * Channels];
    }

    public InterpolatedArray2f(float[] data, int sx, int sy, int c, bool wrap) : base(wrap)
    {
      SX = sx;
      SY = sy;
      Channels = c;

      Data = new float[SX * SY * Channels];

      Copy(data);
    }

    public InterpolatedArray2f(float[,,] data, bool wrap) : base(wrap)
    {
      SX = data.GetLength(0);
      SY = data.GetLength(1);
      Channels = data.GetLength(2);

      Data = new float[SX * SY * Channels];

      Copy(data);
    }

    /// <summary>
    ///   Gets the data.
    /// </summary>
    public float[] Data { get; }

    /// <summary>
    ///   Size on the x dimension.
    /// </summary>
    public int SX { get; }

    /// <summary>
    ///   Size on the y dimension.
    /// </summary>
    public int SY { get; }

    /// <summary>
    ///   Number of channels.
    /// </summary>
    public int Channels { get; }

    /// <summary>
    ///   Get a value from the data array using normal indexing.
    /// </summary>
    public float this[int x, int y, int c]
    {
      get => Data[(x + y * SX) * Channels + c];
      set => Data[(x + y * SX) * Channels + c] = value;
    }

    /// <summary>
    ///   Clear the data in array to 0.
    /// </summary>
    public void Clear()
    {
      Array.Clear(Data, 0, Data.Length);
    }

    /// <summary>
    ///   Copy the specified data.
    /// </summary>
    public void Copy(Array data)
    {
      Array.Copy(data, Data, Data.Length);
    }

    /// <summary>
    ///   Get a channel from array.
    /// </summary>
    public float Get(int x, int y, int c)
    {
      return Data[(x + y * SX) * Channels + c];
    }

    /// <summary>
    ///   Set a channel from array.
    /// </summary>
    public void Set(int x, int y, int c, float v)
    {
      Data[(x + y * SX) * Channels + c] = v;
    }

    /// <summary>
    ///   Set all channels from array
    /// </summary>
    public void Set(int x, int y, float[] v)
    {
      for (var c = 0; c < Channels; c++)
        Data[(x + y * SX) * Channels + c] = v[c];
    }

    /// <summary>
    ///   Get all channels into array
    /// </summary>
    public void Get(int x, int y, float[] v)
    {
      for (var c = 0; c < Channels; c++)
        v[c] = Data[(x + y * SX) * Channels + c];
    }

    /// <summary>
    ///   Get a value from the data array using bilinear filtering.
    /// </summary>
    public void Get(float x, float y, float[] v)
    {
      //un-normalize cords
      if (HalfPixelOffset)
      {
        x *= SX;
        y *= SY;

        x -= 0.5f;
        y -= 0.5f;
      }
      else
      {
        x *= SX - 1;
        y *= SY - 1;
      }

      int x0, x1;
      var fx = Math.Abs(x - (int)x);
      Index(x, SX, out x0, out x1);

      int y0, y1;
      var fy = Math.Abs(y - (int)y);
      Index(y, SY, out y0, out y1);

      for (var c = 0; c < Channels; c++)
      {
        var v0 = Data[(x0 + y0 * SX) * Channels + c] * (1.0f - fx) + Data[(x1 + y0 * SX) * Channels + c] * fx;
        var v1 = Data[(x0 + y1 * SX) * Channels + c] * (1.0f - fx) + Data[(x1 + y1 * SX) * Channels + c] * fx;

        v[c] = v0 * (1.0f - fy) + v1 * fy;
      }
    }

    /// <summary>
    ///   Get a value from the data array using bilinear filtering.
    /// </summary>
    public float Get(float x, float y, int c)
    {
      //un-normalize cords
      if (HalfPixelOffset)
      {
        x *= SX;
        y *= SY;

        x -= 0.5f;
        y -= 0.5f;
      }
      else
      {
        x *= SX - 1;
        y *= SY - 1;
      }

      int x0, x1;
      var fx = Math.Abs(x - (int)x);
      Index(x, SX, out x0, out x1);

      int y0, y1;
      var fy = Math.Abs(y - (int)y);
      Index(y, SY, out y0, out y1);

      var v0 = Data[(x0 + y0 * SX) * Channels + c] * (1.0f - fx) + Data[(x1 + y0 * SX) * Channels + c] * fx;
      var v1 = Data[(x0 + y1 * SX) * Channels + c] * (1.0f - fx) + Data[(x1 + y1 * SX) * Channels + c] * fx;

      return v0 * (1.0f - fy) + v1 * fy;
    }
  }
}