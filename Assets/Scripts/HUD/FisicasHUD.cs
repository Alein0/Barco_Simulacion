using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FisicasHUD : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private OceanSurface1 ocean;

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

    [Header("Texto")]
    [SerializeField] private TMP_Text oceanStateText;


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


    private void SyncUIFromValues()
    {
        if (ocean != null)
        {
            if (oceanStateDropdown != null)
                oceanStateDropdown.SetValueWithoutNotify((int)ocean.currentState);

            if (animateOceanToggle != null)
                animateOceanToggle.SetIsOnWithoutNotify(ocean.animateMesh);
        }

        UpdateTexts();
    }

    private void UpdateTexts()
    {

        if (ocean != null && oceanStateText != null)
        {
            oceanStateText.text = $"Océano: {ocean.currentState}";
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