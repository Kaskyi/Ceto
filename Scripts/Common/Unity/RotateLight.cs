using UnityEngine;

namespace Razomy.Unity.Scripts.Common.Unity
{
  public class RotateLight : MonoBehaviour
  {
    public float speed = 50.0f;

    public Vector3 axis = new(1, 0, 0);

    public KeyCode decrementKey = KeyCode.KeypadMinus;

    public KeyCode incrementKey = KeyCode.KeypadPlus;

    // Use this for initialization
    private void Start()
    {
    }

    private void Update()
    {
      var dt = Time.deltaTime * speed;

      var v = new Vector3(dt, dt, dt);

      if (Input.GetKey(decrementKey))
      {
        v.x *= -axis.x;
        v.y *= -axis.y;
        v.z *= -axis.z;

        transform.Rotate(v);
      }

      if (Input.GetKey(incrementKey))
      {
        v.x *= axis.x;
        v.y *= axis.y;
        v.z *= axis.z;

        transform.Rotate(v);
      }
    }
  }
}