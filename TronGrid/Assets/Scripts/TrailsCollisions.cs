using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class TrailsCollisions : MonoBehaviour
{
    public float colliderRadius = 0.2f;
    public int maxColliders = 100;

    private TrailRenderer trail;
    private List<GameObject> colliderPool = new List<GameObject>();

    void Awake()
    {
        trail = GetComponent<TrailRenderer>();
    }

    void Update()
    {
        UpdateCollidersFromTrail();
    }

    void UpdateCollidersFromTrail()
    {
        int pointCount = trail.positionCount; // Solo puntos visibles actuales

        EnsureColliderPoolSize(pointCount);

        for (int i = 0; i < colliderPool.Count; i++)
        {
            GameObject colGO = colliderPool[i];
            if (i < pointCount)
            {
                Vector3 pos = trail.GetPosition(i);
                colGO.transform.position = pos;
                colGO.SetActive(true);
            }
            else
            {
                colGO.SetActive(false); // Desactivar coliders que ya no están en la estela visible
            }
        }
    }

    void EnsureColliderPoolSize(int size)
    {
        while (colliderPool.Count < size && colliderPool.Count < maxColliders)
        {
            GameObject col = new GameObject("TrailCollider3D");
            SphereCollider sc = col.AddComponent<SphereCollider>();
            sc.radius = colliderRadius;
            sc.isTrigger = true; // Solo detección de colisiones
            col.transform.parent = transform;

            // Añadir componente para detectar colisiones, si quieres
            // col.AddComponent<TrailHit>();

            colliderPool.Add(col);
        }
    }
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Colisión con " + other.name);
    }

}
