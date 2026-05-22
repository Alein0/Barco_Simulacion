using UnityEngine;

/// <summary>
/// Controla el barco:
///   - Rotacion Y segun potenciometro (0-1023 -> 0-360 grados)
///   - Vaivén en el agua (bobbing)
///   - Delega ancla y canon a sus respectivos scripts
/// Fallback teclado: A/D rotan, espacio dispara, F ancla
/// </summary>
[RequireComponent(typeof(AnclaController))]
[RequireComponent(typeof(CanonController))]
public class BarcoController : MonoBehaviour
{
    [Header("Referencias")]
    public SerialController serial;

    [Header("Rotacion")]
    [Tooltip("Velocidad de suavizado de la rotacion")]
    public float rotSmoothing = 5f;

    [Header("Bobbing (vaiven en el agua)")]
    public float bobbingAmplitude = 0.08f;
    public float bobbingFrequency = 0.8f;

    [Header("Fallback Teclado")]
    [Tooltip("Grados por segundo al rotar con A/D")]
    public float keyRotSpeed = 90f;

    private AnclaController _ancla;
    private CanonController _canon;

    private float _targetYaw   = 0f;
    private float _currentYaw  = 0f;
    private float _baseY;
    private bool  _serialOk    = false;

    // Para toggle de ancla con teclado
    private bool  _anclaKeyLast = false;
    private bool  _anclaKeyOn   = false;

    void Start()
    {
        _ancla  = GetComponent<AnclaController>();
        _canon  = GetComponent<CanonController>();
        _baseY  = transform.position.y;
        _serialOk = serial != null;
    }

    void Update()
    {
        HandleInput();
        ApplyRotation();
        ApplyBobbing();
    }

    // ── Entrada ──────────────────────────────────────────────────────────────
    private void HandleInput()
    {
        if (_serialOk && serial.enabled)
        {
            // Potenciometro -> angulo
            _targetYaw = Mathf.Lerp(0f, 360f, serial.potValue / 1023f);

            // Ancla
            _ancla.SetAncla(serial.anclaOn);

            // Canon
            if (serial.canonFired) _canon.Disparar();
        }
        else
        {
            // Fallback teclado
            float axis = Input.GetAxis("Horizontal"); // A/D o flechas
            _targetYaw += axis * keyRotSpeed * Time.deltaTime;
            _targetYaw  = (_targetYaw % 360f + 360f) % 360f;

            // Ancla con F (toggle)
            bool anclaKey = Input.GetKey(KeyCode.F);
            if (anclaKey && !_anclaKeyLast)
            {
                _anclaKeyOn = !_anclaKeyOn;
                _ancla.SetAncla(_anclaKeyOn);
            }
            _anclaKeyLast = anclaKey;

            // Canon con Espacio
            if (Input.GetKeyDown(KeyCode.Space)) _canon.Disparar();
        }
    }

    // ── Rotacion suavizada ───────────────────────────────────────────────────
    private void ApplyRotation()
    {
        _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, rotSmoothing * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
    }

    // ── Vaivén ───────────────────────────────────────────────────────────────
    private void ApplyBobbing()
    {
        if (_ancla != null && _ancla.EstaAnclado) return; // sin vaiven con ancla

        float newY = _baseY + Mathf.Sin(Time.time * bobbingFrequency * Mathf.PI * 2f) * bobbingAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
