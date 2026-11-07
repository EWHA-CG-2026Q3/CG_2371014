using System.Collections.Generic;
using UnityEngine;

// 풍등 메시: 아래가 좁고 위가 넓은 n각뿔대 몸통 + 위를 덮는 뚜껑, 바닥은 열려 있음
// 정점과 삼각형을 모두 반복문으로 만듦 (중간 필수 ① 기하 모델링)
//
//        ___________      ← 위 링 (반지름 topRadius, 높이 height) + 뚜껑
//        \         /
//         \       /       ← 옆면 sides개 (사다리꼴 = 삼각형 2개)
//          \_____/        ← 아래 링 (반지름 bottomRadius, 높이 0), 열려 있음
//
// 면마다 정점을 따로 만들어서 RecalculateNormals()가 면마다 하나의 normal을 갖게 함 (S19)
// → 면 경계가 뭉개지지 않고 한지 주름처럼 각져 보임
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mid_LanternMesh : MonoBehaviour
{
    [SerializeField, Range(3, 32)] int sides = 8;          // 몇 각뿔대인가
    [SerializeField] float bottomRadius = 0.2f;              // 아래 링 반지름
    [SerializeField] float topRadius = 0.3f;                 // 위 링 반지름
    [SerializeField] float height = 0.6f;                    // 몸통 높이
    [SerializeField] float capHeight = 0.06f;                // 뚜껑이 볼록 솟은 높이

    // 행렬을 곱하기 전의 원래 정점 (Mid_LanternFlight가 매 프레임 여기에 행렬을 곱함)
    public Vector3[] BaseVertices { get; private set; }
    public Mesh Mesh { get; private set; }
    public float Height => height;
    public float TotalHeight => height + capHeight;   // 바닥부터 뚜껑 꼭대기까지

    void Awake()
    {
        Build();
    }

    public void Build()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        Vector3 capTop = new Vector3(0f, height + capHeight, 0f);

        for (int i = 0; i < sides; i++)
        {
            // 원 위에 점을 고르게 놓기: 점 i의 각도 = 360° / n × i
            float a0 = 2f * Mathf.PI * i / sides;
            float a1 = 2f * Mathf.PI * (i + 1) / sides;

            Vector3 b0 = new Vector3(Mathf.Cos(a0) * bottomRadius, 0f, Mathf.Sin(a0) * bottomRadius);
            Vector3 b1 = new Vector3(Mathf.Cos(a1) * bottomRadius, 0f, Mathf.Sin(a1) * bottomRadius);
            Vector3 t0 = new Vector3(Mathf.Cos(a0) * topRadius, height, Mathf.Sin(a0) * topRadius);
            Vector3 t1 = new Vector3(Mathf.Cos(a1) * topRadius, height, Mathf.Sin(a1) * topRadius);

            // 1. 바깥 옆면: 밖에서 볼 때 b0 → t0 → t1이 시계 방향 (Unity는 시계 방향이 앞면)
            AddQuad(vertices, triangles, b0, t0, t1, b1);

            // 2. 안쪽 옆면: 바닥이 열려 있어 안이 보이므로, 같은 점을 반대 순서로 한 번 더
            //    이게 없으면 뒷면 컬링 때문에 안쪽이 뚫린 것처럼 보임
            AddQuad(vertices, triangles, b1, t1, t0, b0);

            // 3. 뚜껑 바깥: 가운데 꼭대기 + 위 링의 이웃한 두 점 (위에서 볼 때 시계 방향)
            AddTriangle(vertices, triangles, capTop, t1, t0);

            // 4. 뚜껑 안쪽: 아래에서 올려다볼 때 보이는 면
            AddTriangle(vertices, triangles, capTop, t0, t1);
        }

        Mesh = new Mesh();
        Mesh.name = "Mid_LanternMesh";
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

    // 사각형 a-b-c-d (시계 방향)를 삼각형 두 개로: (a, b, c), (a, c, d)
    // 이 면만의 정점 4개를 새로 만들어서 다른 면과 공유하지 않음
    static void AddQuad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int start = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);
        triangles.Add(start);     triangles.Add(start + 1); triangles.Add(start + 2);
        triangles.Add(start);     triangles.Add(start + 2); triangles.Add(start + 3);
    }

    static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
    {
        int start = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
    }
}
