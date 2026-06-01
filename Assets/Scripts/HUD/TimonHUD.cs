using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimonHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CarController car;
    [SerializeField] private CannonController cannonController;

    [Header("Steering Needle")]
    [SerializeField] private RectTransform steeringNeedle;
    [SerializeField] private float steeringMinAngle = -130f;
    [SerializeField] private float steeringMaxAngle = 130f;
    [SerializeField] private float steeringSmooth = 12f;

    [Header("Sail Aura")]
    [SerializeField] private RectTransform sailAura;
    [SerializeField] private Vector2 sailMinPosition = new Vector2(0f, -120f);
    [SerializeField] private Vector2 sailMaxPosition = new Vector2(0f, 120f);
    [SerializeField] private float sailSmooth = 12f;

    [Header("Texts")]
    [SerializeField] private TMP_Text steeringText;
    [SerializeField] private TMP_Text sailText;
    [SerializeField] private TMP_Text anchorStateText;
    [SerializeField] private TMP_Text cannonText;

    [Header("Buttons")]
    [SerializeField] private Button cannonButton;
    [SerializeField] private Button anchorToggleButton;
    [SerializeField] private Button anchorDownButton;
    [SerializeField] private Button anchorUpButton;

    [Header("Optional")]
    [SerializeField] private bool useAnchorToggle = true;

    private float currentNeedleAngle;
    private Vector2 currentAuraPosition;

    private void Start()
    {
        if (steeringNeedle != null)
            currentNeedleAngle = steeringNeedle.localEulerAngles.z;

        if (sailAura != null)
            currentAuraPosition = sailAura.anchoredPosition;

        SetupButtons();
    }

    private void OnEnable()
    {
        SetupButtons();
    }

    private void OnDisable()
    {
        RemoveButtons();
    }

    private void Update()
    {
        if (car == null)
            return;

        UpdateSteeringNeedle();
        UpdateSailAura();
        UpdateTexts();
    }

    private void UpdateSteeringNeedle()
    {
        if (steeringNeedle == null)
            return;

        float steering01 = car.CurrentSteering01;
        float targetAngle = Mathf.Lerp(steeringMinAngle, steeringMaxAngle, steering01);

        currentNeedleAngle = Mathf.LerpAngle(
            currentNeedleAngle,
            targetAngle,
            Time.deltaTime * steeringSmooth
        );

        steeringNeedle.localRotation = Quaternion.Euler(0f, 0f, currentNeedleAngle);
    }

    private void UpdateSailAura()
    {
        if (sailAura == null)
            return;

        float sail01 = car.CurrentSailExposure01;
        Vector2 targetPos = Vector2.Lerp(sailMinPosition, sailMaxPosition, sail01);

        currentAuraPosition = Vector2.Lerp(
            currentAuraPosition,
            targetPos,
            Time.deltaTime * sailSmooth
        );

        sailAura.anchoredPosition = currentAuraPosition;
    }

    private void UpdateTexts()
    {
        if (steeringText != null)
        {
            steeringText.text =
                $"Timón: {car.CurrentSteering01 * 100f:F0}%";
        }

        if (sailText != null)
        {
            sailText.text =
                $"Vela: {car.CurrentSailExposure01 * 100f:F0}%";
        }

        if (anchorStateText != null)
        {
            anchorStateText.text =
                car.IsAnchorDown
                ? "Ancla: BAJADA"
                : "Ancla: SUBIENDO";
        }
        
       
        
    }

    private void SetupButtons()
    {
        RemoveButtons();

        if (cannonButton != null)
            cannonButton.onClick.AddListener(OnCannonButtonClicked);

        if (useAnchorToggle)
        {
            if (anchorToggleButton != null)
                anchorToggleButton.onClick.AddListener(OnAnchorToggleClicked);
        }
        else
        {
            if (anchorDownButton != null)
                anchorDownButton.onClick.AddListener(OnAnchorDownClicked);

            if (anchorUpButton != null)
                anchorUpButton.onClick.AddListener(OnAnchorUpClicked);
        }
    }

    private void RemoveButtons()
    {
        if (cannonButton != null)
            cannonButton.onClick.RemoveListener(OnCannonButtonClicked);

        if (anchorToggleButton != null)
            anchorToggleButton.onClick.RemoveListener(OnAnchorToggleClicked);

        if (anchorDownButton != null)
            anchorDownButton.onClick.RemoveListener(OnAnchorDownClicked);

        if (anchorUpButton != null)
            anchorUpButton.onClick.RemoveListener(OnAnchorUpClicked);
    }

    public void OnCannonButtonClicked()
    {
        if (cannonController != null)
            cannonController.UIFireButton();
    }

    public void OnAnchorToggleClicked()
    {
        if (car != null)
            car.ToggleAnchorState();
    }

    public void OnAnchorDownClicked()
    {
        if (car != null)
            car.RequestAnchorDown();
    }

    public void OnAnchorUpClicked()
    {
        if (car != null)
            car.RequestAnchorUp();
    }
}