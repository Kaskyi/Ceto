namespace Ceto
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