using UnityEngine;

// 북두칠성: 풍등이 도착한 자리에 별이 켜지고, 켜진 별끼리 선으로 이어짐
// GL 클래스로 별과 선을 직접 그림 (중간 선택 ③ OpenGL 프로그래밍, S06)
//
//   별 번호  0 두베  1 메라크  2 페크다  3 메그레즈  4 알리오스  5 미자르  6 알카이드
//
//        6 ── 5 ── 4 ── 3 ──── 0
//                       │      │
//                       2 ──── 1
//
// 별의 밝기는 켜진 순간 1이고 fadeTime초 동안 서서히 어두워짐 → 풍등이 다시 도착하면 다시 1
// [ExecuteAlways]: Play 전에도 Scene 뷰에 별자리가 보여서 위치를 잡기 쉬움
[ExecuteAlways]
public class Mid_Constellation : MonoBehaviour
{
    [SerializeField] Material glMaterial;                 // Mid_GLLine 셰이더 재질 (Lit은 GL.Color를 무시함)
    [SerializeField] float starSpacing = 2.4f;            // 별 사이 간격 배율
    [SerializeField] float starSize = 0.35f;              // 별 반짝이 크기
    [SerializeField] float lineWidth = 0.06f;             // 선 두께
    [SerializeField] float fadeTime = 21f;                // 켜진 뒤 꺼지기까지 걸리는 시간
    [SerializeField, Range(0f, 1f)] float restGlow = 0.12f; // 아직 켜지지 않은 별의 희미한 밝기
    [SerializeField] Color starColor = new Color(1f, 0.82f, 0.5f);
    [SerializeField] Color lineColor = new Color(0.55f, 0.7f, 1f);

    // 북두칠성 모양 (이 오브젝트 기준 좌표, x는 오른쪽, y는 위)
    static readonly Vector2[] Shape = {
        new Vector2( 1.0f, 1.1f),   // 0 두베
        new Vector2( 1.1f, 0.2f),   // 1 메라크
        new Vector2(-0.1f, 0.0f),   // 2 페크다
        new Vector2(-0.2f, 0.8f),   // 3 메그레즈
        new Vector2(-1.2f, 1.0f),   // 4 알리오스
        new Vector2(-2.0f, 1.2f),   // 5 미자르
        new Vector2(-2.9f, 0.9f),   // 6 알카이드
    };

    // 이어지는 선: 국자 머리 (0-1-2-3-0) + 손잡이 (3-4-5-6)
    static readonly int[,] Edges = {
        { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
        { 3, 4 }, { 4, 5 }, { 5, 6 },
    };

    public int StarCount => Shape.Length;

    float[] lastIgniteTime;
    float now;

    void Awake()
    {
        EnsureInit();
    }

    void Update()
    {
        SetTime(Time.time);
    }

    void EnsureInit()
    {
        if (lastIgniteTime != null) return;
        lastIgniteTime = new float[Shape.Length];
        for (int i = 0; i < lastIgniteTime.Length; i++) lastIgniteTime[i] = float.NegativeInfinity;
    }

    public void SetTime(float time)
    {
        now = time;
    }

    // 별 i의 월드 좌표
    public Vector3 GetStarPosition(int i)
    {
        Vector2 p = Shape[Mathf.Clamp(i, 0, Shape.Length - 1)] * starSpacing;
        return transform.TransformPoint(new Vector3(p.x, p.y, 0f));
    }

    // 풍등이 도착하면 호출: 별 i를 켬
    public void Ignite(int i, float time)
    {
        EnsureInit();
        if (i < 0 || i >= Shape.Length) return;
        lastIgniteTime[i] = time;
    }

    // 별 i의 지금 밝기 (restGlow ~ 1)
    float Glow(int i)
    {
        float age = now - lastIgniteTime[i];
        float lit = float.IsInfinity(age) ? 0f : Mathf.Clamp01(1f - age / fadeTime);
        return Mathf.Lerp(restGlow, 1f, lit);
    }

    // 켜졌던 적이 있는 별인가 (선은 양 끝 별이 모두 켜진 적이 있어야 그림)
    bool IsLit(int i)
    {
        return now - lastIgniteTime[i] < fadeTime;
    }

    void OnRenderObject()
    {
        // 카메라마다 렌더링 때 Unity가 자동으로 호출함
        // URP의 Game 뷰에서는 OnRenderObject 안에서 Camera.current가 null로 들어옴 → 그때는 Main Camera를 씀
        // (카메라는 별과 선을 카메라 쪽으로 돌려 세우는 데만 씀)
        Camera cam = Camera.current != null ? Camera.current : Camera.main;
        if (glMaterial == null || cam == null) return;
        EnsureInit();

        Vector3 camPos = cam.transform.position;
        Vector3 right = cam.transform.right;
        Vector3 up = cam.transform.up;

        glMaterial.SetPass(0);
        GL.PushMatrix();

        // 1. 선: 켜진 별끼리, 카메라를 향한 얇은 사각형으로 (GL.LINES는 1픽셀이라 멀리서 잘 안 보임)
        GL.Begin(GL.QUADS);
        for (int e = 0; e < Edges.GetLength(0); e++)
        {
            int a = Edges[e, 0];
            int b = Edges[e, 1];
            if (!IsLit(a) || !IsLit(b)) continue;

            Vector3 pa = GetStarPosition(a);
            Vector3 pb = GetStarPosition(b);
            // 선 방향과 카메라 방향에 모두 수직인 방향으로 두께를 줌
            Vector3 side = Vector3.Cross(pb - pa, pa - camPos).normalized * (lineWidth * 0.5f);

            GL.Color(lineColor * (Mathf.Min(Glow(a), Glow(b)) * 0.8f));
            GL.Vertex(pa - side);
            GL.Vertex(pa + side);
            GL.Vertex(pb + side);
            GL.Vertex(pb - side);
        }
        GL.End();

        // 2. 별: 카메라를 향한 네 갈래 반짝이 (가로로 긴 마름모 + 세로로 긴 마름모)
        GL.Begin(GL.TRIANGLES);
        for (int i = 0; i < Shape.Length; i++)
        {
            float glow = Glow(i);
            float twinkle = 0.85f + 0.15f * Mathf.Sin(now * 3f + i * 1.7f);
            float size = starSize * (0.5f + 0.5f * glow) * twinkle;
            Vector3 c = GetStarPosition(i);

            GL.Color(starColor * glow);
            AddDiamond(c, right * size, up * size * 0.18f);
            AddDiamond(c, right * size * 0.18f, up * size);
        }
        GL.End();

        GL.PopMatrix();
    }

    // 중심 c에서 가로 반폭 h, 세로 반폭 v인 마름모 = 삼각형 2개
    static void AddDiamond(Vector3 c, Vector3 h, Vector3 v)
    {
        GL.Vertex(c - h); GL.Vertex(c + v); GL.Vertex(c + h);
        GL.Vertex(c - h); GL.Vertex(c + h); GL.Vertex(c - v);
    }
}
