﻿using UnityEngine;

namespace Ceto
{
  /// <summary>
  ///   My hack of a buoyancy script.
  ///   This is just for the demo.
  ///   Dont use this script. I have no idea what I am doing.
  /// </summary>
  [AddComponentMenu("Ceto/Buoyancy/Buoyancy")]
  public class Buoyancy : MonoBehaviour
  {
    public enum MASS_UNIT
    {
      KILOGRAMS,
      TENS_OF_KILOGRAMS,
      TONNES,
      TENS_OF_TONNES
    }

    public float radius = 0.5f;

    [Range(100.0f, 10000.0f)] public float density = 400.0f;

    [Range(0.0f, 100.0f)] public float stickyness = 0.1f;

    public MASS_UNIT unit = MASS_UNIT.TENS_OF_TONNES;

    public float dragCoefficient = 0.3f;

    private readonly float DENSITY_WATER = 999.97f;

    public bool PartOfStructure { get; set; }

    public float Volume { get; private set; }

    public float SubmergedVolume { get; private set; }

    public float PercentageSubmerged => SubmergedVolume / Volume;

    public float SurfaceArea { get; private set; }

    public float Mass { get; private set; }

    public float WaterHeight { get; private set; }

    public Vector3 BuoyantForce { get; private set; }

    public Vector3 DragForce { get; private set; }

    public Vector3 Stickyness { get; private set; }

    public Vector3 TotalForces => BuoyantForce + DragForce + Stickyness;

    private void Start()
    {
      UpdateProperties();
    }

    private void FixedUpdate()
    {
      if (PartOfStructure) return;

      var body = GetComponent<Rigidbody>();

      if (body == null)
        body = gameObject.AddComponent<Rigidbody>();

      body.mass = Mass;

      UpdateProperties();
      UpdateForces(body);

      body.AddForce(TotalForces);
    }

    private void OnDrawGizmos()
    {
      if (!enabled) return;

      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(transform.position, radius);
    }

    public void UpdateProperties()
    {
      Volume = 4.0f / 3.0f * Mathf.PI * Mathf.Pow(radius, 3);

      Mass = Volume * density * GetUnitScale();

      SurfaceArea = 4.0f * Mathf.PI * Mathf.Pow(radius, 2);
    }

    public void UpdateForces(Rigidbody body)
    {
      if (Ocean.Instance == null)
      {
        BuoyantForce = Vector3.zero;
        DragForce = Vector3.zero;
        Stickyness = Vector3.zero;
        return;
      }

      var pos = transform.position;

      WaterHeight = Ocean.Instance.QueryWaves(pos.x, pos.z);

      CalculateSubmersion(radius, pos.y);

      var unitScale = GetUnitScale();

      var Fb = DENSITY_WATER * unitScale * SubmergedVolume;

      BuoyantForce = Physics.gravity * -Fb;

      var velocity = body.velocity;

      var vm = velocity.magnitude;
      velocity = velocity.normalized * vm * vm * -1.0f;

      DragForce = 0.5f * dragCoefficient * DENSITY_WATER * unitScale * SubmergedVolume * velocity;

      //Cant get the ship to stay level on the surface so added this hack.
      //This is not a good idea.
      Stickyness = Vector3.up * (WaterHeight - pos.y) * Mass * stickyness;
    }

    private void CalculateSubmersion(float r, float y)
    {
      var h = WaterHeight - (y - radius);

      var d = 2.0f * r - h;

      if (d <= 0.0f)
      {
        SubmergedVolume = Volume;
        return;
      }

      if (d > 2.0f * r)
      {
        SubmergedVolume = 0.0f;
        return;
      }

      var c = Mathf.Sqrt(h * d);

      SubmergedVolume = Mathf.PI / 6.0f * h * (3.0f * c * c + h * h);
    }

    private float GetUnitScale()
    {
      switch ((int)unit)
      {
        case (int)MASS_UNIT.KILOGRAMS:
          return 1.0f;

        case (int)MASS_UNIT.TENS_OF_KILOGRAMS:
          return 0.1f;

        case (int)MASS_UNIT.TONNES:
          return 0.001f;

        case (int)MASS_UNIT.TENS_OF_TONNES:
          return 0.0001f;
      }

      return 1.0f;
    }
  }
}