using Razomy.Unity.Scripts.Common.Unity;
using Razomy.Unity.Scripts.Grids;
using Razomy.Unity.Scripts.Ocean;
using Razomy.Unity.Scripts.Reflections;
using Razomy.Unity.Scripts.Spectrum;
using Razomy.Unity.Scripts.UnderWater;
using UnityEngine;

//using Ceto.Common.Unity.Utility;
//using uSky;

namespace Razomy.Unity.Scripts.Utility
{
  public class GUIDisplay : MonoBehaviour
  {
    public GameObject m_camera;

    public bool m_hide;

    private readonly Rect m_detailToggle = new(320, 20, 95, 30);

    private readonly Rect m_hideToggle = new(20, 20, 95, 30);

    private readonly Rect m_reflectionsToggle = new(120, 20, 95, 30);

    private readonly Rect m_refractionToggle = new(220, 20, 95, 30);

    private readonly Rect m_settings = new(20, 60, 340, 600);

    private readonly float m_textWidth = 150.0f;

    private FPSCounter m_fps;

    private bool m_supportsDX11;

    private bool m_ultraDetailOn;

    //public GameObject m_uSky;

    private void Start()
    {
      m_fps = GetComponent<FPSCounter>();

      m_supportsDX11 = SystemInfo.graphicsShaderLevel >= 50 && SystemInfo.supportsComputeShaders;
    }

    private void OnGUI()
    {
      if (Ocean.Ocean.Instance == null) return;

      var shipCam = m_camera.GetComponent<ShipCamera>();
      var postEffect = m_camera.GetComponent<UnderWaterPostEffect>();

      var spectrum = Ocean.Ocean.Instance.GetComponent<WaveSpectrum>();
      var reflection = Ocean.Ocean.Instance.GetComponent<PlanarReflection>();
      var underWater = Ocean.Ocean.Instance.GetComponent<UnderWater.UnderWater>();
      var grid = Ocean.Ocean.Instance.GetComponent<ProjectedGrid>();

      if (true)
      {
        GUILayout.BeginArea(m_hideToggle);
        GUILayout.BeginHorizontal("Box");
        m_hide = GUILayout.Toggle(m_hide, " Hide GUI");
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
      }

      shipCam.disableInput = false;
      if (m_hide) return;
      shipCam.disableInput = true;

      if (reflection != null)
      {
        var on = reflection.enabled;

        GUILayout.BeginArea(m_reflectionsToggle);
        GUILayout.BeginHorizontal("Box");
        on = GUILayout.Toggle(on, " Reflection");
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        reflection.enabled = on;
      }

      if (underWater != null)
      {
        var on = underWater.enabled;

        GUILayout.BeginArea(m_refractionToggle);
        GUILayout.BeginHorizontal("Box");
        on = GUILayout.Toggle(on, " Refraction");
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        underWater.enabled = on;
      }

      if (spectrum != null && grid != null)
      {
        GUILayout.BeginArea(m_detailToggle);
        GUILayout.BeginHorizontal("Box");
        m_ultraDetailOn = GUILayout.Toggle(m_ultraDetailOn, " Ultra Detail");
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        if (m_ultraDetailOn)
        {
          grid.resolution = MESH_RESOLUTION.ULTRA;
          spectrum.fourierSize = FOURIER_SIZE.ULTRA_256_GPU;
          spectrum.disableReadBack = !m_supportsDX11;
        }
        else
        {
          grid.resolution = MESH_RESOLUTION.HIGH;
          spectrum.fourierSize = FOURIER_SIZE.MEDIUM_64_CPU;
          spectrum.disableReadBack = true;
        }
      }

      GUILayout.BeginArea(m_settings);
      GUILayout.BeginVertical("Box");

      if (true)
      {
        var windDir = Ocean.Ocean.Instance.windDir;

        GUILayout.BeginHorizontal("Box");
        GUILayout.Label("Wind Direction", GUILayout.MaxWidth(m_textWidth));
        windDir = GUILayout.HorizontalSlider(windDir, 0.0f, 360.0f);
        GUILayout.EndHorizontal();
        Ocean.Ocean.Instance.windDir = windDir;
      }

      /*
      if (m_uSky != null)
      {
          float timeLine = m_uSky.GetComponent<uSkyManager>().Timeline;

          GUILayout.BeginHorizontal("Box");
          GUILayout.Label("Sun Dir", GUILayout.MaxWidth(m_textWidth));
          timeLine = GUILayout.HorizontalSlider(timeLine, 0.0f, 23.0f);
          GUILayout.EndHorizontal();

          m_uSky.GetComponent<uSkyManager>().Timeline = timeLine;
      }
      */

      if (spectrum != null)
      {
        var windSpeed = spectrum.windSpeed;

        GUILayout.BeginHorizontal("Box");
        GUILayout.Label("Wind Speed", GUILayout.MaxWidth(m_textWidth));
        windSpeed = GUILayout.HorizontalSlider(windSpeed, 0.0f, WaveSpectrum.MAX_WIND_SPEED);
        GUILayout.EndHorizontal();

        spectrum.windSpeed = windSpeed;
      }

      if (spectrum != null)
      {
        var waveAge = spectrum.waveAge;

        GUILayout.BeginHorizontal("Box");
        GUILayout.Label("Wave Age", GUILayout.MaxWidth(m_textWidth));
        waveAge = GUILayout.HorizontalSlider(waveAge, WaveSpectrum.MIN_WAVE_AGE, WaveSpectrum.MAX_WAVE_AGE);
        GUILayout.EndHorizontal();

        spectrum.waveAge = waveAge;
      }

      if (spectrum != null)
      {
        var waveSpeed = spectrum.waveSpeed;

        GUILayout.BeginHorizontal("Box");
        GUILayout.Label("Wave Speed", GUILayout.MaxWidth(m_textWidth));
        waveSpeed = GUILayout.HorizontalSlider(waveSpeed, 0.0f, WaveSpectrum.MAX_WAVE_SPEED);
        GUILayout.EndHorizontal();

        spectrum.waveSpeed = waveSpeed;
      }

      if (spectrum != null)
      {
        var choppyness = spectrum.choppyness;

        GUILayout.BeginHorizontal("Box");
        GUILayout.Label("Choppyness", GUILayout.MaxWidth(m_textWidth));
        choppyness = GUILayout.HorizontalSlider(choppyness, 0.0f, WaveSpectrum.MAX_CHOPPYNESS);
        GUILayout.EndHorizontal();

        spectrum.choppyness = choppyness;
      }

      if (spectrum != null)
      {
        var foamAmount = spectrum.foamAmount;

        GUILayout.BeginHorizontal("Box");
        GUILayout.Label("Foam Amount", GUILayout.MaxWidth(m_textWidth));
        foamAmount = GUILayout.HorizontalSlider(foamAmount, 0.0f, WaveSpectrum.MAX_FOAM_AMOUNT);
        GUILayout.EndHorizontal();

        spectrum.foamAmount = foamAmount;
      }

      if (spectrum != null)
      {
        var foamCoverage = spectrum.foamCoverage;

        GUILayout.BeginHorizontal("Box");
        GUILayout.Label("Foam Coverage", GUILayout.MaxWidth(m_textWidth));
        foamCoverage = GUILayout.HorizontalSlider(foamCoverage, 0.0f, WaveSpectrum.MAX_FOAM_COVERAGE);
        GUILayout.EndHorizontal();

        spectrum.foamCoverage = foamCoverage;
      }

      if (reflection != null)
      {
        var iterations = reflection.blurIterations;

        GUILayout.BeginHorizontal("Box");
        GUILayout.Label("Reflection blur", GUILayout.MaxWidth(m_textWidth));
        iterations = (int)GUILayout.HorizontalSlider(iterations, 0, 4);
        GUILayout.EndHorizontal();

        reflection.blurIterations = iterations;
      }

      if (reflection != null)
      {
        var intensity = reflection.reflectionIntensity;

        GUILayout.BeginHorizontal("Box");
        GUILayout.Label("Reflection Intensity", GUILayout.MaxWidth(m_textWidth));
        intensity = GUILayout.HorizontalSlider(intensity, 0.0f, PlanarReflection.MAX_REFLECTION_INTENSITY);
        GUILayout.EndHorizontal();

        reflection.reflectionIntensity = intensity;
      }

      if (underWater != null)
      {
        var intensity = underWater.aboveRefractionIntensity;

        GUILayout.BeginHorizontal("Box");
        GUILayout.Label("Refraction Intensity", GUILayout.MaxWidth(m_textWidth));
        intensity = GUILayout.HorizontalSlider(intensity, 0.0f, UnderWater.UnderWater.MAX_REFRACTION_INTENSITY);
        GUILayout.EndHorizontal();

        underWater.aboveRefractionIntensity = intensity;
      }

      if (spectrum != null)
      {
        var numGrids = spectrum.numberOfGrids;

        GUILayout.BeginHorizontal("Box");
        GUILayout.Label("Num Grids", GUILayout.MaxWidth(m_textWidth));
        numGrids = (int)GUILayout.HorizontalSlider(numGrids, 1, 4);
        GUILayout.EndHorizontal();

        spectrum.numberOfGrids = numGrids;
      }

      if (spectrum != null)
      {
        var scale = spectrum.gridScale;

        GUILayout.BeginHorizontal("Box");
        GUILayout.Label("Grid Scale", GUILayout.MaxWidth(m_textWidth));
        scale = GUILayout.HorizontalSlider(scale, WaveSpectrum.MIN_GRID_SCALE, WaveSpectrum.MAX_GRID_SCALE);
        GUILayout.EndHorizontal();

        spectrum.gridScale = scale;
      }


      if (underWater != null)
      {
        var intensity = underWater.subSurfaceScatterModifier.intensity;

        GUILayout.BeginHorizontal("Box");
        GUILayout.Label("SSS Intensity", GUILayout.MaxWidth(m_textWidth));
        intensity = GUILayout.HorizontalSlider(intensity, 0.0f, 10.0f);
        GUILayout.EndHorizontal();

        underWater.subSurfaceScatterModifier.intensity = intensity;
      }

      if (postEffect != null)
      {
        var blur = postEffect.blurIterations;

        GUILayout.BeginHorizontal("Box");
        GUILayout.Label("Underwater Blur", GUILayout.MaxWidth(m_textWidth));
        blur = (int)GUILayout.HorizontalSlider(blur, 0, 4);
        GUILayout.EndHorizontal();

        postEffect.blurIterations = blur;
      }

      if (true)
      {
        var info =
          @"W to move ship forward. A/D to turn.
Left click and drag to rotate camera.
Keypad +/- to move sun.
Ceto Version " + Ocean.Ocean.VERSION;

        if (m_fps != null) info += "\nCurrent FPS = " + m_fps.FrameRate.ToString("F2");

        GUILayout.BeginHorizontal("Box");
        GUILayout.TextArea(info);
        GUILayout.EndHorizontal();
      }

      GUILayout.EndVertical();
      GUILayout.EndArea();
    }
  }
}