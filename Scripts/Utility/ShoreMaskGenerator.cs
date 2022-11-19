using Razomy.Unity.Scripts.Common.Interpolation;
using UnityEngine;

namespace Razomy.Unity.Scripts.Utility
{
  public static class ShoreMaskGenerator
  {
    public static float[] CreateHeightMap(Terrain terrain)
    {
      var data = terrain.terrainData;

      var resolution = data.heightmapResolution;

      var scale = data.heightmapScale;

      var heights = data.GetHeights(0, 0, resolution, resolution);

      var map = new float[resolution * resolution];

      for (var y = 0; y < resolution; y++)
      for (var x = 0; x < resolution; x++)
        map[x + y * resolution] = heights[y, x] * scale.y + terrain.transform.position.y;

      return map;
    }


    public static Texture2D CreateMask(float[] heightMap, int size, float shoreLevel, float spread,
      TextureFormat format)
    {
      var mask = new Texture2D(size, size, format, false, true);
      mask.filterMode = FilterMode.Bilinear;

      var s2 = size * size;

      var colors = new Color[s2];

      for (var i = 0; i < s2; i++)
      {
        var h = Mathf.Clamp(shoreLevel - heightMap[i], 0.0f, spread);

        h = 1.0f - h / spread;

        colors[i].r = h;
        colors[i].g = h;
        colors[i].b = h;
        colors[i].a = h;
      }

      mask.SetPixels(colors);

      mask.Apply();

      return mask;
    }

    public static Texture2D CreateMask(InterpolatedArray2f heightMap, int width, int height, float shoreLevel,
      float spread, TextureFormat format)
    {
      var mask = new Texture2D(width, height, format, false, true);
      mask.filterMode = FilterMode.Bilinear;

      var colors = new Color[width * height];

      var matches = width == heightMap.SX && height == heightMap.SY;

      for (var y = 0; y < height; y++)
      for (var x = 0; x < width; x++)
      {
        var i = x + y * height;

        var h = 0.0f;

        if (matches)
        {
          h = Mathf.Clamp(shoreLevel - heightMap.Data[i], 0.0f, spread);
        }
        else
        {
          var fx = x / (width - 1.0f);
          var fy = y / (height - 1.0f);
          h = Mathf.Clamp(shoreLevel - heightMap.Get(fx, fy, 0), 0.0f, spread);
        }

        h = 1.0f - h / spread;

        colors[i].r = h;
        colors[i].g = h;
        colors[i].b = h;
        colors[i].a = h;
      }

      mask.SetPixels(colors);

      mask.Apply();

      return mask;
    }

    public static Texture2D CreateClipMask(float[] heightMap, int size, float shoreLevel, TextureFormat format)
    {
      var mask = new Texture2D(size, size, format, false, true);
      mask.filterMode = FilterMode.Bilinear;

      var s2 = size * size;

      var colors = new Color[s2];

      for (var i = 0; i < s2; i++)
      {
        var h = Mathf.Clamp(heightMap[i] - shoreLevel, 0.0f, 1.0f);

        if (h > 0.0f) h = 1.0f;

        colors[i].r = h;
        colors[i].g = h;
        colors[i].b = h;
        colors[i].a = h;
      }

      mask.SetPixels(colors);

      mask.Apply();

      return mask;
    }

    public static Texture2D CreateClipMask(InterpolatedArray2f heightMap, int width, int height, float shoreLevel,
      TextureFormat format)
    {
      var mask = new Texture2D(width, height, format, false, true);
      mask.filterMode = FilterMode.Bilinear;

      var colors = new Color[width * height];

      var matches = width == heightMap.SX && height == heightMap.SY;

      for (var y = 0; y < height; y++)
      for (var x = 0; x < width; x++)
      {
        var i = x + y * height;

        var h = 0.0f;

        if (matches)
        {
          h = Mathf.Clamp(heightMap.Data[i] - shoreLevel, 0.0f, 1.0f);
        }
        else
        {
          var fx = x / (width - 1.0f);
          var fy = y / (height - 1.0f);
          h = Mathf.Clamp(heightMap.Get(fx, fy, 0) - shoreLevel, 0.0f, 1.0f);
        }

        if (h > 0.0f) h = 1.0f;

        colors[i].r = h;
        colors[i].g = h;
        colors[i].b = h;
        colors[i].a = h;
      }

      mask.SetPixels(colors);

      mask.Apply();

      return mask;
    }
  }
}