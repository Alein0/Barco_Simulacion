using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CannonController : MonoBehaviour, IForceGenerator
{
    [Header("Cannon Setup")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private GameObject projectilePrefab;

    [Header("Initial Shot Parameters")]
    [SerializeField] private float minInitialForce = 5f;
    [SerializeField] private float maxInitialForce = 50f;
    [SerializeField] private float chargeSpeed = 20f;

    [Header("Wind Force Parameters")]
    [SerializeField] private float minWindForce = 0f;
    [SerializeField] private float maxWindForce = 100f;
    [SerializeField] private float windChargeSpeed = 30f;
    [SerializeField] private Vector3 windDirection = Vector3.forward;

    [Header("Projectile Pool")]
    [SerializeField] private int poolSize = 50;

    [Header("Trajectory Display")]
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private int trajectoryPoints = 40;
    [SerializeField] private float trajectoryTimeStep = 0.05f;

    [Header("UI Display")]
    [SerializeField] private TextMeshProUGUI velocityDisplay;
    [SerializeField] private TextMeshProUGUI windForceDisplay;

    private Queue<GameObject> projectilePool;
    private float currentCharge;
    private float currentWindForce;
    private bool isCharging;
    private Particle currentProjectile;
    private bool arduinoHolding;
    private float lastShotForce;
    private float lastWindForce;

    private void Start()
    {
        InitializeProjectilePool();
        currentCharge = minInitialForce;
        currentWindForce = minWindForce;

        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
            trajectoryLine.positionCount = trajectoryPoints;
            trajectoryLine.useWorldSpace = true;
        }

        ParticleWorld.Register(this);
    }

    private void OnDestroy()
    {
        ParticleWorld.Unregister(this);
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

    // ? Aplicar viento SOLO a la bola actual
    public void ApplyForces(float dt)
    {
        if (currentProjectile == null)
            return;

        Vector3 windForceVector = windDirection.normalized * currentWindForce;
        currentProjectile.AddForce(windForceVector);

        Debug.DrawRay(currentProjectile.Position, windForceVector.normalized * 2f, Color.cyan, 0.016f);
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            StartCharging();
        }

        if (Input.GetKey(KeyCode.F) && isCharging)
        {
            ChargeShot();
            ChargeWindForce();
        }

        if (Input.GetKeyUp(KeyCode.F) && isCharging)
        {
            Shoot();
        }
    }

    private void HandleArduinoPress()
    {
        if (!isCharging)
        {
            StartCharging();
            arduinoHolding = true;
        }
        else
        {
            Shoot();
            arduinoHolding = false;
        }
    }

    private void HandleArduinoCharge()
    {
        if (arduinoHolding && isCharging)
        {
            ChargeShot();
            ChargeWindForce();
        }
    }

    private void StartCharging()
    {
        isCharging = true;
        currentCharge = minInitialForce;
        currentWindForce = minWindForce;
        currentProjectile = null;

        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = true;
        }

        Debug.Log("START CHARGING - Mantén F para cargar");
    }

    private void ChargeShot()
    {
        currentCharge += chargeSpeed * Time.deltaTime;
        currentCharge = Mathf.Clamp(currentCharge, minInitialForce, maxInitialForce);
    }

    private void ChargeWindForce()
    {
        currentWindForce += windChargeSpeed * Time.deltaTime;
        currentWindForce = Mathf.Clamp(currentWindForce, minWindForce, maxWindForce);
    }

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
            currentProjectile = null;
            return;
        }

        // ? GUARDAR valores ANTES de resetearlos
        float chargeAtShoot = currentCharge;
        float windAtShoot = currentWindForce;

        projectile.SetActive(true);
        Particle particle = projectile.GetComponent<Particle>();

        if (particle != null && shootPoint != null)
        {
            // ? Pasar valores guardados a la corrutina
            StartCoroutine(SetupAndFireProjectile(particle, chargeAtShoot, windAtShoot));
        }
        else
        {
            currentProjectile = null;
        }

        // Resetear después de guardar
        currentCharge = minInitialForce;
        currentWindForce = minWindForce;

        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
        }
    }

    // ? Corrutina con parámetros de carga y viento
    private IEnumerator SetupAndFireProjectile(Particle particle, float chargeAmount, float windAmount)
    {
        // Esperar un frame para que Start() termine
        yield return null;

        Vector3 spawnPos = shootPoint.position;

        // Establecer posición
        particle.Position = spawnPos;

        // Calcular dirección
        Vector3 direction = shootPoint.forward.normalized;

        // ? Usar el chargeAmount guardado, NO currentCharge
        float velocityMagnitude = Mathf.Lerp(
            minInitialForce,
            maxInitialForce,
            (chargeAmount - minInitialForce) / (maxInitialForce - minInitialForce)
        );

        Vector3 finalVelocity = direction * velocityMagnitude;

        // ? APLICAR VELOCIDAD
        particle.Velocity = finalVelocity;
        particle.Rotation = Quaternion.LookRotation(direction);

        // Establecer como proyectil actual para recibir viento
        currentProjectile = particle;

        // ? Usar el windAmount guardado
        lastShotForce = chargeAmount;
        lastWindForce = windAmount;

        Debug.Log($"====== DISPARO ======");
        Debug.Log($"Velocidad Inicial: {finalVelocity.magnitude:F2} m/s");
        Debug.Log($"Dirección: {direction}");
        Debug.Log($"Carga: {chargeAmount:F2}");
        Debug.Log($"Viento aplicado: {windAmount:F2}N");
        Debug.Log($"Posición: {spawnPos}");
        Debug.Log($"====================");
    }

    private void UpdateTrajectoryDisplay()
    {
        if (trajectoryLine == null || !isCharging || shootPoint == null)
            return;

        Vector3[] points = new Vector3[trajectoryPoints];
        Vector3 position = shootPoint.position;
        Vector3 direction = shootPoint.forward.normalized;

        float velocityMagnitude = Mathf.Lerp(
            minInitialForce,
            maxInitialForce,
            (currentCharge - minInitialForce) / (maxInitialForce - minInitialForce)
        );
        Vector3 velocity = direction * velocityMagnitude;

        Particle sampleParticle = projectilePrefab?.GetComponent<Particle>();
        Vector3 gravity = sampleParticle != null ? sampleParticle.gravity : Physics.gravity;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            points[i] = position;
            velocity += gravity * trajectoryTimeStep;
            position += velocity * trajectoryTimeStep;
        }

        trajectoryLine.positionCount = trajectoryPoints;
        trajectoryLine.SetPositions(points);
    }

    private void UpdateVelocityDisplay()
    {
        if (velocityDisplay != null)
        {
            velocityDisplay.text = isCharging
                ? $"Velocidad de disparo: {currentCharge:F1}"
                : $"Último disparo: {lastShotForce:F1}";
        }

    }

    private void InitializeProjectilePool()
    {
        projectilePool = new Queue<GameObject>(poolSize);

        for (int i = 0; i < poolSize; i++)
        {
            GameObject projectile = Instantiate(
                projectilePrefab,
                new Vector3(0, -1000, 0),
                Quaternion.identity
            );

            projectile.SetActive(false);
            projectile.name = $"Projectile_{i}";
            projectilePool.Enqueue(projectile);
        }
    }

    private GameObject GetPooledProjectile()
    {
        return projectilePool.Count > 0 ? projectilePool.Dequeue() : null;
    }

    public void ReturnProjectileToPool(GameObject projectile)
    {
        projectile.SetActive(false);
        projectilePool.Enqueue(projectile);

        if (currentProjectile != null && projectile.GetComponent<Particle>() == currentProjectile)
        {
            currentProjectile = null;
        }
    }

    public void UIFireButton()
    {
        if (!isCharging)
        {
            StartCharging();
            arduinoHolding = true;
        }
        else
        {
            Shoot();
            arduinoHolding = false;
        }
    }

    public float CurrentCharge => currentCharge;
    public float CurrentWindForce => currentWindForce;
}