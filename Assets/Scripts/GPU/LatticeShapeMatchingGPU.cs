using UnityEngine;

public class LatticeShapeMatchingGPU : MonoBehaviour
{
    [Range(0.1f, 1)] public float gridWidth = 0.2f;

    PBDParticle[] particles;
    ShapeMatchCluster[] clusters;

    PBDSimulatorGPU simulator;
    LatticeSkinningGPU skinning;

    private void Start()
    {
        // �^�[�Q�b�g�̃��b�V�����擾���āA���[���h��Ԃ̒��_���W���v�Z
        Mesh targetMesh = GetComponent<MeshFilter>().mesh;
        Vector3[] verticesWorld = new Vector3[targetMesh.vertices.Length];
        for (int i = 0; i < verticesWorld.Length; i++)
        {
            verticesWorld[i] = transform.TransformPoint(targetMesh.vertices[i]);
        }

        simulator = new PBDSimulatorGPU();

        // ���b�V���`��ɍ��킹�ăO���b�h��Ƀp�[�e�B�N����z�u����
        particles = ParticleGenerator.GenerateGridParticles(verticesWorld, targetMesh.triangles, gridWidth);
        Debug.Log("�p�[�e�B�N�����F" + particles.Length);

        // �p�[�e�B�N���ɃV�F�C�v�}�b�`���O�̃N���X�^�����蓖�Ă�
        clusters = ShapeMatchingClusterMaker.AssignGridClusters(particles, gridWidth);
        Debug.Log("�N���X�^���F" + clusters.Length);

        // ����v�Z�p�Ƀf�[�^��ϊ�����

    }

    private void Update()
    {
        // �V�~�����[�V�������s

        // �V�~�����[�V�������ʂ��g�p���ăX�L�j���O�����s
    }

    private void OnDrawGizmos()
    {
        if (particles == null) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < particles.Length; i++)
        {
            Gizmos.DrawSphere(particles[i].pos, 0.01f);
        }
    }
}
