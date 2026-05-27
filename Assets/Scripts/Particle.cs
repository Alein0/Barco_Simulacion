using UnityEngine;

public class Particle : MonoBehaviour
{
    [Header("Initial Conditions")]
    public Vector3 initialVelocity = Vector3.zero;

    [Header("Physical Parameters")]
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    [Min(0f)] public float dragCoefficient = 0.1f;
    [Min(0.0001f)] public float massFactor = 1f;
    [Range(0f, 1f)] public float restitution = 0.8f;
    [Range(0f, 1f)] public float friction = 0.2f;

    [Header("Angular Physics")]
    [Min(0f)] public float angularDrag = 0.5f;

    [Header("Collision")]
    public LayerMask collisionMask;
    [Min(0f)] public float skin = 0.001f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private Vector3 position;
    private Vector3 velocity;
    private Vector3 accumulatedForce;

    private Quaternion rotation;
    private Vector3 angularVelocity;
    private Vector3 accumulatedTorque;

    private float mass;
    private float radius;
    private float inertia;

    private void OnEnable()
    {
        ParticleWorld.Register(this);
    }

    private void OnDisable()
    {
        ParticleWorld.Unregister(this);
    }

    private void OnValidate()
    {
        ComputeMass();
        ComputeRadius();
        ComputeInertia();
    }

    private void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void Start()
    {
        ResetParticle();
    }

    public void Integrate(float dt)
    {
        // Linear
        Vector3 dragForce = -dragCoefficient * velocity;
        Vector3 acceleration = gravity + (accumulatedForce + dragForce) / mass;

        velocity += acceleration * dt;

        // Angular
        Vector3 angularAcceleration = accumulatedTorque / inertia;
        angularVelocity += angularAcceleration * dt;
        angularVelocity *= Mathf.Clamp01(1f - angularDrag * dt);

        // Update rotation from angular velocity
        if (angularVelocity.sqrMagnitude > 0.000001f)
        {
            Quaternion deltaRotation =
                Quaternion.Euler(angularVelocity * Mathf.Rad2Deg * dt);

            rotation = deltaRotation * rotation;
        }

        // Clear accumulators
        accumulatedForce = Vector3.zero;
        accumulatedTorque = Vector3.zero;
    }

    public void Move(float dt)
    {
        Vector3 displacement = velocity * dt;
        float distance = displacement.magnitude;

        if (distance <= Mathf.Epsilon)
            return;

        Vector3 direction = displacement / distance;
        Vector3 castOrigin = position - direction * skin;
        float castDist = distance + skin;

        if (Physics.SphereCast(
            castOrigin,
            radius,
            direction,
            out RaycastHit hit,
            castDist,
            collisionMask,
            QueryTriggerInteraction.Ignore))
        {
            float travel = Mathf.Max(0f, hit.distance - skin);
            position += direction * travel;
            ReflectVelocity(hit.normal);

            Debug.DrawRay(hit.point, hit.normal, Color.green);
        }
        else
        {
            position += displacement;
        }
    }

    public void SetSpawnPosition(Vector3 newPosition)
    {
        initialPosition = newPosition;
        position = newPosition;
        transform.position = newPosition;
    }

    public void UpdateVisuals()
    {
        transform.position = position;
        transform.rotation = rotation;
    }

    public void ResetParticle()
    {
        ComputeMass();
        ComputeRadius();
        ComputeInertia();

        position = initialPosition;
        velocity = initialVelocity;

        rotation = initialRotation;
        angularVelocity = Vector3.zero;

        accumulatedForce = Vector3.zero;
        accumulatedTorque = Vector3.zero;

        transform.position = position;
        transform.rotation = rotation;
    }

    private void ComputeMass()
    {
        Vector3 scale = transform.localScale;
        float volume = scale.x * scale.y * scale.z;
        mass = Mathf.Max(0.0001f, volume * massFactor);
    }

    private void ComputeRadius()
    {
        Vector3 scale = transform.localScale;
        float maxScale = Mathf.Max(scale.x, scale.y, scale.z);
        radius = maxScale * 0.5f;
    }

    private void ComputeInertia()
    {
        // Aproximación de esfera sólida
        inertia = Mathf.Max(0.0001f, (2f / 5f) * mass * radius * radius);
    }

    public void ReflectVelocity(Vector3 normal)
    {
        Vector3 vNormal = Vector3.Dot(velocity, normal) * normal;
        Vector3 vTangent = velocity - vNormal;
        velocity = vTangent * (1f - friction) - vNormal * restitution;
    }

    public void AddForce(Vector3 force)
    {
        accumulatedForce += force;
    }

    public void AddForceAtPoint(Vector3 force, Vector3 worldPoint)
    {
        accumulatedForce += force;

        Vector3 r = worldPoint - position;
        accumulatedTorque += Vector3.Cross(r, force);
    }

    public void AddTorque(Vector3 torque)
    {
        accumulatedTorque += torque;
    }

    public void AddImpulse(Vector3 impulse)
    {
        velocity += impulse / mass;
    }

    public Vector3 GetPointVelocity(Vector3 worldPoint)
    {
        Vector3 r = worldPoint - position;
        return velocity + Vector3.Cross(angularVelocity, r);
    }

    public Vector3 Position
    {
        get => position;
        set
        {
            position = value;
            transform.position = value;
        }
    }

    public Vector3 Velocity
    {
        get => velocity;
        set => velocity = value;
    }

    public Quaternion Rotation
    {
        get => rotation;
        set
        {
            rotation = value;
            transform.rotation = value;
        }
    }

    public Vector3 AngularVelocity
    {
        get => angularVelocity;
        set => angularVelocity = value;
    }

    public float Mass => mass;
    public float Radius => radius;
    public float Restitution => restitution;
}