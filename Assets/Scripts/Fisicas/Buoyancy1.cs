using UnityEngine;

public class Buoyancy1 : MonoBehaviour, IForceGenerator
{
    [Header("Referencia océano")]
    public OceanSurface1 ocean;

    [Header("Objetivo")]
    [Tooltip("Si está asignado, solo aplicará flotabilidad a esta partícula.")]
    public Particle targetParticle;

    [Header("Flotabilidad")]
    public float density = 1000f;

    [Tooltip("Resistencia lineal del agua")]
    public float waterDrag = 1.5f;

    [Tooltip("Resistencia angular básica")]
    public float waterAngularDrag = 0.2f;

    [Header("Puntos de flotación")]
    [Tooltip("Puntos locales donde se evalúa el agua.")]
    public Vector3[] localFloatPoints = new Vector3[]
    {
        new Vector3( 1f, 0f,  1f),
        new Vector3(-1f, 0f,  1f),
        new Vector3( 1f, 0f, -1f),
        new Vector3(-1f, 0f, -1f)
    };

    [Tooltip("Separación entre puntos")]
    public float floatPointSpread = 1f;

    [Header("Suavizado")]
    public float buoyancyStrength = 1f;
    public float surfaceOffset = 0f;
    public float epsilon = 0.01f;

    [Header("Estabilidad")]
    public bool applyWaterNormalAlignment = true;

    [Tooltip("Fuerza para alinearse con el agua")]
    public float alignmentTorque = 0.8f;

    [Header("Suavizado Angular")]

    [Tooltip("Frena lentamente los giros")]
    public float angularDamping = 2.0f;

    [Tooltip("Evita vibraciones pequeñas")]
    public float angularDeadZone = 0.02f;

    [Tooltip("Limita velocidad angular máxima")]
    public float maxAngularSpeed = 0.8f;

    [Tooltip("Qué tan calmado se estabiliza")]
    [Range(0.1f, 1f)]
    public float stabilizationSmoothness = 0.35f;

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

    private void Awake()
    {
        // Buscar océano automáticamente
        if (ocean == null)
        {
            ocean = FindFirstObjectByType<OceanSurface1>();
        }

        if (ocean == null)
        {
            Debug.LogWarning(
                "No se encontró un OceanSurface1 en la escena."
            );
        }
    }

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

            // Solo objetivo específico
            if (targetParticle != null &&
                particle != targetParticle)
                continue;

            Vector3[] points =
                (localFloatPoints != null &&
                 localFloatPoints.Length > 0)
                    ? localFloatPoints
                    : DefaultFloatPoints;

            float pointCount =
                Mathf.Max(1, points.Length);

            float totalVolume =
                (4f / 3f) *
                Mathf.PI *
                particle.Radius *
                particle.Radius *
                particle.Radius;

            float volumePerPoint =
                totalVolume / pointCount;

            float gravityMagnitude =
                particle.gravity.magnitude;

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 localPoint =
                    points[i] *
                    floatPointSpread *
                    particle.Radius;

                Vector3 worldPoint =
                    particle.Position +
                    particle.Rotation * localPoint;

                // Altura del agua
                float waterHeight =
                    ocean.GetWaveHeight(worldPoint) +
                    surfaceOffset;

                float depth =
                    waterHeight - worldPoint.y;

                if (Mathf.Abs(depth) < epsilon)
                    depth = 0f;

                // Punto fuera del agua
                if (depth <= 0f)
                    continue;

                // Nivel de sumersión
                float submergedFraction =
                    Mathf.Clamp01(
                        depth /
                        (particle.Radius * 2f)
                    );

                // =========================
                // FLOTABILIDAD
                // =========================

                float buoyancyMagnitude =
                    density *
                    volumePerPoint *
                    gravityMagnitude *
                    submergedFraction *
                    buoyancyStrength;

                Vector3 buoyancyForce =
                    Vector3.up *
                    buoyancyMagnitude;

                particle.AddForceAtPoint(
                    buoyancyForce,
                    worldPoint
                );

                // =========================
                // DRAG LINEAL
                // =========================

                Vector3 pointVelocity =
                    particle.GetPointVelocity(
                        worldPoint
                    );

                Vector3 dragForce =
                    -pointVelocity *
                    waterDrag *
                    submergedFraction;

                particle.AddForceAtPoint(
                    dragForce,
                    worldPoint
                );

                // =========================
                // SUAVIZADO ANGULAR
                // =========================

                Vector3 angularVel =
                    particle.AngularVelocity;

                // Elimina vibraciones pequeñas
                if (angularVel.magnitude <
                    angularDeadZone)
                {
                    particle.AngularVelocity =
                        Vector3.zero;
                }
                else
                {
                    // Frenado suave
                    Vector3 dampingTorque =
                        -angularVel *
                        angularDamping *
                        submergedFraction;

                    particle.AddTorque(
                        dampingTorque *
                        stabilizationSmoothness
                    );

                    // Drag angular extra
                    particle.AddTorque(
                        -angularVel *
                        waterAngularDrag *
                        submergedFraction *
                        0.5f
                    );

                    // Limitar velocidad angular
                    if (particle.AngularVelocity.magnitude >
                        maxAngularSpeed)
                    {
                        particle.AngularVelocity =
                            particle.AngularVelocity.normalized *
                            maxAngularSpeed;
                    }
                }

                // =========================
                // ALINEACIÓN SUAVE
                // =========================

                if (applyWaterNormalAlignment)
                {
                    Vector3 waterNormal =
                        ocean.GetWaveNormal(
                            worldPoint
                        );

                    Vector3 currentUp =
                        particle.Rotation *
                        Vector3.up;

                    Vector3 torqueAxis =
                        Vector3.Cross(
                            currentUp,
                            waterNormal
                        );

                    Vector3 smoothTorque =
                        torqueAxis *
                        alignmentTorque *
                        submergedFraction *
                        stabilizationSmoothness;

                    particle.AddTorque(
                        smoothTorque
                    );
                }

                // =========================
                // DEBUG
                // =========================

                lastForcePosition =
                    worldPoint;

                lastForceDirection =
                    buoyancyForce.sqrMagnitude >
                    0.000001f
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

        Gizmos.DrawRay(
            lastForcePosition,
            lastForceDirection * 3f
        );
    }
}