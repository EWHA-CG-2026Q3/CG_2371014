using UnityEngine;

// 풍등의 한살이: 점화 → 상승 → 별이 되며 사라짐 → 다시 점화 (계속 순환)
// 이동·회전·크기를 직접 만든 행렬로 합성해 정점에 곱함 (중간 필수 ④ 변환)
//
//   M = T(위치) × Ry(천천히 도는 회전) × Rz(바람에 기우뚱) × S(크기)
//
// 정점에는 오른쪽부터 적용됨: 원점에서 크기 → 기울기 → 회전을 정한 뒤 마지막에 위치로 옮김
// 이 오브젝트의 Transform은 쓰지 않음 (부모 Mid_Lanterns와 함께 원점, 회전 0, 크기 1로 둠)
[RequireComponent(typeof(Mid_LanternMesh))]
public class Mid_LanternFlight : MonoBehaviour
{
    [Header("경로")]
    [SerializeField] Vector3 startPoint = new Vector3(0f, 0.3f, 5f);   // 마당에서 출발하는 자리
    [SerializeField] Mid_Constellation constellation;                    // 도착할 별자리
    [SerializeField] int starIndex;                                      // 이 풍등이 맡은 별 번호

    [Header("시간")]
    [SerializeField] float period = 21f;   // 한살이 한 번의 길이 (초)
    [SerializeField] float phase;          // 출발 시간 차 (초): 풍등마다 다르게

    [Header("움직임")]
    [SerializeField] float spinSpeed = 25f;     // 초당 y축 회전 각도
    [SerializeField] float tiltAngle = 7f;      // 바람에 기우뚱하는 최대 각도
    [SerializeField] float swayAmount = 0.6f;   // 올라가며 좌우로 흔들리는 폭

    // 구간 경계 (0~1 사이의 한살이 진행도 u)
    const float IgniteEnd = 0.08f;   // 0.00–0.08: 점화 (작게 나타나 커짐)
    const float RiseEnd = 0.85f;     // 0.08–0.85: 상승
                                     // 0.85–1.00: 별이 됨 (작아지며 사라짐)

    Mid_LanternMesh lanternMesh;
    bool becameStar;

    // 이번 프레임의 풍등 행렬: 자식인 불꽃(Mid_FlameFlicker)이 여기에 자기 행렬을 곱함
    public float[,] CurrentMatrix { get; private set; } = Mid_Matrix.Identity();
    // 지금 풍등의 크기 배율 (0이면 보이지 않음)
    public float CurrentScale { get; private set; }

    void Awake()
    {
        lanternMesh = GetComponent<Mid_LanternMesh>();
    }

    void Update()
    {
        ApplyAt(Time.time);
    }

    // 시각 time일 때의 풍등 모습을 계산해 메시에 적용
    public void ApplyAt(float time)
    {
        if (lanternMesh == null) lanternMesh = GetComponent<Mid_LanternMesh>();
        if (lanternMesh.BaseVertices == null) lanternMesh.Build();

        // 한살이 진행도 u: 0 → 1을 계속 반복
        float u = Mathf.Repeat(time + phase, period) / period;

        Vector3 starPoint = constellation != null
            ? constellation.GetStarPosition(starIndex)
            : startPoint + new Vector3(0f, 12f, 15f);

        Vector3 position;
        float scale;

        if (u < IgniteEnd)
        {
            // 점화: 출발점에서 불이 켜지며 커짐
            float k = u / IgniteEnd;
            position = startPoint;
            scale = Mathf.SmoothStep(0f, 1f, k);
            becameStar = false;
        }
        else if (u < RiseEnd)
        {
            // 상승: 출발점 → 별 위치. 처음엔 빠르고 위로 갈수록 느려짐 (ease-out)
            float k = (u - IgniteEnd) / (RiseEnd - IgniteEnd);
            float e = 1f - (1f - k) * (1f - k);
            position = Vector3.Lerp(startPoint, starPoint, e);

            // 좌우 흔들림: 출발과 도착에서는 0, 가운데에서 가장 크게 (sin(π·e))
            float sway = Mathf.Sin(Mathf.PI * e) * swayAmount;
            position.x += Mathf.Sin(time * 0.8f + phase) * sway;
            position.z += Mathf.Cos(time * 0.6f + phase) * sway * 0.5f;
            scale = 1f;
            becameStar = false;
        }
        else
        {
            // 별이 됨: 별 위치에서 작아지며 사라짐, 이 구간에 들어서는 순간 별이 켜짐
            float k = (u - RiseEnd) / (1f - RiseEnd);
            position = starPoint;
            scale = 1f - Mathf.SmoothStep(0f, 1f, k);
            if (!becameStar)
            {
                becameStar = true;
                if (constellation != null) constellation.Ignite(starIndex, time);
            }
        }

        float spin = (time + phase) * spinSpeed;
        float tilt = Mathf.Sin(time * 1.3f + phase * 2f) * tiltAngle;

        // M = T × Ry × Rz × S
        float[,] T = Mid_Matrix.Translate(position);
        float[,] Ry = Mid_Matrix.RotateY(spin);
        float[,] Rz = Mid_Matrix.RotateZ(tilt);
        float[,] S = Mid_Matrix.Scale(Vector3.one * scale);
        CurrentMatrix = Mid_Matrix.Multiply(T, Ry, Rz, S);
        CurrentScale = scale;

        // 모든 정점에 M을 곱해 메시를 갱신
        Mesh mesh = lanternMesh.Mesh;
        mesh.vertices = Mid_Matrix.MultiplyPoints(CurrentMatrix, lanternMesh.BaseVertices);
        mesh.RecalculateNormals();
        // 정점을 옮겼으므로 경계 상자도 다시 계산 (안 하면 카메라 밖으로 잘못 판단해 사라질 수 있음)
        mesh.RecalculateBounds();
    }

    // 씬 설정용: 출발점, 별자리, 별 번호, 시간 차를 한 번에 지정
    public void Configure(Vector3 start, Mid_Constellation targetConstellation, int index, float lifePeriod, float startPhase)
    {
        startPoint = start;
        constellation = targetConstellation;
        starIndex = index;
        period = lifePeriod;
        phase = startPhase;
    }
}
