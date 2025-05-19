// Attach this to TrailAnchor
using UnityEngine;

public class FixedRotation : MonoBehaviour
{
    void LateUpdate()
    {
        transform.rotation = Quaternion.identity; // o solo bloquear eje Y si prefieres
    }
}
