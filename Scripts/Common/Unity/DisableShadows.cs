using UnityEngine;

namespace Razomy.Unity.Scripts.Common.Unity
{
  public class DisableShadows : MonoBehaviour
  {
    private float storedShadowDistance;

    private void Start()
    {
    }

    private void OnPostRender()
    {
      QualitySettings.shadowDistance = storedShadowDistance;
    }

    private void OnPreRender()
    {
      storedShadowDistance = QualitySettings.shadowDistance;
      QualitySettings.shadowDistance = 0;
    }
  }
}