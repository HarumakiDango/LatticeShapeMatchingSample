using UnityEngine;
using System.Collections.Generic;

public static class ShapeMatchingClusterMaker
{
    /// <summary>
    /// �e���_�𒆐S�Ƃ������^�̃N���X�^�����蓖�Ă�
    /// </summary>
    public static ShapeMatchCluster[] AssignGridClusters(PBDParticle[] particles, float boxSizeHalf)
    {
        // �덷�Ŏ擾�ł��Ȃ��ꍇ������̂ŏ��������]�T���������Ă���
        boxSizeHalf *= 1.01f;

        List<ShapeMatchCluster> clusters = new List<ShapeMatchCluster>();

        for (int i = 0; i < particles.Length; i++)
        {
            List<PBDParticle> clusterParticles = new List<PBDParticle>();
            List<int> clusterParticleIndices = new List<int>();

            for (int j = 0; j < particles.Length; j++)
            {
                float distX = Mathf.Abs(particles[j].pos.x - particles[i].pos.x);
                float distY = Mathf.Abs(particles[j].pos.y - particles[i].pos.y);
                float distZ = Mathf.Abs(particles[j].pos.z - particles[i].pos.z);

                if (distX <= boxSizeHalf && distY <= boxSizeHalf && distZ <= boxSizeHalf)
                {
                    clusterParticles.Add(particles[j]);
                    clusterParticleIndices.Add(j);
                }
            }

            // �V�F�C�v�}�b�`���O�̌v�Z�����肳���邽�߁A�p�[�e�B�N���������Ȃ�����ꍇ�̓N���X�^�����蓖�ĂȂ��悤�ɂ���
            if (clusterParticles.Count <= 4) continue;

            Debug.Log("���̃N���X�^�ɓo�^���ꂽ�p�[�e�B�N���̐��F" + clusterParticles.Count);

            ShapeMatchCluster cluster = new ShapeMatchCluster(clusterParticles.ToArray(), clusterParticleIndices.ToArray());
            clusters.Add(cluster);
            
        }

        return clusters.ToArray();
    }
}
