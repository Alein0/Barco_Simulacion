using UnityEngine;

public class Buoyancy : MonoBehaviour, IForceGenerator
{
    [Header("Conexión")]
    public Transform waterPlane;

    [Header("Parámetros")]
    public float waterLevel = 0f;
    public float density = 1000f;
    public float epsilon = 0.01f;

    private void OnEnable()
    {
        ParticleWorld.Register(this);
    }

    private void OnDisable()
    {
        ParticleWorld.Unregister(this);
    }

    public void ApplyForces(float dt)
    {
        if (waterPlane != null)
            waterLevel = waterPlane.position.y;

        foreach (Particle particle in ParticleWorld.All)
        {
            float radius = particle.Radius;


            float depth = waterLevel - particle.Position.y;

            if (Mathf.Abs(depth) < epsilon)
                depth = 0f;


            if (depth <= -radius)
                continue;


            float totalVolume = (4f / 3f) * Mathf.PI * radius * radius * radius;

            float submergedVolume;


            if (depth >= radius)
            {
                submergedVolume = totalVolume;
            }

            else
            {
                float h = depth + radius;
                submergedVolume = (Mathf.PI * h * h * (3f * radius - h)) / 3f;
            }

            if (submergedVolume < epsilon)
                continue;


            Vector3 buoyancyDirection = -particle.gravity.normalized;


            float buoyancyMagnitude = density * submergedVolume * particle.gravity.magnitude;

            Vector3 buoyancyForce = buoyancyDirection * buoyancyMagnitude;
            particle.AddForce(buoyancyForce);
        }
    }
}