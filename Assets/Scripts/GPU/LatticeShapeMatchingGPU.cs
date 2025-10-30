using UnityEngine;

public class LatticeShapeMatchingGPU : MonoBehaviour
{
    // [Range(0.1f, 1)] public float gridWidth = 0.2f;
    [Range(0, 20)] public int maxDiv = 5;
    [Range(1.0f, 1.1f)] public float gridScale = 1.01f;
    public bool drawGrid = false;

    private Grid grid;

    PBDParticle[] particles;
    int[,,] particleIDs;
    ShapeMatchCluster[] clusters;

    PBDSimulatorGPU simulator;
    LatticeSkinningGPU skinningSolver;

    private Material material;


    private void Start()
    {
        // �^�[�Q�b�g�̃��b�V�����擾���āA���[���h��Ԃ̒��_���W���v�Z
        Mesh targetMesh = GetComponent<MeshFilter>().mesh;
        Vector3[] verticesWorld = new Vector3[targetMesh.vertices.Length];
        for (int i = 0; i < verticesWorld.Length; i++)
        {
            verticesWorld[i] = transform.TransformPoint(targetMesh.vertices[i]);
        }

        // ���b�V���`��ɍ��킹���O���b�h���`����
        grid = new Grid(verticesWorld, targetMesh.triangles, maxDiv, gridScale);

        // �O���b�h�̓_�̏�Ƀp�[�e�B�N����z�u����
        (particles, particleIDs) = ParticleGenerator.GenerateGridParticles(grid);

        
        Debug.Log("�p�[�e�B�N�����F" + particles.Length);

        // �p�[�e�B�N���ɃV�F�C�v�}�b�`���O�̃N���X�^�����蓖�Ă�
        clusters = ShapeMatchingClusterMaker.AssignGridClusters(particles, grid.cellWidth);
        Debug.Log("�N���X�^���F" + clusters.Length);


        // ���������p�[�e�B�N���ƃN���X�^��n���ăV�~�����[�^���C���X�^���X��
        simulator = new PBDSimulatorGPU(particles, clusters);

        // �X�L�j���O�����s����R���|�[�l���g���C���X�^���X��
        skinningSolver = new LatticeSkinningGPU(particles, particleIDs, grid, verticesWorld);

        // �X�L�j���O���ʂ�n���}�e���A�����擾���Ă���
        material = GetComponent<MeshRenderer>().material;

    }

    private void Update()
    {
        // �V�~�����[�V�������s
        simulator.ExecuteStep(Time.deltaTime);

        // �V�~�����[�V�������ʂ��g�p���ăX�L�j���O�����s
        skinningSolver.Execute(simulator.GetParticleBuffer());
    }

    private void OnRenderObject()
    {
        // ���_�ʒu�̃o�b�t�@���擾���āA���_�V�F�[�_�[�ɓn���ă��b�V����`�悷��
        material.SetBuffer("_VertexBuffer", skinningSolver.GetVertexBuffer());
    }

    private void OnDrawGizmos()
    {
        if (!drawGrid) return;

        if (particles != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < particles.Length; i++)
            {
                Gizmos.DrawSphere(particles[i].pos, 0.009f);
            }
        }

        if (simulator != null)
        {
            Gizmos.color = Color.red;
            ComputeBuffer particleBuffer = simulator.GetParticleBuffer();
            PBDParticleGPU[] particleArray = new PBDParticleGPU[particleBuffer.count];
            particleBuffer.GetData(particleArray);
            for (int i = 0; i < particleArray.Length; i++)
            {
                Gizmos.DrawSphere(particleArray[i].position, 0.01f);
            }
        }
    }

    private void OnDestroy()
    {
        if (simulator != null) simulator.ReleaseBuffers();
        if (skinningSolver != null) skinningSolver.ReleaseBuffers();
    }
}
