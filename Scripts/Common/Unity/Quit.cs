using UnityEngine;

namespace Razomy.Unity.Scripts.Common.Unity
{
  public class Quit : MonoBehaviour
  {
    public KeyCode quitKey = KeyCode.Escape;

    private void OnGUI()
    {
      if (Input.GetKeyDown(quitKey))
        Application.Quit();
    }
  }
}