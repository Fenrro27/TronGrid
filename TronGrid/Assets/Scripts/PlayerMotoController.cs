using UnityEngine;
using UnityEngine.UI;

public class PlayerMotoController : MonoBehaviour
{
    public MotoTronController moto;

    [Header("UI")]
    public Slider turboSlider;
    public Image turboFillImage; // Referencia al fill del slider

    [Header("Turbo UI Settings")]
    public Color normalColor = Color.green;
    public Color lowTurboColor = Color.red;
    public float lowTurboThreshold = 0.2f; // 20%

    void Update()
    {
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        moto.SetInputs(vertical, horizontal);


        if (Input.GetKeyDown(KeyCode.Space))
        {
            moto.ToggleTurbo();
        }

        // Verificamos y actualizamos el slider del turbo
        float turbo = moto.PorcentajeTurboRestante;
        if (turboSlider != null)
        {
            turboSlider.value = turbo;

            // Cambiar el color si está bajo
            if (turboFillImage != null)
            {
                turboFillImage.color = (turbo <= lowTurboThreshold) ? lowTurboColor : normalColor;
            }
        }
    }
}
