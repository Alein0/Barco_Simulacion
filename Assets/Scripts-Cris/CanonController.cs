using System.Collections;
using UnityEngine;

/// <summary>
/// Maneja el disparo del canon.
/// Requiere en el Inspector:
///   canonTransform -> punta del canon (desde donde sale el proyectil)
///   smokeParticles -> ParticleSystem de humo/explosion
///   canonball      -> Prefab de bala (opcional)
///   canonSound     -> AudioClip del disparo (opcional)
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class CanonController : MonoBehaviour
{
    [Header("Canon")]
    [Tooltip("Transform en la punta del canon")]
    public Transform canonTransform;

    [Tooltip("ParticleSystem de explosion/humo en la boca del canon")]
    public ParticleSystem smokeParticles;

    [Header("Proyectil")]
    public GameObject canonballPrefab;
    public float      canonballForce  = 20f;
    public float      canonballLife   = 4f;

    [Header("Retroceso")]
    public float recoilDistance = 0.15f;
    public float recoilDuration = 0.12f;

    [Header("Sonido")]
    public AudioClip canonSound;

    // Cooldown para evitar spam
    public float cooldown = 1.5f;

    private AudioSource _audio;
    private bool        _ready = true;
    private Vector3     _canonLocalOrigin;

    void Start()
    {
        _audio = GetComponent<AudioSource>();
        if (canonTransform != null)
            _canonLocalOrigin = canonTransform.localPosition;
    }

    public void Disparar()
    {
        if (!_ready) return;
        StartCoroutine(DisparoCoroutine());
    }

    private IEnumerator DisparoCoroutine()
    {
        _ready = false;

        // Humo / particulas
        if (smokeParticles != null) smokeParticles.Play();

        // Sonido
        if (canonSound != null && _audio != null)
            _audio.PlayOneShot(canonSound);

        // Proyectil
        if (canonballPrefab != null && canonTransform != null)
        {
            GameObject ball = Instantiate(canonballPrefab,
                                          canonTransform.position,
                                          canonTransform.rotation);
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(canonTransform.forward * canonballForce, ForceMode.Impulse);
            Destroy(ball, canonballLife);
        }

        // Retroceso del canon
        if (canonTransform != null)
        {
            Vector3 recoilPos = _canonLocalOrigin - canonTransform.localRotation * Vector3.forward * recoilDistance;
            float elapsed = 0f;
            while (elapsed < recoilDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / recoilDuration;
                canonTransform.localPosition = Vector3.Lerp(_canonLocalOrigin, recoilPos, t);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < recoilDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / recoilDuration;
                canonTransform.localPosition = Vector3.Lerp(recoilPos, _canonLocalOrigin, t);
                yield return null;
            }
            canonTransform.localPosition = _canonLocalOrigin;
        }

        Debug.Log("[Canon] DISPARADO");
        yield return new WaitForSeconds(cooldown);
        _ready = true;
    }
}
