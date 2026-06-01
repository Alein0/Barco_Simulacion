using UnityEngine;
using TMPro;

public class CarHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CarController car;
    [SerializeField] private BoatWindMover windMover;
    [SerializeField] private CannonController cannonController;
    [SerializeField] private Particle boatParticle;

    [Header("HUD Root")]
    [Tooltip("Arrastra aquí el panel o contenedor que tiene todo el HUD.")]
    [SerializeField] private RectTransform hudPanel;

    [Tooltip("Qué tan lejos se moverá el panel para ocultarlo.")]
    [SerializeField] private Vector2 hiddenOffset = new Vector2(0f, -2500f);

    [Tooltip("Tecla para mostrar/ocultar el HUD.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Alpha1;

    [Header("UI Texts")]
    [SerializeField] private TMP_Text steeringText;
    [SerializeField] private TMP_Text sailText;
    [SerializeField] private TMP_Text anchorStateText;
    [SerializeField] private TMP_Text windText;
    [SerializeField] private TMP_Text windInfoText;
    [SerializeField] private TMP_Text cannonText;
    [SerializeField] private TMP_Text particleInfoText;
    [SerializeField] private TMP_Text actionText;

    [Header("Message Settings")]
    [SerializeField] private float messageDuration = 1.2f;

    private ArduinoSerialReader reader;
    private string currentMessage = "";
    private float messageTimer = 0f;
    private bool eventsBound;

    private Vector2 hudVisiblePosition;
    private bool hudVisible = false;

    private void Start()
    {
        if (hudPanel != null)
        {
            hudVisiblePosition = hudPanel.anchoredPosition;
            HideHUD();
        }
    }

    private void Update()
    {
        if (reader == null)
            reader = ArduinoSerialReader.Instance;

        if (reader != null && !eventsBound)
            BindEvents();

        if (Input.GetKeyDown(toggleKey))
        {
            ToggleHUD();
        }

        // -----------------------------------
        // DATOS DEL ARDUINO
        // -----------------------------------
        if (reader != null)
        {
            if (steeringText != null)
            {
                steeringText.text = $"Dirección: {reader.RawSteering}";
            }

            if (sailText != null)
            {
                sailText.text = $"Vela: {reader.RawSpeed}";
            }
        }

        // -----------------------------------
        // DATOS DEL BARCO (CarController)
        // -----------------------------------
        if (car != null)
        {
            if (anchorStateText != null)
            {
                anchorStateText.text =
                    car.IsAnchorDown
                    ? "Ancla: BAJADA"
                    : "Ancla: SUBIENDO";
            }
        }

        // -----------------------------------
        // INFORMACIÓN DEL VIENTO (BoatWindMover)
        // -----------------------------------
        if (windMover != null)
        {
            if (windText != null)
            {
                windText.text = $"Viento: {windMover.CurrentAngle:F1}°";
            }

            if (windInfoText != null)
            {
                windInfoText.text =
                    $"Ángulo Viento: {windMover.CurrentAngle:F1}°\n" +
                    $"Alineación: {windMover.CurrentAlignmentPercent:F0}%\n" +
                    $"Vela Abierta: {windMover.CurrentSailPercent:F0}%\n" +
                    $"Velocidad Final: {windMover.CurrentFinalSpeed:F2}";
            }
        }


        // -----------------------------------
        // FÍSICAS DEL BARCO (Particle)
        // -----------------------------------
        if (boatParticle != null && particleInfoText != null)
        {
            Vector3 velocity = boatParticle.Velocity;
            Vector3 angularVelocity = boatParticle.AngularVelocity;

            float speed = velocity.magnitude;
            float angularSpeed = angularVelocity.magnitude;

            particleInfoText.text =
                $"Velocidad: {speed:F2}\n" +
                $"Velocidad X: {velocity.x:F2}\n" +
                $"Velocidad Y: {velocity.y:F2}\n" +
                $"Velocidad Z: {velocity.z:F2}\n\n" +
                $"Velocidad Angular: {angularSpeed:F2}\n" +
                $"Masa: {boatParticle.Mass:F2}\n" +
                $"Radio: {boatParticle.Radius:F2}\n" +
                $"Restitución: {boatParticle.Restitution:F2}";
        }

        // -----------------------------------
        // MENSAJES TEMPORALES
        // -----------------------------------
        if (actionText != null)
        {
            if (messageTimer > 0f)
            {
                messageTimer -= Time.deltaTime;
                actionText.text = currentMessage;
            }
            else
            {
                actionText.text = "";
            }
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

    private void BindEvents()
    {
        if (reader == null || eventsBound)
            return;

        reader.OnFire += HandleFire;
        reader.OnAnchorDown += HandleAnchorDown;
        reader.OnAnchorUp += HandleAnchorUp;

        eventsBound = true;
    }

    private void OnDestroy()
    {
        if (reader != null && eventsBound)
        {
            reader.OnFire -= HandleFire;
            reader.OnAnchorDown -= HandleAnchorDown;
            reader.OnAnchorUp -= HandleAnchorUp;
        }
    }

    private void HandleFire()
    {
        ShowMessage("DISPARANDO");
    }

    private void HandleAnchorDown()
    {
        ShowMessage("ANCLA BAJANDO");
    }

    private void HandleAnchorUp()
    {
        ShowMessage("ANCLA SUBIENDO");
    }

    private void ShowMessage(string message)
    {
        currentMessage = message;
        messageTimer = messageDuration;
    }
}