using UnityEngine;

namespace Razomy.Unity.Scripts.Utility
{
  [RequireComponent(typeof(Camera))]
  public class AddRenderTarget : MonoBehaviour
  {
    public int scale = 2;

    private void Start()
    {
      var cam = GetComponent<Camera>();

      cam.targetTexture = new RenderTexture(Screen.width / scale, Screen.height / scale, 24);
    }

    private void OnGUI()
    {
      var cam = GetComponent<Camera>();

      if (cam.targetTexture == null) return;

      var width = cam.targetTexture.width;
      var height = cam.targetTexture.height;

      GUI.DrawTexture(new Rect(10, 10, width, height), cam.targetTexture);
    }
  }
}