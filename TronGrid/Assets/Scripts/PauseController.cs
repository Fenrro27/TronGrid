using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public GameObject[] GameplayUI;

    private bool isPaused = false;

    void Start()
    {
        // Asegúrate de que el menú de pausa está oculto al inicio
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Resume()
    {
        foreach (GameObject ui in GameplayUI)
        {
            ui.SetActive(true);
        }

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        Time.timeScale = 1f; // Reanudar el tiempo
        isPaused = false;
    }

    public void Pause()
    {
        foreach (GameObject ui in GameplayUI)
        {
            ui.SetActive(false);
        }

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);

        Time.timeScale = 0f; // Pausar el tiempo
        isPaused = true;
    }

    public void AbandonarPartida()
    {
        ControladorPuntuacion.instance.DetenerYDestruir();

        // Reanudar tiempo antes de cambiar de escena
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
