using UnityEngine;

// 우리 소프트웨어 래스터라이저: 삼각형을 픽셀로 채워 그림을 그림 (중간 선택 ② 래스터화, S06)
// S06에서 한 것 + 더한 것
//   - barycentric 좌표로 픽셀이 삼각형 안에 있는지 판정 (S06 그대로)
//   - 같은 가중치로 세 꼭짓점의 색을 섞음 → 그라데이션 (정점 색 보간)
//   - 캔버스 전체가 아니라 삼각형을 감싸는 사각형(bounding box)만 검사 → 빠름
//   - 색의 알파로 아래 색과 섞음 → 은은한 빛 번짐 표현
// 원·선·별도 모두 삼각형으로 쪼개서 FillTriangle 하나로 그림
public class Mid_Rasterizer
{
    public readonly int Width;
    public readonly int Height;
    readonly Color[] pixels;

    public Mid_Rasterizer(int width, int height)
    {
        Width = width;
        Height = height;
        pixels = new Color[width * height];
    }

    // 위에서 아래로 두 색을 섞어 배경을 채움
    public void FillGradient(Color top, Color bottom)
    {
        for (int y = 0; y < Height; y++)
        {
            Color c = Color.Lerp(bottom, top, (y + 0.5f) / Height);
            for (int x = 0; x < Width; x++)
                pixels[y * Width + x] = c;
        }
    }

    // 삼각형 채우기: 픽셀 중심의 barycentric 좌표 (w1, w2, w3)가 모두 0 이상이면 안쪽
    // 그 픽셀의 색 = ca × w1 + cb × w2 + cc × w3
    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color ca, Color cb, Color cc)
    {
        float denom = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
        if (Mathf.Abs(denom) < 1e-6f) return; // 넓이가 0인 삼각형

        // 삼각형을 감싸는 사각형 안의 픽셀만 검사
        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
        int maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
        int maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float w1 = ((b.y - c.y) * (px - c.x) + (c.x - b.x) * (py - c.y)) / denom;
                float w2 = ((c.y - a.y) * (px - c.x) + (a.x - c.x) * (py - c.y)) / denom;
                float w3 = 1f - w1 - w2;
                if (w1 < 0f || w2 < 0f || w3 < 0f) continue;

                Color src = ca * w1 + cb * w2 + cc * w3;
                int i = y * Width + x;
                // 알파 블렌딩: 결과 = 아래 색 × (1 − α) + 새 색 × α
                pixels[i] = Color.Lerp(pixels[i], new Color(src.r, src.g, src.b, 1f), src.a);
            }
        }
    }

    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        FillTriangle(a, b, c, color, color, color);
    }

    // 사각형 a-b-c-d = 삼각형 2개
    public void FillQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
    {
        FillTriangle(a, b, c, color);
        FillTriangle(a, c, d, color);
    }

    // 사각형인데 아래 두 점과 위 두 점의 색이 다름 (a, b: 아래 / c, d: 위)
    public void FillQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color bottom, Color top)
    {
        FillTriangle(a, b, c, bottom, bottom, top);
        FillTriangle(a, c, d, bottom, top, top);
    }

    // 원 = 가운데 점 + 원 위의 이웃한 두 점으로 만든 삼각형 segments개 (triangle fan)
    // 가운데 색과 가장자리 색이 달라서, 가장자리 알파를 0으로 두면 빛 번짐이 됨
    public void FillCircle(Vector2 center, float radius, Color centerColor, Color edgeColor, int segments = 48)
    {
        for (int i = 0; i < segments; i++)
        {
            float a0 = 2f * Mathf.PI * i / segments;
            float a1 = 2f * Mathf.PI * (i + 1) / segments;
            Vector2 p0 = center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius;
            Vector2 p1 = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
            FillTriangle(center, p0, p1, centerColor, edgeColor, edgeColor);
        }
    }

    // 두께 있는 선 = 선에 수직 방향으로 폭을 준 사각형
    public void FillLine(Vector2 a, Vector2 b, float width, Color color)
    {
        Vector2 dir = (b - a).normalized;
        Vector2 side = new Vector2(-dir.y, dir.x) * (width * 0.5f);
        FillQuad(a - side, b - side, b + side, a + side, color);
    }

    // 네 갈래 반짝이 별 = 가로로 긴 마름모 + 세로로 긴 마름모
    public void FillSparkle(Vector2 c, float size, Color color)
    {
        float thin = size * 0.22f;
        FillQuad(c + new Vector2(-size, 0f), c + new Vector2(0f, -thin), c + new Vector2(size, 0f), c + new Vector2(0f, thin), color);
        FillQuad(c + new Vector2(-thin, 0f), c + new Vector2(0f, -size), c + new Vector2(thin, 0f), c + new Vector2(0f, size), color);
    }

    // 사다리꼴 풍등: 아래가 좁고 위가 넓음, 아래는 밝은 노랑 → 위는 주황
    public void FillLantern(Vector2 center, float size)
    {
        float h = size * 0.5f;
        FillCircle(center, size * 1.3f, new Color(1f, 0.6f, 0.2f, 0.35f), new Color(1f, 0.5f, 0.15f, 0f), 24);
        FillQuad(
            center + new Vector2(-size * 0.32f, -h), center + new Vector2(size * 0.32f, -h),
            center + new Vector2(size * 0.48f, h), center + new Vector2(-size * 0.48f, h),
            new Color(1f, 0.9f, 0.5f), new Color(0.95f, 0.45f, 0.15f));
    }

    public Texture2D ToTexture()
    {
        Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
