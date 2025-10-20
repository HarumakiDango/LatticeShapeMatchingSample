using UnityEngine;
using Common.Mathematics.LinearAlgebra;

public class ShapeMatchCluster
{
    private Particle[] particles;

    private Vector3 restCenter;
    private Matrix3x3f invRestMatrix;
    private Vector3[] restPositions;

    private int numParticles;

    private const float EPSILON = 1e-6f;

    public ShapeMatchCluster(Particle[] particles)
    {
        this.particles = new Particle[particles.Length];
        particles.CopyTo(this.particles, 0);

        numParticles = particles.Length;

        // ������Ԃ̏d�S�̌v�Z
        restCenter = new Vector3(0, 0, 0);
        for (int i = 0; i < numParticles; i++)
        {
            restCenter += this.particles[i].pos;
        }
        restCenter /= numParticles;

        // ������Ԃ̏d�S����̑��Έʒu���v�Z����
        Matrix3x3f A = new Matrix3x3f();
        restPositions = new Vector3[numParticles];

        for (int i = 0; i < numParticles; i++)
        {
            Vector3 q = this.particles[i].pos - restCenter;
            restPositions[i] = q;

            A[0, 0] += q.x * q.x;
            A[0, 1] += q.x * q.y;
            A[0, 2] += q.x * q.z;

            A[1, 0] += q.y * q.x;
            A[1, 1] += q.y * q.y;
            A[1, 2] += q.y * q.z;

            A[2, 0] += q.z * q.x;
            A[2, 1] += q.z * q.y;
            A[2, 2] += q.z * q.z;
        }

        invRestMatrix = A.Inverse;


        // Debug.Log("�N���X�^�𐶐����܂����B�p�[�e�B�N�����F" + numParticles);
        for (int i = 0; i < numParticles; i++)
        {
            particles[i].numClusters++;
        }

        Debug.Log("�N���X�^�����p�[�e�B�N���̐��F" + numParticles);
    }

    public void ConstrainPositions()
    {
        // ���݂̏d�S���v�Z����
        Vector3 center = new Vector3(0, 0, 0);

        for (int i = 0; i < numParticles; i++)
        {
            center += particles[i].predictedPos;
        }
        center /= particles.Length;

        // Moment Matrix
        Matrix3x3f A = new Matrix3x3f();

        for (int i = 0; i < numParticles; i++)
        {
            Vector3 q = restPositions[i];
            Vector3 p = particles[i].predictedPos - center;

            A[0, 0] += p.x * q.x;
            A[0, 1] += p.x * q.y;
            A[0, 2] += p.x * q.z;

            A[1, 0] += p.y * q.x;
            A[1, 1] += p.y * q.y;
            A[1, 2] += p.y * q.z;

            A[2, 0] += p.z * q.x;
            A[2, 1] += p.z * q.y;
            A[2, 2] += p.z * q.z;
        }

        A = A * invRestMatrix;


        // ��]�s������߂�
        Matrix3x3f R = new Matrix3x3f();
        Matrix3x3fDecomposition.PolarDecompositionStable(A, EPSILON, out R);

        // �p�[�e�B�N���ɂ��̃N���X�^�ł̖ڕW�ʒu�����Z
        for (int i = 0; i < numParticles; i++)
        {
            Vector3 goal = center + R * restPositions[i];
            particles[i].goalPos += goal - particles[i].predictedPos;
        }

    }
    
}
