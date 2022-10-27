using UnityEngine;

namespace Ceto.Common.Unity.Utility
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