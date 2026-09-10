using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S04_CustomPyramidMesh : MonoBehaviour
{
    void Start()
    {
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0f, 0f, 0f),     // 0
            new Vector3(1f, 0f, 0f),     // 1
            new Vector3(1f, 0f, 1f),     // 2
            new Vector3(0f, 0f, 1f),     // 3
            new Vector3(0.5f, 1f, 0.5f), // 4 (꼭짓점)
        };

        int[] triangles = new int[]
        {
            // 밑면 (아래를 향하도록)
            0, 2, 3,
            0, 1, 2,

            // 옆면 앞 (0,1,4)
            0, 4, 1,
            // 옆면 오른쪽 (1,2,4)
            1, 4, 2,
            // 옆면 뒤 (2,3,4)
            2, 4, 3,
            // 옆면 왼쪽 (3,0,4)
            3, 4, 0,
        };

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }
}