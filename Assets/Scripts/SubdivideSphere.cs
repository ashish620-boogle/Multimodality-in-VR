using UnityEngine;
using System.Collections.Generic;

public class SubdivideSphere : MonoBehaviour
{
    public int subdivisions = 4; // Increase for higher poly count

    private void Start()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.mesh = Subdivide(meshFilter.mesh, subdivisions);
        }
    }

    Mesh Subdivide(Mesh mesh, int level)
    {
        if (level <= 0) return mesh;

        List<Vector3> vertices = new List<Vector3>(mesh.vertices);
        List<int> triangles = new List<int>(mesh.triangles);
        Dictionary<string, int> midPointCache = new Dictionary<string, int>();

        List<int> newTriangles = new List<int>();

        for (int i = 0; i < triangles.Count; i += 3)
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];

            int ab = GetMidpointIndex(vertices, midPointCache, a, b);
            int bc = GetMidpointIndex(vertices, midPointCache, b, c);
            int ca = GetMidpointIndex(vertices, midPointCache, c, a);

            newTriangles.AddRange(new int[] { a, ab, ca });
            newTriangles.AddRange(new int[] { ab, b, bc });
            newTriangles.AddRange(new int[] { ca, bc, c });
            newTriangles.AddRange(new int[] { ab, bc, ca });
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = newTriangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return Subdivide(mesh, level - 1);
    }

    int GetMidpointIndex(List<Vector3> vertices, Dictionary<string, int> midPointCache, int indexA, int indexB)
    {
        string key = indexA < indexB ? indexA + "_" + indexB : indexB + "_" + indexA;
        if (midPointCache.TryGetValue(key, out int midIndex)) return midIndex;

        Vector3 midpoint = (vertices[indexA] + vertices[indexB]) / 2f;
        midpoint = midpoint.normalized * 0.5f; // Keep on sphere surface

        midIndex = vertices.Count;
        vertices.Add(midpoint);
        midPointCache[key] = midIndex;

        return midIndex;
    }
}
