using UnityEngine;
using System.Collections.Generic;

public class SoftBodySphere : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] deformedVertices;
    private int[] originalTriangles;
    private List<Spring> springs = new List<Spring>();

    void Start()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("No MeshFilter found! Attach this script to a GameObject with a MeshFilter.");
            return;
        }

        mesh = meshFilter.mesh;
        if (mesh == null)
        {
            Debug.LogError("No mesh assigned to the MeshFilter!");
            return;
        }

        originalVertices = mesh.vertices;
        deformedVertices = new Vector3[originalVertices.Length];
        originalVertices.CopyTo(deformedVertices, 0);

        originalTriangles = mesh.triangles;

        if (originalTriangles.Length == 0)
        {
            Debug.LogError("Mesh has no triangles! Ensure the mesh is imported correctly.");
            return;
        }

        Debug.Log($"Mesh Loaded: {originalVertices.Length} vertices, {originalTriangles.Length / 3} triangles.");

        InitializeSprings();
        ResetMesh();  // Ensure mesh is properly assigned
    }

    void ResetMesh()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null)
        {
            mf.mesh = new Mesh(); // Create a fresh mesh
            mesh = mf.mesh;       // Reassign reference

            // Restore original mesh data
            mesh.vertices = originalVertices;
            mesh.triangles = originalTriangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            Debug.Log("Mesh successfully reset.");
        }
        else
        {
            Debug.LogError("MeshFilter not found!");
        }
    }

    void InitializeSprings()
    {
        springs.Clear();
        for (int i = 0; i < originalVertices.Length; i++)
        {
            for (int j = 0; j < originalVertices.Length; j++)
            {
                if (i != j && Vector3.Distance(originalVertices[i], originalVertices[j]) < 0.2f)
                {
                    springs.Add(new Spring(i, j, Vector3.Distance(originalVertices[i], originalVertices[j])));
                }
            }
        }
    }

    void UpdateMesh()
    {
        // Sanity check for invalid values
        for (int i = 0; i < deformedVertices.Length; i++)
        {
            if (float.IsNaN(deformedVertices[i].x) || float.IsInfinity(deformedVertices[i].x))
            {
                Debug.LogError($"Invalid vertex at index {i}! Stopping mesh update.");
                return;
            }
        }

        // Ensure valid vertex count before updating
        if (deformedVertices.Length < originalTriangles.Length / 3)
        {
            Debug.LogError($"ERROR: deformedVertices array is too small! " +
                           $"Vertices: {deformedVertices.Length}, Triangles: {originalTriangles.Length}");
            return;
        }

        mesh.vertices = deformedVertices;
        mesh.triangles = originalTriangles; // Ensure triangles are set
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        Debug.Log("Mesh successfully updated.");
    }

}

public class Spring
{
    public int vertex1, vertex2;
    public float restLength;

    public Spring(int v1, int v2, float length)
    {
        vertex1 = v1;
        vertex2 = v2;
        restLength = length;
    }
}
