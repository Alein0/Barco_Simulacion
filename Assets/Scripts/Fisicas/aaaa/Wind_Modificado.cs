using UnityEngine;

public class Wind_Modificado : MonoBehaviour, IForceGenerator
{
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
    [Range(0f, 180f)] public float randomDirectionRange = 180f;
    public float changeDirectionEvery = 3f;
    public float directionLerpSpeed = 1f;

    private Vector3 targetWindDirection;
    private float directionTimer;

    public Vector3 CurrentWindDirection
    {
        get { return windDirection.normalized; }
    }

    private void Start()
    {
        if (initialWindDirection.sqrMagnitude < 0.0001f)
            initialWindDirection = Vector3.left;

        windDirection = initialWindDirection.normalized;
        targetWindDirection = windDirection;
        directionTimer = changeDirectionEvery;

        if (targetParticle == null)
            targetParticle = FindFirstObjectByType<Particle>();
    }

    private void Update()
    {
        if (!randomDirection)
            return;

        directionTimer -= Time.deltaTime;

        if (directionTimer <= 0f)
        {
            directionTimer = changeDirectionEvery;

            float halfRange = Mathf.Clamp(randomDirectionRange, 0f, 180f) * 0.5f;
            float angle = Random.Range(-halfRange, halfRange);

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
        if (!GlobalImpact)
        {
            float horizontalDistance = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(worldPoint.x, 0f, worldPoint.z)
            );

            float horizontal01 = Mathf.InverseLerp(ImpactRange, 0f, horizontalDistance);

            if (horizontal01 <= 0f)
                return 0f;

            if (worldPoint.y > transform.position.y + verticalImpactHeight)
                return 0f;

            return Mathf.Clamp01(horizontal01);
        }

        if (worldPoint.y > transform.position.y + verticalImpactHeight)
            return 0f;

        return 1f;
    }

    public bool IsPointAffected(Vector3 worldPoint)
    {
        return GetWindInfluence01(worldPoint) > 0f;
    }

    public void ApplyForces(float dt)
    {
        if (targetParticle == null)
            return;

        float area01 = GetWindInfluence01(targetParticle.Position);
        if (area01 <= 0f)
            return;

        Vector3 windForceBase = windDirection.normalized * strength;

        float noiseOfTheWind = Mathf.PerlinNoise(Time.time * turbulenceFrequency, 0f);
        float realSensation = noiseOfTheWind * turbulenceIntensity;

        Vector3 windForceRealist = windForceBase + (windDirection.normalized * realSensation);

        Vector3 finalForce = windForceRealist * area01 * forceMultiplier;

        targetParticle.AddForce(finalForce);
    }

    public void SetTargetParticle(Particle particle)
    {
        targetParticle = particle;
    }
}