using UnityEngine;

// 보름달: 위도·경도 반복문으로 만든 구 메시 + 우리 래스터라이저로 그린 달 표면 + GL로 그린 달무리
//
// 구 메시 (UV sphere)
//   위도 i = 0(북극) ~ latitudes(남극), 경도 j = 0 ~ longitudes
//   점 (i, j)의 위치: θ = π·i / latitudes, φ = 2π·j / longitudes
//                    (sin θ · cos φ,  cos θ,  sin θ · sin φ) × radius
//   UV = (j / longitudes, 1 − i / latitudes) → 래스터라이저로 그린 직사각형 그림이 구를 감쌈 (S17)
//   이웃한 네 점 (i, j), (i, j+1), (i+1, j), (i+1, j+1)이 사각형 하나 = 삼각형 2개
//
// 달 표면은 안개에 묻히지 않도록 직접 쓴 셰이더(CG/Mid_Moon)로 그림
// 달무리는 카메라를 향한 원(triangle fan)을 더하기 블렌딩으로 그려 빛이 번지는 것처럼 보이게 함
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mid_Moon : MonoBehaviour
{
    [SerializeField] float radius = 1.3f;
    [SerializeField, Range(4, 64)] int latitudes = 18;
    [SerializeField, Range(4, 128)] int longitudes = 36;
    [SerializeField] Material moonMaterial;     // CG/Mid_Moon 셰이더 재질
    [SerializeField] Material glMaterial;       // 달무리용 Mid_GLLine 재질
    [SerializeField] float haloRadius = 4.5f;
    [SerializeField] Color haloColor = new Color(0.32f, 0.32f, 0.27f);

    void Start()
    {
        // 메시와 텍스처는 Play할 때 만듦 (에디터에서는 달무리만 보임)
        if (Application.isPlaying) Build();
    }

    public void Build()
    {
        int columns = longitudes + 1;
        Vector3[] vertices = new Vector3[(latitudes + 1) * columns];
        Vector2[] uv = new Vector2[vertices.Length];

        for (int i = 0; i <= latitudes; i++)
        {
            float theta = Mathf.PI * i / latitudes;
            for (int j = 0; j <= longitudes; j++)
            {
                float phi = 2f * Mathf.PI * j / longitudes;
                vertices[i * columns + j] = new Vector3(
                    Mathf.Sin(theta) * Mathf.Cos(phi),
                    Mathf.Cos(theta),
                    Mathf.Sin(theta) * Mathf.Sin(phi)) * radius;
                uv[i * columns + j] = new Vector2((float)j / longitudes, 1f - (float)i / latitudes);
            }
        }

        int[] triangles = new int[latitudes * longitudes * 6];
        int t = 0;
        for (int i = 0; i < latitudes; i++)
        {
            for (int j = 0; j < longitudes; j++)
            {
                int a = i * columns + j;   // 위 왼쪽
                int b = a + columns;       // 아래 왼쪽
                // 밖에서 볼 때 시계 방향: (a, a+1, b), (a+1, b+1, b)
                triangles[t++] = a;     triangles[t++] = a + 1; triangles[t++] = b;
                triangles[t++] = a + 1; triangles[t++] = b + 1; triangles[t++] = b;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "Mid_MoonSphere";
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        // 정점을 이웃 면끼리 공유 → normal이 평균되어 매끈한 구 (각진 풍등과 반대)
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;

        Material material = moonMaterial != null ? new Material(moonMaterial) : new Material(Shader.Find("CG/Mid_Moon"));
        material.SetTexture("_MainTex", PaintSurface());
        GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    // 달 표면: 밝은 바탕에 어두운 바다(큰 원)와 크레이터(작은 원 + 밝은 테두리)
    // 가로가 경도 360°, 세로가 위도 180°인 그림
    static Texture2D PaintSurface()
    {
        Mid_Rasterizer r = new Mid_Rasterizer(256, 128);
        r.FillGradient(new Color(0.95f, 0.93f, 0.85f), new Color(0.9f, 0.88f, 0.8f));

        Color mare = new Color(0.62f, 0.62f, 0.6f, 0.55f);
        Color mareEdge = new Color(0.62f, 0.62f, 0.6f, 0f);
        Vector3[] maria = {
            new Vector3(60, 80, 22), new Vector3(85, 60, 16), new Vector3(110, 85, 14),
            new Vector3(70, 45, 12), new Vector3(190, 70, 20), new Vector3(215, 50, 12),
        };
        foreach (Vector3 m in maria)
            r.FillCircle(new Vector2(m.x, m.y), m.z, mare, mareEdge, 32);

        System.Random random = new System.Random(5);
        for (int i = 0; i < 40; i++)
        {
            Vector2 c = new Vector2((float)random.NextDouble() * 256, 15 + (float)random.NextDouble() * 98);
            float size = 1.5f + (float)random.NextDouble() * 4f;
            r.FillCircle(c + new Vector2(0.6f, -0.6f), size, new Color(1f, 1f, 0.95f, 0.5f), new Color(1f, 1f, 0.95f, 0.2f), 12);
            r.FillCircle(c, size * 0.8f, new Color(0.7f, 0.69f, 0.66f, 0.6f), new Color(0.75f, 0.74f, 0.7f, 0.3f), 12);
        }
        return r.ToTexture();
    }

    // 달무리: 카메라를 향한 원판을 고리 세 겹으로 나눠, 바깥으로 갈수록 빠르게 어두워지게 함
    // 고리 k = 반지름 rings[k] ~ rings[k+1] 사이의 띠, 띠 하나를 segments개의 사각형(삼각형 2개)으로
    // 더하기 블렌딩이라 색이 0인 가장자리는 보이지 않음 → 경계 없이 번짐
    static readonly float[] Rings = { 0f, 0.3f, 0.6f, 1f };
    static readonly float[] RingBrightness = { 1f, 0.45f, 0.12f, 0f };

    void OnRenderObject()
    {
        Camera cam = Camera.current != null ? Camera.current : Camera.main;
        if (glMaterial == null || cam == null) return;

        Vector3 center = transform.position;
        Vector3 right = cam.transform.right;
        Vector3 up = cam.transform.up;
        const int segments = 48;

        glMaterial.SetPass(0);
        GL.Begin(GL.TRIANGLES);
        for (int k = 0; k < Rings.Length - 1; k++)
        {
            float r0 = Rings[k] * haloRadius, r1 = Rings[k + 1] * haloRadius;
            Color c0 = haloColor * RingBrightness[k], c1 = haloColor * RingBrightness[k + 1];
            for (int i = 0; i < segments; i++)
            {
                float a0 = 2f * Mathf.PI * i / segments;
                float a1 = 2f * Mathf.PI * (i + 1) / segments;
                Vector3 d0 = right * Mathf.Cos(a0) + up * Mathf.Sin(a0);
                Vector3 d1 = right * Mathf.Cos(a1) + up * Mathf.Sin(a1);
                GL.Color(c0); GL.Vertex(center + d0 * r0);
                GL.Color(c1); GL.Vertex(center + d0 * r1);
                GL.Color(c1); GL.Vertex(center + d1 * r1);
                GL.Color(c0); GL.Vertex(center + d0 * r0);
                GL.Color(c1); GL.Vertex(center + d1 * r1);
                GL.Color(c0); GL.Vertex(center + d1 * r0);
            }
        }
        GL.End();
    }
}
