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
    public Matrix3x3f invRestMatrix;
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
    private ComputeShader compute;
    private ComputeBuffer particleBuffer;
    private ComputeBuffer shapeMatchingClusterParamBuffer;
    private ComputeBuffer shapeMatchingClusterParticleBuffer;
    private ComputeBuffer correctionReferenceBuffer;
    private ComputeBuffer correctionReferenceHelperBuffer;

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
            clusterConst.invRestMatrix = clusters[i].invRestMatrix;
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
                reference.index = myClusterIDListArray[i][j];
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
    }

    public void ExecuteStep(float dt)
    {
        UpdateSettings();

        // 外力適用

        // 推定位置計算

        // 拘束を適用して位置を修正

        // 位置の修正結果をもとに、速度と位置を更新
    }

    /// <summary>
    /// ComputeShaderにバッファと変数をバインドする
    /// </summary>
    private void UpdateSettings()
    {

    }
}
