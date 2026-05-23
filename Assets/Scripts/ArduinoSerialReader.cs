using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Input Range")]
    [SerializeField] private int potMin = 0;
    [SerializeField] private int potMax = 1024;
    [SerializeField] private bool invertSteering = false;
    [SerializeField] private bool invertSpeed = false;

    [Header("Boat Steering")]
    [SerializeField] private float steeringSmoothing = 10f;

    [Header("Sail Animation (no AnimatorController needed)")]
    [SerializeField] private GameObject sailTarget;
    [SerializeField] private AnimationClip sailClip;

    [Header("Anchor Animation (no AnimatorController needed)")]
    [SerializeField] private GameObject anchorTarget;
    [SerializeField] private AnimationClip anchorClip;
    [SerializeField] private float anchorTransitionTime = 0.8f;

    [Header("Optional Forward Motion")]
    [SerializeField] private bool moveForward = false;
    [SerializeField] private float maxForwardSpeed = 3f;

    public float CurrentSteering01 { get; private set; }
    public float CurrentSpeed01 { get; private set; }
    public bool IsAnchorDown => anchorProgress > 0.5f;

    private ArduinoSerialReader reader;
    private bool eventsBound;

    private float currentHeading;
    private float anchorProgress;   // 0 = arriba, 1 = abajo
    private float anchorTargetState; // 0 = subir, 1 = bajar

    private void Update()
    {
        if (reader == null)
            reader = ArduinoSerialReader.Instance;

        if (reader == null || !reader.IsConnected)
            return;

        if (!eventsBound)
            BindEvents();

        float steering01 = NormalizePot(reader.RawSteering);
        float speed01 = NormalizePot(reader.RawSpeed);

        if (invertSteering) steering01 = 1f - steering01;
        if (invertSpeed) speed01 = 1f - speed01;

        CurrentSteering01 = steering01;
        CurrentSpeed01 = speed01;

        // Dirección: 0..124 -> 0..360 grados
        float targetHeading = steering01 * 360f;
        currentHeading = Mathf.LerpAngle(
            currentHeading,
            targetHeading,
            Time.deltaTime * steeringSmoothing
        );

        transform.rotation = Quaternion.Euler(0f, currentHeading, 0f);

        if (moveForward)
        {
            float forwardSpeed = speed01 * maxForwardSpeed;
            transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime, Space.Self);
        }

        UpdateSailAnimation(speed01);
        UpdateAnchorAnimation();
    }

    private float NormalizePot(int rawValue)
    {
        if (potMax <= potMin) return 0f;

        rawValue = Mathf.Clamp(rawValue, potMin, potMax);
        return Mathf.InverseLerp(potMin, potMax, rawValue);
    }

    private void UpdateSailAnimation(float speed01)
    {
        if (sailClip == null || sailTarget == null) return;

        // Máximo potenciómetro = frame 0
        // Mínimo potenciómetro = frame final
        float normalizedTime = 1f - speed01;
        float time = normalizedTime * sailClip.length;

        sailClip.SampleAnimation(sailTarget, time);
    }

    private void BindEvents()
    {
        if (reader == null || eventsBound) return;

        reader.OnAnchorDown += HandleAnchorDown;
        reader.OnAnchorUp += HandleAnchorUp;
        eventsBound = true;
    }

    private void OnDestroy()
    {
        if (reader != null && eventsBound)
        {
            reader.OnAnchorDown -= HandleAnchorDown;
            reader.OnAnchorUp -= HandleAnchorUp;
        }
    }

    private void HandleAnchorDown()
    {
        anchorTargetState = 1f;
    }

    private void HandleAnchorUp()
    {
        anchorTargetState = 0f;
    }

    private void UpdateAnchorAnimation()
    {
        if (anchorClip == null || anchorTarget == null) return;

        anchorProgress = Mathf.MoveTowards(
            anchorProgress,
            anchorTargetState,
            Time.deltaTime / Mathf.Max(0.01f, anchorTransitionTime)
        );

        float time = anchorProgress * anchorClip.length;
        anchorClip.SampleAnimation(anchorTarget, time);
    }
}