using UnityEngine;
using System.Collections;

public class DeformableCube : MonoBehaviour
{
    private MeshFilter meshFilter;
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] modifiedVertices;

    public float deformationRadius = 0.3f;
    public float deformationStrength = 0.2f;
    public float smoothSpeed = 5f;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;
        originalVertices = mesh.vertices;
        modifiedVertices = mesh.vertices;

        // Ensure there is a MeshCollider
        if (!GetComponent<MeshCollider>())
        {
            gameObject.AddComponent<MeshCollider>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HapticStylus"))
        {
            Vector3 localPoint = transform.InverseTransformPoint(other.ClosestPoint(transform.position));
            DeformMesh(localPoint, deformationStrength);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("HapticStylus"))
        {
            Vector3 localPoint = transform.InverseTransformPoint(other.ClosestPoint(transform.position));
            DeformMesh(localPoint, deformationStrength);
        }
    }

    void DeformMesh(Vector3 hitPoint, float strength)
    {
        for (int i = 0; i < modifiedVertices.Length; i++)
        {
            float distance = Vector3.Distance(hitPoint, originalVertices[i]);

            if (distance < deformationRadius)
            {
                float effect = (deformationRadius - distance) / deformationRadius * strength;
                modifiedVertices[i] += Vector3.up * effect;
            }
        }

        ApplyMeshChanges();
    }

    void ApplyMeshChanges()
    {
        mesh.vertices = modifiedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshCollider>().sharedMesh = null;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }
}
