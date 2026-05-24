using UnityEngine;
using TMPro;

public class CarHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CarController car;

    [SerializeField] private BoatWindMover windMover;

    [SerializeField] private TMP_Text steeringText;

    [SerializeField] private TMP_Text sailText;

    [SerializeField] private TMP_Text windText;

    [SerializeField] private TMP_Text windInfoText;

    [SerializeField] private TMP_Text actionText;

    [SerializeField] private TMP_Text anchorStateText;

    [Header("Message Settings")]
    [SerializeField] private float messageDuration = 1.2f;

    private ArduinoSerialReader reader;

    private string currentMessage = "";

    private float messageTimer = 0f;

    private bool eventsBound;

    private void Update()
    {
        if (reader == null)
            reader = ArduinoSerialReader.Instance;

        if (reader != null && !eventsBound)
            BindEvents();

        // -----------------------------------
        // DATOS DEL ARDUINO
        // -----------------------------------

        if (reader != null)
        {
            if (steeringText != null)
            {
                steeringText.text =
                    $"Dirección: {reader.RawSteering}";
            }

            if (sailText != null)
            {
                sailText.text =
                    $"Vela: {reader.RawSpeed}";
            }
        }

        // -----------------------------------
        // ESTADO DEL ANCLA
        // -----------------------------------

        if (car != null)
        {
            if (anchorStateText != null)
            {
                anchorStateText.text =
                    car.IsAnchorDown
                    ? "Ancla: BAJANDO"
                    : "Ancla: SUBIENDO";
            }
        }

        // -----------------------------------
        // INFORMACION DEL VIENTO
        // -----------------------------------

        if (
            windMover != null &&
            windInfoText != null
        )
        {
            windInfoText.text =
                $"Ángulo Viento: {windMover.CurrentAngle:F1}°\n" +
                $"Alineación: {windMover.CurrentAlignmentPercent:F0}%\n" +
                $"Vela Abierta: {windMover.CurrentSailPercent:F0}%\n" +
                $"Velocidad Final: {windMover.CurrentFinalSpeed:F2}";
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