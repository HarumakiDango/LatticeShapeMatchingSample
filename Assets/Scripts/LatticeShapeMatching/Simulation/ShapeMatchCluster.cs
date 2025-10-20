using UnityEngine;
using Common.Mathematics.LinearAlgebra;

public class ShapeMatchCluster
{
    private GridParticle[] particles;

    private Vector3 restCenter;
    private Matrix3x3f invRestMatrix;
    private Vector3[] restPositions;

    private int numParticles;
    private float stiffness;

    private const float EPSILON = 0.000001f;

    public ShapeMatchCluster(GridParticle[] particles, float stiffness)
    {
        this.particles = new GridParticle[particles.Length];
        particles.CopyTo(this.particles, 0);

        this.stiffness = stiffness;
        numParticles = particles.Length;

        // 初期状態の重心の計算
        restCenter = new Vector3(0, 0, 0);
        for (int i = 0; i < numParticles; i++)
        {
            restCenter += this.particles[i].pos;
        }
        restCenter /= this.particles.Length;

        // 初期状態の重心からの相対位置を計算する
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


        // Debug.Log("クラスタを生成しました。パーティクル数：" + numParticles);
        for (int i = 0; i < numParticles; i++)
        {
            particles[i].numClusters++;
        }
    }

    public void ConstraintPositions()
    {
        // 現在の重心を計算する
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


        // 回転行列を求める
        Matrix3x3f R = new Matrix3x3f();
        Matrix3x3fDecomposition.PolarDecompositionStable(A, EPSILON, out R);

        // パーティクルにこのクラスタでの目標位置の加重平均を加算
        for (int i = 0; i < numParticles; i++)
        {
            Vector3 goal = center + R * restPositions[i];
            particles[i].AddGoalPos(goal);
        }

    }
    
}
