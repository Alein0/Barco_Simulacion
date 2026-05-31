using TMPro;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Objects To Rotate In Z")]
    [SerializeField] private Transform[] objectsToRotateZ;

    [Header("Objects To Rotate In Y")]
    [SerializeField] private Transform[] objectsToRotateY;

    [Header("Preview Arrow")]
    [SerializeField] private Transform frontObject;
    [SerializeField] private float frontArrowLength = 2f;

    [Header("Steering Input Range")]
    [SerializeField] private int steeringPotMin = 0;
    [SerializeField] private int steeringPotMax = 1024;

    [Header("Steering Angle Limits")]
    [SerializeField] private float minSteeringAngle = 25f;
    [SerializeField] private float maxSteeringAngle = 335f;

    [Header("Sail Input Range")]
    [SerializeField] private int sailPotMin = 0;
    [SerializeField] private int sailPotMax = 600;

    [Header("Invert Options")]
    [SerializeField] private bool invertSteering = false;
    [SerializeField] private bool invertSpeed = false;

    [Header("Boat Steering")]
    [SerializeField] private float steeringSmoothing = 10f;

    [Header("Sail Animation 1")]
    [SerializeField] private GameObject sailTarget;
    [SerializeField] private AnimationClip sailClip;

    [Header("Sail Animation 2")]
    [SerializeField] private GameObject sailTarget2;
    [SerializeField] private AnimationClip sailClip2;

    [Header("Anchor Animation")]
    [SerializeField] private GameObject anchorTarget;
    [SerializeField] private AnimationClip anchorClip;
    [SerializeField] private float anchorTransitionTime = 0.8f;

    [Header("Optional Forward Motion")]
    [SerializeField] private bool moveForward = false;
    [SerializeField] private float maxForwardSpeed = 3f;

    [Header("References")]
    [SerializeField] private CannonController cannonController;
    [SerializeField] private TMP_Text projectileVelocityText;

    public float CurrentSteering01 { get; private set; }
    public float CurrentSpeed01 { get; private set; }
    public int CurrentSailRaw { get; private set; }
    public float CurrentSailExposure01 { get; private set; }

    public bool IsAnchorDown => anchorProgress > 0.5f;
    public float CurrentHeading => currentHeading;
    public float AnchorProgress => anchorProgress;

    private Vector3[] originalRotationsZ;
    private Vector3[] originalRotationsY;

    private ArduinoSerialReader reader;
    private bool eventsBound;

    private float currentHeading;
    private float anchorProgress;
    private float anchorTargetState;

    private void Start()
    {
        originalRotationsZ = new Vector3[objectsToRotateZ.Length];
        for (int i = 0; i < objectsToRotateZ.Length; i++)
        {
            if (objectsToRotateZ[i] != null)
                originalRotationsZ[i] = objectsToRotateZ[i].localEulerAngles;
        }

        originalRotationsY = new Vector3[objectsToRotateY.Length];
        for (int i = 0; i < objectsToRotateY.Length; i++)
        {
            if (objectsToRotateY[i] != null)
                originalRotationsY[i] = objectsToRotateY[i].localEulerAngles;
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

        float steering01 = NormalizePot(
            reader.RawSteering,
            steeringPotMin,
            steeringPotMax
        );

        int rawSail = Mathf.Clamp(
            reader.RawSpeed,
            sailPotMin,
            sailPotMax
        );

        float speed01 = NormalizePot(
            rawSail,
            sailPotMin,
            sailPotMax
        );

        if (invertSteering)
            steering01 = 1f - steering01;

        if (invertSpeed)
            speed01 = 1f - speed01;

        CurrentSteering01 = steering01;
        CurrentSpeed01 = speed01;
        CurrentSailRaw = rawSail;
        CurrentSailExposure01 = speed01;

        // ✅ Ahora el rango del timón es SOLO de 25° a 335°
        float targetHeading = Mathf.Lerp(
            minSteeringAngle,
            maxSteeringAngle,
            steering01
        );

        currentHeading = Mathf.LerpAngle(
            currentHeading,
            targetHeading,
            Time.deltaTime * steeringSmoothing
        );

        if (cannonController != null && projectileVelocityText != null)
        {
            projectileVelocityText.text =
                $"Velocidad Disparo: {cannonController.CurrentCharge:F1}";
        }

        for (int i = 0; i < objectsToRotateZ.Length; i++)
        {
            Transform obj = objectsToRotateZ[i];

            if (obj != null)
            {
                Vector3 baseRot = originalRotationsZ[i];

                obj.localRotation = Quaternion.Euler(
                    baseRot.x,
                    baseRot.y,
                    baseRot.z + currentHeading
                );
            }
        }

        for (int i = 0; i < objectsToRotateY.Length; i++)
        {
            Transform obj = objectsToRotateY[i];

            if (obj != null)
            {
                Vector3 baseRot = originalRotationsY[i];

                obj.localRotation = Quaternion.Euler(
                    baseRot.x,
                    baseRot.y + currentHeading,
                    baseRot.z
                );
            }
        }

        if (moveForward)
        {
            float forwardSpeed = speed01 * maxForwardSpeed;

            foreach (Transform obj in objectsToRotateZ)
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

        UpdateSailAnimation(speed01);
        UpdateAnchorAnimation();
    }

    private float NormalizePot(int rawValue, int min, int max)
    {
        if (max <= min)
            return 0f;

        rawValue = Mathf.Clamp(rawValue, min, max);
        return Mathf.InverseLerp(min, max, rawValue);
    }

    private void UpdateSailAnimation(float speed01)
    {
        float normalizedTime = 1f - speed01;

        if (sailClip != null && sailTarget != null)
        {
            float time1 = normalizedTime * sailClip.length;
            sailClip.SampleAnimation(sailTarget, time1);
        }

        if (sailClip2 != null && sailTarget2 != null)
        {
            float time2 = normalizedTime * sailClip2.length;
            sailClip2.SampleAnimation(sailTarget2, time2);
        }
    }

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
        if (anchorClip == null || anchorTarget == null)
            return;

        anchorProgress = Mathf.MoveTowards(
            anchorProgress,
            anchorTargetState,
            Time.deltaTime / Mathf.Max(0.01f, anchorTransitionTime)
        );

        float time = anchorProgress * anchorClip.length;
        anchorClip.SampleAnimation(anchorTarget, time);
    }

    public void RequestAnchorDown()
    {
        anchorTargetState = 1f;
    }

    public void RequestAnchorUp()
    {
        anchorTargetState = 0f;
    }

    public void ToggleAnchorState()
    {
        if (IsAnchorDown)
            RequestAnchorUp();
        else
            RequestAnchorDown();
    }
}