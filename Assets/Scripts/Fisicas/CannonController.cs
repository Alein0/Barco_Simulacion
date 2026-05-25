using UnityEngine;
using System.Collections.Generic;

public class CannonController : MonoBehaviour
{
    [Header("Cannon Setup")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private ParticleWorld particleWorld;

    [Header("Shooting Parameters")]
    [SerializeField] private float minForce = 5f;
    [SerializeField] private float maxForce = 50f;
    [SerializeField] private float chargeSpeed = 20f;
    [SerializeField] private float maxChargeTime = 2.5f;

    [Header("Projectile Pool")]
    [SerializeField] private int poolSize = 50;

    [Header("Trajectory Display")]
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private int trajectoryPoints = 50;
    [SerializeField] private float trajectoryTimeStep = 0.05f;

    [Header("UI Display")]
    [SerializeField] private TMPro.TextMeshProUGUI velocityDisplay;

    private Queue<GameObject> projectilePool;

    private float currentCharge = 0f;
    private bool isCharging = false;
    private float chargeStartTime;

    private void Start()
    {
        InitializeProjectilePool();

        if (trajectoryLine != null)
            trajectoryLine.enabled = false;
    }

    private void OnEnable()
    {
        if (ArduinoSerialReader.Instance != null)
            ArduinoSerialReader.Instance.OnFire += HandleShoot;
    }

    private void OnDisable()
    {
        if (ArduinoSerialReader.Instance != null)
            ArduinoSerialReader.Instance.OnFire -= HandleShoot;
    }

    private void Update()
    {
        HandleInput();
        UpdateTrajectoryDisplay();
        UpdateVelocityDisplay();
    }

    private void HandleInput()
    {
        // Iniciar carga
        if (Input.GetKeyDown(KeyCode.F))
        {
            StartCharging();
        }

        // Mantener carga
        if (Input.GetKey(KeyCode.F) && isCharging)
        {
            currentCharge += chargeSpeed * Time.deltaTime;
            currentCharge = Mathf.Clamp(currentCharge, minForce, maxForce);
        }

        // Disparar
        if (Input.GetKeyUp(KeyCode.F) && isCharging)
        {
            Shoot();
        }
    }

    private void HandleShoot()
    {
        if (!isCharging)
        {
            StartCharging();
        }
    }

    private void StartCharging()
    {
        isCharging = true;

        currentCharge = minForce;
        chargeStartTime = Time.time;

        Debug.Log("Charging started");

        if (trajectoryLine != null)
            trajectoryLine.enabled = true;
    }

    private void Shoot()
    {
        if (!isCharging)
            return;

        isCharging = false;

        GameObject projectile = GetPooledProjectile();

        if (projectile == null)
        {
            Debug.LogWarning("No projectile available in pool");
            return;
        }

        projectile.SetActive(true);

        Particle particle = projectile.GetComponent<Particle>();

        if (particle != null)
        {
            // Reiniciar partícula
            particle.ResetParticle();

            // IMPORTANTE:
            // usar Position y Rotation del Particle
            // NO transform.position
            particle.Position = shootPoint.position;
            particle.Rotation = shootPoint.rotation;

            // Dirección de disparo
            Vector3 shootDirection = shootPoint.forward;

            // Velocidad final
            Vector3 velocity = shootDirection * currentCharge;

            // Asignar velocidad
            particle.Velocity = velocity;

            Debug.Log("SHOT!");
            Debug.Log("Force: " + currentCharge);
            Debug.Log("Velocity: " + velocity);
        }
        else
        {
            Debug.LogError("Projectile does not contain Particle script");
        }

        currentCharge = 0f;

        if (trajectoryLine != null)
            trajectoryLine.enabled = false;
    }

    private void UpdateTrajectoryDisplay()
    {
        if (trajectoryLine == null || !isCharging)
            return;

        Vector3 shootDirection = shootPoint.forward;
        Vector3 velocity = shootDirection * currentCharge;

        Vector3[] points = new Vector3[trajectoryPoints];

        Vector3 currentPos = shootPoint.position;
        Vector3 currentVel = velocity;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            points[i] = currentPos;

            currentVel += Physics.gravity * trajectoryTimeStep;
            currentPos += currentVel * trajectoryTimeStep;

            if (Physics.Raycast(
                points[i],
                currentVel.normalized,
                currentVel.magnitude * trajectoryTimeStep))
            {
                break;
            }
        }

        trajectoryLine.positionCount = trajectoryPoints;
        trajectoryLine.SetPositions(points);
    }

    private void UpdateVelocityDisplay()
    {
        if (velocityDisplay == null)
            return;

        if (isCharging)
        {
            velocityDisplay.text = $"Velocity: {currentCharge:F1} m/s";
        }
        else
        {
            velocityDisplay.text = "Velocity: 0 m/s";
        }
    }

    private void InitializeProjectilePool()
    {
        projectilePool = new Queue<GameObject>(poolSize);

        for (int i = 0; i < poolSize; i++)
        {
            GameObject projectile = Instantiate(projectilePrefab);

            projectile.SetActive(false);

            projectile.name = $"Projectile_{i}";

            projectilePool.Enqueue(projectile);
        }
    }

    private GameObject GetPooledProjectile()
    {
        if (projectilePool.Count > 0)
        {
            return projectilePool.Dequeue();
        }

        Debug.LogWarning("Projectile pool exhausted!");
        return null;
    }

    public void ReturnProjectileToPool(GameObject projectile)
    {
        projectile.SetActive(false);
        projectilePool.Enqueue(projectile);
    }
    public float CurrentCharge => currentCharge;
}