using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(TrailRenderer))]
public class TrailsCollisions : MonoBehaviour
{
    [Header("Settings")]
    public bool debug = true;
    public bool enableTrailsCollision = true; // Visible y editable en Inspector
    public float segmentSpacing = 0.2f;
    public float colliderLifetime = 2f;
    public float colliderThickness = 0.2f;
    public float offsetBehind = 0.5f;

    private TrailRenderer trail;
    private List<Vector3> points = new List<Vector3>();
    private float timeSinceLastPoint = 0f;

    // Para detectar cambios en tiempo real
    private bool previousCollisionState = true;
    private Transform collidersParent;


    void Start()
    {
        trail = GetComponent<TrailRenderer>();
        points.Clear();
        points.Add(transform.position);
        previousCollisionState = enableTrailsCollision;

        // Crear o encontrar un objeto vacío como contenedor de colisiones
        GameObject parentObj = GameObject.Find("TrailCollidersRoot");
        if (parentObj == null)
        {
            parentObj = new GameObject("TrailCollidersRoot");
        }
        collidersParent = parentObj.transform;
    }


    void Update()
    {
        // Detectamos si se reactivó la generación
        if (!previousCollisionState && enableTrailsCollision)
        {
            points.Clear();
            points.Add(transform.position);
            timeSinceLastPoint = 0f;
        }
        previousCollisionState = enableTrailsCollision;

        if (!enableTrailsCollision)
            return;

        timeSinceLastPoint += Time.deltaTime;
        if (timeSinceLastPoint >= segmentSpacing)
        {
            Vector3 currentPos = transform.position;
            if ((points[points.Count - 1] - currentPos).sqrMagnitude > 0.001f)
            {
                points.Add(currentPos);
                CreateColliderSegment(points[points.Count - 2], currentPos);
                timeSinceLastPoint = 0f;
            }
        }
    }

    void CreateColliderSegment(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;

        if (length < 0.01f) return;

        Vector3 forward = direction.normalized;
        Vector3 midPoint = (start + end) / 2f;
        Vector3 offsetPosition = midPoint - forward * offsetBehind;

        GameObject colliderObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        colliderObj.name = "TrailCollider";
        colliderObj.transform.parent = collidersParent;
        colliderObj.layer = LayerMask.NameToLayer("Estelas");


        colliderObj.transform.position = offsetPosition;
        colliderObj.transform.rotation = Quaternion.LookRotation(forward);
        colliderObj.transform.localScale = new Vector3(colliderThickness, colliderThickness, length);

        BoxCollider box = colliderObj.GetComponent<BoxCollider>();
        if (!box) box = colliderObj.AddComponent<BoxCollider>();

        box.isTrigger = true; 

        if (!debug)
        {
            Destroy(colliderObj.GetComponent<MeshRenderer>());
            Destroy(colliderObj.GetComponent<MeshFilter>());
        }
        else
        {
            var renderer = colliderObj.GetComponent<Renderer>();
            Material debugMat = new Material(Shader.Find("Unlit/Color"));
            debugMat.color = Color.green;
            renderer.material = debugMat;
        }

        Destroy(colliderObj, colliderLifetime);
    }

    public void GenerarUltimoSegmento()
    {
        if (points.Count >= 2)
        {
            Vector3 penultimo = points[points.Count - 2];
            Vector3 ultimo = points[points.Count - 1];
            CreateColliderSegment(penultimo, ultimo);
        }
    }


}
