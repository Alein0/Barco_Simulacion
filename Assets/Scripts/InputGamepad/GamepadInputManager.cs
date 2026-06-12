using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadInputManager : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private CannonController cannonController;
    [SerializeField] private CarController carController;
    [SerializeField] private AnclaController anclaController;

    [Header("Gamepad Settings")]
    [SerializeField] private float steeringSpeed = 1f;
    [SerializeField] private float sailExposureSpeed = 0.5f;
    [SerializeField] private bool debugMode = false;

    private float currentSteering = 0.5f;
    private float currentSailExposure = 0.5f;

    private void Start()
    {
        if (cannonController == null)
            cannonController = FindFirstObjectByType<CannonController>();

        if (carController == null)
            carController = FindFirstObjectByType<CarController>();

        if (anclaController == null)
            anclaController = FindFirstObjectByType<AnclaController>();

        Debug.Log("[Gamepad] Sistema inicializado");
    }

    private void Update()
    {
        if (Gamepad.current == null)
            return;

        HandleCannonInput();
        HandleSteeringInput();
        HandleSailInput();
        HandleAnchorInput();
    }

    private void HandleCannonInput()
    {
        if (Gamepad.current == null)
            return;

        if (Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            cannonController?.UIFireButton();

            if (debugMode)
                Debug.Log("[Gamepad] Disparo");
        }
    }

    private void HandleSteeringInput()
    {
        float horizontal = Gamepad.current.leftStick.x.ReadValue();

        if (Mathf.Abs(horizontal) > 0.1f)
        {
            currentSteering += horizontal * steeringSpeed * Time.deltaTime;
            currentSteering = Mathf.Clamp01(currentSteering);

            carController?.SetGamepadSteering(currentSteering);

            if (debugMode)
                Debug.Log($"[Gamepad] Steering: {currentSteering:F2}");
        }
    }

    private void HandleSailInput()
    {
        float vertical = Gamepad.current.rightStick.y.ReadValue();

        if (Mathf.Abs(vertical) > 0.1f)
        {
            currentSailExposure += vertical * sailExposureSpeed * Time.deltaTime;
            currentSailExposure = Mathf.Clamp01(currentSailExposure);

            carController?.SetGamepadSail(currentSailExposure);

            if (debugMode)
                Debug.Log($"[Gamepad] Sail: {currentSailExposure:F2}");
        }
    }

    private void HandleAnchorInput()
    {
        if (Gamepad.current == null || anclaController == null)
            return;

        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            if (anclaController.IsAnclaDown)
            {
                anclaController.HandleAnchorUp();

                if (debugMode)
                    Debug.Log("[Gamepad] Subiendo ancla");
            }
            else
            {
                anclaController.HandleAnchorDown();

                if (debugMode)
                    Debug.Log("[Gamepad] Bajando ancla");
            }
        }
    }
}