using UnityEngine;

// 액자 속 그림: 우리 래스터라이저(Mid_Rasterizer)로 픽셀을 칠해 만든 텍스처를 캔버스 사각형에 입힘
// (중간 선택 ② 래스터화: "우리 래스터라이저로 그린 그림을 장면 안에 걸기")
//
// 캔버스는 UV 좌표가 있는 사각형 메시 (S17): 정점마다 텍스처의 어느 위치를 읽을지 정해 줌
// 앞면은 -z 쪽을 향함 → 벽(+z)에 붙여 걸면 방 안에서 보임
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mid_Painting : MonoBehaviour
{
    public enum Artwork { Lanterns, BigDipper, MoonPavilion }

    [SerializeField] Artwork artwork = Artwork.Lanterns;
    [SerializeField] float width = 0.9f;
    [SerializeField] float height = 0.675f;
    [SerializeField] int pixelWidth = 320;
    [SerializeField] int pixelHeight = 240;
    [SerializeField] Material canvasMaterial;   // URP Lit (Emission 켬), 텍스처는 실행할 때 채움

    void Start()
    {
        Build();
        Paint();
    }

    // UV가 있는 사각형: 왼쪽 아래 (0,0) ~ 오른쪽 위 (1,1)
    public void Build()
    {
        float w = width * 0.5f;
        float h = height * 0.5f;
        Mesh mesh = new Mesh();
        mesh.name = "Mid_PaintingCanvas";
        mesh.vertices = new Vector3[] {
            new Vector3(-w, -h, 0f),   // 0 왼쪽 아래
            new Vector3(-w,  h, 0f),   // 1 왼쪽 위
            new Vector3( w,  h, 0f),   // 2 오른쪽 위
            new Vector3( w, -h, 0f),   // 3 오른쪽 아래
        };
        mesh.uv = new Vector2[] {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
        };
        // -z 쪽에서 볼 때 시계 방향: 왼쪽 아래 → 왼쪽 위 → 오른쪽 위
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    public void Paint()
    {
        Mid_Rasterizer r = new Mid_Rasterizer(pixelWidth, pixelHeight);
        switch (artwork)
        {
            case Artwork.Lanterns: PaintLanterns(r); break;
            case Artwork.BigDipper: PaintBigDipper(r); break;
            case Artwork.MoonPavilion: PaintMoonPavilion(r); break;
        }
        Texture2D texture = r.ToTexture();

        Material material = canvasMaterial != null
            ? new Material(canvasMaterial)
            : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetTexture("_BaseMap", texture);
        material.SetTexture("_EmissionMap", texture);   // 어두운 정자 안에서도 그림이 은은하게 보이도록
        GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    // ---------- 그림 1. 소원을 띄우다 ----------
    void PaintLanterns(Mid_Rasterizer r)
    {
        r.FillGradient(new Color(0.05f, 0.06f, 0.2f), new Color(0.35f, 0.14f, 0.28f));
        PaintTinyStars(r, 7, 45, 0.45f);

        // 먼 산, 가까운 산, 땅
        Color far = new Color(0.14f, 0.1f, 0.22f);
        Color near = new Color(0.07f, 0.05f, 0.12f);
        r.FillTriangle(new Vector2(-30, 40), new Vector2(80, 125), new Vector2(190, 40), far);
        r.FillTriangle(new Vector2(110, 40), new Vector2(230, 140), new Vector2(350, 40), far);
        r.FillTriangle(new Vector2(-40, 30), new Vector2(60, 88), new Vector2(170, 30), near);
        r.FillTriangle(new Vector2(150, 30), new Vector2(265, 82), new Vector2(370, 30), near);
        r.FillQuad(new Vector2(0, 0), new Vector2(320, 0), new Vector2(320, 34), new Vector2(0, 34), near);

        // 크기가 다른 풍등들: 멀수록 작게
        Vector3[] lanterns = {
            new Vector3(70, 150, 24), new Vector3(150, 190, 16), new Vector3(208, 118, 28),
            new Vector3(255, 178, 14), new Vector3(112, 96, 13), new Vector3(285, 135, 10),
            new Vector3(32, 205, 9), new Vector3(185, 220, 7),
        };
        foreach (Vector3 l in lanterns)
            r.FillLantern(new Vector2(l.x, l.y), l.z);
    }

    // ---------- 그림 2. 일곱 개의 소원 (북두칠성) ----------
    void PaintBigDipper(Mid_Rasterizer r)
    {
        r.FillGradient(new Color(0.02f, 0.03f, 0.1f), new Color(0.1f, 0.12f, 0.3f));

        // 은하수: 대각선으로 지나가는 옅은 띠 (알파가 낮은 큰 삼각형 두 개)
        Color milky = new Color(0.6f, 0.65f, 1f, 0.08f);
        r.FillTriangle(new Vector2(-20, 40), new Vector2(340, 260), new Vector2(60, 260), milky);
        r.FillTriangle(new Vector2(-20, 0), new Vector2(340, 220), new Vector2(340, 160), milky);
        PaintTinyStars(r, 11, 140, 0.6f);

        // 북두칠성: Mid_Constellation과 같은 모양을 캔버스 크기로 옮김
        Vector2[] shape = {
            new Vector2( 1.0f, 1.1f), new Vector2( 1.1f, 0.2f), new Vector2(-0.1f, 0.0f), new Vector2(-0.2f, 0.8f),
            new Vector2(-1.2f, 1.0f), new Vector2(-2.0f, 1.2f), new Vector2(-2.9f, 0.9f),
        };
        Vector2[] p = new Vector2[shape.Length];
        for (int i = 0; i < shape.Length; i++)
            p[i] = new Vector2(Mathf.Lerp(40, 285, (shape[i].x + 3.1f) / 4.4f), Mathf.Lerp(80, 195, (shape[i].y + 0.1f) / 1.4f));

        int[] edges = { 0, 1, 1, 2, 2, 3, 3, 0, 3, 4, 4, 5, 5, 6 };
        for (int e = 0; e < edges.Length; e += 2)
            r.FillLine(p[edges[e]], p[edges[e + 1]], 1.6f, new Color(0.6f, 0.75f, 1f, 0.75f));
        foreach (Vector2 s in p)
        {
            r.FillCircle(s, 12f, new Color(1f, 0.85f, 0.55f, 0.45f), new Color(1f, 0.8f, 0.5f, 0f), 24);
            r.FillSparkle(s, 9f, new Color(1f, 0.95f, 0.75f));
        }

        // 아래에 작은 풍등 하나가 올라가는 중
        r.FillLantern(new Vector2(160, 40), 12);
    }

    // ---------- 그림 3. 달빛 정자 ----------
    void PaintMoonPavilion(Mid_Rasterizer r)
    {
        r.FillGradient(new Color(0.1f, 0.12f, 0.32f), new Color(0.5f, 0.32f, 0.45f));
        PaintTinyStars(r, 3, 40, 0.5f);

        // 달: 넓게 번지는 빛 + 안쪽이 더 밝은 원
        Vector2 moon = new Vector2(232, 172);
        r.FillCircle(moon, 70f, new Color(1f, 0.95f, 0.8f, 0.3f), new Color(1f, 0.9f, 0.7f, 0f));
        r.FillCircle(moon, 32f, new Color(1f, 0.99f, 0.9f), new Color(0.96f, 0.88f, 0.66f));

        Color silhouette = new Color(0.05f, 0.04f, 0.09f);
        // 땅과 물에 비친 달빛
        r.FillQuad(new Vector2(0, 0), new Vector2(320, 0), new Vector2(320, 42), new Vector2(0, 42), silhouette);
        for (int i = 0; i < 4; i++)
        {
            float y = 30 - i * 8;
            float half = 26 - i * 5;
            r.FillQuad(new Vector2(moon.x - half, y), new Vector2(moon.x + half, y),
                       new Vector2(moon.x + half, y + 2.5f), new Vector2(moon.x - half, y + 2.5f), new Color(1f, 0.92f, 0.7f, 0.45f));
        }

        // 정자: 기단, 기둥 4개, 처마가 들린 지붕, 용마루
        r.FillQuad(new Vector2(40, 42), new Vector2(180, 42), new Vector2(180, 54), new Vector2(40, 54), silhouette);
        foreach (float x in new[] { 55f, 88f, 132f, 165f })
            r.FillQuad(new Vector2(x - 3, 54), new Vector2(x + 3, 54), new Vector2(x + 3, 102), new Vector2(x - 3, 102), silhouette);
        r.FillQuad(new Vector2(30, 100), new Vector2(190, 100), new Vector2(160, 126), new Vector2(60, 126), silhouette);
        r.FillTriangle(new Vector2(30, 100), new Vector2(14, 114), new Vector2(54, 108), silhouette);
        r.FillTriangle(new Vector2(190, 100), new Vector2(206, 114), new Vector2(166, 108), silhouette);
        r.FillTriangle(new Vector2(60, 126), new Vector2(160, 126), new Vector2(110, 142), silhouette);

        // 정자에서 막 떠오른 풍등 두 개
        r.FillLantern(new Vector2(118, 165), 13);
        r.FillLantern(new Vector2(78, 205), 9);
    }

    // 작은 별을 흩뿌림: 시드가 같으면 항상 같은 자리
    static void PaintTinyStars(Mid_Rasterizer r, int seed, int count, float topFraction)
    {
        System.Random random = new System.Random(seed);
        for (int i = 0; i < count; i++)
        {
            float x = (float)random.NextDouble() * r.Width;
            float y = r.Height * (1f - topFraction) + (float)random.NextDouble() * r.Height * topFraction;
            float size = 0.8f + (float)random.NextDouble() * 1.6f;
            float alpha = 0.35f + (float)random.NextDouble() * 0.6f;
            r.FillSparkle(new Vector2(x, y), size, new Color(1f, 0.97f, 0.88f, alpha));
        }
    }
}
