using UnityEngine;

// 4×4 동차좌표 행렬로 다이아몬드를 기울이는(shear) 스크립트
// 높이(y)에 비례해 x축 방향으로 밀림: (x, y, z) → (x + k·y, y, z)
// Unity의 Matrix4x4 타입 없이 float[4,4] 배열만으로 계산함
// k = (학번 끝자리 + 1) ÷ 5 = (4 + 1) ÷ 5 = 1 (학번 2371014)
[ExecuteAlways]
[RequireComponent(typeof(DiamondMesh))]
public class S09_ShearMatrix : MonoBehaviour
{
    [SerializeField] float k = 1f;   // 기울임 계수 (−k 확인 시 −1)

    DiamondMesh diamondMesh;

    void OnEnable()
    {
        diamondMesh = GetComponent<DiamondMesh>();
        LogTopVertex();
    }

    void Update()
    {
        if (diamondMesh == null || diamondMesh.BaseVertices == null) return;

        float[,] H = ShearMatrixRaw(k);
        Vector3[] baseVertices = diamondMesh.BaseVertices;
        Vector3[] verts = new Vector3[baseVertices.Length];
        for (int i = 0; i < baseVertices.Length; i++)
            verts[i] = FromHomogeneous(MultiplyMatrixVectorRaw(H, ToHomogeneous(baseVertices[i])));
        diamondMesh.SetVertices(verts);
    }

    // k를 바꿀 때마다 꼭대기 정점 (0.5, 1, 0.5)의 변환 결과를 Console에 출력
    void OnValidate()
    {
        LogTopVertex();
    }

    void LogTopVertex()
    {
        Vector3 top = new Vector3(0.5f, 1f, 0.5f);
        Vector3 result = FromHomogeneous(MultiplyMatrixVectorRaw(ShearMatrixRaw(k), ToHomogeneous(top)));
        Debug.Log($"k = {k}, 꼭대기 정점 {top} → {result}");
    }

    // ---------- 행렬 빌더 ----------

    // 1열: e₁ 그대로 / 2열: e₂ → (k, 1, 0) / 3열: e₃ 그대로 / 4열: 원점 그대로
    float[,] ShearMatrixRaw(float k)
    {
        return new float[,] {
            { 1f, k,  0f, 0f },
            { 0f, 1f, 0f, 0f },
            { 0f, 0f, 1f, 0f },
            { 0f, 0f, 0f, 1f }
        };
    }

    // ---------- 동차좌표 ----------

    Vector4 ToHomogeneous(Vector3 v)
    {
        return new Vector4(v.x, v.y, v.z, 1f);
    }

    Vector3 FromHomogeneous(Vector4 h)
    {
        return new Vector3(h.x, h.y, h.z);
    }

    Vector4 MultiplyMatrixVectorRaw(float[,] M, Vector4 v)
    {
        float[] input = { v.x, v.y, v.z, v.w };
        float[] result = new float[4];
        for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++)
                result[row] += M[row, col] * input[col];
        return new Vector4(result[0], result[1], result[2], result[3]);
    }
}
