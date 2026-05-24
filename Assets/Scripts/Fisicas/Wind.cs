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

    public Vector3 CurrentWindDirection
    {
        get
        {
            return windDirection.normalized;
        }
    }

    private void OnEnable()
    {
        ParticleWorld.Register((IForceGenerator)this);
    }

    private void OnDisable()
    {
        ParticleWorld.Unregister((IForceGenerator)this);
    }

    public void ApplyForces(float dt)
    {
        Vector3 windForceBase = windDirection.normalized * strength;

        float noiseOfTheWind = Mathf.PerlinNoise(Time.time * turbulenceFrequency, 0f);
        float realSensation = noiseOfTheWind * turbulenceIntensity;

        Vector3 windForceRealist = windForceBase + (windDirection.normalized * realSensation);

        foreach (Particle p in ParticleWorld.All)
        {
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