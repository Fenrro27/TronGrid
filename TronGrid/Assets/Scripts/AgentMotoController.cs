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

    [Header("Estela por Proximidad Global")]
    public float distanciaMaxEstelaGlobal = 15f;
    public float agresividadGlobalUmbral = 0.6f;

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
        if (moto.reaparicionesTotales != reaparicionesPrevias)
        {
            reaparicionesPrevias = moto.reaparicionesTotales;
            RecalcularObjetivoYModo();
        }

        float frontDist = RaycastDist(transform.forward);
        float leftDist = RaycastDist(Quaternion.AngleAxis(-rayAngle, Vector3.up) * transform.forward);
        float rightDist = RaycastDist(Quaternion.AngleAxis(rayAngle, Vector3.up) * transform.forward);

        float threatFront = AmenazaDifusa(frontDist);
        float threatLeft = AmenazaDifusa(leftDist);
        float threatRight = AmenazaDifusa(rightDist);

        float vertical;
        float horizontal;

        if (esKamikaze && objetivoActual != null)
        {
            Vector3 dirToTarget = (objetivoActual.PosicionActual - transform.position).normalized;
            float angleToTarget = Vector3.SignedAngle(transform.forward, dirToTarget, Vector3.up);
            vertical = 1f;
            horizontal = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);
            moto.SetInputs(vertical, horizontal);

            // Activar turbo si está cerca del objetivo
            float distanciaAlObjetivo = Vector3.Distance(moto.PosicionActual, objetivoActual.PosicionActual);
            if (distanciaAlObjetivo < 10f && !moto.TurboActivo)
            {
                ControlTurbo(true);
                Debug.Log("Kamikaze activó turbo por proximidad");
            }
            else if (distanciaAlObjetivo >= 10f && moto.TurboActivo)
            {
                ControlTurbo(false);
                Debug.Log("Kamikaze desactivó turbo por distancia");
            }

            Debug.Log("Modo kamikaze activado");
            return;
        }


        if (objetivoActual != null)
        {
            Vector3 puntoPredicho = objetivoActual.PosicionActual + objetivoActual.DireccionActual * objetivoActual.VelocidadActual * tiempoPrediccion;
            Vector3 dirToInterception = (puntoPredicho - transform.position).normalized;
            float angleToTarget = Vector3.SignedAngle(transform.forward, dirToInterception, Vector3.up);

            vertical = 1f;
            horizontal = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);
        }
        else
        {
            vertical = Mathf.Clamp01(1f - threatFront);
            horizontal = EvaluarDireccionDifusa(threatFront, threatLeft, threatRight);
        }

        EvaluarYControlarTurbo(threatFront);
        moto.SetInputs(vertical, horizontal);
    }

    float EvaluarDireccionDifusa(float amenazaFrente, float amenazaIzquierda, float amenazaDerecha)
    {
        var reglas = new List<(float fuerza, float direccion)>
        {
            (Mathf.Min(PertenenciaAltaPorValor(amenazaFrente), PertenenciaBajaPorValor(amenazaDerecha)), 1f),
            (Mathf.Min(PertenenciaAltaPorValor(amenazaFrente), PertenenciaBajaPorValor(amenazaIzquierda)), -1f),
            (Mathf.Min(PertenenciaMediaPorValor(amenazaFrente), PertenenciaMediaPorValor(amenazaIzquierda), PertenenciaMediaPorValor(amenazaDerecha)), 0f),
            (PertenenciaBajaPorValor(amenazaFrente), 0f)
        };

        float sumaFuerzas = reglas.Sum(r => r.fuerza);
        if (sumaFuerzas == 0) return 0f;

        return reglas.Sum(r => r.fuerza * r.direccion) / sumaFuerzas;
    }

    void EvaluarYControlarTurbo(float amenazaFrontal)
    {
        if (objetivoActual == null) return;

        float distancia = Vector3.Distance(moto.PosicionActual, objetivoActual.PosicionActual);
        float velocidad = moto.VelocidadActual;
        float carga = moto.PorcentajeTurboRestante;

        float distCerca = PertenenciaCerca(distancia);
        float distMedia = PertenenciaMediaDistancia(distancia);
        float distLejos = PertenenciaLejos(distancia);

        float amenazaBaja = PertenenciaBajaPorValor(amenazaFrontal);
        float amenazaAlta = PertenenciaAltaPorValor(amenazaFrontal);

        float velLenta = PertenenciaLenta(velocidad);
        float velRapida = PertenenciaRapida(velocidad);

        float cargaAlta = PertenenciaAlta(carga);
        float cargaBaja = PertenenciaBaja(carga);

        float agresividadGlobal = AgresividadGlobalDifusa();
        bool aumentarAgresividad = agresividadGlobal > agresividadGlobalUmbral;

        if (aumentarAgresividad)
        {
            distCerca = Mathf.Max(distCerca, 0.6f);  // fuerza la distancia a parecer más corta
            cargaAlta = Mathf.Max(cargaAlta, 0.6f);  // fuerza considerar suficiente carga
        }

        List<(float fuerza, bool activar)> reglas = new List<(float, bool)>
        {
            (Mathf.Min(distCerca, amenazaBaja, velRapida, cargaAlta), true),
            (Mathf.Min(distMedia, amenazaBaja, velRapida, cargaAlta), true),
            (Mathf.Max(distLejos, amenazaAlta), false),
            (amenazaAlta, false),
            (Mathf.Min(distCerca, amenazaBaja, velLenta, cargaBaja), false)
        };

        float activarF = reglas.Where(r => r.activar).Sum(r => r.fuerza);
        float desactivarF = reglas.Where(r => !r.activar).Sum(r => r.fuerza);

        if (activarF > desactivarF && !moto.TurboActivo)
        {
            ControlTurbo(true);
            Debug.Log("Turbo activado por lógica difusa");
        }
        else if (desactivarF >= activarF && moto.TurboActivo)
        {
            ControlTurbo(false);
            Debug.Log("Turbo desactivado por lógica difusa");
        }
    }

    void RecalcularObjetivoYModo()
    {
        soloPerseguirPlayers = Random.value <= 0.6f;
        esKamikaze = Random.value <= 0.1f;
        usarObjetivoAleatorio = Random.value <= 0.6f;

        var posiblesObjetivos = MotoTronController.TodasLasMotos
            .Where(m => m != moto && m != null && !m.EstaMuerta)
            .ToList();

        List<MotoTronController> candidatos = soloPerseguirPlayers
            ? posiblesObjetivos.Where(m => m.CompareTag("Player")).ToList()
            : posiblesObjetivos;

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

        objetivoActual = usarObjetivoAleatorio && !soloPerseguirPlayers
            ? candidatos[Random.Range(0, candidatos.Count)]
            : candidatos.OrderBy(m => Vector3.Distance(moto.PosicionActual, m.PosicionActual)).FirstOrDefault();
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

    float AgresividadGlobalDifusa()
    {
        float agresividadMax = 0f;

        foreach (var otraMoto in MotoTronController.TodasLasMotos)
        {
            if (otraMoto == null || otraMoto == moto || otraMoto.EstaMuerta)
                continue;

            float distancia = Vector3.Distance(moto.PosicionActual, otraMoto.PosicionActual);
            if (distancia > distanciaMaxEstelaGlobal)
                continue;

            float agresividad = AmenazaDifusa(distancia);
            if (agresividad > agresividadMax)
                agresividadMax = agresividad;
        }

        return agresividadMax;
    }

    float PertenenciaCerca(float d) => Mathf.Clamp01((15f - d) / 10f);
    float PertenenciaMediaDistancia(float d) => Mathf.Clamp01(1f - Mathf.Abs(d - 20f) / 10f);
    float PertenenciaLejos(float d) => Mathf.Clamp01((d - 20f) / 10f);

    float PertenenciaLenta(float v) => Mathf.Clamp01((5f - v) / 5f);
    float PertenenciaRapida(float v) => Mathf.Clamp01((v - 5f) / 5f);

    float PertenenciaBaja(float p) => Mathf.Clamp01((0.3f - p) / 0.3f);
    float PertenenciaAlta(float p) => Mathf.Clamp01((p - 0.5f) / 0.5f);

    float PertenenciaBajaPorValor(float val) => Mathf.Clamp01(1f - val);
    float PertenenciaAltaPorValor(float val) => Mathf.Clamp01(val);
    float PertenenciaMediaPorValor(float val) => Mathf.Clamp01(1f - Mathf.Abs(val - 0.5f) / 0.25f);
}
