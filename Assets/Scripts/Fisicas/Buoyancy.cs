using UnityEngine;

public class Buoyancy : MonoBehaviour, IForceGenerator
{
    [Header("Referencia océano")]
    public OceanSurface ocean;

    [Header("Objetivo")]
    [Tooltip("Si está asignado, solo aplicará flotabilidad a esta partícula. Si queda vacío, aplicará a todas.")]
    public Particle targetParticle;

    [Header("Flotabilidad")]
    public float density = 1000f;
    public float waterDrag = 1.5f;
    public float waterAngularDrag = 0.5f;

    [Header("Puntos de flotación")]
    [Tooltip("Puntos locales donde se evalúa el agua. Si está vacío, usa cuatro puntos por defecto.")]
    public Vector3[] localFloatPoints = new Vector3[]
    {
        new Vector3( 1f, 0f,  1f),
        new Vector3(-1f, 0f,  1f),
        new Vector3( 1f, 0f, -1f),
        new Vector3(-1f, 0f, -1f)
    };

    [Tooltip("Escala de separación entre puntos de flotación.")]
    public float floatPointSpread = 1f;

    [Header("Suavizado")]
    public float buoyancyStrength = 1f;
    public float surfaceOffset = 0f;
    public float epsilon = 0.01f;

    [Header("Estabilidad")]
    public bool applyWaterNormalAlignment = true;
    public float alignmentTorque = 2f;

    [Header("Debug")]
    public bool showForceGizmo = true;

    private Vector3 lastForcePosition;
    private Vector3 lastForceDirection;

    private static readonly Vector3[] DefaultFloatPoints =
    {
        new Vector3( 1f, 0f,  1f),
        new Vector3(-1f, 0f,  1f),
        new Vector3( 1f, 0f, -1f),
        new Vector3(-1f, 0f, -1f)
    };

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
        if (ocean == null)
            return;

        var particles = ParticleWorld.All;

        for (int p = 0; p < particles.Count; p++)
        {
            Particle particle = particles[p];

            if (targetParticle != null && particle != targetParticle)
                continue;

            Vector3[] points =
                (localFloatPoints != null && localFloatPoints.Length > 0)
                    ? localFloatPoints
                    : DefaultFloatPoints;

            float pointCount = Mathf.Max(1, points.Length);
            float totalVolume =
                (4f / 3f) * Mathf.PI *
                particle.Radius * particle.Radius * particle.Radius;

            float volumePerPoint = totalVolume / pointCount;
            float gravityMagnitude = particle.gravity.magnitude;

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 localPoint = points[i] * floatPointSpread * particle.Radius;
                Vector3 worldPoint = particle.Position + particle.Rotation * localPoint;

                float waterHeight = ocean.GetWaveHeight(worldPoint) + surfaceOffset;
                float depth = waterHeight - worldPoint.y;

                if (Mathf.Abs(depth) < epsilon)
                    depth = 0f;

                if (depth <= 0f)
                    continue;

                // 0 = apenas toca el agua, 1 = completamente "sumergido" en ese punto
                float submergedFraction = Mathf.Clamp01(depth / (particle.Radius * 2f));

                // Fuerza de flotación por punto
                float buoyancyMagnitude =
                    density *
                    volumePerPoint *
                    gravityMagnitude *
                    submergedFraction *
                    buoyancyStrength;

                Vector3 buoyancyForce = Vector3.up * buoyancyMagnitude;
                particle.AddForceAtPoint(buoyancyForce, worldPoint);

                // Drag del agua en el punto
                Vector3 pointVelocity = particle.GetPointVelocity(worldPoint);
                Vector3 dragForce = -pointVelocity * waterDrag * submergedFraction;
                particle.AddForceAtPoint(dragForce, worldPoint);

                // Suaviza giro excesivo en el agua
                particle.AddTorque(-particle.AngularVelocity * waterAngularDrag * submergedFraction);

                // Alineación suave con la normal del agua
                if (applyWaterNormalAlignment)
                {
                    Vector3 waterNormal = ocean.GetWaveNormal(worldPoint);
                    Vector3 currentUp = particle.Rotation * Vector3.up;

                    Vector3 torqueAxis = Vector3.Cross(currentUp, waterNormal);
                    particle.AddTorque(torqueAxis * alignmentTorque * submergedFraction);
                }

                lastForcePosition = worldPoint;
                lastForceDirection = buoyancyForce.sqrMagnitude > 0.000001f
                    ? buoyancyForce.normalized
                    : Vector3.up;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showForceGizmo)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(lastForcePosition, lastForceDirection * 3f);
    }
}