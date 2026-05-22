using UnityEngine;

/// <summary>
/// Gestiona la animacion del ancla.
/// Asigna en el Inspector:
///   anclaObject  -> el GameObject del ancla (o su cadena)
///   anclaDropY   -> posicion Y cuando esta bajada
///   anclaRaisedY -> posicion Y cuando esta subida
/// </summary>
public class AnclaController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("GameObject del ancla (hijo del barco)")]
    public Transform anclaObject;

    [Tooltip("Posicion local Y cuando el ancla esta bajada")]
    public float anclaDropY   = -2.5f;

    [Tooltip("Posicion local Y cuando el ancla esta subida")]
    public float anclaRaisedY = 0f;

    [Header("Animacion")]
    public float anclaSpeed   = 2f;

    [Header("UI")]
    [Tooltip("Panel o imagen que indica ancla activa (opcional)")]
    public UnityEngine.UI.Image anclaIcon;

    public bool EstaAnclado { get; private set; } = false;

    private float _targetY;

    void Start()
    {
        _targetY = anclaRaisedY;
        if (anclaIcon) anclaIcon.enabled = false;
    }

    void Update()
    {
        if (anclaObject == null) return;

        float currentY = anclaObject.localPosition.y;
        float newY     = Mathf.MoveTowards(currentY, _targetY, anclaSpeed * Time.deltaTime);
        anclaObject.localPosition = new Vector3(
            anclaObject.localPosition.x,
            newY,
            anclaObject.localPosition.z
        );
    }

    /// <summary>Llamado por BarcoController cada frame.</summary>
    public void SetAncla(bool bajar)
    {
        if (EstaAnclado == bajar) return;
        EstaAnclado = bajar;
        _targetY    = bajar ? anclaDropY : anclaRaisedY;

        if (anclaIcon) anclaIcon.enabled = bajar;
        Debug.Log($"[Ancla] {(bajar ? "BAJADA" : "SUBIDA")}");
    }
}
