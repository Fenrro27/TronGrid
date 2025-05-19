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


    private float currentSpeed = 0f;
    private float verticalInput = 0f;
    private float horizontalInput = 0f;
    private bool turboActivo = false;
    private float tiempoTurbo = 20f; // tiempo restante o recargado

    void Update()
    {
        ApplyMovement();

        if (turboActivo)
        {
            // Consumir turbo
            tiempoTurbo -= Time.deltaTime;

            if (tiempoTurbo <= 0f)
            {
                DesactivarTurbo();
            }
        }
        else
        {
            // Recargar turbo a la mitad de velocidad (doble tiempo)
            if (tiempoTurbo < tiempoMaximoTurbo)
            {
                tiempoTurbo += Time.deltaTime * 0.5f;
                tiempoTurbo = Mathf.Min(tiempoTurbo, tiempoMaximoTurbo);
            }
        }
    }

    public void SetInputs(float vertical, float horizontal)
    {
        verticalInput = Mathf.Clamp(vertical, -1f, 1f);
        horizontalInput = Mathf.Clamp(horizontal, -1f, 1f);
    }

    private void ApplyMovement()
    {
        if (turboActivo)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, velocidadTurbo, acceleration * Time.deltaTime);
        }
        else
        {
            if (Mathf.Abs(verticalInput) > 0.01f)
            {
                currentSpeed += verticalInput * acceleration * Time.deltaTime;

                if (currentSpeed < 0f && turboActivo)
                {
                    currentSpeed = 0f; // Evita retroceso durante el turbo
                }
            }
            else
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeSpeed * Time.deltaTime);
            }

            // Limita la velocidad hacia atrás a -1 solo si el turbo está desactivado
            float minSpeed = turboActivo ? 0f : -1f;
            currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
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
        if (steeringVisual != null)
        {
            float visualTurn = -horizontalInput * maxVisualTurnAngle;
            steeringVisual.localRotation = Quaternion.Euler(0f, 0f, visualTurn);
        }
    }

    private void UpdateWheelRotation()
    {
        if (frontWheel == null) return;

        float distanceMoved = currentSpeed * Time.deltaTime;
        float rotationAngle = (distanceMoved / wheelRadius) * Mathf.Rad2Deg;
        frontWheel.Rotate(Vector3.right, rotationAngle, Space.Self);
    }

    public void ActivarTurbo()
    {
        if (turboActivo || tiempoTurbo <= 5f) return; // Tienes que tener al menos 5 segundo de turbo

        turboActivo = true;

        foreach (GameObject trail in turboTrails)
        {
            if (trail != null)
            {
                TrailRenderer tr = trail.GetComponent<TrailRenderer>();
                if (tr != null)
                    tr.emitting = true;
            }
        }
    }

    public void DesactivarTurbo()
    {
        if (!turboActivo) return;

        turboActivo = false;

        foreach (GameObject trail in turboTrails)
        {
            if (trail != null)
            {
                TrailRenderer tr = trail.GetComponent<TrailRenderer>();
                if (tr != null)
                    tr.emitting = false;
            }
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
        if (turboTrails == null || turboTrails.Length == 0) return;

        // Cantidad de desplazamiento lateral dependiendo del giro
        float swayOffset = horizontalInput * trailSwayAmount;

        foreach (GameObject trail in turboTrails)
        {
            if (trail == null) continue;

            // Guardamos la posición original para evitar acumulaciones
            Vector3 baseLocalPos = trail.transform.localPosition;
            baseLocalPos.x = swayOffset;  // Solo modificamos X
            trail.transform.localPosition = baseLocalPos;
        }
    }

}
