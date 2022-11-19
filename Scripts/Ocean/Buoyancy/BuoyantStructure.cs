using UnityEngine;

namespace Razomy.Unity.Scripts.Ocean.Buoyancy
{
  [AddComponentMenu("Ceto/Buoyancy/BuoyantStructure")]
  public class BuoyantStructure : MonoBehaviour
  {
    public float maxAngularVelocity = 0.05f;

    private Buoyancy[] m_buoyancy;

    private void Start()
    {
      m_buoyancy = GetComponentsInChildren<Buoyancy>();

      var count = m_buoyancy.Length;
      for (var i = 0; i < count; i++)
        m_buoyancy[i].PartOfStructure = true;
    }

    private void FixedUpdate()
    {
      var body = GetComponent<Rigidbody>();

      if (body == null)
        body = gameObject.AddComponent<Rigidbody>();

      var mass = 0.0f;

      var count = m_buoyancy.Length;
      for (var i = 0; i < count; i++)
      {
        if (!m_buoyancy[i].enabled) continue;

        m_buoyancy[i].UpdateProperties();
        mass += m_buoyancy[i].Mass;
      }

      body.mass = mass;

      var pos = transform.position;
      var force = Vector3.zero;
      var torque = Vector3.zero;

      for (var i = 0; i < count; i++)
      {
        if (!m_buoyancy[i].enabled) continue;

        m_buoyancy[i].UpdateForces(body);

        var p = m_buoyancy[i].transform.position;
        var f = m_buoyancy[i].TotalForces;
        var r = p - pos;

        force += f;
        torque += Vector3.Cross(r, f);
      }

      body.maxAngularVelocity = maxAngularVelocity;
      body.AddForce(force);
      body.AddTorque(torque);
    }
  }
}