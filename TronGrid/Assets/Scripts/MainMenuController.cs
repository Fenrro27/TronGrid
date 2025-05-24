using System.Collections;
using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public float velocidadRotacion = 5f;
    public float duracionFade = 0.5f;
    public float escalar1 = 0.8f;
    public float escalar2 = 1f;
    public CanvasGroup[] submenus;

    private Quaternion ejeRotacion;
    private bool rotando = false;
    private int posicion = 0;

    private float[] posiciones = { 0f, 90f, 180f, 270f };

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        posicion = 0;
        ejeRotacion = Quaternion.Euler(0f, posiciones[posicion], 0f);
        transform.rotation = ejeRotacion;

        for (int i = 0; i < submenus.Length; i++)
        {
            submenus[i].alpha = (i == posicion) ? 1f : 0f;
            submenus[i].interactable = (i == posicion);
            submenus[i].blocksRaycasts = (i == posicion);

            Transform panelTransform = submenus[i].transform;
            panelTransform.localScale = (i == posicion) ? Vector3.one : Vector3.one * escalar1;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!rotando)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                rotarIzquierda();
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                rotarDerecha();
            }
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, ejeRotacion, velocidadRotacion * Time.deltaTime * 100);

            if (Quaternion.Angle(transform.rotation, ejeRotacion) < 0.1f)
            {
                transform.rotation = ejeRotacion;
                rotando = false;
                StartCoroutine(efectosSubmenus());
            }
        }
    }

    void rotarIzquierda()
    {
        if (posicion == 0)
            posicion = posiciones.Length - 1;
        else
            posicion--;

        modificarEjeRotacion();
    }

    void rotarDerecha()
    {
        posicion = (posicion + 1) % posiciones.Length;
        modificarEjeRotacion();
    }

    void modificarEjeRotacion()
    {
        ejeRotacion = Quaternion.Euler(0f, posiciones[posicion], 0f);
        rotando = true;
        actualizarSubmenus();
    }

    void actualizarSubmenus()
    {
        for (int i = 0; i < submenus.Length; i++)
        {
            if (i == posicion)
            {
                submenus[i].alpha = 1f;
                submenus[i].interactable = true;
                submenus[i].blocksRaycasts = true;
            }
            else
            {
                submenus[i].alpha = 0f;
                submenus[i].interactable = false;
                submenus[i].blocksRaycasts = false;
            }
        }
    }

    IEnumerator efectosSubmenus()
    {
        for (int i = 0; i < submenus.Length; i++)
        {
            bool isActive = (i == posicion);

            // Lanza animaciones de alpha y escala
            StartCoroutine(fadeSubmenu(submenus[i], submenus[i].alpha, isActive ? 1f : 0f));
            StartCoroutine(escalarSubmenu(submenus[i].transform, isActive ? escalar1 : escalar2, isActive ? escalar2 : escalar1));

            // Actualiza estado interactivo
            submenus[i].interactable = isActive;
            submenus[i].blocksRaycasts = isActive;
        }

        yield return null;
    }

    IEnumerator fadeSubmenu(CanvasGroup submenu, float start, float end)
    {
        float elapsed = 0f;
        while (elapsed < duracionFade)
        {
            elapsed += Time.deltaTime;
            submenu.alpha = Mathf.Lerp(start, end, elapsed / duracionFade);
            yield return null;
        }
        submenu.alpha = end;
    }

    IEnumerator escalarSubmenu(Transform submenu, float start, float end)
    {
        float elapsed = 0f;
        while (elapsed < duracionFade)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(start, end, elapsed / duracionFade);
            submenu.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }
        submenu.localScale = Vector3.one * end;
    }
}
