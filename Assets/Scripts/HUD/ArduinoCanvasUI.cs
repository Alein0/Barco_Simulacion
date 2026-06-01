using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ArduinoCanvasUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private TMP_Text connectionStatusText;
    [SerializeField] private TMP_Text portText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text steeringText;
    [SerializeField] private TMP_Text normalizedSpeedText;
    [SerializeField] private TMP_Text normalizedSteeringText;
    [SerializeField] private TMP_InputField portInputField;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button exitButton;

    [Header("Opcional")]
    [SerializeField] private bool autoFindReader = true;

    private ArduinoSerialReader reader;

    private void Start()
    {
        if (autoFindReader)
        {
            reader = FindFirstObjectByType<ArduinoSerialReader>();
        }

        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectClicked);

        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(OnDisconnectClicked);

        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshUI);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);

        SubscribeToReader();
        RefreshUI();
    }

    private void OnDestroy()
    {
        UnsubscribeFromReader();

        if (connectButton != null)
            connectButton.onClick.RemoveListener(OnConnectClicked);

        if (disconnectButton != null)
            disconnectButton.onClick.RemoveListener(OnDisconnectClicked);

        if (refreshButton != null)
            refreshButton.onClick.RemoveListener(RefreshUI);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitGame);
    }

    private void Update()
    {
        RefreshValues();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitGame();
        }
    }

    private void SubscribeToReader()
    {
        if (reader == null) return;

        reader.OnConnected += HandleConnected;
        reader.OnDisconnected += HandleDisconnected;
    }

    private void UnsubscribeFromReader()
    {
        if (reader == null) return;

        reader.OnConnected -= HandleConnected;
        reader.OnDisconnected -= HandleDisconnected;
    }

    private void HandleConnected(string port)
    {
        RefreshUI();
    }

    private void HandleDisconnected()
    {
        RefreshUI();
    }

    public void OnConnectClicked()
    {
        if (reader == null)
        {
            reader = FindFirstObjectByType<ArduinoSerialReader>();
        }

        if (reader == null)
        {
            SetStatus("ArduinoSerialReader no encontrado");
            return;
        }

        string port = portInputField != null ? portInputField.text.Trim() : "";

        if (!string.IsNullOrEmpty(port))
        {
            reader.ConnectTo(port);
        }
        else
        {
            reader.Connect();
        }

        RefreshUI();
    }

    public void OnDisconnectClicked()
    {
        if (reader == null)
        {
            reader = FindFirstObjectByType<ArduinoSerialReader>();
        }

        if (reader != null)
        {
            reader.Disconnect();
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (reader == null)
        {
            reader = FindFirstObjectByType<ArduinoSerialReader>();
        }

        if (reader == null)
        {
            SetStatus("ArduinoSerialReader no encontrado");
            SetPort("-");
            SetRawValues("-", "-");
            SetNormalizedValues("-", "-");
            return;
        }

        if (reader.IsConnected)
        {
            SetStatus("Conectado");
            SetPort(string.IsNullOrEmpty(reader.ConnectedPort) ? "-" : reader.ConnectedPort);
        }
        else
        {
            SetStatus("Desconectado");
            SetPort("-");
        }

        RefreshValues();
    }

    private void RefreshValues()
    {
        if (reader == null) return;

        SetRawValues(reader.RawSpeed.ToString(), reader.RawSteering.ToString());

        reader.GetNormalizedValues(out float speed, out float steering);
        SetNormalizedValues(speed.ToString("0.00"), steering.ToString("0.00"));
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetStatus(string value)
    {
        if (connectionStatusText != null)
            connectionStatusText.text = "Estado: " + value;
    }

    private void SetPort(string value)
    {
        if (portText != null)
            portText.text = "Puerto: " + value;
    }

    private void SetRawValues(string speed, string steering)
    {
        if (speedText != null)
            speedText.text = "Velocidad raw: " + speed;

        if (steeringText != null)
            steeringText.text = "Timón raw: " + steering;
    }

    private void SetNormalizedValues(string speed, string steering)
    {
        if (normalizedSpeedText != null)
            normalizedSpeedText.text = "Velocidad normalizada: " + speed;

        if (normalizedSteeringText != null)
            normalizedSteeringText.text = "Timón normalizado: " + steering;
    }
}