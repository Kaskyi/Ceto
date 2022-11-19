using UnityEngine;

namespace Razomy.Unity.Scripts.Common.Unity
{
  public class DisableFog : MonoBehaviour
  {
    private bool revertFogState;

    private void Start()
    {
    }

    private void OnPostRender()
    {
      RenderSettings.fog = revertFogState;
    }

    private void OnPreRender()
    {
      revertFogState = RenderSettings.fog;
      RenderSettings.fog = false;
    }
  }
}