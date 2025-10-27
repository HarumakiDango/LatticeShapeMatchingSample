using UnityEngine;
using System.Collections.Generic;

public class LatticeManager : MonoBehaviour
{
    public Vector3 gridRange = new Vector3(1, 1, 1);
    public float gridUnitSize = 0.2f;

    public int clusterSize = 1;

    private bool[,,] isValidPoint;
    public PBDParticle[,,] particles;

    private int numGridX;
    private int numGridY;
    private int numGridZ;

    private void Start()
    {
        numGridX = Mathf.CeilToInt(gridRange.x / gridUnitSize);
        numGridY = Mathf.CeilToInt(gridRange.y / gridUnitSize);
        numGridZ = Mathf.CeilToInt(gridRange.z / gridUnitSize);

        // 元形状のメッシュを取得
        Mesh originalMesh = GetComponent<ShapeMatchSkinnedMesh>().originalMesh;


        // ボクセルごとに、メッシュの状態（外側・内側・交差）を判定する
        isValidPoint = new bool[numGridX, numGridY, numGridZ];
        for (int z = 0; z < numGridZ - 1; z++)
        {
            for (int y = 0; y < numGridY - 1; y++)
            {
                for (int x = 0; x < numGridX - 1; x++)
                {
                    Vector3 center = new Vector3((x + 0.5f) * gridUnitSize, (y + 0.5f) * gridUnitSize, (z + 0.5f) * gridUnitSize);

                    bool intersect = IsIntersectMesh(center, gridUnitSize, originalMesh);
                    bool inside = IsVoxelInsideMesh(center, gridUnitSize, originalMesh);

                    if (!(intersect || inside)) continue;

                    // 交差もしくは内側にある場合は、このボクセルの8頂点を有効化
                    isValidPoint[x, y, z] = true;
                    isValidPoint[x + 1, y, z] = true;
                    isValidPoint[x, y + 1, z] = true;
                    isValidPoint[x + 1, y + 1, z] = true;
                    isValidPoint[x, y, z + 1] = true;
                    isValidPoint[x + 1, y, z + 1] = true;
                    isValidPoint[x, y + 1, z + 1] = true;
                    isValidPoint[x + 1, y + 1, z + 1] = true;
                }
            }
        }


        // パーティクルを初期化
        particles = new PBDParticle[numGridX, numGridY, numGridZ];
        for (int z = 0; z < numGridZ; z++)
        {
            for (int y = 0; y < numGridY; y++)
            {
                for (int x = 0; x < numGridX; x++)
                {
                    Vector3 initPos = new Vector3(gridUnitSize * x, gridUnitSize * y, gridUnitSize * z);
                    particles[x, y, z] = new PBDParticle(initPos);
                }
            }
        }

        // シェイプマッチングのクラスタを登録
        List<ShapeMatchCluster> clusterList = new List<ShapeMatchCluster>();
        for (int z = 0; z < numGridZ; z++)
        {
            for (int y = 0; y < numGridY; y++)
            {
                for (int x = 0; x < numGridX; x++)
                {
                    if (!isValidPoint[x, y, z]) continue;

                    List<PBDParticle> clusterParticleList = new List<PBDParticle>();

                    for (int k = z - clusterSize; k <= z + clusterSize; k++)
                    {
                        for (int j = y - clusterSize; j <= y + clusterSize; j++)
                        {
                            for (int i = x - clusterSize; i <= x + clusterSize; i++)
                            {
                                if (i >= 0 && j >= 0 && k >= 0 && i < numGridX && j < numGridY && k < numGridZ)
                                {
                                    if (isValidPoint[i, j, k])
                                    {
                                        clusterParticleList.Add(particles[i, j, k]);
                                        particles[i, j, k].numClusters++;
                                    }
                                }
                            }
                        }
                    }

                    if (clusterParticleList.Count >= 5)
                    clusterList.Add(new ShapeMatchCluster(clusterParticleList.ToArray()));
                }
            }
        }

        // シミュレーターに入力する
        List<PBDParticle> simulationParticleList = new List<PBDParticle>();
        for (int z = 0; z < numGridZ; z++)
        {
            for (int y = 0; y < numGridY; y++)
            {
                for (int x = 0; x < numGridX; x++)
                {
                    if (isValidPoint[x, y, z]) simulationParticleList.Add(particles[x, y, z]);
                }
            }
        }

        GetComponent<Simulator>().Initialize(simulationParticleList.ToArray(), clusterList.ToArray());
    }

    /// <summary>
    /// ボクセルとメッシュの面が交差しているかどうか判定する
    /// </summary>
    private bool IsIntersectMesh(Vector3 voxelCenter, float voxelSize, Mesh mesh)
    {
        float half = voxelSize * 0.5f;

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // ボクセルの8頂点を定義
        Vector3[] corners = new Vector3[8];
        corners[0] = voxelCenter + new Vector3(-half, -half, -half);
        corners[1] = voxelCenter + new Vector3(half, -half, -half);
        corners[2] = voxelCenter + new Vector3(half, half, -half);
        corners[3] = voxelCenter + new Vector3(-half, half, -half);
        corners[4] = voxelCenter + new Vector3(-half, -half, half);
        corners[5] = voxelCenter + new Vector3(half, -half, half);
        corners[6] = voxelCenter + new Vector3(half, half, half);
        corners[7] = voxelCenter + new Vector3(-half, half, half);

        // ボクセルの6面を2つの三角形で定義
        int[,] faceTris = new int[,]
        {
            {0, 1, 2}, {0, 2, 3}, // -Z
            {5, 4, 7}, {5, 7, 6}, // +Z
            {1, 5, 6}, {1, 6, 2}, // +X
            {4, 0, 3}, {4, 3, 7}, // -X
            {3, 2, 6}, {3, 6, 7}, // +Y
            {4, 5, 1}, {4, 1, 0}  // -Y
        };

        // メッシュの全三角形に対して交差判定を実行する
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // 頂点座標をワールド座標に変換
            Vector3 v0 = transform.TransformPoint(vertices[triangles[i]]);
            Vector3 v1 = transform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 v2 = transform.TransformPoint(vertices[triangles[i + 2]]);

            // 三角形の重心がボクセル内にある場合（ボクセルがメッシュを内包する場合）も、交差判定とする
            Vector3 triCenter = (v0 + v1 + v2) / 3.0f;
            if (IsPointInsideVoxel(triCenter, voxelCenter, half)) return true;

            // 各ボクセル面（三角形）との交差判定
            for (int f = 0; f < faceTris.GetLength(0); f++)
            {
                Vector3 t0 = corners[faceTris[f, 0]];
                Vector3 t1 = corners[faceTris[f, 1]];
                Vector3 t2 = corners[faceTris[f, 2]];

                if (GeometryMath.IsTriangleIntersectTriangle(v0, v1, v2, t0, t1, t2)) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 点とボクセルの内外判定
    /// </summary>
    private bool IsPointInsideVoxel(Vector3 p, Vector3 center, float half)
    {
        return Mathf.Abs(p.x - center.x) <= half &&
               Mathf.Abs(p.y - center.y) <= half &&
               Mathf.Abs(p.z - center.z) <= half;
    }

    /// <summary>
    /// ボクセルがメッシュの内側にあるかどうか判定する
    /// ボクセルの中心からレイを飛ばして、メッシュと交差する回数が奇数なら内側、偶数なら外側
    /// </summary>
    private bool IsVoxelInsideMesh(Vector3 voxelCenter, float voxelSize, Mesh mesh)
    {
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;

        Vector3 rayDir = Vector3.right;
        int hitCount = 0;

        // メッシュ内の全三角形とレイの交差判定を実行する
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = transform.TransformPoint(vertices[triangles[i]]);
            Vector3 v1 = transform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 v2 = transform.TransformPoint(vertices[triangles[i + 2]]);

            if (GeometryMath.RayIntersectsTriangle(voxelCenter, rayDir, v0, v1, v2, out float t))
            {
                if (t > 0) hitCount++;
            }
        }

        // 奇数ならtrue、偶数ならfalse
        return (hitCount % 2) == 1;
    }

    private void Update()
    {
        
    }
}
