using UnityEngine;

namespace Razomy.Unity.Scripts.Common.Unity
{
  public class Wireframe : MonoBehaviour
  {
    public bool on;

    public KeyCode toggleKey = KeyCode.F2;

    private void Start()
    {
    }

    private void Update()
    {
      if (Input.GetKeyDown(toggleKey)) on = !on;
    }

    private void OnPostRender()
    {
      if (on)
        GL.wireframe = false;
    }

    private void OnPreRender()
    {
      if (on)
        GL.wireframe = true;
    }
  }
}