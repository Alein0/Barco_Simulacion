using UnityEngine;

public class BoatAnchorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoatWindMover boatWindMover;

    private void OnEnable()
    {
        if (ArduinoSerialReader.Instance != null)
        {
            ArduinoSerialReader.Instance.OnAnchorDown += ActivateAnchor;

            ArduinoSerialReader.Instance.OnAnchorUp += DeactivateAnchor;
        }
    }

    private void OnDisable()
    {
        if (ArduinoSerialReader.Instance != null)
        {
            ArduinoSerialReader.Instance.OnAnchorDown -= ActivateAnchor;

            ArduinoSerialReader.Instance.OnAnchorUp -= DeactivateAnchor;
        }
    }

    private void ActivateAnchor()
    {
        if (boatWindMover != null)
        {
            boatWindMover.SetAnchor(true);

            Debug.Log("ANCLA ACTIVADA");
        }
    }

    private void DeactivateAnchor()
    {
        if (boatWindMover != null)
        {
            boatWindMover.SetAnchor(false);

            Debug.Log("ANCLA DESACTIVADA");
        }
    }
}