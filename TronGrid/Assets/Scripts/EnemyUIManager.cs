using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class EnemyUIManager : MonoBehaviour
{
    [Header("C�mara principal")]
    public Camera camara;

    [Header("Canvas UI")]
    public RectTransform canvasTransform;

    [Header("Prefab del marcador (Image UI)")]
    public GameObject marcadorPrefab;

    [Header("Enemigos a seguir (asignar manualmente)")]
    public List<Transform> enemigos = new List<Transform>();

    [Header("Escalado de marcador por distancia")]
    public float distanciaMaxVisible = 50f;
    public float distanciaMinOculta = 3f;
    public Vector2 tama�oMax = new Vector2(40f, 40f);
    public Vector2 tama�oMin = new Vector2(15f, 15f);

    private Dictionary<Transform, RectTransform> marcadores = new Dictionary<Transform, RectTransform>();

    void Start()
    {
        if (camara == null)
        {
            camara = Camera.main;
        }

        foreach (Transform enemigo in enemigos)
        {
            if (enemigo != null && !marcadores.ContainsKey(enemigo))
            {
                GameObject nuevoMarcador = Instantiate(marcadorPrefab, canvasTransform);
                RectTransform rt = nuevoMarcador.GetComponent<RectTransform>();
                rt.gameObject.SetActive(false);
                marcadores.Add(enemigo, rt);
            }
        }
    }

    void Update()
    {
        List<(Transform enemigo, float distancia, Vector3 screenPos)> visibles = new List<(Transform, float, Vector3)>();

        foreach (Transform enemigo in enemigos)
        {
            if (enemigo == null || !marcadores.ContainsKey(enemigo)) continue;

            Vector3 worldPos = enemigo.position + Vector3.up;
            Vector3 screenPos = camara.WorldToScreenPoint(worldPos);
            float distancia = Vector3.Distance(camara.transform.position, worldPos);

            bool visible = screenPos.z > 0 &&
                           screenPos.x >= 0 && screenPos.x <= Screen.width &&
                           screenPos.y >= 0 && screenPos.y <= Screen.height &&
                           distancia > distanciaMinOculta;

            if (visible)
            {
                visibles.Add((enemigo, distancia, screenPos));
            }
        }

        // Mostrar solo los 3 m�s cercanos
        var top3 = visibles.OrderBy(e => e.distancia).Take(3).ToList();

        // Activar y actualizar los top 3
        foreach (var entry in top3)
        {
            RectTransform marcador = marcadores[entry.enemigo];
            marcador.gameObject.SetActive(true);
            marcador.position = entry.screenPos;

            float t = Mathf.InverseLerp(distanciaMaxVisible, distanciaMinOculta, entry.distancia);
            Vector2 tama�o = Vector2.Lerp(tama�oMin, tama�oMax, t);
            marcador.sizeDelta = tama�o;
        }

        // Desactivar todos los dem�s
        foreach (var kvp in marcadores)
        {
            if (!top3.Any(e => e.enemigo == kvp.Key))
            {
                kvp.Value.gameObject.SetActive(false);
            }
        }
    }
}
