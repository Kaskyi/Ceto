using System;
using Ceto.Common.Unity.Utility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ceto
{
  /// <summary>
  ///   Class to perform fourier transform
  ///   on the GPU.
  /// </summary>
  public class FourierGPU
  {
    /// <summary>
    ///   These refer to the passes in the shader.
    /// </summary>
    private const int PASS_X_1 = 0, PASS_Y_1 = 1;

    private const int PASS_X_2 = 2, PASS_Y_2 = 3;
    private const int PASS_X_3 = 4, PASS_Y_3 = 5;
    private const int PASS_X_4 = 6, PASS_Y_4 = 7;

    private readonly Material m_fourier;
    private readonly float m_fsize;

    /// <summary>
    ///   Arrays to hold the buffers to blit into for MRT.
    /// </summary>
    private readonly RenderBuffer[] m_pass0RT2;

    private readonly RenderBuffer[] m_pass0RT3;
    private readonly RenderBuffer[] m_pass0RT4;

    /// <summary>
    ///   Arrays to hold the buffers to blit into for MRT.
    /// </summary>
    private readonly RenderBuffer[] m_pass1RT2;

    private readonly RenderBuffer[] m_pass1RT3;
    private readonly RenderBuffer[] m_pass1RT4;

    private Texture2D[] m_butterflyLookupTable;

    public FourierGPU(int size, Shader sdr)
    {
      if (!Mathf.IsPowerOfTwo(size))
        throw new ArgumentException("Fourier grid size must be pow2 number");

      m_fourier = new Material(sdr);

      this.size = size; //must be pow2 num
      m_fsize = this.size;
      passes = (int)(Mathf.Log(m_fsize) / Mathf.Log(2.0f));

      m_butterflyLookupTable = new Texture2D[passes];

      ComputeButterflyLookupTable();

      m_fourier.SetFloat("Ceto_FourierSize", m_fsize);

      m_pass0RT2 = new RenderBuffer[2];
      m_pass1RT2 = new RenderBuffer[2];

      m_pass0RT3 = new RenderBuffer[3];
      m_pass1RT3 = new RenderBuffer[3];

      m_pass0RT4 = new RenderBuffer[4];
      m_pass1RT4 = new RenderBuffer[4];
    }

    /// <summary>
    ///   The fourier size. Must be a pow2 number.
    /// </summary>
    public int size { get; }

    /// <summary>
    ///   Number of passes in the fourier transform.
    /// </summary>
    public int passes { get; }

    public void Release()
    {
      var count = m_butterflyLookupTable.Length;
      for (var i = 0; i < count; i++) Object.Destroy(m_butterflyLookupTable[i]);

      m_butterflyLookupTable = null;
    }

    private int BitReverse(int i)
    {
      var j = i;
      var Sum = 0;
      var W = 1;
      var M = size / 2;
      while (M != 0)
      {
        j = (i & M) > M - 1 ? 1 : 0;
        Sum += j * W;
        W *= 2;
        M /= 2;
      }

      return Sum;
    }

    private Texture2D Make1DTex(int i)
    {
      var tex = new Texture2D(size, 1, TextureFormat.RGBAFloat, false, true);
      tex.filterMode = FilterMode.Point;
      tex.wrapMode = TextureWrapMode.Clamp;
      tex.hideFlags = HideFlags.HideAndDontSave;
      tex.name = "Ceto Fouier GPU Butterfly Lookup";
      return tex;
    }

    private void ComputeButterflyLookupTable()
    {
      float S = size;
      float S1 = size - 1;

      for (var i = 0; i < passes; i++)
      {
        var nBlocks = (int)Mathf.Pow(2, passes - 1 - i);
        var nHInputs = (int)Mathf.Pow(2, i);

        m_butterflyLookupTable[i] = Make1DTex(i);

        for (var j = 0; j < nBlocks; j++)
        for (var k = 0; k < nHInputs; k++)
        {
          int i1, i2, j1, j2;
          if (i == 0)
          {
            i1 = j * nHInputs * 2 + k;
            i2 = j * nHInputs * 2 + nHInputs + k;
            j1 = BitReverse(i1);
            j2 = BitReverse(i2);
          }
          else
          {
            i1 = j * nHInputs * 2 + k;
            i2 = j * nHInputs * 2 + nHInputs + k;
            j1 = i1;
            j2 = i2;
          }

          var wr = Mathf.Cos(2.0f * Mathf.PI * (k * nBlocks) / S);
          var wi = Mathf.Sin(2.0f * Mathf.PI * (k * nBlocks) / S);

          m_butterflyLookupTable[i].SetPixel(i1, 0, new Color(j1 / S1, j2 / S1, wr, wi));

          m_butterflyLookupTable[i].SetPixel(i2, 0, new Color(j1 / S1, j2 / S1, -wr, -wi));
        }

        m_butterflyLookupTable[i].Apply();
      }
    }

    /// <summary>
    ///   Perform fourier transform on one textures.
    /// </summary>
    public int PeformFFT(RenderTexture[] data0)
    {
      if (m_butterflyLookupTable == null) return -1;

      var pass0 = data0[0];
      var pass1 = data0[1];

      int i;
      var idx = 0;
      int idx1;
      var j = 0;

      for (i = 0; i < passes; i++, j++)
      {
        idx = j % 2;
        idx1 = (j + 1) % 2;

        m_fourier.SetTexture("Ceto_ButterFlyLookUp", m_butterflyLookupTable[i]);

        m_fourier.SetTexture("Ceto_ReadBuffer0", data0[idx1]);

        if (idx == 0)
          Graphics.Blit(null, pass0, m_fourier, PASS_X_1);
        else
          Graphics.Blit(null, pass1, m_fourier, PASS_X_1);
      }

      for (i = 0; i < passes; i++, j++)
      {
        idx = j % 2;
        idx1 = (j + 1) % 2;

        m_fourier.SetTexture("Ceto_ButterFlyLookUp", m_butterflyLookupTable[i]);

        m_fourier.SetTexture("Ceto_ReadBuffer0", data0[idx1]);

        if (idx == 0)
          Graphics.Blit(null, pass0, m_fourier, PASS_Y_1);
        else
          Graphics.Blit(null, pass1, m_fourier, PASS_Y_1);
      }

      return idx;
    }

    /// <summary>
    ///   Perform fourier transform on two textures.
    /// </summary>
    public int PeformFFT(RenderTexture[] data0, RenderTexture[] data1)
    {
      if (m_butterflyLookupTable == null) return -1;

      if (SystemInfo.supportedRenderTargetCount < 2)
        throw new InvalidOperationException("System does not support at least 2 render targets");

      //RenderTexture[] pass0 = new RenderTexture[] { data0[0], data1[0] };
      //RenderTexture[] pass1 = new RenderTexture[] { data0[1], data1[1] };

      m_pass0RT2[0] = data0[0].colorBuffer;
      m_pass0RT2[1] = data1[0].colorBuffer;

      m_pass1RT2[0] = data0[1].colorBuffer;
      m_pass1RT2[1] = data1[1].colorBuffer;

      var depth0 = data0[0].depthBuffer;
      var depth1 = data0[1].depthBuffer;

      int i;
      var idx = 0;
      int idx1;
      var j = 0;

      for (i = 0; i < passes; i++, j++)
      {
        idx = j % 2;
        idx1 = (j + 1) % 2;

        m_fourier.SetTexture("Ceto_ButterFlyLookUp", m_butterflyLookupTable[i]);

        m_fourier.SetTexture("Ceto_ReadBuffer0", data0[idx1]);
        m_fourier.SetTexture("Ceto_ReadBuffer1", data1[idx1]);

        //if (idx == 0)
        //    RTUtility.MultiTargetBlit(pass0, m_fourier, PASS_X_2);
        //else
        //    RTUtility.MultiTargetBlit(pass1, m_fourier, PASS_X_2);

        if (idx == 0)
          RTUtility.MultiTargetBlit(m_pass0RT2, depth0, m_fourier, PASS_X_2);
        else
          RTUtility.MultiTargetBlit(m_pass1RT2, depth1, m_fourier, PASS_X_2);
      }

      for (i = 0; i < passes; i++, j++)
      {
        idx = j % 2;
        idx1 = (j + 1) % 2;

        m_fourier.SetTexture("Ceto_ButterFlyLookUp", m_butterflyLookupTable[i]);

        m_fourier.SetTexture("Ceto_ReadBuffer0", data0[idx1]);
        m_fourier.SetTexture("Ceto_ReadBuffer1", data1[idx1]);

        //if (idx == 0)
        //	RTUtility.MultiTargetBlit(pass0, m_fourier, PASS_Y_2);
        //else
        //	RTUtility.MultiTargetBlit(pass1, m_fourier, PASS_Y_2);

        if (idx == 0)
          RTUtility.MultiTargetBlit(m_pass0RT2, depth0, m_fourier, PASS_Y_2);
        else
          RTUtility.MultiTargetBlit(m_pass1RT2, depth1, m_fourier, PASS_Y_2);
      }

      return idx;
    }

    /// <summary>
    ///   Perform fourier transform on three textures.
    /// </summary>
    public int PeformFFT(RenderTexture[] data0, RenderTexture[] data1, RenderTexture[] data2)
    {
      if (m_butterflyLookupTable == null) return -1;

      if (SystemInfo.supportedRenderTargetCount < 3)
        throw new InvalidOperationException("System does not support at least 3 render targets");

      //RenderTexture[] pass0 = new RenderTexture[] { data0[0], data1[0], data2[0] };
      //RenderTexture[] pass1 = new RenderTexture[] { data0[1], data1[1], data2[1] };

      m_pass0RT3[0] = data0[0].colorBuffer;
      m_pass0RT3[1] = data1[0].colorBuffer;
      m_pass0RT3[2] = data2[0].colorBuffer;

      m_pass1RT3[0] = data0[1].colorBuffer;
      m_pass1RT3[1] = data1[1].colorBuffer;
      m_pass1RT3[2] = data2[1].colorBuffer;

      var depth0 = data0[0].depthBuffer;
      var depth1 = data0[1].depthBuffer;

      int i;
      var idx = 0;
      int idx1;
      var j = 0;

      for (i = 0; i < passes; i++, j++)
      {
        idx = j % 2;
        idx1 = (j + 1) % 2;

        m_fourier.SetTexture("Ceto_ButterFlyLookUp", m_butterflyLookupTable[i]);

        m_fourier.SetTexture("Ceto_ReadBuffer0", data0[idx1]);
        m_fourier.SetTexture("Ceto_ReadBuffer1", data1[idx1]);
        m_fourier.SetTexture("Ceto_ReadBuffer2", data2[idx1]);

        //if (idx == 0)
        //	RTUtility.MultiTargetBlit(pass0, m_fourier, PASS_X_3);
        //else
        //	RTUtility.MultiTargetBlit(pass1, m_fourier, PASS_X_3);

        if (idx == 0)
          RTUtility.MultiTargetBlit(m_pass0RT3, depth0, m_fourier, PASS_X_3);
        else
          RTUtility.MultiTargetBlit(m_pass1RT3, depth1, m_fourier, PASS_X_3);
      }

      for (i = 0; i < passes; i++, j++)
      {
        idx = j % 2;
        idx1 = (j + 1) % 2;

        m_fourier.SetTexture("Ceto_ButterFlyLookUp", m_butterflyLookupTable[i]);

        m_fourier.SetTexture("Ceto_ReadBuffer0", data0[idx1]);
        m_fourier.SetTexture("Ceto_ReadBuffer1", data1[idx1]);
        m_fourier.SetTexture("Ceto_ReadBuffer2", data2[idx1]);

        //if (idx == 0)
        //	RTUtility.MultiTargetBlit(pass0, m_fourier, PASS_Y_3);
        //else
        //	RTUtility.MultiTargetBlit(pass1, m_fourier, PASS_Y_3);

        if (idx == 0)
          RTUtility.MultiTargetBlit(m_pass0RT3, depth0, m_fourier, PASS_Y_3);
        else
          RTUtility.MultiTargetBlit(m_pass1RT3, depth1, m_fourier, PASS_Y_3);
      }

      return idx;
    }

    /// <summary>
    ///   Perform fourier transform on four textures.
    /// </summary>
    public int PeformFFT(RenderTexture[] data0, RenderTexture[] data1, RenderTexture[] data2, RenderTexture[] data3)
    {
      if (m_butterflyLookupTable == null) return -1;

      if (SystemInfo.supportedRenderTargetCount < 4)
        throw new InvalidOperationException("System does not support at least 4 render targets");

      //RenderTexture[] pass0 = new RenderTexture[] { data0[0], data1[0], data2[0], data3[0] };
      //RenderTexture[] pass1 = new RenderTexture[] { data0[1], data1[1], data2[1], data3[1] };

      m_pass0RT4[0] = data0[0].colorBuffer;
      m_pass0RT4[1] = data1[0].colorBuffer;
      m_pass0RT4[2] = data2[0].colorBuffer;
      m_pass0RT4[3] = data3[0].colorBuffer;

      m_pass1RT4[0] = data0[1].colorBuffer;
      m_pass1RT4[1] = data1[1].colorBuffer;
      m_pass1RT4[2] = data2[1].colorBuffer;
      m_pass1RT4[3] = data3[1].colorBuffer;

      var depth0 = data0[0].depthBuffer;
      var depth1 = data0[1].depthBuffer;

      int i;
      var idx = 0;
      int idx1;
      var j = 0;

      for (i = 0; i < passes; i++, j++)
      {
        idx = j % 2;
        idx1 = (j + 1) % 2;

        m_fourier.SetTexture("Ceto_ButterFlyLookUp", m_butterflyLookupTable[i]);

        m_fourier.SetTexture("Ceto_ReadBuffer0", data0[idx1]);
        m_fourier.SetTexture("Ceto_ReadBuffer1", data1[idx1]);
        m_fourier.SetTexture("Ceto_ReadBuffer2", data2[idx1]);
        m_fourier.SetTexture("Ceto_ReadBuffer3", data3[idx1]);

        //if (idx == 0)
        //	RTUtility.MultiTargetBlit(pass0, m_fourier, PASS_X_4);
        //else
        //	RTUtility.MultiTargetBlit(pass1, m_fourier, PASS_X_4);

        if (idx == 0)
          RTUtility.MultiTargetBlit(m_pass0RT4, depth0, m_fourier, PASS_X_4);
        else
          RTUtility.MultiTargetBlit(m_pass1RT4, depth1, m_fourier, PASS_X_4);
      }

      for (i = 0; i < passes; i++, j++)
      {
        idx = j % 2;
        idx1 = (j + 1) % 2;

        m_fourier.SetTexture("Ceto_ButterFlyLookUp", m_butterflyLookupTable[i]);

        m_fourier.SetTexture("Ceto_ReadBuffer0", data0[idx1]);
        m_fourier.SetTexture("Ceto_ReadBuffer1", data1[idx1]);
        m_fourier.SetTexture("Ceto_ReadBuffer2", data2[idx1]);
        m_fourier.SetTexture("Ceto_ReadBuffer3", data3[idx1]);

        //if (idx == 0)
        //	RTUtility.MultiTargetBlit(pass0, m_fourier, PASS_Y_4);
        //else
        //	RTUtility.MultiTargetBlit(pass1, m_fourier, PASS_Y_4);

        if (idx == 0)
          RTUtility.MultiTargetBlit(m_pass0RT4, depth0, m_fourier, PASS_Y_4);
        else
          RTUtility.MultiTargetBlit(m_pass1RT4, depth1, m_fourier, PASS_Y_4);
      }

      return idx;
    }
  }
}