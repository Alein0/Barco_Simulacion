using UnityEngine;
using UnityEngine.UI;

public class PauseMenuHUD : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button quitButton;

    private bool _isPaused = false;
    private bool _escKeyLast = false;

    void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (quitButton != null)
            quitButton.onClick.AddListener(Quit);

        Debug.Log("[Pausa] PauseMenuManager iniciado");
    }

    void Update()
    {
        HandleEscapeInput();
    }

    private void HandleEscapeInput()
    {
        bool escKey = Input.GetKey(KeyCode.Escape);

        if (escKey && !_escKeyLast)
        {
            _isPaused = !_isPaused;

            if (_isPaused)
                Pause();
            else
                Resume();
        }

        _escKeyLast = escKey;
    }

    private void Pause()
    {
        _isPaused = true;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (SimulationManager.Instance != null)
            SimulationManager.Instance.Pause();

        Time.timeScale = 0f; // Pausar el tiempo de Unity también

        Debug.Log("[Pausa] Juego pausado");
    }

    private void Resume()
    {
        _isPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (SimulationManager.Instance != null)
            SimulationManager.Instance.Play();

        Time.timeScale = 1f; // Reanudar el tiempo

        Debug.Log("[Pausa] Juego reanudado");
    }

    private void Quit()
    {
        Time.timeScale = 1f; // Restaurar timeScale antes de salir

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    public bool IsPaused => _isPaused;
}