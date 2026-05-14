using UnityEngine;

public class Buoyancy : MonoBehaviour, IForceGenerator
{
    [Header("Parámetros de flotación")]
    [Min(0f)] public float waterLevel = 1f;
    [Min(0f)] public float density = 1f; // kg/m³ (agua típicamente 1000)
    [Min(0f)] public float drag = 0.5f; // Resistencia del agua (opcional)

    [Header("Física")]
    private float gravity = 9.81f;
    private Plane waterPlane;

    private void OnEnable()
    {
        ParticleWorld.Register((IForceGenerator)this);
        UpdateWaterPlane();
    }

    private void OnDisable()
    {
        ParticleWorld.Unregister((IForceGenerator)this);
    }

    private void OnValidate()
    {
        UpdateWaterPlane();
    }

    private void UpdateWaterPlane()
    {
        // Define un plano horizontal en y = waterLevel con normal hacia arriba
        waterPlane = new Plane(Vector3.up, new Vector3(0, waterLevel, 0));
    }

    public void ApplyForces(float dt)
    {
        if (ParticleWorld.All == null) return;

        foreach (Particle particle in ParticleWorld.All)
        {
            if (particle == null) continue;

            float depth = waterLevel - particle.Position.y;

            // Solo aplicar fuerza si la partícula está debajo del agua
            if (depth <= 0f) continue;

            // Volumen sumergido aproximado: profundidad × sección transversal
            // Simplificación: asumimos que el volumen es proporcional a la profundidad
            float scale = particle.Mass; // Altura de la partícula
            float submergedVolume = depth * particle.Mass * particle.Mass;

            // Fuerza de flotación: F_buoyancy = density * V_submerged * g (hacia arriba)
            float buoyancyForce = density * submergedVolume * gravity;

            // Aplicar fuerza hacia arriba (positivo Y)
            particle.AddForce(Vector3.up * buoyancyForce);

            // Resistencia del agua: proporcional a la velocidad (amortiguamiento)
            // F_drag = -drag * velocity (opone el movimiento)
            Vector3 dragForce = -drag * particle.Velocity;
            particle.AddForce(dragForce);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Dibuja el nivel del agua en la escena
        float range = 10f;
        Vector3 center = transform.position;
        Vector3 cornerA = center + new Vector3(-range, waterLevel, -range);
        Vector3 cornerB = center + new Vector3(range, waterLevel, -range);
        Vector3 cornerC = center + new Vector3(range, waterLevel, range);
        Vector3 cornerD = center + new Vector3(-range, waterLevel, range);

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawLine(cornerA, cornerB);
        Gizmos.DrawLine(cornerB, cornerC);
        Gizmos.DrawLine(cornerC, cornerD);
        Gizmos.DrawLine(cornerD, cornerA);
    }
}
