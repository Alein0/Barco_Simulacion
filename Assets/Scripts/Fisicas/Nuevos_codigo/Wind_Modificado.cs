using System;
using UnityEngine;

public class Wind_Modificado : MonoBehaviour, IForceGenerator
{
    public static event Action<Vector3> OnWindDirectionChanged;

    [Header("Connection")]
    public float ImpactRange = 10f;
    public bool GlobalImpact = false;

    [Header("Vertical Wind Area")]
    [SerializeField] private float verticalImpactHeight = 2f;

    [Header("Target Particle")]
    [SerializeField] private Particle targetParticle;

    [Header("Parameters")]
    [SerializeField] private Vector3 initialWindDirection = new Vector3(-1f, 0f, 0f);
    public Vector3 windDirection = Vector3.left;
    public float strength = 5f;
    public float forceMultiplier = 10000f;
    public float turbulenceIntensity = 2f;
    public float turbulenceFrequency = 1.5f;

    [Header("Random Wind Direction")]
    public bool randomDirection = true;
    [Range(0f, 180f)]
    public float randomDirectionRange = 180f;
    public float changeDirectionEvery = 3f;
    public float directionLerpSpeed = 1f;

    [Header("3D Wind Arrow")]
    [SerializeField] private Transform windArrow;
    [SerializeField] private bool showWindArrow = true;
    [SerializeField] private float arrowHeight = 2f;
    [SerializeField] private float arrowDistance = 1.5f;
    [SerializeField] private bool invertArrowDirection = false;

    // Runtime
    private Vector3 targetWindDirection;
    private float directionTimer;
    private Vector3 lastNotifiedDirection;

    public Vector3 CurrentWindDirection => windDirection.sqrMagnitude > 0.0001f ? windDirection.normalized : Vector3.left;

    private void Start()
    {
        if (initialWindDirection.sqrMagnitude < 0.0001f)
            initialWindDirection = Vector3.left;

        windDirection = initialWindDirection.normalized;
        targetWindDirection = windDirection;
        directionTimer = changeDirectionEvery;

        lastNotifiedDirection = windDirection.normalized;

        if (targetParticle == null)
            targetParticle = FindFirstObjectByType<Particle>();

        UpdateWindArrow();
        OnWindDirectionChanged?.Invoke(CurrentWindDirection);
    }

    private void Update()
    {
        if (randomDirection)
        {
            directionTimer -= Time.deltaTime;
            if (directionTimer <= 0f)
            {
                directionTimer = changeDirectionEvery;
                float halfRange = Mathf.Clamp(randomDirectionRange, 0f, 180f) * 0.5f;
                float angle = UnityEngine.Random.Range(-halfRange, halfRange);

                Vector3 baseDir = windDirection.sqrMagnitude < 0.0001f
                    ? initialWindDirection.normalized
                    : windDirection.normalized;

                targetWindDirection = Quaternion.AngleAxis(angle, Vector3.up) * baseDir;

                if (targetWindDirection.sqrMagnitude < 0.0001f)
                    targetWindDirection = initialWindDirection.normalized;
            }

            windDirection = Vector3.Slerp(
                windDirection.normalized,
                targetWindDirection.normalized,
                Time.deltaTime * directionLerpSpeed
            );
        }

        Vector3 currentNormalized = CurrentWindDirection;
        if (Vector3.Distance(currentNormalized, lastNotifiedDirection) > 0.001f)
        {
            lastNotifiedDirection = currentNormalized;
            OnWindDirectionChanged?.Invoke(currentNormalized);
        }

        UpdateWindArrow();
    }

    private void UpdateWindArrow()
    {
        if (!showWindArrow || windArrow == null)
            return;

        Vector3 dir = CurrentWindDirection;

        if (invertArrowDirection)
            dir = -dir;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        // Posición de la flecha sobre el objeto del viento
        windArrow.position = transform.position + Vector3.up * arrowHeight;

        // Rotación según la dirección del viento
        windArrow.rotation = Quaternion.LookRotation(dir, Vector3.up);

        // Opcional: mover un poco la flecha hacia adelante
        windArrow.position += dir * arrowDistance;
    }

    private void OnEnable()
    {
        ParticleWorld.Register((IForceGenerator)this);
    }

    private void OnDisable()
    {
        ParticleWorld.Unregister((IForceGenerator)this);
    }

    public float GetWindInfluence01(Vector3 worldPoint)
    {
        // Vertical check
        if (worldPoint.y > transform.position.y + verticalImpactHeight) return 0f;

        if (!GlobalImpact)
        {
            Vector3 worldPosXZ = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 pointXZ = new Vector3(worldPoint.x, 0f, worldPoint.z);

            float horizontalDistance = Vector3.Distance(worldPosXZ, pointXZ);
            float horizontal01 = Mathf.InverseLerp(ImpactRange, 0f, horizontalDistance);

            if (horizontal01 <= 0f) return 0f;
            return Mathf.Clamp01(horizontal01);
        }

        return 1f;
    }

    public bool IsPointAffected(Vector3 worldPoint)
    {
        return GetWindInfluence01(worldPoint) > 0f;
    }

    public void ApplyForces(float dt)
    {
        if (targetParticle == null) return;

        float area01 = GetWindInfluence01(targetParticle.Position);
        if (area01 <= 0f) return;

        Vector3 windBase = CurrentWindDirection * strength;
        float noise = Mathf.PerlinNoise(Time.time * turbulenceFrequency, 0f);
        float gust = noise * turbulenceIntensity;
        Vector3 windReal = windBase + (CurrentWindDirection * gust);

        Vector3 finalForce = windReal * area01 * forceMultiplier;
        targetParticle.AddForce(finalForce);
    }

    public void SetTargetParticle(Particle particle)
    {
        targetParticle = particle;
    }
}