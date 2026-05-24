using UnityEngine;

public class BoatWindMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CarController boatController;

    [SerializeField] private Wind wind;

    [SerializeField] private Transform frontObject;

    [Header("Wind Movement")]
    [SerializeField] private float windSmoothing = 2f;

    [SerializeField] private float maxWindSpeed = 10f;

    [Header("Alignment Boost")]
    [SerializeField] private float minAlignmentAngle = 0f;

    [SerializeField] private float maxAlignmentAngle = 90f;

    // -----------------------------------
    // DEBUG PUBLICO PARA HUD
    // -----------------------------------

    public float CurrentAngle { get; private set; }

    public float CurrentAlignmentPercent { get; private set; }

    public float CurrentSailPercent { get; private set; }

    public float CurrentFinalSpeed { get; private set; }

    private Vector3 currentWindVelocity;

    private void Awake()
    {
        if (boatController == null)
            boatController = GetComponent<CarController>();
    }

    private void Update()
    {
        if (
            boatController == null ||
            wind == null ||
            frontObject == null
        )
            return;

        int sailValue = boatController.CurrentSailRaw;

        bool windShouldBeActive = sailValue != 0;

        if (wind.gameObject.activeSelf != windShouldBeActive)
        {
            wind.gameObject.SetActive(windShouldBeActive);
        }

        // -----------------------------------
        // SI LA VELA ESTA EN 0
        // -----------------------------------

        if (!windShouldBeActive)
        {
            CurrentAngle = 0f;
            CurrentAlignmentPercent = 0f;
            CurrentSailPercent = 0f;
            CurrentFinalSpeed = 0f;

            currentWindVelocity = Vector3.Lerp(
                currentWindVelocity,
                Vector3.zero,
                Time.deltaTime * (windSmoothing * 8f)
            );

            transform.position +=
                currentWindVelocity * Time.deltaTime;

            return;
        }

        // -----------------------------------
        // DIRECCION DEL VIENTO
        // -----------------------------------

        Vector3 windDir =
            Vector3.ProjectOnPlane(
                wind.CurrentWindDirection,
                Vector3.up
            ).normalized;

        // -----------------------------------
        // DIRECCION DEL BARCO
        // -----------------------------------

        Vector3 boatForward =
            Vector3.ProjectOnPlane(
                frontObject.forward,
                Vector3.up
            ).normalized;

        if (
            windDir.sqrMagnitude < 0.0001f ||
            boatForward.sqrMagnitude < 0.0001f
        )
            return;

        // -----------------------------------
        // ANGULO ENTRE BARCO Y VIENTO
        // -----------------------------------

        float angle =
            Vector3.Angle(
                boatForward,
                windDir
            );

        CurrentAngle = angle;

        // -----------------------------------
        // ALINEACION
        // -----------------------------------

        float alignment01 =
            Mathf.InverseLerp(
                maxAlignmentAngle,
                minAlignmentAngle,
                angle
            );

        CurrentAlignmentPercent =
            alignment01 * 100f;

        // -----------------------------------
        // APERTURA DE LA VELA
        // -----------------------------------

        float sailForce =
            boatController.CurrentSailExposure01;

        CurrentSailPercent =
            sailForce * 100f;

        // -----------------------------------
        // FUERZA FINAL
        // -----------------------------------

        float finalWindForce =
            maxWindSpeed *
            sailForce *
            alignment01;

        CurrentFinalSpeed =
            finalWindForce;

        // -----------------------------------
        // VELOCIDAD OBJETIVO
        // -----------------------------------

        Vector3 targetVelocity =
            windDir * finalWindForce;

        // -----------------------------------
        // SUAVIZADO
        // -----------------------------------

        currentWindVelocity = Vector3.Lerp(
            currentWindVelocity,
            targetVelocity,
            Time.deltaTime * windSmoothing
        );

        // -----------------------------------
        // MOVIMIENTO FINAL
        // -----------------------------------

        transform.position +=
            currentWindVelocity * Time.deltaTime;
    }
}