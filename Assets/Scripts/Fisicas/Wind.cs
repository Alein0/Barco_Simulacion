using UnityEngine;

public class Wind : MonoBehaviour, IForceGenerator
{
    [Header("Connection")]
    public float ImpactRange = 10f;
    public bool GlobalImpact = false;

    [Header("Parameters")]
    public Vector3 windDirection = Vector3.right;
    public float strength = 5f;
    public float turbulenceIntensity = 2f;
    public float turbulenceFrequency = 1.5f;

    [Header("Random Wind Direction")]
    public bool randomDirection = true;
    public float changeDirectionEvery = 3f;
    public float directionLerpSpeed = 1f;

    private Vector3 targetWindDirection;
    private float directionTimer;

    public Vector3 CurrentWindDirection
    {
        get
        {
            return windDirection.normalized;
        }
    }

    private void Start()
    {
        targetWindDirection = windDirection.normalized;
        directionTimer = changeDirectionEvery;
    }

    private void Update()
    {
        if (!randomDirection)
            return;

        directionTimer -= Time.deltaTime;

        if (directionTimer <= 0f)
        {
            directionTimer = changeDirectionEvery;

            // Dirección aleatoria en el plano XZ (horizontal)
            Vector2 random2D = Random.insideUnitCircle.normalized;
            targetWindDirection = new Vector3(random2D.x, 0f, random2D.y);

            if (targetWindDirection.sqrMagnitude < 0.0001f)
                targetWindDirection = Vector3.right;
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
        Debug.LogWarning($"Wind.OnDisable() llamado. StackTrace:\n{System.Environment.StackTrace}");
        ParticleWorld.Unregister((IForceGenerator)this);
    }

    public void ApplyForces(float dt)
    {
        Vector3 windForceBase = windDirection.normalized * strength;

        float noiseOfTheWind = Mathf.PerlinNoise(Time.time * turbulenceFrequency, 0f);
        float realSensation = noiseOfTheWind * turbulenceIntensity;

        Vector3 windForceRealist = windForceBase + (windDirection.normalized * realSensation);

        if (ParticleWorld.All == null || ParticleWorld.All.Count == 0)
            return;

        foreach (Particle p in ParticleWorld.All)
        {
            if (p == null) continue;

            float distance = Vector3.Distance(transform.position, p.Position);

            if (GlobalImpact || distance <= ImpactRange)
            {
                p.AddForce(windForceRealist);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!GlobalImpact)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, ImpactRange);
        }

        DrawArrow(
            transform.position,
            windDirection.normalized,
            Mathf.Max(2f, strength),
            Color.cyan
        );
    }

    private void DrawArrow(Vector3 start, Vector3 direction, float length, Color color)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Gizmos.color = color;

        Vector3 dir = direction.normalized;
        Vector3 end = start + dir * length;

        Gizmos.DrawLine(start, end);

        Quaternion rotation = Quaternion.LookRotation(dir);
        Vector3 right = rotation * Quaternion.Euler(0f, 160f, 0f) * Vector3.forward;
        Vector3 left = rotation * Quaternion.Euler(0f, 200f, 0f) * Vector3.forward;

        Gizmos.DrawLine(end, end + right * (length * 0.2f));
        Gizmos.DrawLine(end, end + left * (length * 0.2f));
    }
}