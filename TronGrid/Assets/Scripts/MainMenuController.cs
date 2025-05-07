using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    private Transform camera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        camera = GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        // Girar a la izquierda
        if (Input.GetKeyDown(KeyCode.A))
        {
            camera.Rotate(0.0f, -90.0f, 0.0f);
        }

        // Girar a la derecha
        if (Input.GetKeyDown(KeyCode.D))
        {
            camera.Rotate(0.0f, 90.0f, 0.0f);
        }
    }
}
