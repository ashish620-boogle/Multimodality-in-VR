using UnityEngine;
using System.Collections;

public class deform : MonoBehaviour
{
    private MeshFilter meshFilter;
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] modifiedVertices;
    private bool isHovering = false;

    public float deformationRadius = 0.5f;  // Affected area size
    public float hoverDeformation = -0.3f;  // How much it sinks in on hover
    public float clickDeformation = -0.15f; // Extra depth when clicking
    public float smoothSpeed = 5f; // How smoothly it deforms and resets

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;

        originalVertices = mesh.vertices;
        modifiedVertices = mesh.vertices;
    }

    void Update()
    {
        if (DetectMouseHover(out Vector3 hitPoint, out Vector3 hitNormal))
        {
            isHovering = true;
            float depth = Input.GetMouseButton(0) ? clickDeformation : hoverDeformation;
            DeformMesh(hitPoint, hitNormal, depth);
        }
        else
        {
            if (isHovering)
            {
                isHovering = false;
                StopAllCoroutines();
                StartCoroutine(ResetMesh());
            }
        }
    }

    bool DetectMouseHover(out Vector3 hitPoint, out Vector3 hitNormal)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && hit.collider.gameObject == gameObject)
        {
            hitPoint = transform.InverseTransformPoint(hit.point); // Convert to local space
            hitNormal = transform.InverseTransformDirection(hit.normal); // Convert normal to local space
            return true;
        }

        hitPoint = Vector3.zero;
        hitNormal = Vector3.zero;
        return false;
    }

    void DeformMesh(Vector3 hitPoint, Vector3 hitNormal, float depth)
    {
        for (int i = 0; i < modifiedVertices.Length; i++)
        {
            float distance = Vector3.Distance(hitPoint, originalVertices[i]);

            if (distance < deformationRadius)
            {
                float strength = (deformationRadius - distance) / deformationRadius * depth;
                modifiedVertices[i] = Vector3.Lerp(modifiedVertices[i], originalVertices[i] + hitNormal * strength, Time.deltaTime * smoothSpeed);
            }
        }

        ApplyMeshChanges();
    }

    IEnumerator ResetMesh()
    {
        float elapsedTime = 0f;
        float resetDuration = 0.5f;

        while (elapsedTime < resetDuration)
        {
            for (int i = 0; i < modifiedVertices.Length; i++)
            {
                modifiedVertices[i] = Vector3.Lerp(modifiedVertices[i], originalVertices[i], elapsedTime / resetDuration);
            }

            ApplyMeshChanges();
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        modifiedVertices = (Vector3[])originalVertices.Clone();
        ApplyMeshChanges();
    }

    void ApplyMeshChanges()
    {
        mesh.vertices = modifiedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
