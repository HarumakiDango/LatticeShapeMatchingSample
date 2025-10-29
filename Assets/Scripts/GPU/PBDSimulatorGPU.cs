using UnityEngine;
using System.Collections.Generic;
using Common.Mathematics.LinearAlgebra;

// パーティクルが持つ基本パラメータ
public struct PBDParticleGPU
{
    public Vector3 position;
    public Vector3 predictedPosition;
    public Vector3 velocity;
}

// シェイプマッチングのクラスタごとに持つパラメータ
public struct ShapeMatchingClusterParam
{
    public Vector3 restCenter;
    public Vector3 invRestMatrixRow0;
    public Vector3 invRestMatrixRow1;
    public Vector3 invRestMatrixRow2;
    public int numClusterParticles;
    public int startParticleIndex;
}

// シェイプマッチングのクラスタが持つパーティクルに関するパラメータ
public struct ShapeMatchingClusterParticle
{
    public int particleIndex;
    public Vector3 restPosition;
    public Vector3 correction;
}

// ShapeMatchingClusterParticlesの中の、自分のパーティクルのcorrectionが登録されているインデックス
public struct CorrectionReference
{
    public int index; // ShapeMatchingClusterParticlesの何番目に登録されているか
}

// 自分のパーティクルのCorrectionReferenceを取得するためのパラメータ
public struct CorrectionReferenceHelper
{
    public int startIndex; // CorrectionReferenceのバッファの中で、自身のパーティクルの開始位置
    public int numClusters; // このパーティクルが登録されているクラスタの数
}

public class PBDSimulatorGPU
{
    private const int THREAD_GROUP_SIZE_X = 64;

    private ComputeShader compute;
    private ComputeBuffer particleBuffer;
    private ComputeBuffer shapeMatchingClusterParamBuffer;
    private ComputeBuffer shapeMatchingClusterParticleBuffer;
    private ComputeBuffer correctionReferenceBuffer;
    private ComputeBuffer correctionReferenceHelperBuffer;

    private int addExternalForceKernel;
    private int predictPositionKernel;
    private int shapeMatchingSolverKernel;
    private int averageGoalPosKernel;
    private int updatePosAndVelKernel;

    private int numIterations = 2;
    private int numSubsteps = 3;
    private float dampCoeff = 0.98f;
    private Vector3 gravity = new Vector3(0, -9.8f, 0);
    private float stiffness = 0.99f;
    private float colliderSize = 1;

    public PBDSimulatorGPU(PBDParticle[] particles, ShapeMatchCluster[] clusters)
    {
        // ComputeShaderをロード
        compute = Resources.Load<ComputeShader>("ShapeMatchingPBD");

        // CPU用のパーティクルとクラスタのクラスを、GPU用の構造体に変換する
        PBDParticleGPU[] particlesGPU = new PBDParticleGPU[particles.Length];
        for (int i = 0; i < particles.Length; i++)
        {
            PBDParticleGPU p = new PBDParticleGPU();
            p.position = particles[i].pos;
            p.predictedPosition = particles[i].predictedPos;
            p.velocity = particles[i].vel;
            particlesGPU[i] = p;
        }

        ShapeMatchingClusterParam[] clusterParams = new ShapeMatchingClusterParam[clusters.Length];
        List<ShapeMatchingClusterParticle> clusterParticles = new List<ShapeMatchingClusterParticle>();
        int clusterParticleCount = 0;

        // パーティクルごとの、登録されているクラスタのインデックスのリストを作成する
        List<int>[] myClusterIDListArray = new List<int>[particlesGPU.Length];
        for (int i = 0; i < myClusterIDListArray.Length; i++)
        {
            myClusterIDListArray[i] = new List<int>();
        }

        for (int i = 0; i < clusters.Length; i++)
        {
            ShapeMatchingClusterParam clusterConst = new ShapeMatchingClusterParam();
            clusterConst.restCenter = clusters[i].restCenter;
            clusterConst.invRestMatrixRow0 = new Vector3(clusters[i].invRestMatrix.GetRow(0).x, clusters[i].invRestMatrix.GetRow(0).y, clusters[i].invRestMatrix.GetRow(0).z);
            clusterConst.invRestMatrixRow1 = new Vector3(clusters[i].invRestMatrix.GetRow(1).x, clusters[i].invRestMatrix.GetRow(1).y, clusters[i].invRestMatrix.GetRow(1).z);
            clusterConst.invRestMatrixRow2 = new Vector3(clusters[i].invRestMatrix.GetRow(2).x, clusters[i].invRestMatrix.GetRow(2).y, clusters[i].invRestMatrix.GetRow(2).z);
            clusterConst.numClusterParticles = clusters[i].numParticles;
            clusterConst.startParticleIndex = clusterParticleCount;
            clusterParams[i] = clusterConst;

            for (int j = 0; j < clusters[i].numParticles; j++)
            {
                ShapeMatchingClusterParticle clusterParticle = new ShapeMatchingClusterParticle();
                clusterParticle.particleIndex = clusters[i].particleIndices[j];
                clusterParticle.restPosition = clusters[i].restPositions[j];
                clusterParticle.correction = Vector3.zero;
                clusterParticles.Add(clusterParticle);

                // particleIndex番目のパーティクルが、i番目のクラスタに登録されている
                myClusterIDListArray[clusterParticle.particleIndex].Add(i);

                clusterParticleCount++;
            }
            
        }

        CorrectionReferenceHelper[] referenceHelpers = new CorrectionReferenceHelper[particlesGPU.Length];
        List<CorrectionReference> references = new List<CorrectionReference>();
        int referenceCount = 0;
        for (int i = 0; i < particlesGPU.Length; i++)
        {
            CorrectionReferenceHelper referenceHelper = new CorrectionReferenceHelper();
            referenceHelper.startIndex = referenceCount;
            referenceHelper.numClusters = myClusterIDListArray[i].Count;
            referenceHelpers[i] = referenceHelper;

            for (int j = 0; j < myClusterIDListArray[i].Count; j++)
            {
                CorrectionReference reference = new CorrectionReference();
                reference.index = myClusterIDListArray[i][j]; // ij
                references.Add(reference);

                referenceCount++;
            }
        }

        // 作成した構造体の配列からバッファを作る
        particleBuffer = ComputeHelper.CreateStructuredBuffer(particlesGPU);
        shapeMatchingClusterParamBuffer = ComputeHelper.CreateStructuredBuffer(clusterParams);
        shapeMatchingClusterParticleBuffer = ComputeHelper.CreateStructuredBuffer(clusterParticles.ToArray());
        correctionReferenceBuffer = ComputeHelper.CreateStructuredBuffer(references.ToArray());
        correctionReferenceHelperBuffer = ComputeHelper.CreateStructuredBuffer(referenceHelpers);

        // カーネルIDを取得
        addExternalForceKernel = compute.FindKernel("AddExternalForce");
        predictPositionKernel = compute.FindKernel("PredictPosition");
        shapeMatchingSolverKernel = compute.FindKernel("ShapeMatchingSolver");
        averageGoalPosKernel = compute.FindKernel("AverageGoalPos");
        updatePosAndVelKernel = compute.FindKernel("UpdatePosAndVel");
    }

    public void ExecuteStep(float dt)
    {
        float subDt = dt / numSubsteps;

        UpdateSettings(subDt);

        for (int i = 0; i < numSubsteps; i++)
        {
            Substep(subDt);
        }
        
    }

    private void Substep(float subDt)
    {
        // 外力適用
        compute.Dispatch(addExternalForceKernel, Mathf.CeilToInt((float)particleBuffer.count / THREAD_GROUP_SIZE_X), 1, 1);

        // 推定位置計算
        compute.Dispatch(predictPositionKernel, Mathf.CeilToInt((float)particleBuffer.count / THREAD_GROUP_SIZE_X), 1, 1);

        // 拘束を適用して位置を修正
        for (int i = 0; i < numIterations; i++)
        {
            compute.Dispatch(shapeMatchingSolverKernel, Mathf.CeilToInt((float)shapeMatchingClusterParamBuffer.count / THREAD_GROUP_SIZE_X), 1, 1);
            compute.Dispatch(averageGoalPosKernel, Mathf.CeilToInt((float)particleBuffer.count / THREAD_GROUP_SIZE_X), 1, 1);
        }



        // 位置の修正結果をもとに、速度と位置を更新
        compute.Dispatch(updatePosAndVelKernel, Mathf.CeilToInt((float)particleBuffer.count / THREAD_GROUP_SIZE_X), 1, 1);
    }

    /// <summary>
    /// ComputeShaderにバッファと変数をバインドする
    /// </summary>
    private void UpdateSettings(float dt)
    {
        ComputeHelper.SetBuffer(compute, particleBuffer, "Particles", addExternalForceKernel, predictPositionKernel, shapeMatchingSolverKernel, averageGoalPosKernel, updatePosAndVelKernel);
        ComputeHelper.SetBuffer(compute, shapeMatchingClusterParamBuffer, "ClusterParams", shapeMatchingSolverKernel);
        ComputeHelper.SetBuffer(compute, shapeMatchingClusterParticleBuffer, "ClusterParticles", shapeMatchingSolverKernel, averageGoalPosKernel);
        ComputeHelper.SetBuffer(compute, correctionReferenceBuffer, "References", averageGoalPosKernel);
        ComputeHelper.SetBuffer(compute, correctionReferenceHelperBuffer, "ReferenceHelpers", averageGoalPosKernel);

        compute.SetInt("numParticles", particleBuffer.count);
        compute.SetInt("numClusters", shapeMatchingClusterParamBuffer.count);
        compute.SetFloat("dt", dt);
        compute.SetFloat("dampCoeff", dampCoeff);
        compute.SetVector("gravity", gravity);
        float k = 1 - Mathf.Pow(1 - Mathf.Clamp01(stiffness), 1f / (numIterations * numSubsteps));
        compute.SetFloat("k", k);
        compute.SetFloat("colliderSize", colliderSize);
    }

    /// <summary>
    /// 外部からパーティクルのバッファを取得するためのメソッド
    /// </summary>
    public ComputeBuffer GetParticleBuffer()
    {
        return particleBuffer;
    }

    /// <summary>
    /// メモリの解放
    /// </summary>
    public void ReleaseBuffers()
    {
        particleBuffer.Release();
        shapeMatchingClusterParamBuffer.Release();
        shapeMatchingClusterParticleBuffer.Release();
        correctionReferenceBuffer.Release();
        correctionReferenceHelperBuffer.Release();
    }
}
