using System.Collections.Generic;
using UnityEngine;

// 액자 틀 메시: 그림 둘레를 감싸는 네 변을 반복문으로 만듦 (중간 선택 ① 메시 하나 더)
//
//   O1 ─────────── O2      O: 바깥 모서리 4개, I: 안쪽 모서리 4개
//   │  I1 ───── I2  │      변 i = O[i], O[i+1], I[i+1], I[i]로 둘러싸인 띠
//   │  │  그림   │  │      한 변마다 앞면 + 바깥 옆면 + 안쪽 옆면 (뒷면은 벽에 붙으므로 생략)
//   │  I0 ───── I3  │
//   O0 ─────────── O3
//
// 앞면은 -z 쪽을 향하고, 액자는 z = 0(벽)에서 z = -depth까지 튀어나옴
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mid_FrameMesh : MonoBehaviour
{
    [SerializeField] float innerWidth = 0.9f;     // 그림이 보이는 구멍의 가로
    [SerializeField] float innerHeight = 0.675f;  // 그림이 보이는 구멍의 세로
    [SerializeField] float border = 0.07f;        // 틀의 폭
    [SerializeField] float depth = 0.05f;         // 벽에서 튀어나온 두께
    [SerializeField] float innerDepth = 0.02f;    // 그림이 틀 앞면보다 들어가 있는 깊이

    void Awake()
    {
        Build();
    }

    public void Build()
    {
        float iw = innerWidth * 0.5f, ih = innerHeight * 0.5f;
        float ow = iw + border, oh = ih + border;

        // 왼쪽 아래부터 시계 방향(앞에서 볼 때)으로 모서리 4개
        Vector2[] outer = { new Vector2(-ow, -oh), new Vector2(-ow, oh), new Vector2(ow, oh), new Vector2(ow, -oh) };
        Vector2[] inner = { new Vector2(-iw, -ih), new Vector2(-iw, ih), new Vector2(iw, ih), new Vector2(iw, -ih) };

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        float front = -depth;
        float recess = -depth + innerDepth;

        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            Vector2 sideMid = (outer[i] + outer[j]) * 0.5f;
            Vector3 outward = new Vector3(sideMid.x, sideMid.y, 0f).normalized;   // 이 변의 바깥 방향

            // 1. 앞면 띠
            AddQuad(vertices, triangles,
                At(outer[i], front), At(outer[j], front), At(inner[j], front), At(inner[i], front),
                Vector3.back);
            // 2. 바깥 옆면: 벽(z = 0)에서 앞면까지
            AddQuad(vertices, triangles,
                At(outer[i], 0f), At(outer[j], 0f), At(outer[j], front), At(outer[i], front),
                outward);
            // 3. 안쪽 옆면: 앞면에서 그림이 놓인 깊이까지, 그림 쪽(안쪽)을 향함
            AddQuad(vertices, triangles,
                At(inner[i], front), At(inner[j], front), At(inner[j], recess), At(inner[i], recess),
                -outward);
        }

        Mesh mesh = new Mesh();
        mesh.name = "Mid_FrameMesh";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    static Vector3 At(Vector2 p, float z) => new Vector3(p.x, p.y, z);

    // 사각형 a-b-c-d를 삼각형 2개로. facing 쪽에서 볼 때 앞면이 되도록 winding을 맞춤
    // Unity에서 삼각형 (a, b, c)의 앞면 방향 = cross(b − a, c − a) → facing과 반대면 순서를 뒤집음
    static void AddQuad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 facing)
    {
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), facing) < 0f)
        {
            Vector3 t = b; b = d; d = t;
        }
        int start = vertices.Count;
        vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
        triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
        triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
    }
}
