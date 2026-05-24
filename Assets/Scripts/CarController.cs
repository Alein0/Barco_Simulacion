using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Objects To Rotate")]
    [SerializeField] private Transform[] objectsToRotate;

    [Header("Steering Input Range")]
    [SerializeField] private int steeringPotMin = 0;
    [SerializeField] private int steeringPotMax = 124;

    [Header("Sail Input Range")]
    [SerializeField] private int sailPotMin = 0;
    [SerializeField] private int sailPotMax = 124;

    [Header("Invert Options")]
    [SerializeField] private bool invertSteering = false;
    [SerializeField] private bool invertSpeed = false;

    [Header("Boat Steering")]
    [SerializeField] private float steeringSmoothing = 10f;

    // =========================
    // GUARDAR ROTACION ORIGINAL
    // =========================

    private Vector3[] originalRotations;

    // =========================
    // SAIL ANIMATION 1
    // =========================

    [Header("Sail Animation 1")]
    [SerializeField] private GameObject sailTarget;
    [SerializeField] private AnimationClip sailClip;

    // =========================
    // SAIL ANIMATION 2
    // =========================

    [Header("Sail Animation 2")]
    [SerializeField] private GameObject sailTarget2;
    [SerializeField] private AnimationClip sailClip2;

    // =========================
    // ANCHOR
    // =========================

    [Header("Anchor Animation")]
    [SerializeField] private GameObject anchorTarget;
    [SerializeField] private AnimationClip anchorClip;
    [SerializeField] private float anchorTransitionTime = 0.8f;

    // =========================
    // MOVEMENT
    // =========================

    [Header("Optional Forward Motion")]
    [SerializeField] private bool moveForward = false;
    [SerializeField] private float maxForwardSpeed = 3f;

    public float CurrentSteering01 { get; private set; }
    public float CurrentSpeed01 { get; private set; }

    public bool IsAnchorDown => anchorProgress > 0.5f;

    private ArduinoSerialReader reader;
    private bool eventsBound;

    private float currentHeading;
    private float anchorProgress;
    private float anchorTargetState;

    // =========================
    // START
    // =========================

    private void Start()
    {
        originalRotations = new Vector3[objectsToRotate.Length];

        for (int i = 0; i < objectsToRotate.Length; i++)
        {
            if (objectsToRotate[i] != null)
            {
                originalRotations[i] =
                    objectsToRotate[i].localEulerAngles;
            }
        }
    }

    private void Update()
    {
        if (reader == null)
            reader = ArduinoSerialReader.Instance;

        if (reader == null || !reader.IsConnected)
            return;

        if (!eventsBound)
            BindEvents();

        // =========================
        // INPUTS
        // =========================

        float steering01 = NormalizePot(
            reader.RawSteering,
            steeringPotMin,
            steeringPotMax
        );

        float speed01 = NormalizePot(
            reader.RawSpeed,
            sailPotMin,
            sailPotMax
        );

        if (invertSteering)
            steering01 = 1f - steering01;

        if (invertSpeed)
            speed01 = 1f - speed01;

        CurrentSteering01 = steering01;
        CurrentSpeed01 = speed01;

        // =========================
        // ROTATION
        // =========================

        float targetHeading = steering01 * 360f;

        currentHeading = Mathf.LerpAngle(
            currentHeading,
            targetHeading,
            Time.deltaTime * steeringSmoothing
        );

        for (int i = 0; i < objectsToRotate.Length; i++)
        {
            Transform obj = objectsToRotate[i];

            if (obj != null)
            {
                Vector3 baseRot = originalRotations[i];

                obj.localRotation = Quaternion.Euler(
                    baseRot.x,
                    baseRot.y,
                    baseRot.z + currentHeading
                );
            }
        }

        // =========================
        // OPTIONAL MOVEMENT
        // =========================

        if (moveForward)
        {
            float forwardSpeed = speed01 * maxForwardSpeed;

            foreach (Transform obj in objectsToRotate)
            {
                if (obj != null)
                {
                    obj.Translate(
                        Vector3.forward * forwardSpeed * Time.deltaTime,
                        Space.Self
                    );
                }
            }
        }

        // =========================
        // ANIMATIONS
        // =========================

        UpdateSailAnimation(speed01);
        UpdateAnchorAnimation();
    }

    // =========================
    // NORMALIZE POTENTIOMETER
    // =========================

    private float NormalizePot(int rawValue, int min, int max)
    {
        if (max <= min)
            return 0f;

        rawValue = Mathf.Clamp(rawValue, min, max);

        return Mathf.InverseLerp(min, max, rawValue);
    }

    // =========================
    // TWO SAIL ANIMATIONS
    // =========================

    private void UpdateSailAnimation(float speed01)
    {
        float normalizedTime = 1f - speed01;

        if (sailClip != null && sailTarget != null)
        {
            float time1 = normalizedTime * sailClip.length;

            sailClip.SampleAnimation(
                sailTarget,
                time1
            );
        }

        if (sailClip2 != null && sailTarget2 != null)
        {
            float time2 = normalizedTime * sailClip2.length;

            sailClip2.SampleAnimation(
                sailTarget2,
                time2
            );
        }
    }

    // =========================
    // EVENTS
    // =========================

    private void BindEvents()
    {
        if (reader == null || eventsBound)
            return;

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

    // =========================
    // ANCHOR EVENTS
    // =========================

    private void HandleAnchorDown()
    {
        anchorTargetState = 1f;
    }

    private void HandleAnchorUp()
    {
        anchorTargetState = 0f;
    }

    // =========================
    // ANCHOR ANIMATION
    // =========================

    private void UpdateAnchorAnimation()
    {
        if (anchorClip == null || anchorTarget == null)
            return;

        anchorProgress = Mathf.MoveTowards(
            anchorProgress,
            anchorTargetState,
            Time.deltaTime / Mathf.Max(0.01f, anchorTransitionTime)
        );

        float time = anchorProgress * anchorClip.length;

        anchorClip.SampleAnimation(
            anchorTarget,
            time
        );
    }
}