using UnityEngine;

// 頂点が属するボクセルと、ボクセル内部での位置
struct VertexPosInVoxel
{
    // この頂点が属するボクセル
    public int voxelX;
    public int voxelY;
    public int voxelZ;

    // ボクセル内部での位置を示す補間パラメータ
    public float s; // X方向
    public float t; // Y方向
    public float u; // Z方向
}

/// <summary>
/// Lattice Shape Matchingのシミュレーション結果を使って、元形状のスキニングを実行する
/// </summary>
public class ShapeMatchSkinnedMesh : MonoBehaviour
{
    public Mesh originalMesh;

    private Mesh skinnedMesh;
    private VertexPosInVoxel[] vertexPosInVoxelArray;


    private void Start()
    {
        // 初期状態の各頂点に対して、所属するボクセルと、ボクセル内部での位置を保存する
        int numVertices = originalMesh.vertexCount;
        LatticeManager manager = GetComponent<LatticeManager>();
        float gridUnitSize = manager.gridUnitSize;

        vertexPosInVoxelArray = new VertexPosInVoxel[numVertices];

        for (int i = 0; i < numVertices; i++)
        {
            // ワールド座標に変換
            Vector3 vertexPosWorld = transform.TransformPoint(originalMesh.vertices[i]);

            int voxelX = Mathf.FloorToInt(vertexPosWorld.x / gridUnitSize);
            int voxelY = Mathf.FloorToInt(vertexPosWorld.y / gridUnitSize);
            int voxelZ = Mathf.FloorToInt(vertexPosWorld.z / gridUnitSize);

            float s = vertexPosWorld.x / gridUnitSize - Mathf.FloorToInt(vertexPosWorld.x / gridUnitSize);
            float t = vertexPosWorld.y / gridUnitSize - Mathf.FloorToInt(vertexPosWorld.y / gridUnitSize);
            float u = vertexPosWorld.z / gridUnitSize - Mathf.FloorToInt(vertexPosWorld.z / gridUnitSize);

            VertexPosInVoxel v = new VertexPosInVoxel();

            v.voxelX = voxelX;
            v.voxelY = voxelY;
            v.voxelZ = voxelZ;
            v.s = s;
            v.t = t;
            v.u = u;

            vertexPosInVoxelArray[i] = v;
        }

        // 元形状メッシュをコピーしたメッシュを作成して描画する
        skinnedMesh = Instantiate(originalMesh);

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = skinnedMesh;

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
    }

    private void LateUpdate()
    {
        // スキニング実行
        PBDParticle[,,] p = GetComponent<LatticeManager>().particles;

        Vector3[] skinnedVertices = new Vector3[skinnedMesh.vertices.Length];

        for (int i = 0; i < skinnedMesh.vertices.Length; i++)
        {
            Vector3 vertexPos = skinnedMesh.vertices[i];
            Vector3 skinnedPos = new Vector3(0, 0, 0);

            int vX = vertexPosInVoxelArray[i].voxelX;
            int vY = vertexPosInVoxelArray[i].voxelY;
            int vZ = vertexPosInVoxelArray[i].voxelZ;
            float s = vertexPosInVoxelArray[i].s;
            float t = vertexPosInVoxelArray[i].t;
            float u = vertexPosInVoxelArray[i].u;

            skinnedPos =
                (1 - u) * (1 - t) * (1 - s) * p[vX, vY, vZ].pos +
                (1 - u) * (1 - t) * s * p[vX + 1, vY, vZ].pos +
                (1 - u) * t * (1 - s) * p[vX, vY + 1, vZ].pos +
                u * (1 - t) * (1 - s) * p[vX, vY, vZ + 1].pos +
                u * t * (1 - s) * p[vX, vY + 1, vZ + 1].pos +
                u * (1 - t) * s * p[vX + 1, vY, vZ + 1].pos +
                (1 - u) * t * s * p[vX + 1, vY + 1, vZ].pos +
                u * t * s * p[vX + 1, vY + 1, vZ + 1].pos;

            skinnedVertices[i] = transform.InverseTransformPoint(skinnedPos);
        }

        skinnedMesh.SetVertices(skinnedVertices);
        skinnedMesh.RecalculateNormals();
    }
}
