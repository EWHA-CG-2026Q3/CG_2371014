using UnityEngine;

// 중간 프로젝트에서 쓰는 4×4 행렬 도구
// S09_TRS_Raw처럼 Unity의 Matrix4x4 없이 float[4,4] 배열만으로 계산함
// 행렬은 열벡터 규칙: 점 p에 M을 적용하면 M × (x, y, z, 1)
// 따라서 A × B는 "B를 먼저 적용하고 그다음 A를 적용"한다는 뜻
public static class Mid_Matrix
{
    // ---------- 행렬 빌더 ----------

    // 대각선이 1인 단위 행렬: 아무것도 바꾸지 않음
    public static float[,] Identity()
    {
        return new float[,] {
            { 1f, 0f, 0f, 0f },
            { 0f, 1f, 0f, 0f },
            { 0f, 0f, 1f, 0f },
            { 0f, 0f, 0f, 1f }
        };
    }

    // 4열: 원점이 t로 이동
    public static float[,] Translate(Vector3 t)
    {
        return new float[,] {
            { 1f, 0f, 0f, t.x },
            { 0f, 1f, 0f, t.y },
            { 0f, 0f, 1f, t.z },
            { 0f, 0f, 0f, 1f  }
        };
    }

    // 대각선: e₁, e₂, e₃가 각각 sx, sy, sz배
    public static float[,] Scale(Vector3 s)
    {
        return new float[,] {
            { s.x, 0f,  0f,  0f },
            { 0f,  s.y, 0f,  0f },
            { 0f,  0f,  s.z, 0f },
            { 0f,  0f,  0f,  1f }
        };
    }

    // x축 회전: e₂ → (0, c, s), e₃ → (0, −s, c)
    public static float[,] RotateX(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad);
        float s = Mathf.Sin(rad);
        return new float[,] {
            { 1f, 0f,  0f, 0f },
            { 0f, c,  -s,  0f },
            { 0f, s,   c,  0f },
            { 0f, 0f,  0f, 1f }
        };
    }

    // y축 회전: e₁ → (c, 0, −s), e₃ → (s, 0, c)
    public static float[,] RotateY(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad);
        float s = Mathf.Sin(rad);
        return new float[,] {
            { c,  0f, s,  0f },
            { 0f, 1f, 0f, 0f },
            { -s, 0f, c,  0f },
            { 0f, 0f, 0f, 1f }
        };
    }

    // z축 회전: e₁ → (c, s, 0), e₂ → (−s, c, 0)
    public static float[,] RotateZ(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad);
        float s = Mathf.Sin(rad);
        return new float[,] {
            { c,  -s, 0f, 0f },
            { s,   c, 0f, 0f },
            { 0f, 0f, 1f, 0f },
            { 0f, 0f, 0f, 1f }
        };
    }

    // ---------- 곱셈 ----------

    // 4×4 × 4×4: 결과의 (row, col) = A의 row행과 B의 col열의 내적
    public static float[,] Multiply(float[,] A, float[,] B)
    {
        float[,] result = new float[4, 4];
        for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++)
                for (int k = 0; k < 4; k++)
                    result[row, col] += A[row, k] * B[k, col];
        return result;
    }

    // 여러 행렬을 왼쪽부터 차례로 곱함: Multiply(T, R, S) = T × R × S
    // 점에는 오른쪽 행렬(S)부터 적용됨
    public static float[,] Multiply(params float[][,] matrices)
    {
        float[,] result = Identity();
        foreach (float[,] M in matrices)
            result = Multiply(result, M);
        return result;
    }

    // 점에 행렬 적용: (x, y, z) → 동차좌표 (x, y, z, 1) → M을 곱함 → 네 번째 성분을 뗌
    public static Vector3 MultiplyPoint(float[,] M, Vector3 p)
    {
        float[] input = { p.x, p.y, p.z, 1f };
        float[] result = new float[4];
        for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++)
                result[row] += M[row, col] * input[col];
        return new Vector3(result[0], result[1], result[2]);
    }

    // 모든 정점에 같은 행렬을 적용
    public static Vector3[] MultiplyPoints(float[,] M, Vector3[] points)
    {
        Vector3[] result = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
            result[i] = MultiplyPoint(M, points[i]);
        return result;
    }
}
