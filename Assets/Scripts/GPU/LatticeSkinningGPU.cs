using UnityEngine;

public struct SkinningVoxel
{
    public int pID0, pID1, pID2, pID3, pID4, pID5, pID6, pID7;
    public float s, t, u;
}

public class LatticeSkinningGPU
{
    private const int THREAD_GROUP_SIZE_X = 64;

    private SkinningVoxel[] skinningVoxels;

    private ComputeShader compute;
    private ComputeBuffer skinningVoxelBuffer;
    private ComputeBuffer vertexBuffer;
    
    public LatticeSkinningGPU(PBDParticle[] particles, int[,,] particleIDs, Grid grid, Vector3[] vertices)
    {
        compute = Resources.Load<ComputeShader>("Skinning");

        // 各頂点がスキニングに使用するパーティクルのインデックスと、初期状態での位置関係を保存する
        skinningVoxels = new SkinningVoxel[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            SkinningVoxel voxel = new SkinningVoxel();

            float diffX = vertices[i].x - grid.bBoxMin.x;
            int pointMinX = Mathf.FloorToInt(diffX / grid.cellWidth);
            float diffY = vertices[i].y - grid.bBoxMin.y;
            int pointMinY = Mathf.FloorToInt(diffY / grid.cellWidth);
            float diffZ = vertices[i].z - grid.bBoxMin.z;
            int pointMinZ = Mathf.FloorToInt(diffZ / grid.cellWidth);

            Debug.Log("pointMinX: " + pointMinX + "pointMinY: " + pointMinY + "pointMinZ: " + pointMinZ);

            voxel.pID0 = particleIDs[pointMinX, pointMinY, pointMinZ];
            voxel.pID1 = particleIDs[pointMinX + 1, pointMinY, pointMinZ];
            voxel.pID2 = particleIDs[pointMinX, pointMinY + 1, pointMinZ];
            voxel.pID3 = particleIDs[pointMinX + 1, pointMinY + 1, pointMinZ];
            voxel.pID4 = particleIDs[pointMinX, pointMinY, pointMinZ + 1];
            voxel.pID5 = particleIDs[pointMinX + 1, pointMinY, pointMinZ + 1];
            voxel.pID6 = particleIDs[pointMinX, pointMinY + 1, pointMinZ + 1];
            voxel.pID7 = particleIDs[pointMinX + 1, pointMinY + 1, pointMinZ + 1];

            float s = diffX / grid.cellWidth - pointMinX;
            float t = diffY / grid.cellWidth - pointMinY;
            float u = diffZ / grid.cellWidth - pointMinZ;

            voxel.s = s;
            voxel.t = t;
            voxel.u = u;

            skinningVoxels[i] = voxel;
        }

        // バッファに値を代入する
        skinningVoxelBuffer = ComputeHelper.CreateStructuredBuffer(skinningVoxels);
        vertexBuffer = ComputeHelper.CreateStructuredBuffer(vertices);

    }

    public void Execute(ComputeBuffer particleBuffer)
    {
        UpdateSettings(particleBuffer);

        compute.Dispatch(0, Mathf.CeilToInt((float)skinningVoxelBuffer.count / THREAD_GROUP_SIZE_X), 1, 1);
    }

    private void UpdateSettings(ComputeBuffer particleBuffer)
    {
        ComputeHelper.SetBuffer(compute, particleBuffer, "Particles", 0);
        ComputeHelper.SetBuffer(compute, skinningVoxelBuffer, "SkinningVoxels", 0);
        ComputeHelper.SetBuffer(compute, vertexBuffer, "Vertices", 0);
    }

    public ComputeBuffer GetVertexBuffer()
    {
        return vertexBuffer;
    }

    public void ReleaseBuffers()
    {
        skinningVoxelBuffer.Release();
        vertexBuffer.Release();
    }
}
