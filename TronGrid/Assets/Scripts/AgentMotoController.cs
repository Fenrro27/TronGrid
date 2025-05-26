using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class AgentMotoController : MonoBehaviour
{
    [Header("Referencias")]
    public MotoTronController moto;

    [Header("Sensado")]
    public float rayDistance = 10f;
    public float rayAngle = 30f;
    public Color rayColorSafe = Color.green;
    public Color rayColorHit = Color.red;

    [Header("Persecución")]
    public float tiempoPrediccion = 1.5f;

    [Header("Colisiones")]
    public LayerMask layerAmenazas;

    [Header("Turbo Ofensivo")]
    public float distanciaMaxTurboOfensivo = 20f;
    public float amenazaMaximaTurbo = 0.3f;
    public float cargaTurboMinima = 0.5f;
    public float velocidadMinimaTurbo = 5f;

    private bool estadoTurboAnterior = false;
    private bool soloPerseguirPlayers;
    private bool esKamikaze;
    private bool usarObjetivoAleatorio;


    private int reaparicionesPrevias = -1;
    private MotoTronController objetivoActual;

    void Start()
    {
        RecalcularObjetivoYModo();
        reaparicionesPrevias = moto.reaparicionesTotales;
    }

    void Update()
    {
        // Verificar si se ha incrementado el contador de reapariciones
        if (moto.reaparicionesTotales != reaparicionesPrevias)
        {
            reaparicionesPrevias = moto.reaparicionesTotales;
            RecalcularObjetivoYModo();
        }

        // Sensado
        float frontDist = RaycastDist(transform.forward);
        float leftDist = RaycastDist(Quaternion.AngleAxis(-rayAngle, Vector3.up) * transform.forward);
        float rightDist = RaycastDist(Quaternion.AngleAxis(rayAngle, Vector3.up) * transform.forward);

        float threatFront = AmenazaDifusa(frontDist);
        float threatLeft = AmenazaDifusa(leftDist);
        float threatRight = AmenazaDifusa(rightDist);

        float vertical;
        float horizontal;

        // Comportamiento kamikaze (solo si hay objetivo)
        bool puedeSerKamikaze = esKamikaze && objetivoActual != null;

        if (puedeSerKamikaze)
        {
            Vector3 dirToTarget = (objetivoActual.PosicionActual - transform.position).normalized;
            float angleToTarget = Vector3.SignedAngle(transform.forward, dirToTarget, Vector3.up);

            vertical = 1f;
            horizontal = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);

            moto.SetInputs(vertical, horizontal);
            Debug.Log("Modo kamikaze activado");
            return;
        }

        // Evaluación de amenazas normales
        if (threatFront > 0.6f && (threatLeft > 0.6f || threatRight > 0.6f))
        {
            vertical = 0f;
            horizontal = threatLeft < threatRight ? -1f : 1f;

            if (moto.TurboActivo)
                ControlTurbo(false);
        }
        else
        {
            if (objetivoActual != null)
            {
                // Persecución con predicción
                Vector3 puntoPredicho = objetivoActual.PosicionActual + objetivoActual.DireccionActual * objetivoActual.VelocidadActual * tiempoPrediccion;
                Vector3 dirToInterception = (puntoPredicho - transform.position).normalized;
                float angleToTarget = Vector3.SignedAngle(transform.forward, dirToInterception, Vector3.up);

                vertical = 1f;
                horizontal = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);

                // Evaluación de ofensiva
                float distanciaAlObjetivo = Vector3.Distance(moto.PosicionActual, objetivoActual.PosicionActual);
                float agresividad = AgresividadDifusa(distanciaAlObjetivo, distanciaMaxTurboOfensivo);

                bool puedeActivarTurbo =
                    distanciaAlObjetivo < distanciaMaxTurboOfensivo &&
                    moto.PorcentajeTurboRestante >= cargaTurboMinima &&
                    moto.VelocidadActual >= velocidadMinimaTurbo &&
                    threatFront < amenazaMaximaTurbo &&
                    !moto.TurboActivo;

                if (puedeActivarTurbo && agresividad > 0.5f)
                {
                    ControlTurbo(true);
                    Debug.Log("Turbo ofensivo activado (agresividad: " + agresividad.ToString("F2") + ")");
                }

                if (moto.TurboActivo)
                {
                    bool debeDesactivar =
                        distanciaAlObjetivo > distanciaMaxTurboOfensivo ||
                        threatFront > amenazaMaximaTurbo ||
                        agresividad < 0.3f;

                    if (debeDesactivar)
                    {
                        ControlTurbo(false);
                        Debug.Log("Turbo desactivado para conservar energía (agresividad: " + agresividad.ToString("F2") + ")");
                    }
                }
            }
            else
            {
                vertical = Mathf.Clamp01(1f - threatFront);
                horizontal = DecidirDireccion(threatFront, threatLeft, threatRight);
            }
        }

        moto.SetInputs(vertical, horizontal);
    }

    void RecalcularObjetivoYModo()
    {
        Debug.Log("Recalculando Objetivo Y Modo");

        soloPerseguirPlayers = Random.value <= 0.5f;
        esKamikaze = Random.value <= 0.05f;
        usarObjetivoAleatorio = Random.value <= 0.4f; // 30% de probabilidad de objetivo aleatorio

        if (soloPerseguirPlayers) Debug.Log("Sigue a jugador");

        // Intentar obtener objetivos según la prioridad: jugadores -> cualquiera
        var posiblesObjetivos = MotoTronController.TodasLasMotos
            .Where(m => m != moto && m != null && !m.EstaMuerta)
            .ToList();

        List<MotoTronController> candidatos = soloPerseguirPlayers
            ? posiblesObjetivos.Where(m => m.CompareTag("Player")).ToList()
            : posiblesObjetivos;

        // Si no hay jugadores disponibles, usar todos
        if (candidatos.Count == 0)
        {
            candidatos = posiblesObjetivos;
            soloPerseguirPlayers = false;
        }
        if (candidatos.Count == 0)
        {
            objetivoActual = null;
            return;
        }

        if (usarObjetivoAleatorio && !soloPerseguirPlayers)
        {
            int indice = Random.Range(0, candidatos.Count);
            objetivoActual = candidatos[indice];
            Debug.Log("Objetivo aleatorio seleccionado");
        }
        else
        {
            objetivoActual = candidatos
                .OrderBy(m => Vector3.Distance(moto.PosicionActual, m.PosicionActual))
                .FirstOrDefault();
            Debug.Log("Objetivo más cercano seleccionado");
        }
    }



    void ControlTurbo(bool activar)
    {
        if (activar != estadoTurboAnterior)
        {
            moto.ToggleTurbo();
            estadoTurboAnterior = activar;
        }
    }

    float RaycastDist(Vector3 direction)
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, rayDistance, layerAmenazas))
        {
            Debug.DrawRay(transform.position, direction * hit.distance, rayColorHit);
            return hit.distance;
        }
        Debug.DrawRay(transform.position, direction * rayDistance, rayColorSafe);
        return rayDistance;
    }

    float AmenazaDifusa(float distancia)
    {
        if (distancia >= rayDistance) return 0f;
        if (distancia <= 1f) return 1f;
        return 1f - ((distancia - 1f) / (rayDistance - 1f));
    }

    float AgresividadDifusa(float distancia, float distanciaMax)
    {
        if (distancia >= distanciaMax) return 0f;
        if (distancia <= 5f) return 1f;
        return 1f - ((distancia - 5f) / (distanciaMax - 5f));
    }

    float DecidirDireccion(float amenazaFrente, float amenazaIzquierda, float amenazaDerecha)
    {
        if (amenazaFrente > 0.6f)
        {
            if (amenazaIzquierda < amenazaDerecha) return -1f;
            if (amenazaDerecha < amenazaIzquierda) return 1f;
            return Random.value < 0.5f ? -1f : 1f;
        }
        return 0f;
    }
}
