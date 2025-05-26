using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotoTronController : MonoBehaviour
{
    [Header("Moto Settings")]
    public float maxSpeed = 20f;
    public float acceleration = 5f;
    public float brakeSpeed = 5f;
    public float turnSpeed = 100f;
    public float minSpeedToTurn = 1f;

    [Header("Visual Steering")]
    public Transform steeringVisual;
    public float maxVisualTurnAngle = 30f;

    [Header("Dynamic Turning")]
    public float turnSpeedAtLowSpeed = 150f;
    public float turnSpeedAtMaxSpeed = 30f;

    [Header("Wheel Rotation")]
    public Transform frontWheel;
    public float wheelRadius = 0.33f;

    [Header("Turbo Settings")]
    public float velocidadTurbo = 30f;
    public float tiempoMaximoTurbo = 20f;
    public GameObject[] turboTrails;
    public float trailSwayAmount = 0.04f;

    [Header("Trail Collision Settings")]
    public TrailsCollisions trailCollision;

    public AudioClip audioNormal;
    public AudioClip audioTurbo;
    public float duracionFade = 1.0f;

    private float currentSpeed = 0f;
    private float verticalInput = 0f;
    private float horizontalInput = 0f;
    private bool turboActivo = false;
    private float tiempoTurbo;
    private bool muerte = false;

    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;
    private bool tieneAgentAI = false;
    private int nReapariciones=0;


    private AudioSource efectoSonido;
    private Coroutine fadeCoroutine;

    public static List<MotoTronController> TodasLasMotos = new List<MotoTronController>();

    private void OnEnable()
    {
        if (!TodasLasMotos.Contains(this))
            TodasLasMotos.Add(this);
    }

    private void OnDisable()
    {
        TodasLasMotos.Remove(this);
    }

    private void OnDestroy()
    {
        TodasLasMotos.Remove(this);
    }

    void OnApplicationQuit()
    {
        TodasLasMotos.Clear();
    }


    private void Start()
    {
        efectoSonido = GetComponent<AudioSource>();
        efectoSonido.clip = audioNormal;
        efectoSonido.volume = 0f;
        tiempoTurbo = tiempoMaximoTurbo;

        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;

        tieneAgentAI = GetComponent<AgentMotoController>() != null;
    }

    void Update()
    {
        ApplyMovement();

        if (turboActivo)
        {
            tiempoTurbo -= Time.deltaTime;

            if (tiempoTurbo <= 0f)
            {
                DesactivarTurbo();
            }
        }
        else
        {
            if (tiempoTurbo < tiempoMaximoTurbo)
            {
                tiempoTurbo += Time.deltaTime * 0.5f;
                tiempoTurbo = Mathf.Min(tiempoTurbo, tiempoMaximoTurbo);
            }
        }
    }

    public void SetInputs(float vertical, float horizontal)
    {
        if (muerte) return;
        verticalInput = Mathf.Clamp(vertical, -1f, 1f);
        horizontalInput = Mathf.Clamp(horizontal, -1f, 1f);
    }

    private void ApplyMovement()
    {
        if (muerte)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeSpeed * Time.deltaTime * 2);
            DetenerSonido();
        }
        else
        {
            if (turboActivo)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, velocidadTurbo, acceleration * Time.deltaTime);
            }
            else
            {
                if (Mathf.Abs(verticalInput) > 0.01f)
                {
                    IniciarSonido();

                    currentSpeed += verticalInput * acceleration * Time.deltaTime;

                    if (currentSpeed < 0f && turboActivo)
                    {
                        currentSpeed = 0f;
                    }
                }
                else
                {
                    currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeSpeed * Time.deltaTime);
                    DetenerSonido();
                }

                float minSpeed = turboActivo ? 0f : -1f;
                currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
            }
        }

        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

        if (Mathf.Abs(currentSpeed) >= minSpeedToTurn)
        {
            float speedFactor = Mathf.InverseLerp(0f, maxSpeed, Mathf.Abs(currentSpeed));
            float dynamicTurnSpeed = Mathf.Lerp(turnSpeedAtLowSpeed, turnSpeedAtMaxSpeed, speedFactor);
            float turn = horizontalInput * dynamicTurnSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up * turn);
        }

        UpdateSteeringVisual();
        UpdateWheelRotation();
        UpdateTurboTrailSway();
    }

    private void UpdateSteeringVisual()
    {
        if (muerte) return;

        if (steeringVisual != null)
        {
            float visualTurn = -horizontalInput * maxVisualTurnAngle;
            steeringVisual.localRotation = Quaternion.Euler(0f, 0f, visualTurn);
        }
    }

    private void UpdateWheelRotation()
    {
        if (muerte) return;

        if (frontWheel == null) return;

        float distanceMoved = currentSpeed * Time.deltaTime;
        float rotationAngle = (distanceMoved / wheelRadius) * Mathf.Rad2Deg;
        frontWheel.Rotate(Vector3.right, rotationAngle, Space.Self);
    }

    public void ActivarTurbo()
    {
        if (muerte) return;

        if (turboActivo || tiempoTurbo <= 5f) return;

        turboActivo = true;
        IniciarSonido(audioTurbo);

        foreach (GameObject trail in turboTrails)
        {
            if (trail != null)
            {
                TrailRenderer tr = trail.GetComponent<TrailRenderer>();
                if (tr != null)
                    tr.emitting = true;
            }
        }

        if (trailCollision != null)
        {
            trailCollision.enableTrailsCollision = true;
        }
    }

    public void DesactivarTurbo()
    {
        if (muerte) return;

        if (!turboActivo) return;

        turboActivo = false;
        IniciarSonido(audioNormal);

        foreach (GameObject trail in turboTrails)
        {
            if (trail != null)
            {
                TrailRenderer tr = trail.GetComponent<TrailRenderer>();
                if (tr != null)
                    tr.emitting = false;
            }
        }

        if (trailCollision != null)
        {
            trailCollision.enableTrailsCollision = false;
            trailCollision.GenerarUltimoSegmento();

        }
    }

    public void ToggleTurbo()
    {
        if (turboActivo)
            DesactivarTurbo();
        else
            ActivarTurbo();
    }

    private void UpdateTurboTrailSway()
    {
        if (muerte) return;

        if (turboTrails == null || turboTrails.Length == 0) return;

        float swayOffset = horizontalInput * trailSwayAmount;

        foreach (GameObject trail in turboTrails)
        {
            if (trail == null) continue;

            Vector3 baseLocalPos = trail.transform.localPosition;
            baseLocalPos.x = swayOffset;
            trail.transform.localPosition = baseLocalPos;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name.Contains("TrailCollider") || other.gameObject.CompareTag("GridLimit"))
        {
            Debug.Log("Colision");

            ManejarMuerte();

        }
        else if (other.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Colision Agente");

            ManejarMuerte();


        }
        else if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("Colision Jugador");

            ManejarMuerte();


        }
    }

    private void ManejarMuerte()
    {
        if (muerte) return;

        Debug.Log("Muerte detectada en: " + gameObject.name);
        transform.Find("Flynns Moto").gameObject.SetActive(false);
        DesactivarTurbo();
        muerte = true;

        if (tieneAgentAI)
            StartCoroutine(ReaparecerDespuesDeTiempo(5f));
    }

    private IEnumerator ReaparecerDespuesDeTiempo(float segundos)
    {
        nReapariciones++;
        yield return new WaitForSeconds(segundos);

        // Restaurar estado
        transform.position = posicionInicial;
        transform.rotation = rotacionInicial;
        currentSpeed = 0f;
        tiempoTurbo = tiempoMaximoTurbo;
        muerte = false;

        // Reactivar visual
        Transform visual = transform.Find("Flynns Moto");
        if (visual != null) visual.gameObject.SetActive(true);

        Debug.Log("Reapareció: " + gameObject.name);
    }


    public void IniciarSonido(AudioClip clip = null)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (clip != null && efectoSonido.clip != clip)
        {
            efectoSonido.clip = clip;
            efectoSonido.volume = 0f;
        }

        fadeCoroutine = StartCoroutine(FadeIn(efectoSonido, duracionFade));
    }

    public void DetenerSonido()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeOut(efectoSonido, duracionFade));
    }

    IEnumerator FadeIn(AudioSource source, float duration)
    {
        if (!source.isPlaying)
            source.Play();

        float targetVolume = 1f;

        while (source.volume < targetVolume)
        {
            source.volume += Time.deltaTime / duration;
            source.volume = Mathf.Min(source.volume, targetVolume);
            yield return null;
        }

        source.volume = targetVolume;
    }

    IEnumerator FadeOut(AudioSource source, float duration)
    {
        float startVolume = source.volume;

        while (source.volume > 0f)
        {
            source.volume -= Time.deltaTime / duration;
            source.volume = Mathf.Max(source.volume, 0f);
            yield return null;
        }

        source.volume = 0f;
        source.Stop();
    }

    // Variables solo de lectura 

    // Porcentaje actual de carga de turbo (0 a 1)
    public float PorcentajeTurboRestante => tiempoTurbo / tiempoMaximoTurbo;

    // Velocidad actual en unidades por segundo
    public float VelocidadActual => currentSpeed;

    // Si el turbo está activo
    public bool TurboActivo => turboActivo;

    // Posición actual de la moto
    public Vector3 PosicionActual => transform.position;

    // Rotación actual de la moto (como Quaternion)
    public Quaternion RotacionActual => transform.rotation;

    // Rotación como ángulo en Y (útil para lógica de dirección)
    public float RotacionY => transform.eulerAngles.y;

    // Dirección actual de la moto (vector normalizado hacia adelante)
    public Vector3 DireccionActual => transform.forward;

    public bool EstaMuerta => muerte;

    //Numero de veces que ha respawneado la moto
    public int reaparicionesTotales => nReapariciones;

}
