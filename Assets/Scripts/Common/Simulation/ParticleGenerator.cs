using UnityEngine;
using System.Collections.Generic;
public static class ParticleGenerator
{
    /// <summary>
    /// �O���b�h�̓_�̏�Ƀp�[�e�B�N����z�u����
    /// </summary>
    public static (PBDParticle[], int[,,]) GenerateGridParticles(Grid grid)
    {
        List<PBDParticle> particleList = new List<PBDParticle>();
        int[,,] particleIDs = new int[grid.numPointsX, grid.numPointsY, grid.numPointsZ];
        Debug.Log("numPointsX: " + grid.numPointsX + "numPointsY: " + grid.numPointsY + "numPointsZ: " + grid.numPointsZ);
        int particleCount = 0;

        for (int z = 0; z < grid.numPointsZ; z++)
        {
            for (int y = 0; y < grid.numPointsY; y++)
            {
                for (int x = 0; x < grid.numPointsX; x++)
                {
                    if (grid.isValidPoints[x, y, z])
                    {
                        PBDParticle particle = new PBDParticle(grid.pointPositions[x, y, z]);
                        particleList.Add(particle);

                        particleIDs[x, y, z] = particleCount;
                        particleCount++;
                    }
                }
            }
        }

        return (particleList.ToArray(), particleIDs);
    }

    /// <summary>
    /// ���b�V���`��ɉ����ăO���b�h��Ƀp�[�e�B�N����z�u����
    /// </summary>
    public static PBDParticle[] GenerateGridParticles(Vector3[] vertices, int[] triangles, float gridWidth)
    {
        // ���_�z�񂩂�o�E���f�B���O�{�b�N�X�����߂�
        (Vector3 boundingBoxMax, Vector3 boundingBoxMin) = GetBoundingBox(vertices);

        // ���������Ƃ̃O���b�h�̐�
        int numGridX = Mathf.CeilToInt((boundingBoxMax.x - boundingBoxMin.x) / gridWidth) + 1;
        int numGridY = Mathf.CeilToInt((boundingBoxMax.y - boundingBoxMin.y) / gridWidth) + 1;
        int numGridZ = Mathf.CeilToInt((boundingBoxMax.z - boundingBoxMin.z) / gridWidth) + 1;
        Debug.Log("numGridX: " + numGridX + "numGridY: " + numGridY + "numGridZ: " + numGridZ);

        // �O���b�h�̒��_��Ƀp�[�e�B�N����z�u���邩�ǂ����̃t���O
        bool[,,] isValidPoint = new bool[numGridX, numGridY, numGridZ];

        // �{�N�Z���ƃ��b�V���̃|���S���̌�������E���������s���A�p�[�e�B�N����z�u����K�v�����邩�ǂ������肷��
        for (int z = 0; z < numGridZ - 1; z++)
        {
            for (int y = 0; y < numGridY - 1; y++)
            {
                for (int x = 0; x < numGridX - 1; x++)
                {
                    Vector3 center = new Vector3(
                        boundingBoxMin.x + (x + 0.5f) * gridWidth,
                        boundingBoxMin.y + (y + 0.5f) * gridWidth,
                        boundingBoxMin.z + (z + 0.5f) * gridWidth);

                    bool intersect = IsIntersectMesh(center, gridWidth, vertices, triangles);
                    bool inside = IsVoxelInsideMesh(center, gridWidth, vertices, triangles);

                    if (!(intersect || inside)) continue;

                    // �����������͓����ɂ���ꍇ�́A���̃{�N�Z����8���_��L����
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

        // �p�[�e�B�N���̐���
        List<PBDParticle> particles = new List<PBDParticle>();

        for (int z = 0; z < numGridZ; z++)
        {
            for (int y = 0; y < numGridY; y++)
            {
                for (int x = 0; x < numGridX; x++)
                {
                    if (isValidPoint[x, y, z])
                    {
                        Vector3 pos = new Vector3(boundingBoxMin.x + gridWidth * x, boundingBoxMin.y + gridWidth * y, boundingBoxMin.z + gridWidth * z);
                        PBDParticle p = new PBDParticle(pos);
                        particles.Add(p);
                    }
                }
            }
        }

        return particles.ToArray();
    }

    /// <summary>
    /// ���͂������_�z��̃o�E���f�B���O�{�b�N�X��XYZ+��XYZ-�̍��W�����߂�
    /// </summary>
    /// <param name="vertices">���_�z��</param>
    /// <returns>XYZ+�EXYZ-</returns>
    private static (Vector3, Vector3) GetBoundingBox(Vector3[] vertices)
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
    /// �{�N�Z���ƃ��b�V���̖ʂ��������Ă��邩�ǂ������肷��
    /// </summary>
    private static bool IsIntersectMesh(Vector3 voxelCenter, float voxelSize, Vector3[] vertices, int[] triangles)
    {
        float half = voxelSize * 0.5f;

        // �{�N�Z����8���_���`
        Vector3[] corners = new Vector3[8];
        corners[0] = voxelCenter + new Vector3(-half, -half, -half);
        corners[1] = voxelCenter + new Vector3(half, -half, -half);
        corners[2] = voxelCenter + new Vector3(half, half, -half);
        corners[3] = voxelCenter + new Vector3(-half, half, -half);
        corners[4] = voxelCenter + new Vector3(-half, -half, half);
        corners[5] = voxelCenter + new Vector3(half, -half, half);
        corners[6] = voxelCenter + new Vector3(half, half, half);
        corners[7] = voxelCenter + new Vector3(-half, half, half);

        // �{�N�Z����6�ʂ�2�̎O�p�`�Œ�`
        int[,] faceTris = new int[,]
        {
            {0, 1, 2}, {0, 2, 3}, // -Z
            {5, 4, 7}, {5, 7, 6}, // +Z
            {1, 5, 6}, {1, 6, 2}, // +X
            {4, 0, 3}, {4, 3, 7}, // -X
            {3, 2, 6}, {3, 6, 7}, // +Y
            {4, 5, 1}, {4, 1, 0}  // -Y
        };

        // ���b�V���̑S�O�p�`�ɑ΂��Č�����������s����
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // ���_���W�����[���h���W�ɕϊ�
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];

            // �O�p�`�̏d�S���{�N�Z�����ɂ���ꍇ�i�{�N�Z�������b�V��������ꍇ�j���A��������Ƃ���
            Vector3 triCenter = (v0 + v1 + v2) / 3.0f;
            if (IsPointInsideVoxel(triCenter, voxelCenter, half)) return true;

            // �e�{�N�Z���ʁi�O�p�`�j�Ƃ̌�������
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
    /// �_�ƃ{�N�Z���̓��O����
    /// </summary>
    private static bool IsPointInsideVoxel(Vector3 p, Vector3 center, float half)
    {
        return Mathf.Abs(p.x - center.x) <= half &&
               Mathf.Abs(p.y - center.y) <= half &&
               Mathf.Abs(p.z - center.z) <= half;
    }

    /// <summary>
    /// �{�N�Z�������b�V���̓����ɂ��邩�ǂ������肷��
    /// �{�N�Z���̒��S���烌�C���΂��āA���b�V���ƌ�������񐔂���Ȃ�����A�����Ȃ�O��
    /// </summary>
    private static bool IsVoxelInsideMesh(Vector3 voxelCenter, float voxelSize, Vector3[] vertices, int[] triangles)
    {
        Vector3 rayDir = Vector3.right;
        int hitCount = 0;

        // ���b�V�����̑S�O�p�`�ƃ��C�̌�����������s����
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

        // ��Ȃ�true�A�����Ȃ�false
        return (hitCount % 2) == 1;
    }
}
