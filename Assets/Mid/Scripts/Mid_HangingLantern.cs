using UnityEngine;

// 정자 천장에 매단 등: 줄 끝에 매달린 채 바람에 살랑살랑 흔들리는 진자
// 회전의 중심을 원점이 아닌 매단 점(pivot)으로 옮기는 행렬 합성 (중간 ④ 변환 응용)
//
//   M = T(pivot) × Rz(흔들림) × Rx(흔들림) × T(0, −(줄 길이 + 등 높이), 0) × Ry(천천히 돎)
//
// 오른쪽부터 읽으면: 등을 제자리에서 돌리고 → 줄 길이만큼 아래로 내린 뒤 → 원점(= 매단 점) 기준으로 흔들고
// → 매단 점을 천장 위치로 옮김. 그래서 등이 자기 중심이 아니라 매단 점을 중심으로 흔들림
// 줄은 GL 선으로 매단 점에서 등 꼭대기까지 그림 (S06)
[RequireComponent(typeof(Mid_LanternMesh))]
public class Mid_HangingLantern : MonoBehaviour
{
    [SerializeField] Vector3 pivot = new Vector3(0f, 4f, 0f);   // 천장에 매단 점
    [SerializeField] float stringLength = 0.5f;                   // 줄 길이
    [SerializeField] float swingAngle = 5f;                       // 최대로 흔들리는 각도
    [SerializeField] float swingSpeed = 1.2f;                     // 흔들리는 빠르기
    [SerializeField] float phase;                                 // 등마다 다르게
    [SerializeField] float spinSpeed = 8f;                        // 초당 y축 회전 각도
    [SerializeField] Material glMaterial;                         // 줄을 그릴 Mid_GLLine 재질
    [SerializeField] Color stringColor = new Color(0.55f, 0.4f, 0.18f);

    Mid_LanternMesh lanternMesh;
    Vector3 stringEnd;

    void Awake()
    {
        lanternMesh = GetComponent<Mid_LanternMesh>();
    }

    void Update()
    {
        ApplyAt(Time.time);
    }

    public void ApplyAt(float time)
    {
        if (lanternMesh == null) lanternMesh = GetComponent<Mid_LanternMesh>();
        if (lanternMesh.BaseVertices == null) lanternMesh.Build();

        float t = time * swingSpeed + phase;
        float swingZ = Mathf.Sin(t) * swingAngle;
        float swingX = Mathf.Sin(t * 0.73f + 1.3f) * swingAngle * 0.6f;

        // 매단 점을 원점으로 둔 흔들림
        float[,] swing = Mid_Matrix.Multiply(
            Mid_Matrix.Translate(pivot),
            Mid_Matrix.RotateZ(swingZ),
            Mid_Matrix.RotateX(swingX));

        float drop = stringLength + lanternMesh.TotalHeight;   // 등 꼭대기(뚜껑)가 줄 끝에 오도록
        float[,] M = Mid_Matrix.Multiply(
            swing,
            Mid_Matrix.Translate(new Vector3(0f, -drop, 0f)),
            Mid_Matrix.RotateY((time + phase) * spinSpeed));

        Mesh mesh = lanternMesh.Mesh;
        mesh.vertices = Mid_Matrix.MultiplyPoints(M, lanternMesh.BaseVertices);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // 줄 끝 = 매단 점에서 줄 길이만큼 아래인 점에 같은 흔들림 행렬을 곱한 자리
        stringEnd = Mid_Matrix.MultiplyPoint(swing, new Vector3(0f, -stringLength, 0f));
    }

    void OnRenderObject()
    {
        if (glMaterial == null) return;
        glMaterial.SetPass(0);
        GL.Begin(GL.LINES);
        GL.Color(stringColor);
        GL.Vertex(pivot);
        GL.Vertex(stringEnd);
        GL.End();
    }

    public void Configure(Vector3 hangPoint, float length, float startPhase, Material lineMaterial)
    {
        pivot = hangPoint;
        stringLength = length;
        phase = startPhase;
        glMaterial = lineMaterial;
    }
}
