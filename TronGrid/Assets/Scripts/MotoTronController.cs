using UnityEngine;

public class MotoTronController : MonoBehaviour
{
    [Header("Moto Settings")]
    public float maxSpeed = 20f;
    public float acceleration = 50f;
    public float brakeSpeed = 10f;
    public float turnSpeed = 100f;
    public float minSpeedToTurn = 1f;

    [Header("Visual Steering")]
    public Transform steeringVisual; // Referencia al objeto hijo (e.g. manubrio)
    public float maxVisualTurnAngle = 30f; // Ángulo máximo de rotación visual

    [Header("Dynamic Turning")]
    public float turnSpeedAtLowSpeed = 150f;
    public float turnSpeedAtMaxSpeed = 30f;

    [Header("Wheel Rotation")]
    public Transform frontWheel; // Referencia visual a la rueda delantera
    public float wheelRadius = 0.33f; // Ajusta según tu modelo



    private float currentSpeed = 0f;
    private float verticalInput = 0f;
    private float horizontalInput = 0f;

    void Update()
    {
        ApplyMovement();
    }

    public void SetInputs(float vertical, float horizontal)
    {
        verticalInput = Mathf.Clamp(vertical, -1f, 1f);
        horizontalInput = Mathf.Clamp(horizontal, -1f, 1f);
    }

    private void ApplyMovement()
    {
        // Acelerar o frenar
        if (Mathf.Abs(verticalInput) > 0.01f)
        {
            currentSpeed += verticalInput * acceleration * Time.deltaTime;
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeSpeed * Time.deltaTime);
        }

        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);

        // Movimiento hacia adelante
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

        // Giro si hay suficiente velocidad
        if (Mathf.Abs(currentSpeed) > minSpeedToTurn)
        {
            // Normalizamos la velocidad entre 0 y 1
            float speedFactor = Mathf.InverseLerp(0f, maxSpeed, Mathf.Abs(currentSpeed));

            // Calculamos el giro basado en la velocidad
            float dynamicTurnSpeed = Mathf.Lerp(turnSpeedAtLowSpeed, turnSpeedAtMaxSpeed, speedFactor);
            float turn = horizontalInput * dynamicTurnSpeed * Time.deltaTime;

            transform.Rotate(Vector3.up * turn);
        }


        UpdateSteeringVisual();
        UpdateWheelRotation();

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

        // Calcular la distancia recorrida en este frame
        float distanceMoved = currentSpeed * Time.deltaTime;

        // Ángulo de rotación: θ = distancia / radio (en radianes), luego se pasa a grados
        float rotationAngle = (distanceMoved / wheelRadius) * Mathf.Rad2Deg;

        // Girar la rueda sobre su eje local X
        frontWheel.Rotate(Vector3.right, rotationAngle, Space.Self);
    }

}
