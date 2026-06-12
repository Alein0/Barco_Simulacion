using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;

/// <summary>
/// Panel de control de parámetros de física en tiempo real.
/// Se muestra/oculta con la tecla Tab.
/// Modifica los campos de los scripts enlazados mediante reflexión.
/// </summary>
public class PhysicsParametersPanel : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] private bool startVisible = false;

    [Header("Controllers")]
    [SerializeField] private CannonController cannonController;
    [SerializeField] private BoatWindMover_Modificado boatWindMover;
    [SerializeField] private AnclaController anclaController;
    [SerializeField] private OceanSurface1 oceanSurface;

    [Header("CANNON - Initial Shot Parameters")]
    [SerializeField] private Slider minInitialForceSlider;
    [SerializeField] private TextMeshProUGUI minInitialForceLabel;
    [SerializeField] private Slider maxInitialForceSlider;
    [SerializeField] private TextMeshProUGUI maxInitialForceLabel;

    [Header("CANNON - Wind Force Parameters")]
    [SerializeField] private Slider minWindForceSlider;
    [SerializeField] private TextMeshProUGUI minWindForceLabel;
    [SerializeField] private Slider maxWindForceSlider;
    [SerializeField] private TextMeshProUGUI maxWindForceLabel;

    [Header("BOAT WIND MOVER - Wind Boost")]
    [SerializeField] private Slider maxWindMultiplierSlider;
    [SerializeField] private TextMeshProUGUI maxWindMultiplierLabel;

    [Header("ANCHOR")]
    [SerializeField] private Slider anclaFrictionSlider;
    [SerializeField] private TextMeshProUGUI anclaFrictionLabel;

    [Header("OCEAN STATE")]
    [SerializeField] private TMP_Dropdown oceanStateDropdown;

    private bool isVisible = false;

    private void Awake()
    {
        if (panelCanvasGroup == null)
            panelCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        Debug.Log("[PhysicsPanel] Inicializando panel...");

        // Buscar referencias automáticamente si no están asignadas
        if (cannonController == null)
            cannonController = FindFirstObjectByType<CannonController>();
        if (boatWindMover == null)
            boatWindMover = FindFirstObjectByType<BoatWindMover_Modificado>();
        if (anclaController == null)
            anclaController = FindFirstObjectByType<AnclaController>();
        if (oceanSurface == null)
            oceanSurface = FindFirstObjectByType<OceanSurface1>();

        // Inicializar sliders
        InitializeSliders();

        // Establecer visibilidad inicial
        SetPanelVisible(startVisible);

        Debug.Log("[PhysicsPanel] Panel listo");
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }
    }

    /// <summary>
    /// Alterna la visibilidad del panel.
    /// </summary>
    private void TogglePanel()
    {
        SetPanelVisible(!isVisible);
    }

    /// <summary>
    /// Establece la visibilidad del panel.
    /// </summary>
    private void SetPanelVisible(bool visible)
    {
        isVisible = visible;

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = visible ? 1f : 0f;
            panelCanvasGroup.interactable = visible;
            panelCanvasGroup.blocksRaycasts = visible;
        }

        Debug.Log($"[PhysicsPanel] Panel {(visible ? "ABIERTO" : "CERRADO")}");
    }

    /// <summary>
    /// Inicializa todos los sliders con sus listeners.
    /// </summary>
    private void InitializeSliders()
    {
        // ===== CANNON - Initial Shot =====
        if (minInitialForceSlider != null && cannonController != null)
        {
            minInitialForceSlider.minValue = 0f;
            minInitialForceSlider.maxValue = 500f;
            minInitialForceSlider.value = cannonController.minInitialForce;
            minInitialForceSlider.onValueChanged.AddListener((value) =>
            {
                cannonController.minInitialForce = value;
                if (minInitialForceLabel != null)
                    minInitialForceLabel.text = $"{value:F2}";
                Debug.Log($"[PhysicsPanel] minInitialForce = {value:F2}");
            });
            if (minInitialForceLabel != null)
                minInitialForceLabel.text = $"Min Force: {cannonController.minInitialForce:F2}";
        }

        if (maxInitialForceSlider != null && cannonController != null)
        {
            maxInitialForceSlider.minValue = 0f;
            maxInitialForceSlider.maxValue = 500f;
            maxInitialForceSlider.value = cannonController.maxInitialForce;
            maxInitialForceSlider.onValueChanged.AddListener((value) =>
            {
                cannonController.maxInitialForce = value;
                if (maxInitialForceLabel != null)
                    maxInitialForceLabel.text = $"{value:F2}";
                Debug.Log($"[PhysicsPanel] maxInitialForce = {value:F2}");
            });
            if (maxInitialForceLabel != null)
                maxInitialForceLabel.text = $"{cannonController.maxInitialForce:F2}";
        }

        // ===== CANNON - Wind Force =====
        if (minWindForceSlider != null && cannonController != null)
        {
            minWindForceSlider.minValue = 0f;
            minWindForceSlider.maxValue = 1000f;
            minWindForceSlider.value = cannonController.minWindForce;
            minWindForceSlider.onValueChanged.AddListener((value) =>
            {
                cannonController.minWindForce = value;
                if (minWindForceLabel != null)
                    minWindForceLabel.text = $" {value:F2}";
                Debug.Log($"[PhysicsPanel] minWindForce = {value:F2}");
            });
            if (minWindForceLabel != null)
                minWindForceLabel.text = $"{cannonController.minWindForce:F2}";
        }

        if (maxWindForceSlider != null && cannonController != null)
        {
            maxWindForceSlider.minValue = 0f;
            maxWindForceSlider.maxValue = 1000f;
            maxWindForceSlider.value = cannonController.maxWindForce;
            maxWindForceSlider.onValueChanged.AddListener((value) =>
            {
                cannonController.maxWindForce = value;
                if (maxWindForceLabel != null)
                    maxWindForceLabel.text = $"{value:F2}";
                Debug.Log($"[PhysicsPanel] maxWindForce = {value:F2}");
            });
            if (maxWindForceLabel != null)
                maxWindForceLabel.text = $"{cannonController.maxWindForce:F2}";
        }

        // ===== BOAT WIND MOVER =====
        if (maxWindMultiplierSlider != null && boatWindMover != null)
        {
            maxWindMultiplierSlider.minValue = 0.1f;
            maxWindMultiplierSlider.maxValue = 50f;
            maxWindMultiplierSlider.value = boatWindMover.maxWindMultiplier;
            maxWindMultiplierSlider.onValueChanged.AddListener((value) =>
            {
                boatWindMover.maxWindMultiplier = value;
                if (maxWindMultiplierLabel != null)
                    maxWindMultiplierLabel.text = $"{value:F2}";
                Debug.Log($"[PhysicsPanel] maxWindMultiplier = {value:F2}");
            });
            if (maxWindMultiplierLabel != null)
                maxWindMultiplierLabel.text = $"{boatWindMover.maxWindMultiplier:F2}";
        }

        // ===== ANCHOR =====
        if (anclaFrictionSlider != null && anclaController != null)
        {
            anclaFrictionSlider.minValue = 0.0f;
            anclaFrictionSlider.maxValue = 1f;
            anclaFrictionSlider.value = anclaController.anclaFrictionMultiplier;
            anclaFrictionSlider.onValueChanged.AddListener((value) =>
            {
                anclaController.anclaFrictionMultiplier = value;
                if (anclaFrictionLabel != null)
                    anclaFrictionLabel.text = $"{value:F3}";
                Debug.Log($"[PhysicsPanel] anclaFrictionMultiplier = {value:F3}");
            });
            if (anclaFrictionLabel != null)
                anclaFrictionLabel.text = $"{anclaController.anclaFrictionMultiplier:F3}";
        }

        // ===== OCEAN STATE =====
        if (oceanStateDropdown != null && oceanSurface != null)
        {
            oceanStateDropdown.options.Clear();
            oceanStateDropdown.options.Add(new TMP_Dropdown.OptionData("Bajo"));
            oceanStateDropdown.options.Add(new TMP_Dropdown.OptionData("Normal"));
            oceanStateDropdown.options.Add(new TMP_Dropdown.OptionData("Fuerte"));

            oceanStateDropdown.value = (int)oceanSurface.currentState;

            oceanStateDropdown.onValueChanged.AddListener((value) =>
            {
                oceanSurface.ApplyState((OceanSurface1.OceanState)value);
                Debug.Log($"[PhysicsPanel] Ocean State = {(OceanSurface1.OceanState)value}");
            });
        }
    }
}