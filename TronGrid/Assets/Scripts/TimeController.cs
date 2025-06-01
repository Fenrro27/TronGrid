using UnityEngine;
using TMPro;

public class TimeController : MonoBehaviour
{
    [Header("Tiempo de la partida")]
    private float tiempoRestante;
    private bool partidaActiva = false;

    [Header("Referencias UI")]
    public TMP_Text tiempoTexto;
    public GameObject pantallaFin; // Asigna en el inspector
    public GameObject[] GameplayUI;

    void Start()
    {
        tiempoRestante = SettingsManager.MinutosDePartida * 60f;
        partidaActiva = true;

        // Asegura que la pantalla de fin esté oculta al inicio
        if (pantallaFin != null)
            pantallaFin.SetActive(false);

        // Por si se reanuda desde pausa
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (partidaActiva)
        {
            tiempoRestante -= Time.deltaTime;
            if (tiempoRestante < 0f)
                tiempoRestante = 0f;

            // Mostrar en formato mm:ss
            int minutos = Mathf.FloorToInt(tiempoRestante / 60f);
            int segundos = Mathf.FloorToInt(tiempoRestante % 60f);
            tiempoTexto.text = string.Format("{0:00}:{1:00}", minutos, segundos);

            if (tiempoRestante == 0f)
            {
                partidaActiva = false;
                FinDeLaPartida();
            }
        }
    }

    void FinDeLaPartida()
    {
        Debug.Log("¡Fin de la partida!");

        // Pausar el juego
        Time.timeScale = 0f;

        // Mostrar la pantalla de fin
        tiempoTexto.gameObject.SetActive(false);
        foreach (GameObject ui in GameplayUI)
        {
            ui.SetActive(false);
        }

        if (pantallaFin != null)
            pantallaFin.SetActive(true);

        ControladorPuntuacion.instance.record();
        ControladorPuntuacion.instance.DetenerYDestruir();
    }
}
