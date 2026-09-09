using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S03_CustomPolygonMesh_Square : MonoBehaviour
{
    void Start()
    {
        // TODO 1: 4개의 정점 좌표 (사각형 기준)
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0f, 1f, 0f), // 0번 정점 (좌측 상단)
            new Vector3(1f, 1f, 0f), // 1번 정점 (우측 상단)
            new Vector3(1f, 0f, 0f), // 2번 정점 (우측 하단)
            new Vector3(0f, 0f, 0f)  // 3번 정점 (좌측 하단)
        };

        // TODO 2: 정점 3개씩 묶어 2개의 삼각형(사각형 완성)으로 구성
        // 시계 방향(또는 반시계 방향) 순서가 같아야 면이 정상적으로 보입니다.
        int[] triangles = new int[]
        {
            0, 1, 2, // 첫 번째 삼각형 (0 -> 1 -> 2)
            0, 2, 3  // 두 번째 삼각형 (0 -> 2 -> 3)
        };

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }
}