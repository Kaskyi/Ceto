using UnityEngine;

namespace Razomy.Unity.Scripts.Common.Unity
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