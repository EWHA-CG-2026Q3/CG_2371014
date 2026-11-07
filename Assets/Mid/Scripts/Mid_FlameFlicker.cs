using UnityEngine;

// 풍등 안의 불꽃: 부모인 풍등의 행렬에 자기 로컬 행렬을 곱해 따라다니면서, 자기 자리에서 따로 일렁임
// (중간 선택 ④ 부모-자식으로 함께 움직이기 + 다른 물체에 다른 움직임 하나 더)
//
//   M_flame = M_lantern × M_local
//   M_local = T(풍등 안 자리) × Ry(빙글) × S(위아래로 늘었다 줄었다)
//
// S10의 Hierarchy(localToWorld = 부모 × 자식 로컬)를 Transform 대신 우리 코드로 계산함
// 풍등보다 나중에 Update해야 이번 프레임의 풍등 행렬을 쓸 수 있으므로 실행 순서를 뒤로 미룸
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Mid_FlameMesh))]
public class Mid_FlameFlicker : MonoBehaviour
{
    [SerializeField] Mid_LanternFlight parentLantern;                     // 부모 풍등 (비우면 부모 오브젝트에서 찾음)
    [SerializeField] Vector3 localOffset = new Vector3(0f, 0.06f, 0f);    // 풍등 안에서 불꽃이 놓인 자리
    [SerializeField] float flickerSpeed = 11f;                             // 일렁이는 빠르기
    [SerializeField] float flickerAmount = 0.3f;                           // 위아래로 늘어나는 정도
    [SerializeField] float spinSpeed = 120f;                               // 초당 y축 회전 각도
    [SerializeField] Light flameLight;                                     // 불꽃이 주변을 비추는 Point Light (선택)
    [SerializeField] float lightIntensity = 1.5f;

    Mid_FlameMesh flameMesh;
    float seed;

    void Awake()
    {
        flameMesh = GetComponent<Mid_FlameMesh>();
        if (parentLantern == null) parentLantern = GetComponentInParent<Mid_LanternFlight>();
        seed = Random.Range(0f, 100f);
    }

    void Update()
    {
        ApplyAt(Time.time);
    }

    public void ApplyAt(float time)
    {
        if (flameMesh == null) flameMesh = GetComponent<Mid_FlameMesh>();
        if (flameMesh.BaseVertices == null) flameMesh.Build();
        if (parentLantern == null) parentLantern = GetComponentInParent<Mid_LanternFlight>();

        // 일렁임: 빠르기가 다른 sin 두 개를 더해 규칙적으로 보이지 않게
        float t = time + seed;
        float flicker = Mathf.Sin(t * flickerSpeed) * 0.6f + Mathf.Sin(t * flickerSpeed * 2.3f) * 0.4f;
        float stretchY = 1f + flicker * flickerAmount;
        float squeezeXZ = 1f - flicker * flickerAmount * 0.3f;

        float[,] localT = Mid_Matrix.Translate(localOffset);
        float[,] localRy = Mid_Matrix.RotateY(t * spinSpeed);
        float[,] localS = Mid_Matrix.Scale(new Vector3(squeezeXZ, stretchY, squeezeXZ));
        float[,] local = Mid_Matrix.Multiply(localT, localRy, localS);

        // 부모 행렬 × 자식 로컬 행렬: 풍등이 움직이고 돌고 작아지면 불꽃도 그대로 따라감
        float[,] parent = parentLantern != null ? parentLantern.CurrentMatrix : Mid_Matrix.Identity();
        float[,] M = Mid_Matrix.Multiply(parent, local);

        Mesh mesh = flameMesh.Mesh;
        mesh.vertices = Mid_Matrix.MultiplyPoints(M, flameMesh.BaseVertices);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // 불빛도 같은 행렬로 옮김: 불꽃 로컬 좌표의 한 점(조금 위)에 M을 곱한 자리
        // 풍등이 별이 되며 작아지면(CurrentScale → 0) 불빛도 함께 꺼짐
        if (flameLight != null)
        {
            flameLight.transform.position = Mid_Matrix.MultiplyPoint(M, new Vector3(0f, 0.08f, 0f));
            float lanternScale = parentLantern != null ? parentLantern.CurrentScale : 1f;
            flameLight.intensity = lightIntensity * lanternScale * (1f + flicker * 0.15f);
        }
    }
}
