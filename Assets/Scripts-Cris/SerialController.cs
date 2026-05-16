using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

/// <summary>
/// Lee el puerto serial del Arduino en un hilo separado y expone
/// los valores parseados para que los demas scripts los consuman.
/// </summary>
public class SerialController : MonoBehaviour
{
    [Header("Configuracion Puerto Serial")]
    [Tooltip("Ejemplo: COM3  /  /dev/ttyUSB0")]
    public string portName = "COM3";
    public int baudRate = 9600;

    // Valores parseados, accesibles desde otros scripts
    [HideInInspector] public int  potValue   = 512;  // 0-1023
    [HideInInspector] public bool anclaOn    = false;
    [HideInInspector] public bool canonFired = false; // pulso de 1 frame

    private SerialPort _port;
    private Thread     _readThread;
    private volatile bool _running = false;

    // Buffer entre hilo y hilo principal
    private readonly object _lock = new object();
    private int  _rawPot   = 512;
    private bool _rawAncla = false;
    private bool _rawCanon = false;

    void Start()
    {
        try
        {
            _port = new SerialPort(portName, baudRate) { ReadTimeout = 100 };
            _port.Open();
            _running = true;
            _readThread = new Thread(ReadLoop) { IsBackground = true };
            _readThread.Start();
            Debug.Log($"[Serial] Conectado a {portName} @ {baudRate}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Serial] No se pudo abrir {portName}: {e.Message}\n" +
                              "Funcionando en modo teclado.");
        }
    }

    void Update()
    {
        lock (_lock)
        {
            potValue   = _rawPot;
            anclaOn    = _rawAncla;
            canonFired = _rawCanon;
            _rawCanon  = false; // consumido
        }
    }

    void OnDestroy()
    {
        _running = false;
        _readThread?.Join(200);
        if (_port != null && _port.IsOpen) _port.Close();
    }

    // ── Hilo de lectura ──────────────────────────────────────────────────────
    private void ReadLoop()
    {
        while (_running)
        {
            if (_port == null || !_port.IsOpen) { Thread.Sleep(100); continue; }
            try
            {
                string line = _port.ReadLine();
                ParseLine(line.Trim());
            }
            catch (TimeoutException) { }
            catch (Exception e)
            {
                Debug.LogWarning($"[Serial] Error lectura: {e.Message}");
            }
        }
    }

    // Formato esperado: POT:512,ANCLA:1,CANON:0
    private void ParseLine(string line)
    {
        string[] parts = line.Split(',');
        int  pot   = _rawPot;
        bool ancla = _rawAncla;
        bool canon = false;

        foreach (string part in parts)
        {
            string[] kv = part.Split(':');
            if (kv.Length != 2) continue;
            switch (kv[0])
            {
                case "POT":   int.TryParse(kv[1], out pot);          break;
                case "ANCLA": ancla = kv[1] == "1";                  break;
                case "CANON": canon = kv[1] == "1";                  break;
            }
        }

        lock (_lock)
        {
            _rawPot   = pot;
            _rawAncla = ancla;
            if (canon) _rawCanon = true; // preserva el pulso hasta que Update lo lea
        }
    }
}
