using UnityEngine;

public class AnclaController : MonoBehaviour
{
    [Header("Referencias")]
    public Particle barcoParticle;
    public CarController carController;
    public AnimationClip anchorClip;
    public GameObject anchorTarget;
    public float anchorTransitionTime = 0.8f;

    [Header("Fricción del Ancla")]
    public float anclaFrictionMultiplier = 0.95f;

    private bool _anclaDown = false;
    private bool _spaceKeyLast = false;
    private float anchorProgress = 0f;
    private float anchorTargetState = 0f;
    private ArduinoSerialReader arduinoReader;
    private bool _arduinoConnected = false;

    void Start()
    {
        if (barcoParticle == null)
            barcoParticle = GetComponent<Particle>();

        if (carController == null)
            carController = GetComponent<CarController>();

        if (carController == null)
            carController = FindObjectOfType<CarController>();

        // Buscar Arduino (puede no estar disponible)
        try
        {
            arduinoReader = ArduinoSerialReader.Instance;
            if (arduinoReader != null)
            {
                arduinoReader.OnAnchorDown += HandleAnchorDown;
                arduinoReader.OnAnchorUp += HandleAnchorUp;
                _arduinoConnected = true;
                Debug.Log("[Ancla] Arduino conectado");
            }
        }
        catch
        {
            Debug.LogWarning("[Ancla] Arduino no disponible");
            _arduinoConnected = false;
        }

        Debug.Log($"[Ancla] Start - Particle: {(barcoParticle != null)}, CarController: {(carController != null)}, Arduino: {_arduinoConnected}");
    }

    void Update()
    {
        HandleKeyboardInput();
        UpdateAnchorAnimation();
    }

    void FixedUpdate()
    {
        if (!_anclaDown || barcoParticle == null) return;

        barcoParticle.Velocity *= anclaFrictionMultiplier;

        if (barcoParticle.Velocity.magnitude < 0.01f)
        {
            barcoParticle.Velocity = Vector3.zero;
        }
    }

    private void HandleKeyboardInput()
    {
        bool spaceKey = Input.GetKey(KeyCode.Space);

        if (spaceKey && !_spaceKeyLast)
        {
            Debug.Log("[Ancla] SPACE presionado");

            _anclaDown = !_anclaDown;
            anchorTargetState = _anclaDown ? 1f : 0f;

            Debug.Log($"[Ancla] Estado: {(_anclaDown ? "BAJADA" : "SUBIDA")}, Progress: {anchorProgress}");
        }

        _spaceKeyLast = spaceKey;
    }

    private void HandleAnchorDown()
    {
        Debug.Log("[Ancla] Arduino - BAJADA");
        _anclaDown = true;
        anchorTargetState = 1f;
    }

    private void HandleAnchorUp()
    {
        Debug.Log("[Ancla] Arduino - SUBIDA");
        _anclaDown = false;
        anchorTargetState = 0f;
    }

    private void UpdateAnchorAnimation()
    {
        if (anchorClip == null || anchorTarget == null)
        {
            Debug.LogWarning("[Ancla] AnchorClip o AnchorTarget no asignados!");
            return;
        }

        // Interpolar el progreso hacia el estado objetivo
        anchorProgress = Mathf.MoveTowards(
            anchorProgress,
            anchorTargetState,
            Time.deltaTime / Mathf.Max(0.01f, anchorTransitionTime)
        );

        // Aplicar la animación
        float time = anchorProgress * anchorClip.length;
        anchorClip.SampleAnimation(anchorTarget, time);

        Debug.Log($"[Ancla] Progress: {anchorProgress:F2}, Time: {time:F2}, TargetState: {anchorTargetState}");
    }

    private void OnDestroy()
    {
        if (_arduinoConnected && arduinoReader != null)
        {
            arduinoReader.OnAnchorDown -= HandleAnchorDown;
            arduinoReader.OnAnchorUp -= HandleAnchorUp;
        }
    }

    public bool IsAnclaDown => _anclaDown;
}