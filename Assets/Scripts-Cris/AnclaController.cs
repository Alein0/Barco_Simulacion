using UnityEngine;

/// <summary>
/// Gestiona la animacion y física del ancla.
/// Cuando el ancla toca una piedra y está bajada, el barco se detiene.
/// 
/// Asigna en el Inspector:
///   anclaObject  -> el GameObject del ancla (debe tener Rigidbody y Collider)
///   piedraLayer  -> el Layer de las piedras
/// </summary>
public class AnclaController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("GameObject del ancla (hijo del barco)")]
    public Transform anclaObject;

    [Tooltip("Posicion local Y cuando el ancla esta bajada")]
    public float anclaDropY = -2.5f;

    [Tooltip("Posicion local Y cuando el ancla esta subida")]
    public float anclaRaisedY = 0f;

    [Header("Animacion")]
    public float anclaSpeed = 2f;

    [Header("Física del Ancla")]
    [Tooltip("Habilitar gravedad en el ancla")]
    public bool useAnclaPhysics = true;
    [Tooltip("Drag del ancla en el agua")]
    public float anclaDrag = 3f;
    [Tooltip("Layer de las piedras para detectar colisión")]
    public LayerMask piedraLayer;

    [Header("UI")]
    [Tooltip("Panel o imagen que indica ancla activa (opcional)")]
    public UnityEngine.UI.Image anclaIcon;

    public bool EstaAnclado { get; private set; } = false;
    public bool AnclaEnContacto { get; private set; } = false; // Detecta contacto con piedra

    private float _targetY;
    private Rigidbody _anclaRb;
    private Collider _anclaCollider;
    private float _lastContactTime = -10f;

    void Start()
    {
        _targetY = anclaRaisedY;
        if (anclaIcon) anclaIcon.enabled = false;

        // Obtener componentes del ancla
        if (anclaObject != null)
        {
            _anclaRb = anclaObject.GetComponent<Rigidbody>();
            _anclaCollider = anclaObject.GetComponent<Collider>();

            // Si no existen, crearlos
            if (_anclaRb == null && useAnclaPhysics)
            {
                _anclaRb = anclaObject.gameObject.AddComponent<Rigidbody>();
                _anclaRb.mass = 2f;
                _anclaRb.linearDamping = anclaDrag;
                _anclaRb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
            }

            if (_anclaCollider == null)
            {
                _anclaCollider = anclaObject.gameObject.AddComponent<BoxCollider>();
                var box = _anclaCollider as BoxCollider;
                if (box != null)
                {
                    box.size = new Vector3(0.3f, 0.5f, 0.3f);
                    box.center = Vector3.zero;
                }
            }

            // Configurar el Rigidbody
            if (_anclaRb != null)
            {
                _anclaRb.isKinematic = true; // Por defecto, cinemático
            }
        }
    }

    void Update()
    {
        if (anclaObject == null) return;

        // Animar la posición del ancla
        float currentY = anclaObject.localPosition.y;
        float newY = Mathf.MoveTowards(currentY, _targetY, anclaSpeed * Time.deltaTime);
        anclaObject.localPosition = new Vector3(
            anclaObject.localPosition.x,
            newY,
            anclaObject.localPosition.z
        );

        // Si el ancla está bajada, activar física
        if (EstaAnclado)
        {
            if (_anclaRb != null)
                _anclaRb.isKinematic = false; // Activar gravedad/física
        }
        else
        {
            if (_anclaRb != null)
                _anclaRb.isKinematic = true; // Desactivar física
        }
    }

    void FixedUpdate()
    {
        if (!EstaAnclado || _anclaRb == null || _anclaCollider == null) return;

        // Detectar colisión con piedras
        DetectarContactoPiedra();
    }

    /// <summary>
    /// Detecta si el ancla está en contacto con una piedra.
    /// Si lo está, marca AnclaEnContacto = true
    /// </summary>
    private void DetectarContactoPiedra()
    {
        AnclaEnContacto = false;

        // Raycast desde el ancla hacia abajo
        RaycastHit hit;
        float maxDistance = 2f;

        if (Physics.Raycast(anclaObject.position, Vector3.down, out hit, maxDistance, piedraLayer))
        {
            AnclaEnContacto = true;
            _lastContactTime = Time.time;
            Debug.Log($"[Ancla] Contacto con piedra: {hit.collider.name}");
        }
        else if (Time.time - _lastContactTime > 0.5f) // Timeout si no hay contacto reciente
        {
            AnclaEnContacto = false;
        }
    }

    /// <summary>
    /// Llamado por BarcoController cada frame.
    /// </summary>
    public void SetAncla(bool bajar)
    {
        if (EstaAnclado == bajar) return;
        EstaAnclado = bajar;
        _targetY = bajar ? anclaDropY : anclaRaisedY;

        if (anclaIcon) anclaIcon.enabled = bajar;
        Debug.Log($"[Ancla] {(bajar ? "BAJADA" : "SUBIDA")}");
    }
}