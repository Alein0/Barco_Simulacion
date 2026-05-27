using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FisicasHUD : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private OceanSurface1 ocean;
    [SerializeField] private Wind wind;
    [SerializeField] private Buoyancy1 buoyancy;

    [Header("HUD Root")]
    [Tooltip("Arrastra aquí el panel o contenedor que tiene todo el HUD.")]
    [SerializeField] private RectTransform hudPanel;

    [Tooltip("Qué tan lejos se moverá el panel para ocultarlo.")]
    [SerializeField] private Vector2 hiddenOffset = new Vector2(0f, -2500f);

    [Tooltip("Tecla para mostrar/ocultar el HUD.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Alpha1;

    [Header("Océano")]
    [SerializeField] private TMP_Dropdown oceanStateDropdown;
    [SerializeField] private Toggle animateOceanToggle;

    [Header("Viento")]
    [SerializeField] private Slider windStrengthSlider;
    [SerializeField] private Slider windDirectionSlider;   // 0 a 360
    [SerializeField] private Slider turbulenceIntensitySlider;
    [SerializeField] private Slider turbulenceFrequencySlider;

    [Header("Flotabilidad")]
    [SerializeField] private Slider buoyancyDensitySlider;
    [SerializeField] private Slider waterDragSlider;
    [SerializeField] private Slider waterAngularDragSlider;
    [SerializeField] private Slider alignmentTorqueSlider;
    [SerializeField] private Toggle applyAlignmentToggle;

    [Header("Texto")]
    [SerializeField] private TMP_Text windStrengthText;
    [SerializeField] private TMP_Text windDirectionText;
    [SerializeField] private TMP_Text oceanStateText;
    [SerializeField] private TMP_Text buoyancyDensityText;

    private Vector2 hudVisiblePosition;
    private bool hudVisible = false;

    private void Start()
    {
        if (hudPanel != null)
        {
            hudVisiblePosition = hudPanel.anchoredPosition;
            HideHUD();
        }

        SyncUIFromValues();
        ApplyAllFromUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleHUD();
        }
    }

    private void ToggleHUD()
    {
        hudVisible = !hudVisible;

        if (hudVisible)
            ShowHUD();
        else
            HideHUD();
    }

    private void ShowHUD()
    {
        if (hudPanel != null)
            hudPanel.anchoredPosition = hudVisiblePosition;
    }

    private void HideHUD()
    {
        if (hudPanel != null)
            hudPanel.anchoredPosition = hudVisiblePosition + hiddenOffset;
    }

    public void ApplyAllFromUI()
    {
        ApplyOcean();
        ApplyWind();
        ApplyBuoyancy();
        UpdateTexts();
    }

    public void ApplyOcean()
    {
        if (ocean != null)
        {
            if (oceanStateDropdown != null)
            {
                int index = Mathf.Clamp(oceanStateDropdown.value, 0, 2);
                ocean.ApplyState((OceanSurface1.OceanState)index);
            }

            if (animateOceanToggle != null)
            {
                ocean.animateMesh = animateOceanToggle.isOn;
            }
        }
    }

    public void ApplyWind()
    {
        if (wind == null)
            return;

        if (windStrengthSlider != null)
            wind.strength = windStrengthSlider.value;

        if (turbulenceIntensitySlider != null)
            wind.turbulenceIntensity = turbulenceIntensitySlider.value;

        if (turbulenceFrequencySlider != null)
            wind.turbulenceFrequency = turbulenceFrequencySlider.value;

        if (windDirectionSlider != null)
        {
            float angle = windDirectionSlider.value;
            wind.windDirection = AngleToDirection(angle);
        }
    }

    public void ApplyBuoyancy()
    {
        if (buoyancy == null)
            return;

        if (buoyancyDensitySlider != null)
            buoyancy.density = buoyancyDensitySlider.value;

        if (waterDragSlider != null)
            buoyancy.waterDrag = waterDragSlider.value;

        if (waterAngularDragSlider != null)
            buoyancy.waterAngularDrag = waterAngularDragSlider.value;

        if (alignmentTorqueSlider != null)
            buoyancy.alignmentTorque = alignmentTorqueSlider.value;

        if (applyAlignmentToggle != null)
            buoyancy.applyWaterNormalAlignment = applyAlignmentToggle.isOn;
    }

    public void OnAnySliderChanged(float value)
    {
        ApplyAllFromUI();
    }

    public void OnAnyToggleChanged(bool value)
    {
        ApplyAllFromUI();
    }

    public void OnOceanDropdownChanged(int value)
    {
        ApplyAllFromUI();
    }

    public void OnWindDirectionChanged(float value)
    {
        if (wind != null && windDirectionSlider != null)
        {
            wind.windDirection = AngleToDirection(windDirectionSlider.value);
        }

        UpdateTexts();
    }

    private void SyncUIFromValues()
    {
        if (ocean != null)
        {
            if (oceanStateDropdown != null)
                oceanStateDropdown.SetValueWithoutNotify((int)ocean.currentState);

            if (animateOceanToggle != null)
                animateOceanToggle.SetIsOnWithoutNotify(ocean.animateMesh);
        }

        if (wind != null)
        {
            if (windStrengthSlider != null)
                windStrengthSlider.SetValueWithoutNotify(wind.strength);

            if (turbulenceIntensitySlider != null)
                turbulenceIntensitySlider.SetValueWithoutNotify(wind.turbulenceIntensity);

            if (turbulenceFrequencySlider != null)
                turbulenceFrequencySlider.SetValueWithoutNotify(wind.turbulenceFrequency);

            if (windDirectionSlider != null)
                windDirectionSlider.SetValueWithoutNotify(DirectionToAngle(wind.windDirection));
        }

        if (buoyancy != null)
        {
            if (buoyancyDensitySlider != null)
                buoyancyDensitySlider.SetValueWithoutNotify(buoyancy.density);

            if (waterDragSlider != null)
                waterDragSlider.SetValueWithoutNotify(buoyancy.waterDrag);

            if (waterAngularDragSlider != null)
                waterAngularDragSlider.SetValueWithoutNotify(buoyancy.waterAngularDrag);

            if (alignmentTorqueSlider != null)
                alignmentTorqueSlider.SetValueWithoutNotify(buoyancy.alignmentTorque);

            if (applyAlignmentToggle != null)
                applyAlignmentToggle.SetIsOnWithoutNotify(buoyancy.applyWaterNormalAlignment);
        }

        UpdateTexts();
    }

    private void UpdateTexts()
    {
        if (wind != null)
        {
            if (windStrengthText != null)
                windStrengthText.text = $"Viento: {wind.strength:0.0}";

            if (windDirectionText != null)
                windDirectionText.text = $"Dirección: {DirectionToAngle(wind.windDirection):0}°";
        }

        if (ocean != null && oceanStateText != null)
        {
            oceanStateText.text = $"Océano: {ocean.currentState}";
        }

        if (buoyancy != null && buoyancyDensityText != null)
        {
            buoyancyDensityText.text = $"Densidad: {buoyancy.density:0.0}";
        }
    }

    private Vector3 AngleToDirection(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)).normalized;
    }

    private float DirectionToAngle(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return 0f;

        float angle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;

        return angle;
    }
}