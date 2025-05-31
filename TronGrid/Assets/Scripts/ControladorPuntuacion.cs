using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ControladorPuntuacion : MonoBehaviour
{
    public static ControladorPuntuacion instance;

    public string nombreJugador;

    private static bool destruidaManual = false;

    private int puntosJugador = 0;

    private TextMeshProUGUI puntuacion;

    private RecordsController records;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += asignarDependencias;

            if (destruidaManual)
            {
                destruidaManual = false;
            }
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void puntuar(int puntos)
    {
        puntosJugador += puntos;
        puntuacion.text = puntosJugador.ToString();
    }

    private void asignarDependencias(Scene scene, LoadSceneMode mode)
    {

        if (SceneManager.GetActiveScene().name == "GridScene")
        {
            puntuacion = GameObject.Find("Puntuacion")?.GetComponent<TextMeshProUGUI>();
            records = GameObject.Find("ControladorRecords")?.GetComponent<RecordsController>();
        }
    }

    public void record() {
        records.guardarRecord(nombreJugador, puntosJugador);
    }

    public void DetenerYDestruir()
    {
        instance = null;
        destruidaManual = true;
        Destroy(gameObject);
    }
}
