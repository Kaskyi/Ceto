﻿using System;
using System.Collections.Generic;
using Ceto.Common.Unity.Utility;
using UnityEngine;

namespace Ceto
{
  /// <summary>
  ///   A buffer that uses FFT on the GPU to transform
  ///   the spectrum. The type of data produced depends on the
  ///   initialization material used.
  /// </summary>
  public class WaveSpectrumBufferGPU : WaveSpectrumBuffer
  {
    /// <summary>
    ///   A name for the buffers to help when profiling
    /// </summary>
    private readonly string m_bufferName;

    /// <summary>
    ///   The buffers generated by this object.
    /// </summary>
    private readonly Buffer[] m_buffers;

    /// <summary>
    ///   A list of the currently enabled data.
    /// </summary>
    private readonly IList<RenderTexture[]> m_enabledData;

    /// <summary>
    ///   Does the actual FFT on the GPU.
    /// </summary>
    private readonly FourierGPU m_fourier;

    private readonly Vector4 m_offset;

    private readonly RenderBuffer[] m_tmpBuffer2;
    private readonly RenderBuffer[] m_tmpBuffer3;
    private readonly RenderBuffer[] m_tmpBuffer4;

    /// <summary>
    ///   Temporary arrays to reduce allocations.
    /// </summary>
    private readonly IList<RenderTexture> m_tmpList;

    /// <summary>
    ///   Current read index.
    /// </summary>
    private int m_index = -1;

    /// <summary>
    ///   If sampling is still enabled.
    /// </summary>
    private bool m_samplingEnabled;

    public WaveSpectrumBufferGPU(int size, Shader fourierSdr, int numBuffers)
    {
      if (numBuffers < 1 || numBuffers > 4)
        throw new InvalidOperationException("Number of buffers is " + numBuffers +
                                            " but must be between (inclusive) 1 and 4");


      m_buffers = new Buffer[numBuffers];

      m_fourier = new FourierGPU(size, fourierSdr);

      //Temporary arrays to reduce allocations.
      m_tmpList = new List<RenderTexture>();
      m_tmpBuffer2 = new RenderBuffer[2];
      m_tmpBuffer3 = new RenderBuffer[3];
      m_tmpBuffer4 = new RenderBuffer[4];

      m_offset = new Vector4(1.0f + 0.5f / Size, 1.0f + 0.5f / Size, 0, 0);

      for (var i = 0; i < numBuffers; i++) m_buffers[i] = CreateBuffer(size);

      m_enabledData = new List<RenderTexture[]>();
      UpdateEnabledData();

      m_bufferName = "Ceto Wave Spectrum GPU Buffer";
    }

    /// <summary>
    ///   Has the data requested been created.
    ///   GPU buffers always create their data
    ///   as soon as requested so this is always true.
    /// </summary>
    public override bool Done => true;

    /// <summary>
    ///   The fourier size of the buffer.
    /// </summary>
    public override int Size => m_fourier.size;

    /// <summary>
    ///   Does this buffer run on the GPU. Always true.
    /// </summary>
    public override bool IsGPU => true;

    /// <summary>
    ///   Create a buffer for this fourier size.
    ///   A buffer requires two textures.
    ///   During the FFT one texture is written into
    ///   while the other is read from and then they swap.
    ///   This is the read/write method (also know as ping/pong).
    /// </summary>
    private Buffer CreateBuffer(int size)
    {
      var buffer = new Buffer();

      buffer.data = new RenderTexture[2];

      return buffer;
    }

    /// <summary>
    ///   Get the read texture at this idx.
    ///   If buffer is disabled or not a valid index
    ///   a blank texture is returned.
    /// </summary>
    public override Texture GetTexture(int idx)
    {
      if (m_index == -1) return Texture2D.blackTexture;

      if (idx < 0 || idx >= m_buffers.Length) return Texture2D.blackTexture;

      if (m_buffers[idx].disabled) return Texture2D.blackTexture;

      return m_buffers[idx].data[m_index];
    }

    /// <summary>
    ///   Release the buffers.
    /// </summary>
    public override void Release()
    {
      m_tmpList.Clear();

      m_fourier.Release();

      var count = m_buffers.Length;
      for (var i = 0; i < count; i++)
      {
        //RTUtility.ReleaseAndDestroy(m_buffers[i].data);
        m_buffers[i].data[0] = null;
        m_buffers[i].data[1] = null;
      }
    }

    /// <summary>
    ///   Initialize the enabled buffers with the current conditions spectrum for this time.
    /// </summary>
    protected override void Initilize(WaveSpectrumCondition condition, float time)
    {
      if (InitMaterial == null)
        throw new InvalidOperationException("GPU buffer has not had its Init material set");

      if (InitPass == -1)
        throw new InvalidOperationException("GPU buffer has not had its Init material pass set");

      InitMaterial.SetTexture("Ceto_Spectrum01",
        condition.Spectrum01 != null ? condition.Spectrum01 : Texture2D.blackTexture);
      InitMaterial.SetTexture("Ceto_Spectrum23",
        condition.Spectrum23 != null ? condition.Spectrum23 : Texture2D.blackTexture);
      InitMaterial.SetTexture("Ceto_WTable", condition.WTable);
      InitMaterial.SetVector("Ceto_InverseGridSizes", condition.InverseGridSizes());
      InitMaterial.SetVector("Ceto_GridSizes", condition.GridSizes);
      InitMaterial.SetVector("Ceto_Offset", m_offset);
      InitMaterial.SetFloat("Ceto_Time", time);

      m_tmpList.Clear();

      var count = m_buffers.Length;
      for (var i = 0; i < count; i++)
        if (!m_buffers[i].disabled)
          m_tmpList.Add(m_buffers[i].data[1]);

      count = m_tmpList.Count;

      if (count == 0) return;

      if (count == 1)
      {
        Graphics.Blit(null, m_tmpList[0], InitMaterial, InitPass);
      }
      else if (count == 2)
      {
        m_tmpBuffer2[0] = m_tmpList[0].colorBuffer;
        m_tmpBuffer2[1] = m_tmpList[1].colorBuffer;
        RTUtility.MultiTargetBlit(m_tmpBuffer2, m_tmpList[0].depthBuffer, InitMaterial, InitPass);
      }
      else if (count == 3)
      {
        m_tmpBuffer3[0] = m_tmpList[0].colorBuffer;
        m_tmpBuffer3[1] = m_tmpList[1].colorBuffer;
        m_tmpBuffer3[2] = m_tmpList[2].colorBuffer;
        RTUtility.MultiTargetBlit(m_tmpBuffer3, m_tmpList[0].depthBuffer, InitMaterial, InitPass);
      }
      else if (count == 4)
      {
        m_tmpBuffer4[0] = m_tmpList[0].colorBuffer;
        m_tmpBuffer4[1] = m_tmpList[1].colorBuffer;
        m_tmpBuffer4[2] = m_tmpList[2].colorBuffer;
        m_tmpBuffer4[3] = m_tmpList[3].colorBuffer;
        RTUtility.MultiTargetBlit(m_tmpBuffer4, m_tmpList[0].depthBuffer, InitMaterial, InitPass);
      }
    }

    /// <summary>
    ///   Updates the list of the data from the enabled buffers
    /// </summary>
    public void UpdateEnabledData()
    {
      m_enabledData.Clear();
      var count = m_buffers.Length;
      for (var i = 0; i < count; i++)
        if (!m_buffers[i].disabled)
          m_enabledData.Add(m_buffers[i].data);
    }

    /// <summary>
    ///   Enables the data for the buffer at this idx.
    ///   If idx is -1 all the buffers will be enabled.
    /// </summary>
    public override void EnableBuffer(int idx)
    {
      var count = m_buffers.Length;
      if (idx < -1 || idx >= count) return;

      if (idx == -1)
        for (var i = 0; i < count; i++)
          m_buffers[i].disabled = false;
      else
        m_buffers[idx].disabled = false;

      UpdateEnabledData();
    }

    /// <summary>
    ///   Disables the data for the buffer at this idx.
    ///   If idx is -1 all the buffers will be disabled.
    /// </summary>
    public override void DisableBuffer(int idx)
    {
      var count = m_buffers.Length;
      if (idx < -1 || idx >= count) return;

      if (idx == -1)
        for (var i = 0; i < count; i++)
          m_buffers[i].disabled = true;
      else
        m_buffers[idx].disabled = true;

      UpdateEnabledData();
    }

    /// <summary>
    ///   Returns the number of enabled buffers.
    /// </summary>
    public override int EnabledBuffers()
    {
      return m_enabledData.Count;
    }

    /// <summary>
    ///   Is this buffer enabled.
    /// </summary>
    public override bool IsEnabledBuffer(int idx)
    {
      if (idx < 0 || idx >= m_buffers.Length) return false;

      return !m_buffers[idx].disabled;
    }

    /// <summary>
    ///   Fill the buffer with temporary render textures.
    /// </summary>
    private void CreateTextures()
    {
      var count = m_enabledData.Count;
      for (var i = 0; i < count; i++)
      for (var j = 0; j < 2; j++)
      {
        if (m_enabledData[i][j] != null)
          RenderTexture.ReleaseTemporary(m_enabledData[i][j]);

        var tex = RenderTexture.GetTemporary(Size, Size, 0, RenderTextureFormat.ARGBFloat,
          RenderTextureReadWrite.Linear);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.name = m_bufferName;
        tex.anisoLevel = 0;
        tex.Create();

        m_enabledData[i][j] = tex;
      }
    }

    /// <summary>
    ///   Creates the data for this conditions spectrum for this time value.
    /// </summary>
    public override void Run(WaveSpectrumCondition condition, float time)
    {
      TimeValue = time;
      HasRun = true;
      BeenSampled = false;

      if (m_samplingEnabled)
        throw new InvalidOperationException("Can not run if sampling enabled");

      UpdateEnabledData();
      CreateTextures();

      //There are no buffers enabled return.
      var count = m_enabledData.Count;
      if (count == 0) return;

      //Initialize buffers.
      Initilize(condition, time);

      //Perform the FFT. Supports running the FFT on 1-4 buffers at one.
      if (count == 1)
        m_index = m_fourier.PeformFFT(m_enabledData[0]);
      else if (count == 2)
        m_index = m_fourier.PeformFFT(m_enabledData[0], m_enabledData[1]);
      else if (count == 3)
        m_index = m_fourier.PeformFFT(m_enabledData[0], m_enabledData[1], m_enabledData[2]);
      else if (count == 4)
        m_index = m_fourier.PeformFFT(m_enabledData[0], m_enabledData[1], m_enabledData[2], m_enabledData[3]);
    }

    /// <summary>
    ///   For the FFT the data must be in point/clamp mode
    ///   but to sample the data it needs to be in bilinear/repeat mode.
    ///   Change to bilinear/repeat here. Only changes the read buffer.
    /// </summary>
    public override void EnableSampling()
    {
      if (m_index == -1) return;

      m_samplingEnabled = true;

      var count = m_buffers.Length;
      for (var i = 0; i < count; i++)
      {
        if (m_buffers[i].data[m_index] == null) continue;

        m_buffers[i].data[m_index].filterMode = FilterMode.Bilinear;
        m_buffers[i].data[m_index].wrapMode = TextureWrapMode.Repeat;
      }
    }

    /// <summary>
    ///   For the FFT the data must be in point/clamp mode
    ///   but to sample the data it needs to be in bilinear/repeat mode.
    ///   Change to point/clamp here. Only changes the read buffer.
    /// </summary>
    public override void DisableSampling()
    {
      if (m_index == -1) return;

      m_samplingEnabled = false;

      var count = m_buffers.Length;
      for (var i = 0; i < count; i++)
      {
        if (m_buffers[i].data[m_index] == null) continue;

        m_buffers[i].data[m_index].filterMode = FilterMode.Point;
        m_buffers[i].data[m_index].wrapMode = TextureWrapMode.Clamp;
      }
    }

    /// <summary>
    ///   Holds the actual buffer data.
    /// </summary>
    private struct Buffer
    {
      //Array to hold the read/write data
      public RenderTexture[] data;
      public bool disabled;
    }
  }
}