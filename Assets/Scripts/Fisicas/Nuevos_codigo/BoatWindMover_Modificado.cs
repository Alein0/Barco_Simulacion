using UnityEngine;

public class BoatWindMover_Modificado : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CarController boatController;
    [SerializeField] private Wind_Modificado wind;
    [SerializeField] private Transform frontObject;
    [SerializeField] private Transform sailObject;

    [Header("Particle Physics")]
    [SerializeField] private Particle boatParticle;

    [Header("Wind Boost By Sail")]
    [SerializeField] private float maxWindMultiplier = 2f;
    [SerializeField] private float multiplierSmoothSpeed = 2f;

    [Header("Wind Force")]
    [SerializeField] private float windForceScale = 25f;

    [Header("Rudder (Steering) Force")]
    [SerializeField] private float rudderForceMax = 80f;     
    [SerializeField] private float rudderBySpeed = 1.0f;
    [Range(0f, 0.3f)]
    [SerializeField] private float rudderDeadZone = 0.05f;

    [Header("Where to Apply Rudder Force (popa)")]
    [Tooltip("Punto base en la popa (local). Más negativo en Z = más palanca.")]
    [SerializeField] private Vector3 rudderLocalOffset = new Vector3(0f, 0f, -6.0f);

    [Header("Steering Force Axis (LOCAL)")]
    [Tooltip("Quieres empujar en Z: deja (0,0,1).")]
    [SerializeField] private Vector3 localSteeringAxis = new Vector3(0f, 0f, 1f);

    [Header("IMPORTANT: Make Z force TURN the boat")]
    [Tooltip("Esto es CLAVE: mueve el punto del timón a izquierda/derecha para generar torque con una fuerza en Z.")]
    [SerializeField] private float rudderSideOffsetMax = 1.2f;

    [Header("Extra Strength On Z")]
    [SerializeField] private float extraZMultiplier = 5f;

    [Header("Debug Draw")]
    [SerializeField] private bool drawForces = true;
    [SerializeField] private float debugArrowScale = 0.08f;
    [SerializeField] private float debugDuration = 0f;

    private float baseWindStrength;
    private float currentMultiplier = 1f;

    private Vector3 lastWindPoint, lastWindForce;
    private Vector3 lastRudderPoint, lastRudderForce;
    private bool hasLastForces;

    private void Start()
    {
        if (wind != null)
            baseWindStrength = wind.strength;
    }

    private void Update()
    {
        if (boatController == null || wind == null)
            return;

        float sailPercent = boatController.CurrentSailExposure01;

        float targetMultiplier = Mathf.Lerp(1f, maxWindMultiplier, sailPercent);

        currentMultiplier = Mathf.Lerp(
            currentMultiplier,
            targetMultiplier,
            Time.deltaTime * multiplierSmoothSpeed
        );

        wind.strength = baseWindStrength * currentMultiplier;
    }

    private void FixedUpdate()
    {
        if (boatController == null || wind == null || boatParticle == null)
            return;

        ApplyWindForce();
        ApplyRudderForce();

        if (drawForces && hasLastForces)
        {
            Debug.DrawRay(lastWindPoint, lastWindForce * debugArrowScale, Color.cyan, debugDuration);
            Debug.DrawRay(lastRudderPoint, lastRudderForce * debugArrowScale, Color.yellow, debugDuration);
        }
    }

    private void ApplyWindForce()
    {
        if (sailObject == null)
            return;

        Vector3 windDir = wind.windDirection.normalized;
        Vector3 windForce = windDir * (wind.strength * windForceScale);

        boatParticle.AddForceAtPoint(windForce, sailObject.position);

        lastWindPoint = sailObject.position;
        lastWindForce = windForce;
        hasLastForces = true;
    }

    private void ApplyRudderForce()
    {
        float steer01 = boatController.CurrentSteering01;     
        float steerSigned = (steer01 - 0.5f) * 2f;           

        if (Mathf.Abs(steerSigned) < rudderDeadZone)
        {
            Vector3 p0 = transform.TransformPoint(rudderLocalOffset);
            lastRudderPoint = p0;
            lastRudderForce = Vector3.zero;
            return;
        }

        // Normaliza fuera de deadzone
        float sign = Mathf.Sign(steerSigned);
        float mag = (Mathf.Abs(steerSigned) - rudderDeadZone) / Mathf.Max(0.0001f, (1f - rudderDeadZone));
        mag = Mathf.Clamp01(mag);

        float steer = sign * mag;
        steer = -steer;

        float speed01 = boatController.CurrentSpeed01;
        float speedFactor = Mathf.Lerp(0.2f, 1f, speed01) * rudderBySpeed;

 
        Vector3 axisLocal = localSteeringAxis.normalized; 
        Vector3 forceWorld = transform.TransformDirection(axisLocal) * (steer * rudderForceMax * speedFactor);


        forceWorld.z *= extraZMultiplier;
        Vector3 localPoint = rudderLocalOffset + new Vector3(steer * rudderSideOffsetMax, 0f, 0f);
        Vector3 rudderPointWorld = transform.TransformPoint(localPoint);

        boatParticle.AddForceAtPoint(forceWorld, rudderPointWorld);

        lastRudderPoint = rudderPointWorld;
        lastRudderForce = forceWorld;
        hasLastForces = true;
    }

    private void OnDrawGizmos()
    {
        if (!drawForces) return;

        Gizmos.color = Color.magenta;
        Vector3 p = transform.TransformPoint(rudderLocalOffset);
        Gizmos.DrawSphere(p, 0.08f);
    }
}