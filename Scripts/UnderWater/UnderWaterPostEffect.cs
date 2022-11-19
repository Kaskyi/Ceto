using Razomy.Unity.Scripts.Ocean;
using Razomy.Unity.Scripts.Ocean.Querys;
using Razomy.Unity.Scripts.Utility;
using UnityEngine;

namespace Razomy.Unity.Scripts.UnderWater
{
  [AddComponentMenu("Ceto/Camera/UnderWaterPostEffect")]
  [RequireComponent(typeof(Camera))]
  public class UnderWaterPostEffect : MonoBehaviour
  {
    /// <summary>
    ///   The near plane points of a frustum box.
    /// </summary>
    private static readonly Vector4[] m_corners =
    {
      new(-1, -1, -1, 1),
      new(1, -1, -1, 1),
      new(1, 1, -1, 1),
      new(-1, 1, -1, 1)
    };

    /// <summary>
    ///   This will disable the post effect if over a clip overlay.
    /// </summary>
    public bool disableOnClip = true;

    /// <summary>
    ///   If true this will make this script set the underwater mode to
    ///   ABOVE_ONLY or ABOVE_AND_BELOW depending if the post effect runs.
    ///   This means the the under side mesh and mask will not run if not needed
    ///   but you have to hand over control of that to this script.
    /// </summary>
    public bool controlUnderwaterMode;

    /// <summary>
    ///   Multiple the under water color by sun.
    ///   So underwater fog goes dark at night.
    ///   0 is no attenuation and 1 is full.
    /// </summary>
    [Range(0.0f, 1.0f)] public float attenuationBySun = 0.8f;

    /// <summary>
    ///   The blur mode. Down sampling is faster but will lose resolution.
    /// </summary>
    public ImageBlur.BLUR_MODE blurMode = ImageBlur.BLUR_MODE.OFF;

    /// Blur iterations - larger number means more blur.
    [Range(0, 4)] public int blurIterations = 3;

    public Shader underWaterPostEffectSdr;

    [HideInInspector] public Shader blurShader;

    /// Blur spread for each iteration. Lower values
    /// give better looking blur, but require more iterations to
    /// get large blurs. Value is usually between 0.5 and 1.0.
    [Range(0.5f, 1.0f)]
    /*public*/
    private readonly float blurSpread = 0.6f;

    private ImageBlur m_imageBlur;

    private Material m_material;

    private WaveQuery m_query;

    private bool m_underWaterIsVisible;

    private void Start()
    {
      m_material = new Material(underWaterPostEffectSdr);

      m_imageBlur = new ImageBlur(blurShader);

      m_query = new WaveQuery();

      //Dont think you need to toggle depth mode
      //if image effect not using ImageEffectOpaque tag

      /*
      Camera cam = GetComponent<Camera>();

      //If rendering mode deferred and dx9 then toggling the depth
      //mode cause some strange issue with underwater effect
      //if using the opaque ocean materials.
      if (cam.actualRenderingPath == RenderingPath.Forward)
          GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
      */
    }

    private void LateUpdate()
    {
      var cam = GetComponent<Camera>();

      m_underWaterIsVisible = UnderWaterIsVisible(cam);

      if (controlUnderwaterMode && Ocean.Ocean.Instance != null && Ocean.Ocean.Instance.UnderWater is UnderWater)
      {
        var underwater = Ocean.Ocean.Instance.UnderWater;

        if (!m_underWaterIsVisible)
          underwater.underwaterMode = UNDERWATER_MODE.ABOVE_ONLY;
        else
          underwater.underwaterMode = UNDERWATER_MODE.ABOVE_AND_BELOW;
      }
    }

    //[ImageEffectOpaque]
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
      if (!ShouldRenderEffect())
      {
        Graphics.Blit(source, destination);
        return;
      }

      var cam = GetComponent<Camera>();

      var CAMERA_NEAR = cam.nearClipPlane;
      var CAMERA_FAR = cam.farClipPlane;
      var CAMERA_FOV = cam.fieldOfView;
      var CAMERA_ASPECT_RATIO = cam.aspect;

      var frustumCorners = Matrix4x4.identity;

      var fovWHalf = CAMERA_FOV * 0.5f;

      var toRight = cam.transform.right * CAMERA_NEAR * Mathf.Tan(fovWHalf * Mathf.Deg2Rad) * CAMERA_ASPECT_RATIO;
      var toTop = cam.transform.up * CAMERA_NEAR * Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

      var topLeft = cam.transform.forward * CAMERA_NEAR - toRight + toTop;
      var CAMERA_SCALE = topLeft.magnitude * CAMERA_FAR / CAMERA_NEAR;

      topLeft.Normalize();
      topLeft *= CAMERA_SCALE;

      var topRight = cam.transform.forward * CAMERA_NEAR + toRight + toTop;
      topRight.Normalize();
      topRight *= CAMERA_SCALE;

      var bottomRight = cam.transform.forward * CAMERA_NEAR + toRight - toTop;
      bottomRight.Normalize();
      bottomRight *= CAMERA_SCALE;

      var bottomLeft = cam.transform.forward * CAMERA_NEAR - toRight - toTop;
      bottomLeft.Normalize();
      bottomLeft *= CAMERA_SCALE;

      frustumCorners.SetRow(0, topLeft);
      frustumCorners.SetRow(1, topRight);
      frustumCorners.SetRow(2, bottomRight);
      frustumCorners.SetRow(3, bottomLeft);

      m_material.SetMatrix("_FrustumCorners", frustumCorners);

      var mulCol = Ocean.Ocean.Instance.SunColor() * Mathf.Max(0.0f, Vector3.Dot(Vector3.up, Ocean.Ocean.Instance.SunDir()));
      mulCol = Color.Lerp(Color.white, mulCol, attenuationBySun);

      m_material.SetColor("_MultiplyCol", mulCol);

      var belowTex = RenderTexture.GetTemporary(source.width, source.height, 0);
      CustomGraphicsBlit(source, belowTex, m_material, 0);

      m_imageBlur.BlurIterations = blurIterations;
      m_imageBlur.BlurMode = blurMode;
      m_imageBlur.BlurSpread = blurSpread;
      m_imageBlur.Blur(belowTex);

      m_material.SetTexture("_BelowTex", belowTex);
      Graphics.Blit(source, destination, m_material, 1);

      RenderTexture.ReleaseTemporary(belowTex);
    }

    private bool ShouldRenderEffect()
    {
      if (underWaterPostEffectSdr == null || m_material == null || SystemInfo.graphicsShaderLevel < 30) return false;

      if (Ocean.Ocean.Instance == null || Ocean.Ocean.Instance.UnderWater == null || Ocean.Ocean.Instance.Grid == null) return false;

      if (!Ocean.Ocean.Instance.gameObject.activeInHierarchy) return false;

      if (!Ocean.Ocean.Instance.UnderWater.enabled || !Ocean.Ocean.Instance.Grid.enabled) return false;

      if (Ocean.Ocean.Instance.UnderWater.underwaterMode == UNDERWATER_MODE.ABOVE_ONLY) return false;

      if (!m_underWaterIsVisible) return false;

      return true;
    }

    private void CustomGraphicsBlit(RenderTexture source, RenderTexture dest, Material mat, int pass)
    {
      RenderTexture.active = dest;

      mat.SetTexture("_MainTex", source);

      GL.PushMatrix();
      GL.LoadOrtho();

      mat.SetPass(pass);

      GL.Begin(GL.QUADS);

      //This custom blit is needed as information about what corner verts relate to what frustum corners is needed
      //A index to the frustum corner is store in the z pos of vert

      GL.MultiTexCoord2(0, 0.0f, 0.0f);
      GL.Vertex3(0.0f, 0.0f, 3.0f); // BL

      GL.MultiTexCoord2(0, 1.0f, 0.0f);
      GL.Vertex3(1.0f, 0.0f, 2.0f); // BR

      GL.MultiTexCoord2(0, 1.0f, 1.0f);
      GL.Vertex3(1.0f, 1.0f, 1.0f); // TR

      GL.MultiTexCoord2(0, 0.0f, 1.0f);
      GL.Vertex3(0.0f, 1.0f, 0.0f); // TL

      GL.End();
      GL.PopMatrix();
    }

    private bool UnderWaterIsVisible(Camera cam)
    {
      if (Ocean.Ocean.Instance == null) return false;

      var pos = cam.transform.position;

      if (disableOnClip)
      {
        m_query.posX = pos.x;
        m_query.posZ = pos.z;
        m_query.mode = QUERY_MODE.CLIP_TEST;

        Ocean.Ocean.Instance.QueryWaves(m_query);

        if (m_query.result.isClipped)
          return false;
      }

      var upperRange = Ocean.Ocean.Instance.FindMaxDisplacement(true) + Ocean.Ocean.Instance.level;

      if (pos.y < upperRange)
        return true;

      var ivp = (cam.projectionMatrix * cam.worldToCameraMatrix).inverse;

      for (var i = 0; i < 4; i++)
      {
        var p = ivp * m_corners[i];
        p.y /= p.w;

        if (p.y < upperRange) return true;
      }

      return false;
    }
  }
}