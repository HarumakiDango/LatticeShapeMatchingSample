using UnityEngine;

public class Grid
{
    public Vector3 bBoxMin;
    public Vector3 bBoxMax;
    public float cellWidth;
    public int numPointsX;
    public int numPointsY;
    public int numPointsZ;

    public Vector3[,,] pointPositions; // x,y,z
    public bool[,,] isValidPoints;
    public int[,,] particleIDs;

    public Grid(Vector3[] vertices, int[] triangles, int maxDiv)
    {
        // 頂点配列からバウンディングボックスを求める
        (bBoxMax, bBoxMin) = GetBoundingBox(vertices);

        // バウンディングボックスの最長辺を最大分割数で割り、ボクセルの幅を求める
        float edgeXLength = bBoxMax.x - bBoxMin.x;
        float edgeYLength = bBoxMax.y - bBoxMin.y;
        float edgeZLength = bBoxMax.z - bBoxMin.z;

        if (edgeXLength > edgeYLength && edgeXLength > edgeZLength)
        {
            cellWidth = edgeXLength / (maxDiv + 1);
            numPointsX = maxDiv + 2;
            numPointsY = Mathf.CeilToInt(edgeYLength / cellWidth) + 1;
            numPointsZ = Mathf.CeilToInt(edgeZLength / cellWidth) + 1;
        }
        else if (edgeYLength > edgeXLength && edgeYLength > edgeZLength)
        {
            cellWidth = edgeYLength / (maxDiv + 1);
            numPointsX = Mathf.CeilToInt(edgeXLength / cellWidth) + 1;
            numPointsY = maxDiv + 2;
            numPointsZ = Mathf.CeilToInt(edgeZLength / cellWidth) + 1;
        }
        else
        {
            cellWidth = edgeZLength / (maxDiv + 1);
            numPointsX = Mathf.CeilToInt(edgeXLength / cellWidth) + 1;
            numPointsY = Mathf.CeilToInt(edgeYLength / cellWidth) + 1;
            numPointsZ = maxDiv + 2;
        }

        // グリッドの頂点上にパーティクルを配置するかどうかのフラグ
        isValidPoints = new bool[numPointsX, numPointsY, numPointsZ];

        // ボクセルとメッシュのポリゴンの交差判定・内包判定を実行し、パーティクルを配置する必要があるかどうか判定する
        for (int z = 0; z < numPointsZ - 1; z++)
        {
            for (int y = 0; y < numPointsY - 1; y++)
            {
                for (int x = 0; x < numPointsX - 1; x++)
                {
                    Vector3 center = new Vector3(
                                    bBoxMin.x + (x + 0.5f) * cellWidth,
                                    bBoxMin.y + (y + 0.5f) * cellWidth,
                                    bBoxMin.z + (z + 0.5f) * cellWidth);

                    bool intersect = IsIntersectMesh(center, cellWidth, vertices, triangles);
                    bool inside = IsVoxelInsideMesh(center, cellWidth, vertices, triangles);

                    if (!(intersect || inside)) continue;

                    // 交差もしくは内側にある場合は、このボクセルの8頂点を有効化
                    isValidPoints[x, y, z] = true;
                    isValidPoints[x + 1, y, z] = true;
                    isValidPoints[x, y + 1, z] = true;
                    isValidPoints[x + 1, y + 1, z] = true;
                    isValidPoints[x, y, z + 1] = true;
                    isValidPoints[x + 1, y, z + 1] = true;
                    isValidPoints[x, y + 1, z + 1] = true;
                    isValidPoints[x + 1, y + 1, z + 1] = true;
                }
            }
        }

        pointPositions = new Vector3[numPointsX, numPointsY, numPointsZ];
        for (int z = 0; z < numPointsZ; z++)
        {
            for (int y = 0; y < numPointsY; y++)
            {
                for (int x = 0; x < numPointsX; x++)
                {
                    pointPositions[x, y, z] = new Vector3(
                        bBoxMin.x + x * cellWidth,
                        bBoxMin.y + y * cellWidth,
                        bBoxMin.z + z * cellWidth);
                }
            }
        }
    }

    /// <summary>
    /// 入力した頂点配列のバウンディングボックスのXYZ+とXYZ-の座標を求める
    /// </summary>
    /// <param name="vertices">頂点配列</param>
    /// <returns>XYZ+・XYZ-</returns>
    private (Vector3, Vector3) GetBoundingBox(Vector3[] vertices)
    {
        Vector3 max = new Vector3(-Mathf.Infinity, -Mathf.Infinity, -Mathf.Infinity);
        Vector3 min = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

        for (int i = 0; i < vertices.Length; i++)
        {
            if (vertices[i].x > max.x) max.x = vertices[i].x;
            if (vertices[i].y > max.y) max.y = vertices[i].y;
            if (vertices[i].z > max.z) max.z = vertices[i].z;

            if (vertices[i].x < min.x) min.x = vertices[i].x;
            if (vertices[i].y < min.y) min.y = vertices[i].y;
            if (vertices[i].z < min.z) min.z = vertices[i].z;
        }

        return (max, min);
    }

    /// <summary>
    /// ボクセルとメッシュの面が交差しているかどうか判定する
    /// </summary>
    private static bool IsIntersectMesh(Vector3 voxelCenter, float voxelSize, Vector3[] vertices, int[] triangles)
    {
        float half = voxelSize * 0.5f;

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
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];

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
    private static bool IsPointInsideVoxel(Vector3 p, Vector3 center, float half)
    {
        return Mathf.Abs(p.x - center.x) <= half &&
               Mathf.Abs(p.y - center.y) <= half &&
               Mathf.Abs(p.z - center.z) <= half;
    }

    /// <summary>
    /// ボクセルがメッシュの内側にあるかどうか判定する
    /// ボクセルの中心からレイを飛ばして、メッシュと交差する回数が奇数なら内側、偶数なら外側
    /// </summary>
    private static bool IsVoxelInsideMesh(Vector3 voxelCenter, float voxelSize, Vector3[] vertices, int[] triangles)
    {
        Vector3 rayDir = Vector3.right;
        int hitCount = 0;

        // メッシュ内の全三角形とレイの交差判定を実行する
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];

            if (GeometryMath.RayIntersectsTriangle(voxelCenter, rayDir, v0, v1, v2, out float t))
            {
                if (t > 0) hitCount++;
            }
        }

        // 奇数ならtrue、偶数ならfalse
        return (hitCount % 2) == 1;
    }
}
