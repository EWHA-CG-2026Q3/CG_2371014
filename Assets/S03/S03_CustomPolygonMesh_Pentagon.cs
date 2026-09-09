using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S03_CustomPolygonMesh_Pentagon : MonoBehaviour
{
    [SerializeField] private int vertexCount = 5;
    [SerializeField] private float radius = 1f;

    void Start()
    {
        // 정오각형(정점 5개)을 원 위에 배치하고, 0번 정점을 기준으로 팬(fan) 삼각분할한다.
        Vector3[] vertices = new Vector3[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            float angle = Mathf.Deg2Rad * (90f - i * (360f / vertexCount));
            vertices[i] = new Vector3(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle), 0f);
        }

        int[] triangles = new int[(vertexCount - 2) * 3];
        for (int i = 0; i < vertexCount - 2; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }
}
