using System.Collections.Generic;
using UnityEngine;

// 불꽃 메시: 가운데 링 + 위 꼭짓점 + 아래 꼭짓점으로 만든 n각 쌍뿔
// 위 꼭짓점을 더 높게 두어 위로 길쭉한 불꽃 모양 (중간 선택 ① 메시 하나 더)
//
//          ^          ← 위 꼭짓점 (높이 topHeight)
//         / \
//        /   \
//       <-----> ← 가운데 링 (반지름 radius, 높이 0), sides개의 점
//        \   /
//          v          ← 아래 꼭짓점 (깊이 bottomDepth)
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mid_FlameMesh : MonoBehaviour
{
    [SerializeField, Range(3, 16)] int sides = 6;
    [SerializeField] float radius = 0.06f;
    [SerializeField] float topHeight = 0.22f;
    [SerializeField] float bottomDepth = 0.05f;

    public Vector3[] BaseVertices { get; private set; }
    public Mesh Mesh { get; private set; }

    void Awake()
    {
        Build();
    }

    public void Build()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        Vector3 top = new Vector3(0f, topHeight, 0f);
        Vector3 bottom = new Vector3(0f, -bottomDepth, 0f);

        for (int i = 0; i < sides; i++)
        {
            float a0 = 2f * Mathf.PI * i / sides;
            float a1 = 2f * Mathf.PI * (i + 1) / sides;
            Vector3 p0 = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
            Vector3 p1 = new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);

            // 위쪽 삼각형: 위 꼭짓점 + 이웃한 두 점 (밖에서 볼 때 시계 방향)
            AddTriangle(vertices, triangles, top, p1, p0);
            // 아래쪽 삼각형: 아래 꼭짓점 + 이웃한 두 점 (순서를 반대로 해야 바깥을 향함)
            AddTriangle(vertices, triangles, bottom, p0, p1);
        }

        Mesh = new Mesh();
        Mesh.name = "Mid_FlameMesh";
        Mesh.SetVertices(vertices);
        Mesh.SetTriangles(triangles, 0);
        Mesh.RecalculateNormals();
        Mesh.RecalculateBounds();

        BaseVertices = vertices.ToArray();

        GetComponent<MeshFilter>().sharedMesh = Mesh;
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer.sharedMaterial == null)
            meshRenderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }

    // 면마다 정점을 따로 만들어 각진 음영을 유지 (S19)
    static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
    {
        int start = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
    }
}
