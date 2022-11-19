using Razomy.Unity.Scripts.Grids;
using Razomy.Unity.Scripts.Ocean.Overlays;
using Razomy.Unity.Scripts.Reflections;
using Razomy.Unity.Scripts.UnderWater;

namespace Razomy.Unity.Scripts.Ocean.CameraData
{
  /// <summary>
  ///   Holds all the data for a camera.
  ///   Each camera rendering the ocean has its own copy.
  /// </summary>
  public class CameraData
  {
    public bool checkedForSettings;
    public DepthData depth;
    public MaskData mask;
    public WaveOverlayData overlay;
    public ProjectionData projection;
    public ReflectionData reflection;
    public OceanCameraSettings settings;
  }
}