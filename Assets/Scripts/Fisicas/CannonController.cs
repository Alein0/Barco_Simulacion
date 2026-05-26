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

    [Header("Projectile Pool")]
    [SerializeField] private int poolSize = 50;

    [Header("Trajectory Display")]
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private int trajectoryPoints = 40;
    [SerializeField] private float trajectoryTimeStep = 0.05f;

    [Header("UI Display")]
    [SerializeField] private TMPro.TextMeshProUGUI velocityDisplay;

    private Queue<GameObject> projectilePool;

    private float currentCharge;
    private bool isCharging;

    // Arduino
    private bool arduinoHolding;

    // Último disparo
    private float lastShotForce;

    private void Start()
    {
        InitializeProjectilePool();

        currentCharge = minForce;

        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
            trajectoryLine.positionCount = trajectoryPoints;

            // IMPORTANTE
            trajectoryLine.useWorldSpace = true;
        }
    }

    private void OnEnable()
    {
        if (ArduinoSerialReader.Instance != null)
        {
            ArduinoSerialReader.Instance.OnFire += HandleArduinoPress;
        }
    }

    private void OnDisable()
    {
        if (ArduinoSerialReader.Instance != null)
        {
            ArduinoSerialReader.Instance.OnFire -= HandleArduinoPress;
        }
    }

    private void Update()
    {
        HandleKeyboardInput();

        HandleArduinoCharge();

        UpdateTrajectoryDisplay();

        UpdateVelocityDisplay();
    }

    // =====================================================
    // KEYBOARD
    // =====================================================

    private void HandleKeyboardInput()
    {
        // EMPEZAR CARGA
        if (Input.GetKeyDown(KeyCode.F))
        {
            StartCharging();
        }

        // MANTENER CARGA
        if (Input.GetKey(KeyCode.F) && isCharging)
        {
            ChargeShot();
        }

        // DISPARAR
        if (Input.GetKeyUp(KeyCode.F) && isCharging)
        {
            Shoot();
        }
    }

    // =====================================================
    // ARDUINO
    // =====================================================

    private void HandleArduinoPress()
    {
        // Primer toque inicia carga
        if (!isCharging)
        {
            StartCharging();

            arduinoHolding = true;
        }
        else
        {
            // Segundo toque dispara
            Shoot();

            arduinoHolding = false;
        }
    }

    private void HandleArduinoCharge()
    {
        if (arduinoHolding && isCharging)
        {
            ChargeShot();
        }
    }

    // =====================================================
    // CHARGE
    // =====================================================

    private void StartCharging()
    {
        isCharging = true;

        currentCharge = minForce;

        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = true;
        }

        Debug.Log("START CHARGING");
    }

    private void ChargeShot()
    {
        currentCharge += chargeSpeed * Time.deltaTime;

        currentCharge = Mathf.Clamp(
            currentCharge,
            minForce,
            maxForce
        );
    }

    // =====================================================
    // SHOOT
    // =====================================================

    private void Shoot()
    {
        if (!isCharging)
            return;

        isCharging = false;

        arduinoHolding = false;

        GameObject projectile = GetPooledProjectile();

        if (projectile == null)
        {
            Debug.LogWarning("POOL EMPTY");
            return;
        }

        projectile.SetActive(true);

        Particle particle =
            projectile.GetComponent<Particle>();

        if (particle != null)
        {
            // Reiniciar física
            particle.ResetParticle();

            // POSICIÓN EXACTA DEL CAÑÓN
            particle.Position = shootPoint.position;

            particle.Rotation = shootPoint.rotation;

            // Dirección exacta del barco
            Vector3 direction = shootPoint.forward.normalized;

            // Velocidad final
            Vector3 velocity =
                direction * currentCharge;

            particle.Velocity = velocity;

            lastShotForce = currentCharge;

            Debug.Log("SHOT");
            Debug.Log("FORCE: " + currentCharge);
            Debug.Log("VELOCITY: " + velocity);
        }

        currentCharge = minForce;

        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
        }
    }

    // =====================================================
    // TRAJECTORY
    // =====================================================

    private void UpdateTrajectoryDisplay()
    {
        if (
            trajectoryLine == null ||
            !isCharging
        )
            return;

        Vector3[] points =
            new Vector3[trajectoryPoints];

        Vector3 position = shootPoint.position;

        Vector3 velocity =
            shootPoint.forward.normalized * currentCharge;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            points[i] = position;

            // Física
            velocity += Physics.gravity * trajectoryTimeStep;

            position += velocity * trajectoryTimeStep;
        }

        trajectoryLine.positionCount =
            trajectoryPoints;

        trajectoryLine.SetPositions(points);
    }

    // =====================================================
    // UI
    // =====================================================

    private void UpdateVelocityDisplay()
    {
        if (velocityDisplay == null)
            return;

        if (isCharging)
        {
            velocityDisplay.text =
                $"Potencia: {currentCharge:F1}";
        }
        else
        {
            velocityDisplay.text =
                $"Último Disparo: {lastShotForce:F1}";
        }
    }

    // =====================================================
    // POOL
    // =====================================================

    private void InitializeProjectilePool()
    {
        projectilePool =
            new Queue<GameObject>(poolSize);

        for (int i = 0; i < poolSize; i++)
        {
            GameObject projectile =
                Instantiate(projectilePrefab);

            projectile.SetActive(false);

            projectile.name =
                $"Projectile_{i}";

            projectilePool.Enqueue(projectile);
        }
    }

    private GameObject GetPooledProjectile()
    {
        if (projectilePool.Count > 0)
        {
            return projectilePool.Dequeue();
        }

        return null;
    }

    public void ReturnProjectileToPool(
        GameObject projectile
    )
    {
        projectile.SetActive(false);

        projectilePool.Enqueue(projectile);
    }

    // =====================================================
    // GETTERS
    // =====================================================

    public float CurrentCharge => currentCharge;

    public float LastShotForce => lastShotForce;

    public bool IsCharging => isCharging;
}