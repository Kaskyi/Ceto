using UnityEngine;

namespace Ceto
{
  public class ShipCamera : MonoBehaviour
  {
    private const float MAX_ACCELERATION = 1.0f;

    private const float ACCELERATION_RATE = 1.0f;

    private const float DECELERATION_RATE = 0.25f;

    public bool disableInterpolation;

    public bool disableInput;

    public GameObject m_ship;

    public Vector3 m_forward = new(0, 0, 1);

    public float m_shipMoveSpeed = 20.0f;

    public float m_shipTurnSpeed = 2.0f;

    [Range(0.01f, 1.0f)] public float shipSmoothness = 0.5f;

    public float m_camRotationSpeed = 10.0f;

    public float m_camScrollSpeed = 1.0f;

    public float m_camStartRotationX = 180.0f;

    public float m_camStartRotationY = 60.0f;

    public float m_camStartDistance = 100.0f;

    [Range(0.01f, 1.0f)] public float camSmoothness = 0.5f;

    private float m_acceleration;

    private GameObject m_dummy;

    private Position m_position, m_target;

    private Vector3 m_previousPos, m_velocity;

    private GameObject Ship
    {
      get
      {
        if (m_ship == null)
        {
          m_dummy = new GameObject();
          m_ship = m_dummy;
        }

        return m_ship;
      }
    }

    private void Start()
    {
      m_position.camRotation.x = m_camStartRotationX;
      m_position.camRotation.y = m_camStartRotationY;
      m_position.camDistance = m_camStartDistance;
      m_position.forwardAmount = Vector3.zero;

      m_target = m_position;

      m_previousPos = Ship.transform.position;
    }

    private void LateUpdate()
    {
      ProcessInput();

      InterpolateToTarget();

      MoveShip();
    }

    private void OnDestroy()
    {
      if (m_dummy != null)
        DestroyImmediate(m_dummy);
    }

    private void MoveShip()
    {
      Ship.transform.position += m_position.forwardAmount;

      var eulerAngles = Ship.transform.eulerAngles;

      eulerAngles.y += m_position.turnAmount;
      Ship.transform.eulerAngles = eulerAngles;

      var ct = Mathf.Cos(m_position.camRotation.y * Mathf.Deg2Rad);
      var st = Mathf.Sin(m_position.camRotation.y * Mathf.Deg2Rad);
      var cp = Mathf.Cos(m_position.camRotation.x * Mathf.Deg2Rad);
      var sp = Mathf.Sin(m_position.camRotation.x * Mathf.Deg2Rad);

      var lookAt = Ship.transform.position;
      var pos = lookAt + new Vector3(sp * st, ct, cp * st) * m_position.camDistance;

      transform.position = pos;
      transform.LookAt(lookAt);

      m_velocity = Ship.transform.position - m_previousPos;
      m_previousPos = Ship.transform.position;
    }

    private void InterpolateToTarget()
    {
      if (disableInterpolation || Time.timeScale == 0.0f)
      {
        m_position = m_target;
        return;
      }

      float smoothness;

      smoothness = 1.0f / Mathf.Clamp(camSmoothness, 0.01f, 1.0f);
      var camLerp = Mathf.Clamp01(Time.deltaTime * smoothness);

      smoothness = 1.0f / Mathf.Clamp(shipSmoothness, 0.01f, 1.0f);
      var shipLerp = Mathf.Clamp01(Time.deltaTime * smoothness);

      m_position.camDistance = Mathf.Lerp(m_position.camDistance, m_target.camDistance, camLerp);
      m_position.camRotation = Vector2.Lerp(m_position.camRotation, m_target.camRotation, camLerp);

      m_position.forwardAmount = Vector3.Lerp(m_position.forwardAmount, m_target.forwardAmount, shipLerp);
      m_position.turnAmount = Mathf.Lerp(m_position.turnAmount, m_target.turnAmount, shipLerp);
    }

    private void ProcessInput()
    {
      if (disableInput) return;

      var speed = m_shipMoveSpeed;
      var velocity = m_velocity.magnitude;

      m_target.forwardAmount = Vector3.zero;
      m_target.turnAmount = 0.0f;

      //move left
      if (Input.GetKey(KeyCode.A))
      {
        var deg = speed * velocity * m_shipTurnSpeed;

        m_target.turnAmount -= deg * Time.deltaTime;
        m_target.camRotation.x -= deg * Time.deltaTime;
      }

      //move right
      if (Input.GetKey(KeyCode.D))
      {
        var deg = speed * velocity * m_shipTurnSpeed;

        m_target.turnAmount += deg * Time.deltaTime;
        m_target.camRotation.x += deg * Time.deltaTime;
      }

      Vector3 forward = Ship.transform.localToWorldMatrix * m_forward;
      forward.Normalize();

      //move forward
      if (Input.GetKey(KeyCode.W))
        m_acceleration += Time.deltaTime * ACCELERATION_RATE;
      else
        m_acceleration -= Time.deltaTime * DECELERATION_RATE;

      m_acceleration = Mathf.Clamp(m_acceleration, 0.0f, MAX_ACCELERATION);

      m_target.forwardAmount += forward * speed * m_acceleration * Time.deltaTime;

      var dt = Time.deltaTime * 1000.0f * m_camScrollSpeed;
      var amount = Mathf.Pow(1.02f, Mathf.Min(dt, 1.0f));

      if (Input.GetAxis("Mouse ScrollWheel") < 0.0f)
        m_target.camDistance *= amount;
      else if (Input.GetAxis("Mouse ScrollWheel") > 0.0f) m_target.camDistance /= amount;

      m_target.camDistance = Mathf.Max(1.0f, m_target.camDistance);
      m_target.camRotation.y = Mathf.Clamp(m_target.camRotation.y, 20.0f, 160.0f);

      if (Input.GetMouseButton(0))
      {
        m_target.camRotation.y += Input.GetAxis("Mouse Y") * m_camRotationSpeed;
        m_target.camRotation.x += Input.GetAxis("Mouse X") * m_camRotationSpeed;
      }
    }

    private struct Position
    {
      public Vector3 forwardAmount;
      public float turnAmount;
      public Vector2 camRotation;
      public float camDistance;
    }
  }
}