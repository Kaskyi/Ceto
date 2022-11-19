using UnityEngine;

namespace Razomy.Unity.Scripts.Utility
{
  public class ImageBlur
  {
    public enum BLUR_MODE
    {
      OFF = 0,
      NO_DOWNSAMPLE = 1,
      DOWNSAMPLE_2 = 2,
      DOWNSAMPLE_4 = 4
    }

    private readonly Vector2[] m_offsets = new Vector2[4];

    public Material m_blurMaterial;

    public ImageBlur(Shader blurShader)
    {
      BlurIterations = 1;
      BlurSpread = 0.6f;
      BlurMode = BLUR_MODE.DOWNSAMPLE_2;

      if (blurShader != null)
        m_blurMaterial = new Material(blurShader);
    }

    public BLUR_MODE BlurMode { get; set; }

    /// Blur iterations - larger number means more blur.
    public int BlurIterations { get; set; }

    /// Blur spread for each iteration. Lower values
    /// give better looking blur, but require more iterations to
    /// get large blurs. Value is usually between 0.5 and 1.0.
    public float BlurSpread { get; set; }

    public void Blur(RenderTexture source)
    {
      var blurDownSample = (int)BlurMode;

      if (BlurIterations > 0 && m_blurMaterial != null && blurDownSample > 0)
      {
        var rtW = source.width / blurDownSample;
        var rtH = source.height / blurDownSample;

        var buffer = RenderTexture.GetTemporary(rtW, rtH, 0, source.format, RenderTextureReadWrite.Default);

        // Copy source to the smaller texture.
        DownSample(source, buffer);

        // Blur the small texture
        for (var i = 0; i < BlurIterations; i++)
        {
          var buffer2 = RenderTexture.GetTemporary(rtW, rtH, 0, source.format, RenderTextureReadWrite.Default);
          FourTapCone(buffer, buffer2, i);
          RenderTexture.ReleaseTemporary(buffer);
          buffer = buffer2;
        }

        Graphics.Blit(buffer, source);
        RenderTexture.ReleaseTemporary(buffer);
      }
    }

    // Performs one blur iteration.
    private void FourTapCone(RenderTexture source, RenderTexture dest, int iteration)
    {
      var off = 0.5f + iteration * BlurSpread;

      m_offsets[0].x = -off;
      m_offsets[0].y = -off;

      m_offsets[1].x = -off;
      m_offsets[1].y = off;

      m_offsets[2].x = off;
      m_offsets[2].y = off;

      m_offsets[3].x = off;
      m_offsets[3].y = -off;

      Graphics.BlitMultiTap(source, dest, m_blurMaterial, m_offsets);
    }

    // Downsamples the texture to a quarter resolution.
    private void DownSample(RenderTexture source, RenderTexture dest)
    {
      var off = 1.0f;

      m_offsets[0].x = -off;
      m_offsets[0].y = -off;

      m_offsets[1].x = -off;
      m_offsets[1].y = off;

      m_offsets[2].x = off;
      m_offsets[2].y = off;

      m_offsets[3].x = off;
      m_offsets[3].y = -off;

      Graphics.BlitMultiTap(source, dest, m_blurMaterial, m_offsets);
    }
  }
}