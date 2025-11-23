using UnityEngine;

public class DeformableSphere : MonoBehaviour
{
    public float deformationRadius = 0.5f;  // Affects how much area deforms
    public float deformationStrength = 0.1f; // How deep the deformation goes

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] modifiedVertices;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        modifiedVertices = mesh.vertices;
    }

    void OnCollisionEnter(Collision collision)
    {
        Vector3 hitPoint = collision.contacts[0].point;

        for (int i = 0; i < modifiedVertices.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(originalVertices[i]);
            float distance = Vector3.Distance(worldPos, hitPoint);

            if (distance < deformationRadius)
            {
                float deformation = (1 - (distance / deformationRadius)) * deformationStrength;
                modifiedVertices[i] -= transform.InverseTransformDirection(collision.contacts[0].normal) * deformation;
            }
        }

        mesh.vertices = modifiedVertices;
        mesh.RecalculateNormals();
    }
}
