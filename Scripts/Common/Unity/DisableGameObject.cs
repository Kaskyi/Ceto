using UnityEngine;

namespace Ceto.Common.Unity.Utility
{
  public class DisableGameObject : MonoBehaviour
  {
    private void Update()
    {
      gameObject.SetActive(false);
    }

    private void OnEnable()
    {
      gameObject.SetActive(false);
    }
  }
}