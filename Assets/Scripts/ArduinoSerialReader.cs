using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;

public class ArduinoSerialReader : MonoBehaviour
{
    public static ArduinoSerialReader Instance { get; private set; }

    [Header("Serial Configuration")]
    [SerializeField] private string portName = "";
    [SerializeField] private int baudRate = 9600;
    [SerializeField] private int readTimeoutMs = 100;
    [SerializeField] private bool autoConnectOnStart = true;

    [Header("Auto-Detect Settings")]
    [SerializeField] private bool autoDetectPort = true;
    [SerializeField] private float reconnectInterval = 3f;
    [SerializeField] private int scanTimeoutMs = 300;

    public int RawSpeed { get; private set; }
    public int RawSteering { get; private set; }
    public bool IsConnected { get; private set; }
    public string ConnectedPort { get; private set; }

    public System.Action<string> OnConnected;
    public System.Action OnDisconnected;
    public System.Action OnFire;
    public System.Action OnAnchorDown;
    public System.Action OnAnchorUp;

    private SerialPort serialPort;
    private Thread readThread;
    private volatile bool running;
    private readonly object dataLock = new object();
    private readonly Queue<string> commandQueue = new Queue<string>();

    private float reconnectTimer;
    private bool intentionalDisconnect;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (autoConnectOnStart) Connect();
    }

    void Update()
    {
        if (!IsConnected && !intentionalDisconnect && autoDetectPort)
        {
            reconnectTimer -= Time.deltaTime;
            if (reconnectTimer <= 0f)
            {
                reconnectTimer = reconnectInterval;
                TryAutoConnect();
            }
        }

        ProcessCommands();
    }

    public void Connect()
    {
        intentionalDisconnect = false;

        if (string.IsNullOrEmpty(portName) || autoDetectPort)
            TryAutoConnect();
        else
            ConnectTo(portName);
    }

    public void ConnectTo(string port)
    {
        if (IsConnected) Disconnect(intentional: false);

        intentionalDisconnect = false;

        try
        {
            serialPort = new SerialPort(port, baudRate)
            {
                ReadTimeout = readTimeoutMs,
                NewLine = "\n"
            };
            serialPort.Open();

            running = true;
            readThread = new Thread(ReadLoop) { IsBackground = true };
            readThread.Start();

            IsConnected = true;
            ConnectedPort = port;

            Debug.Log($"[Arduino] Connected to {port}");
            OnConnected?.Invoke(port);
        }
        catch (System.Exception e)
        {
            IsConnected = false;
            Debug.LogWarning($"[Arduino] Could not open {port}: {e.Message}");
            CleanupSerial();
        }
    }

    public void Disconnect() => Disconnect(intentional: true);

    public static string[] GetAvailablePorts() => SerialPort.GetPortNames();

    public void GetNormalizedValues(out float speed, out float steering)
    {
        lock (dataLock)
        {
            speed = (RawSpeed - 512) / 512f;
            steering = (RawSteering - 512) / 512f;
        }
        speed = Mathf.Clamp(speed, -1f, 1f);
        steering = Mathf.Clamp(steering, -1f, 1f);
    }

    private void TryAutoConnect()
    {
        string[] ports = SerialPort.GetPortNames();
        if (ports.Length == 0)
        {
            Debug.Log("[Arduino] No serial ports found.");
            return;
        }

        Debug.Log($"[Arduino] Scanning {ports.Length} port(s): {string.Join(", ", ports)}");

        foreach (string port in ports)
        {
            if (TryHandshake(port))
            {
                ConnectTo(port);
                return;
            }
        }

        Debug.Log("[Arduino] No Arduino found on any port. Will retry...");
    }

    private bool TryHandshake(string port)
    {
        SerialPort probe = null;
        try
        {
            probe = new SerialPort(port, baudRate)
            {
                ReadTimeout = scanTimeoutMs,
                NewLine = "\n"
            };
            probe.Open();

            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    string line = probe.ReadLine();
                    if (IsValidArduinoLine(line))
                    {
                        Debug.Log($"[Arduino] Found device on {port} (line: \"{line.Trim()}\")");
                        return true;
                    }
                }
                catch (System.TimeoutException) { }
            }
        }
        catch (System.Exception e)
        {
            Debug.Log($"[Arduino] Skipping {port}: {e.Message}");
        }
        finally
        {
            try { probe?.Close(); } catch { }
        }
        return false;
    }

    private bool IsValidArduinoLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return false;
        string[] parts = line.Trim().Split(',');
        return parts.Length == 2
            && int.TryParse(parts[0], out _)
            && int.TryParse(parts[1], out _);
    }

    private void ReadLoop()
    {
        while (running)
        {
            try
            {
                string line = serialPort.ReadLine();
                ParseLine(line);
            }
            catch (System.TimeoutException)
            {
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Arduino] Read error on {ConnectedPort}: {e.Message}");
                running = false;
                IsConnected = false;

                UnityMainThreadDispatcher.Enqueue(() =>
                {
                    CleanupSerial();
                    ConnectedPort = null;
                    OnDisconnected?.Invoke();
                    Debug.Log("[Arduino] Device disconnected. Will attempt reconnect...");
                });
            }
        }
    }

    private void ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        line = line.Trim();

        // Formato numérico: velocidad,timon
        string[] parts = line.Split(',');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out int speed) &&
            int.TryParse(parts[1], out int steering))
        {
            lock (dataLock)
            {
                RawSpeed = speed;
                RawSteering = steering;
            }
            return;
        }

        // Comandos de botones
        lock (commandQueue)
        {
            commandQueue.Enqueue(line);
        }
    }

    private void ProcessCommands()
    {
        lock (commandQueue)
        {
            while (commandQueue.Count > 0)
            {
                string cmd = commandQueue.Dequeue();

                if (cmd == "DISPARO")
                {
                    OnFire?.Invoke();
                }
                else if (cmd == "ANCLA_BAJA")
                {
                    OnAnchorDown?.Invoke();
                }
                else if (cmd == "ANCLA_SUBE")
                {
                    OnAnchorUp?.Invoke();
                }
            }
        }
    }

    private void Disconnect(bool intentional)
    {
        intentionalDisconnect = intentional;
        running = false;

        if (readThread != null && readThread.IsAlive)
            readThread.Join(500);

        CleanupSerial();
        IsConnected = false;
        ConnectedPort = null;

        OnDisconnected?.Invoke();
        Debug.Log($"[Arduino] Disconnected (intentional: {intentional}).");
    }

    private void CleanupSerial()
    {
        try { if (serialPort != null && serialPort.IsOpen) serialPort.Close(); }
        catch { }
        serialPort = null;
    }

    void OnApplicationQuit() => Disconnect(intentional: true);

    void OnDestroy()
    {
        if (Instance == this)
        {
            Disconnect(intentional: true);
            Instance = null;
        }
    }
}