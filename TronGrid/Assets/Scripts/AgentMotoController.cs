using UnityEngine;
using System.Linq;

public class AgentMotoController : MonoBehaviour
{
    [Header("Referencias")]
    public MotoTronController moto;

    [Header("Sensado")]
    public float rayDistance = 10f;
    public float rayAngle = 30f;
    public Color rayColorSafe = Color.green;
    public Color rayColorHit = Color.red;

    [Header("Lógica Difusa - Turbo")]
    public float cargaTurboMinima = 0.5f;
    public float amenazaMaximaTurbo = 0.3f;
    public float velocidadMinimaTurbo = 5f;

    [Header("Ataque")]
    public float tiempoPrediccion = 1.5f;
    public float radioAtaque = 5f;

    [Header("Colisiones con Estelas y Obstáculos")]
    public LayerMask layerEstelas;     // Layer de estelas
    public LayerMask layerObstaculos;  // Layer de muros y motos

    // Control para evitar togglear el turbo muy rápido
    private float tiempoTurboMinActivo = 0.5f;
    private float tiempoDesdeCambioTurbo = 0f;
    private bool estadoTurboAnterior = false;

    void Update()
    {
        tiempoDesdeCambioTurbo += Time.deltaTime;

        // Sensado con detección combinada (estelas + obstáculos)
        float frontDist = RaycastDistConAmenazas(transform.forward);
        float leftDist = RaycastDistConAmenazas(Quaternion.AngleAxis(-rayAngle, Vector3.up) * transform.forward);
        float rightDist = RaycastDistConAmenazas(Quaternion.AngleAxis(rayAngle, Vector3.up) * transform.forward);

        float threatFront = AmenazaDifusa(frontDist);
        float threatLeft = AmenazaDifusa(leftDist);
        float threatRight = AmenazaDifusa(rightDist);

        float vertical;
        float horizontal;

        if (threatFront > 0.6f && (threatLeft > 0.6f || threatRight > 0.6f))
        {
            if (moto.TurboActivo) ControlTurbo(false);
            vertical = 0f;
            horizontal = threatLeft < threatRight ? -1f : 1f;
        }
        else
        {
            MotoTronController objetivo = BuscarObjetivo();

            if (objetivo != null && threatFront < 0.4f)
            {
                Vector3 puntoPredicho = objetivo.PosicionActual + objetivo.DireccionActual * objetivo.VelocidadActual * tiempoPrediccion;
                Vector3 dirToInterception = (puntoPredicho - transform.position).normalized;
                float angleToTarget = Vector3.SignedAngle(transform.forward, dirToInterception, Vector3.up);

                vertical = 1f;
                horizontal = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);

                float distanciaAlPunto = Vector3.Distance(moto.PosicionActual, puntoPredicho);

                float agresividadTurbo = Mathf.Clamp01(1f - (distanciaAlPunto / radioAtaque));
                if (!moto.TurboActivo && distanciaAlPunto < radioAtaque && moto.PorcentajeTurboRestante >= cargaTurboMinima)
                {
                    if (agresividadTurbo > 0.5f)
                        ControlTurbo(true);
                }
            }
            else
            {
                vertical = Mathf.Clamp01(1f - threatFront);
                horizontal = DecidirDireccion(threatFront, threatLeft, threatRight);
            }

            bool objetivoLejano = objetivo != null && Vector3.Distance(moto.PosicionActual, objetivo.PosicionActual) > 20f;

            bool puedeActivarTurbo =
                moto.PorcentajeTurboRestante >= cargaTurboMinima &&
                moto.VelocidadActual >= velocidadMinimaTurbo &&
                !moto.TurboActivo &&
                (objetivoLejano && threatFront < amenazaMaximaTurbo);

            if (puedeActivarTurbo)
                ControlTurbo(true);
            else if (moto.TurboActivo && threatFront > amenazaMaximaTurbo)
                ControlTurbo(false);
        }

        moto.SetInputs(vertical, horizontal);
    }

    void ControlTurbo(bool activar)
    {
        if (activar != estadoTurboAnterior && tiempoDesdeCambioTurbo >= tiempoTurboMinActivo)
        {
            moto.ToggleTurbo(); // Esto activa o desactiva el turbo

            estadoTurboAnterior = activar;
            tiempoDesdeCambioTurbo = 0f;
        }
    }

    MotoTronController BuscarObjetivo()
    {
        return MotoTronController.TodasLasMotos
            .Where(m => m != moto && m != null && !m.EstaMuerta)
            .OrderBy(m => Vector3.Distance(moto.PosicionActual, m.PosicionActual))
            .FirstOrDefault();
    }

    float RaycastDist(Vector3 direction, LayerMask layer)
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, rayDistance, layer))
        {
            Debug.DrawRay(transform.position, direction * hit.distance, rayColorHit);
            return hit.distance;
        }
        Debug.DrawRay(transform.position, direction * rayDistance, rayColorSafe);
        return rayDistance;
    }

    // Devuelve la distancia mínima entre la estela y los obstáculos normales
    float RaycastDistConAmenazas(Vector3 direction)
    {
        float distEstela = RaycastDist(direction, layerEstelas);
        float distObstaculo = RaycastDist(direction, layerObstaculos);
        float distanciaMin = Mathf.Min(distEstela, distObstaculo);
        return distanciaMin;
    }

    float AmenazaDifusa(float distancia)
    {
        if (distancia >= rayDistance) return 0f;
        if (distancia <= 1f) return 1f;
        return 1f - ((distancia - 1f) / (rayDistance - 1f));
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
