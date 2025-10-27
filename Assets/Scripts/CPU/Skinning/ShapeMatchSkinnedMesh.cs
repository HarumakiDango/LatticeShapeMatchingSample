using UnityEngine;

// ���_��������{�N�Z���ƁA�{�N�Z�������ł̈ʒu
struct VertexPosInVoxel
{
    // ���̒��_��������{�N�Z��
    public int voxelX;
    public int voxelY;
    public int voxelZ;

    // �{�N�Z�������ł̈ʒu��������ԃp�����[�^
    public float s; // X����
    public float t; // Y����
    public float u; // Z����
}

/// <summary>
/// Lattice Shape Matching�̃V�~�����[�V�������ʂ��g���āA���`��̃X�L�j���O�����s����
/// </summary>
public class ShapeMatchSkinnedMesh : MonoBehaviour
{
    public Mesh originalMesh;

    private Mesh skinnedMesh;
    private VertexPosInVoxel[] vertexPosInVoxelArray;


    private void Start()
    {
        // ������Ԃ̊e���_�ɑ΂��āA��������{�N�Z���ƁA�{�N�Z�������ł̈ʒu��ۑ�����
        int numVertices = originalMesh.vertexCount;
        LatticeManager manager = GetComponent<LatticeManager>();
        float gridUnitSize = manager.gridUnitSize;

        vertexPosInVoxelArray = new VertexPosInVoxel[numVertices];

        for (int i = 0; i < numVertices; i++)
        {
            // ���[���h���W�ɕϊ�
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

        // ���`�󃁃b�V�����R�s�[�������b�V�����쐬���ĕ`�悷��
        skinnedMesh = Instantiate(originalMesh);

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = skinnedMesh;

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
    }

    private void LateUpdate()
    {
        // �X�L�j���O���s
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
