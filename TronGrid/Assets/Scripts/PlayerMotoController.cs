using UnityEngine;

public class PlayerMotoController : MonoBehaviour
{
    public MotoTronController moto;

    void Update()
    {
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        moto.SetInputs(vertical, horizontal);
    }
}
