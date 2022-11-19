using Razomy.Unity.Scripts.Common.Unity;
using Razomy.Unity.Scripts.Ocean.CameraData;
using UnityEngine;

namespace Razomy.Unity.Scripts.Reflections
{
  /// <summary>
  ///   Holds the reflection cam and if the
  ///   reflections have been updated this frame.
  /// </summary>
  public class ReflectionData : ViewData
  {
    /// <summary>
    ///   The camera that renders the reflections.
    /// </summary>
    public Camera cam;

    /// <summary>
    ///   The render target for the camera.
    ///   Two targets are used for stero rendering (left/right eye).
    ///   If stero rendering not used target1 will be null.
    /// </summary>
    public RenderTexture target0, target1;

    public void DestroyCamera()
    {
      if (cam == null) return;
      cam.targetTexture = null;
      Object.Destroy(cam.gameObject);
      Object.Destroy(cam);
      cam = null;
    }

    public void DestroyTargets()
    {
      RTUtility.ReleaseAndDestroy(target0);
      RTUtility.ReleaseAndDestroy(target1);
      target0 = null;
      target1 = null;
    }
  }
}