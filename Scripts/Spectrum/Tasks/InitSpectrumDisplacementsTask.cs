using System;
using System.Collections;
using Razomy.Unity.Scripts.Common.Threading.Tasks;
using Razomy.Unity.Scripts.Ocean;
using Razomy.Unity.Scripts.Spectrum.Buffers;
using Razomy.Unity.Scripts.Spectrum.Conditions;
using UnityEngine;

namespace Razomy.Unity.Scripts.Spectrum.Tasks
{
  public class InitSpectrumDisplacementsTask : ThreadedTask
  {
    private Vector3[] m_ktable1, m_ktable2, m_ktable3, m_ktable4;

    private Color[] m_spectrum01;

    private Color[] m_spectrum23;

    private Color[] m_wtable;


    public InitSpectrumDisplacementsTask(DisplacementBufferCPU buffer, WaveSpectrumCondition condition, float time)
      : base(true)
    {
      Buffer = buffer;
      NumGrids = condition.Key.NumGrids;
      Size = condition.Key.Size;
      SpectrumType = condition.Key.SpectrumType;
      TimeValue = time;

      Reset(condition, time);

      CreateKTables(condition.InverseGridSizes());
    }

    public int NumGrids { get; }

    public int Size { get; }

    public int LastUpdated { get; protected set; }

    public float TimeValue { get; protected set; }

    public SPECTRUM_TYPE SpectrumType { get; }

    protected DisplacementBufferCPU Buffer { get; }

    protected Vector4[] Data0 { get; set; }
    protected Vector4[] Data1 { get; set; }
    protected Vector4[] Data2 { get; set; }

    public void Reset(WaveSpectrumCondition condition, float time)
    {
      if (condition.Key.SpectrumType != SpectrumType)
        throw new InvalidOperationException("Trying to reset a Unified InitSpectrum task with wrong condition type = " +
                                            condition.Key.SpectrumType);

      if (condition.Key.Size != Size)
        throw new InvalidOperationException("Trying to reset a Unified InitSpectrum task with wrong condition size = " +
                                            condition.Key.Size);

      base.Reset();

      var S2 = Size * Size;

      if (m_spectrum01 == null)
        m_spectrum01 = new Color[S2];

      if (m_spectrum23 == null && NumGrids > 2)
        m_spectrum23 = new Color[S2];

      if (m_wtable == null)
        m_wtable = new Color[S2];

      TimeValue = time;

      Data0 = Buffer.GetReadBuffer(0);
      Data1 = Buffer.GetReadBuffer(1);
      Data2 = Buffer.GetReadBuffer(2);

      var buffer0 = Buffer.GetBuffer(0);
      var buffer1 = Buffer.GetBuffer(1);
      var buffer2 = Buffer.GetBuffer(2);

      if (buffer0 != null)
      {
        if (NumGrids > 2)
          buffer0.doublePacked = true;
        else
          buffer0.doublePacked = false;
      }

      if (buffer1 != null)
      {
        if (NumGrids > 1)
          buffer1.doublePacked = true;
        else
          buffer1.doublePacked = false;
      }

      if (buffer2 != null)
      {
        if (NumGrids > 3)
          buffer2.doublePacked = true;
        else
          buffer2.doublePacked = false;
      }

      if (LastUpdated != condition.LastUpdated)
      {
        LastUpdated = condition.LastUpdated;

        if (m_spectrum01 != null && condition.SpectrumData01 != null)
          Array.Copy(condition.SpectrumData01, m_spectrum01, S2);

        if (m_spectrum23 != null && condition.SpectrumData23 != null)
          Array.Copy(condition.SpectrumData23, m_spectrum23, S2);

        if (m_wtable != null && condition.WTableData != null)
          Array.Copy(condition.WTableData, m_wtable, S2);
      }
    }

    public override IEnumerator Run()
    {
      if (NumGrids == 1)
        InitilizeGrids1();
      else if (NumGrids == 2)
        InitilizeGrids2();
      else if (NumGrids == 3)
        InitilizeGrids3();
      else if (NumGrids == 4) InitilizeGrids4();

      FinishedRunning();
      return null;
    }

    private void InitilizeGrids1()
    {
      Vector2 uv, st, h1, n1;
      Vector3 k1;
      Color s12, s12c;
      int i, j;
      float w;
      float c, s;

      var ifsize = 1.0f / Size;

      //float t = Time.realtimeSinceStartup;

      for (var y = 0; y < Size; y++)
      for (var x = 0; x < Size; x++)
      {
        if (Cancelled) return;

        uv.x = x * ifsize;
        uv.y = y * ifsize;

        st.x = uv.x > 0.5f ? uv.x - 1.0f : uv.x;
        st.y = uv.y > 0.5f ? uv.y - 1.0f : uv.y;

        i = x + y * Size;
        j = (Size - x) % Size + (Size - y) % Size * Size;

        s12 = m_spectrum01[i];
        s12c = m_spectrum01[j];

        w = m_wtable[i].r * TimeValue;

        c = Mathf.Cos(w);
        s = Mathf.Sin(w);

        h1.x = (s12.r + s12c.r) * c - (s12.g + s12c.g) * s;
        h1.y = (s12.r - s12c.r) * s + (s12.g - s12c.g) * c;

        if (Data0 != null)
        {
          Data0[i].x = h1.x;
          Data0[i].y = h1.y;
          Data0[i].z = 0.0f;
          Data0[i].w = 0.0f;
        }

        if (Data1 != null)
        {
          k1 = m_ktable1[i];

          n1.x = -(k1.x * h1.y) - k1.y * h1.x;
          n1.y = k1.x * h1.x - k1.y * h1.y;

          Data1[i].x = n1.x * k1.z;
          Data1[i].y = n1.y * k1.z;
          Data1[i].z = 0.0f;
          Data1[i].w = 0.0f;
        }
      }

      //Debug.Log("InitSpectrum 1 grid time = " + (Time.realtimeSinceStartup - t) * 1000.0f);
    }

    private void InitilizeGrids2()
    {
      Vector2 uv, st;
      Vector3 k1, k2;
      Vector2 h1, h2;
      Vector2 n1, n2;
      Color s12, s12c;
      int i, j;
      Color w;
      float c, s;

      var ifsize = 1.0f / Size;

      //float t = Time.realtimeSinceStartup;

      for (var y = 0; y < Size; y++)
      for (var x = 0; x < Size; x++)
      {
        if (Cancelled) return;

        uv.x = x * ifsize;
        uv.y = y * ifsize;

        st.x = uv.x > 0.5f ? uv.x - 1.0f : uv.x;
        st.y = uv.y > 0.5f ? uv.y - 1.0f : uv.y;

        i = x + y * Size;
        j = (Size - x) % Size + (Size - y) % Size * Size;

        s12 = m_spectrum01[i];
        s12c = m_spectrum01[j];

        w = m_wtable[i];

        w.r *= TimeValue;
        w.g *= TimeValue;

        c = Mathf.Cos(w.r);
        s = Mathf.Sin(w.r);

        h1.x = (s12.r + s12c.r) * c - (s12.g + s12c.g) * s;
        h1.y = (s12.r - s12c.r) * s + (s12.g - s12c.g) * c;

        c = Mathf.Cos(w.g);
        s = Mathf.Sin(w.g);

        h2.x = (s12.b + s12c.b) * c - (s12.a + s12c.a) * s;
        h2.y = (s12.b - s12c.b) * s + (s12.a - s12c.a) * c;

        if (Data0 != null)
        {
          Data0[i].x = h1.x + -h2.y;
          Data0[i].y = h1.y + h2.x;
          Data0[i].z = 0.0f;
          Data0[i].w = 0.0f;
        }

        if (Data1 != null)
        {
          k1 = m_ktable1[i];
          k2 = m_ktable2[i];

          n1.x = -(k1.x * h1.y) - k1.y * h1.x;
          n1.y = k1.x * h1.x - k1.y * h1.y;

          n2.x = -(k2.x * h2.y) - k2.y * h2.x;
          n2.y = k2.x * h2.x - k2.y * h2.y;

          Data1[i].x = n1.x * k1.z;
          Data1[i].y = n1.y * k1.z;
          Data1[i].z = n2.x * k2.z;
          Data1[i].w = n2.y * k2.z;
        }
      }

      //Debug.Log("InitSpectrum 4 grids time = " + (Time.realtimeSinceStartup - t) * 1000.0f);
    }

    private void InitilizeGrids3()
    {
      Vector2 uv, st;
      Vector3 k1, k2, k3;
      Vector2 h1, h2, h3;
      Vector2 n1, n2, n3;
      Color s12, s34, s12c, s34c;
      int i, j;
      Color w;
      float c, s;

      var ifsize = 1.0f / Size;

      //float t = Time.realtimeSinceStartup;

      for (var y = 0; y < Size; y++)
      for (var x = 0; x < Size; x++)
      {
        if (Cancelled) return;

        uv.x = x * ifsize;
        uv.y = y * ifsize;

        st.x = uv.x > 0.5f ? uv.x - 1.0f : uv.x;
        st.y = uv.y > 0.5f ? uv.y - 1.0f : uv.y;

        i = x + y * Size;
        j = (Size - x) % Size + (Size - y) % Size * Size;

        s12 = m_spectrum01[i];
        s34 = m_spectrum23[i];

        s12c = m_spectrum01[j];
        s34c = m_spectrum23[j];

        w = m_wtable[i];

        w.r *= TimeValue;
        w.g *= TimeValue;
        w.b *= TimeValue;
        w.a *= TimeValue;

        c = Mathf.Cos(w.r);
        s = Mathf.Sin(w.r);

        h1.x = (s12.r + s12c.r) * c - (s12.g + s12c.g) * s;
        h1.y = (s12.r - s12c.r) * s + (s12.g - s12c.g) * c;

        c = Mathf.Cos(w.g);
        s = Mathf.Sin(w.g);

        h2.x = (s12.b + s12c.b) * c - (s12.a + s12c.a) * s;
        h2.y = (s12.b - s12c.b) * s + (s12.a - s12c.a) * c;

        c = Mathf.Cos(w.b);
        s = Mathf.Sin(w.b);

        h3.x = (s34.r + s34c.r) * c - (s34.g + s34c.g) * s;
        h3.y = (s34.r - s34c.r) * s + (s34.g - s34c.g) * c;

        if (Data0 != null)
        {
          Data0[i].x = h1.x + -h2.y;
          Data0[i].y = h1.y + h2.x;
          Data0[i].z = h3.x;
          Data0[i].w = h3.y;
        }

        if (Data1 != null)
        {
          k1 = m_ktable1[i];
          k2 = m_ktable2[i];

          n1.x = -(k1.x * h1.y) - k1.y * h1.x;
          n1.y = k1.x * h1.x - k1.y * h1.y;

          n2.x = -(k2.x * h2.y) - k2.y * h2.x;
          n2.y = k2.x * h2.x - k2.y * h2.y;

          Data1[i].x = n1.x * k1.z;
          Data1[i].y = n1.y * k1.z;
          Data1[i].z = n2.x * k2.z;
          Data1[i].w = n2.y * k2.z;
        }

        if (Data2 != null)
        {
          k3 = m_ktable3[i];

          n3.x = -(k3.x * h3.y) - k3.y * h3.x;
          n3.y = k3.x * h3.x - k3.y * h3.y;

          Data2[i].x = n3.x * k3.z;
          Data2[i].y = n3.y * k3.z;
          Data2[i].z = 0.0f;
          Data2[i].w = 0.0f;
        }
      }

      //Debug.Log("InitSpectrum 4 grids time = " + (Time.realtimeSinceStartup - t) * 1000.0f);
    }

    private void InitilizeGrids4()
    {
      Vector2 uv, st;
      Vector3 k1, k2, k3, k4;
      Vector2 h1, h2, h3, h4;
      Vector2 n1, n2, n3, n4;
      Color s12, s34, s12c, s34c;
      int i, j;
      Color w;
      float c, s;

      var ifsize = 1.0f / Size;

      //float t = Time.realtimeSinceStartup;

      for (var y = 0; y < Size; y++)
      for (var x = 0; x < Size; x++)
      {
        if (Cancelled) return;

        uv.x = x * ifsize;
        uv.y = y * ifsize;

        st.x = uv.x > 0.5f ? uv.x - 1.0f : uv.x;
        st.y = uv.y > 0.5f ? uv.y - 1.0f : uv.y;

        i = x + y * Size;
        j = (Size - x) % Size + (Size - y) % Size * Size;

        s12 = m_spectrum01[i];
        s34 = m_spectrum23[i];

        s12c = m_spectrum01[j];
        s34c = m_spectrum23[j];

        w = m_wtable[i];

        w.r *= TimeValue;
        w.g *= TimeValue;
        w.b *= TimeValue;
        w.a *= TimeValue;

        c = Mathf.Cos(w.r);
        s = Mathf.Sin(w.r);

        h1.x = (s12.r + s12c.r) * c - (s12.g + s12c.g) * s;
        h1.y = (s12.r - s12c.r) * s + (s12.g - s12c.g) * c;

        c = Mathf.Cos(w.g);
        s = Mathf.Sin(w.g);

        h2.x = (s12.b + s12c.b) * c - (s12.a + s12c.a) * s;
        h2.y = (s12.b - s12c.b) * s + (s12.a - s12c.a) * c;

        c = Mathf.Cos(w.b);
        s = Mathf.Sin(w.b);

        h3.x = (s34.r + s34c.r) * c - (s34.g + s34c.g) * s;
        h3.y = (s34.r - s34c.r) * s + (s34.g - s34c.g) * c;

        c = Mathf.Cos(w.a);
        s = Mathf.Sin(w.a);

        h4.x = (s34.b + s34c.b) * c - (s34.a + s34c.a) * s;
        h4.y = (s34.b - s34c.b) * s + (s34.a - s34c.a) * c;

        if (Data0 != null)
        {
          Data0[i].x = h1.x + -h2.y;
          Data0[i].y = h1.y + h2.x;
          Data0[i].z = h3.x + -h4.y;
          Data0[i].w = h3.y + h4.x;
        }

        if (Data1 != null)
        {
          k1 = m_ktable1[i];
          k2 = m_ktable2[i];

          n1.x = -(k1.x * h1.y) - k1.y * h1.x;
          n1.y = k1.x * h1.x - k1.y * h1.y;

          n2.x = -(k2.x * h2.y) - k2.y * h2.x;
          n2.y = k2.x * h2.x - k2.y * h2.y;

          Data1[i].x = n1.x * k1.z;
          Data1[i].y = n1.y * k1.z;
          Data1[i].z = n2.x * k2.z;
          Data1[i].w = n2.y * k2.z;
        }

        if (Data2 != null)
        {
          k3 = m_ktable3[i];
          k4 = m_ktable4[i];

          n3.x = -(k3.x * h3.y) - k3.y * h3.x;
          n3.y = k3.x * h3.x - k3.y * h3.y;

          n4.x = -(k4.x * h4.y) - k4.y * h4.x;
          n4.y = k4.x * h4.x - k4.y * h4.y;

          Data2[i].x = n3.x * k3.z;
          Data2[i].y = n3.y * k3.z;
          Data2[i].z = n4.x * k4.z;
          Data2[i].w = n4.y * k4.z;
        }
      }

      //Debug.Log("InitSpectrum 4 grids time = " + (Time.realtimeSinceStartup - t) * 1000.0f);
    }

    private void CreateKTables(Vector4 inverseGridSizes)
    {
      var ifsize = 1.0f / Size;

      if (NumGrids > 0)
        m_ktable1 = new Vector3[Size * Size];
      if (NumGrids > 1)
        m_ktable2 = new Vector3[Size * Size];
      if (NumGrids > 2)
        m_ktable3 = new Vector3[Size * Size];
      if (NumGrids > 3)
        m_ktable4 = new Vector3[Size * Size];

      int i;
      Vector2 uv, st, k1, k2, k3, k4;
      float K1, K2, K3, K4, IK1, IK2, IK3, IK4;

      //float t = Time.realtimeSinceStartup;

      for (var y = 0; y < Size; y++)
      for (var x = 0; x < Size; x++)
      {
        uv.x = x * ifsize;
        uv.y = y * ifsize;

        st.x = uv.x > 0.5f ? uv.x - 1.0f : uv.x;
        st.y = uv.y > 0.5f ? uv.y - 1.0f : uv.y;

        i = x + y * Size;

        if (NumGrids > 0)
        {
          k1.x = st.x * inverseGridSizes.x;
          k1.y = st.y * inverseGridSizes.x;
          K1 = Mathf.Sqrt(k1.x * k1.x + k1.y * k1.y);
          IK1 = K1 == 0.0f ? 0.0f : 1.0f / K1;

          m_ktable1[i].x = k1.x;
          m_ktable1[i].y = k1.y;
          m_ktable1[i].z = IK1;
        }

        if (NumGrids > 1)
        {
          k2.x = st.x * inverseGridSizes.y;
          k2.y = st.y * inverseGridSizes.y;
          K2 = Mathf.Sqrt(k2.x * k2.x + k2.y * k2.y);
          IK2 = K2 == 0.0f ? 0.0f : 1.0f / K2;

          m_ktable2[i].x = k2.x;
          m_ktable2[i].y = k2.y;
          m_ktable2[i].z = IK2;
        }

        if (NumGrids > 2)
        {
          k3.x = st.x * inverseGridSizes.z;
          k3.y = st.y * inverseGridSizes.z;
          K3 = Mathf.Sqrt(k3.x * k3.x + k3.y * k3.y);
          IK3 = K3 == 0.0f ? 0.0f : 1.0f / K3;

          m_ktable3[i].x = k3.x;
          m_ktable3[i].y = k3.y;
          m_ktable3[i].z = IK3;
        }

        if (NumGrids > 3)
        {
          k4.x = st.x * inverseGridSizes.w;
          k4.y = st.y * inverseGridSizes.w;
          K4 = Mathf.Sqrt(k4.x * k4.x + k4.y * k4.y);
          IK4 = K4 == 0.0f ? 0.0f : 1.0f / K4;

          m_ktable4[i].x = k4.x;
          m_ktable4[i].y = k4.y;
          m_ktable4[i].z = IK4;
        }
      }

      //Debug.Log("Create KTable time = " + (Time.realtimeSinceStartup - t) * 1000.0f);
    }
  }
}