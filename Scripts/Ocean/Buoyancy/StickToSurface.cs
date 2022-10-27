using UnityEngine;

namespace Ceto
{
  /// <summary>
  ///   This just makes the objects origin y height match wave height.
  ///   No buoyancy. I use this to test wave query's are correct but
  ///   if you just need something to sit on surface of water cheaply
  ///   then this can used.
  /// </summary>
  [AddComponentMenu("Ceto/Buoyancy/StickToSurface")]
  public class StickToSurface : MonoBehaviour
  {
    private void Start()
    {
    }


    private void Update()
    {
      if (Ocean.Instance == null) return;

      var pos = transform.position;

      pos.y = Ocean.Instance.QueryWaves(pos.x, pos.z);

      transform.position = pos;
    }
  }
}