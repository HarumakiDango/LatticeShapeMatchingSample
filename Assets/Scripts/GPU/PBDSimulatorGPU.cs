using UnityEngine;
using System.Collections.Generic;
using Common.Mathematics.LinearAlgebra;

// �p�[�e�B�N��������{�p�����[�^
public struct PBDParticleGPU
{
    public Vector3 position;
    public Vector3 predictedPosition;
    public Vector3 velocity;
}

// �V�F�C�v�}�b�`���O�̃N���X�^���ƂɎ��p�����[�^
public struct ShapeMatchingClusterParam
{
    public Vector3 restCenter;
    public Vector3 invRestMatrixRow0;
    public Vector3 invRestMatrixRow1;
    public Vector3 invRestMatrixRow2;
    public int numClusterParticles;
    public int startParticleIndex;
}

// �V�F�C�v�}�b�`���O�̃N���X�^�����p�[�e�B�N���Ɋւ���p�����[�^
public struct ShapeMatchingClusterParticle
{
    public int particleIndex;
    public Vector3 restPosition;
    public Vector3 correction;
}

// ShapeMatchingClusterParticles�̒��́A�����̃p�[�e�B�N����correction���o�^����Ă���C���f�b�N�X
public struct CorrectionReference
{
    public int index; // ShapeMatchingClusterParticles�̉��Ԗڂɓo�^����Ă��邩
}

// �����̃p�[�e�B�N����CorrectionReference���擾���邽�߂̃p�����[�^
public struct CorrectionReferenceHelper
{
    public int startIndex; // CorrectionReference�̃o�b�t�@�̒��ŁA���g�̃p�[�e�B�N���̊J�n�ʒu
    public int numClusters; // ���̃p�[�e�B�N�����o�^����Ă���N���X�^�̐�
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
        // ComputeShader�����[�h
        compute = Resources.Load<ComputeShader>("ShapeMatchingPBD");

        // CPU�p�̃p�[�e�B�N���ƃN���X�^�̃N���X���AGPU�p�̍\���̂ɕϊ�����
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

        // �p�[�e�B�N�����Ƃ́A�o�^����Ă���N���X�^�̃C���f�b�N�X�̃��X�g���쐬����
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

                // particleIndex�Ԗڂ̃p�[�e�B�N�����Ai�Ԗڂ̃N���X�^�ɓo�^����Ă���
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

        // �쐬�����\���̂̔z�񂩂�o�b�t�@�����
        particleBuffer = ComputeHelper.CreateStructuredBuffer(particlesGPU);
        shapeMatchingClusterParamBuffer = ComputeHelper.CreateStructuredBuffer(clusterParams);
        shapeMatchingClusterParticleBuffer = ComputeHelper.CreateStructuredBuffer(clusterParticles.ToArray());
        correctionReferenceBuffer = ComputeHelper.CreateStructuredBuffer(references.ToArray());
        correctionReferenceHelperBuffer = ComputeHelper.CreateStructuredBuffer(referenceHelpers);

        // �J�[�l��ID���擾
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
        // �O�͓K�p
        compute.Dispatch(addExternalForceKernel, Mathf.CeilToInt((float)particleBuffer.count / THREAD_GROUP_SIZE_X), 1, 1);

        // ����ʒu�v�Z
        compute.Dispatch(predictPositionKernel, Mathf.CeilToInt((float)particleBuffer.count / THREAD_GROUP_SIZE_X), 1, 1);

        // �S����K�p���Ĉʒu���C��
        for (int i = 0; i < numIterations; i++)
        {
            compute.Dispatch(shapeMatchingSolverKernel, Mathf.CeilToInt((float)shapeMatchingClusterParamBuffer.count / THREAD_GROUP_SIZE_X), 1, 1);
            compute.Dispatch(averageGoalPosKernel, Mathf.CeilToInt((float)particleBuffer.count / THREAD_GROUP_SIZE_X), 1, 1);
        }



        // �ʒu�̏C�����ʂ����ƂɁA���x�ƈʒu���X�V
        compute.Dispatch(updatePosAndVelKernel, Mathf.CeilToInt((float)particleBuffer.count / THREAD_GROUP_SIZE_X), 1, 1);
    }

    /// <summary>
    /// ComputeShader�Ƀo�b�t�@�ƕϐ����o�C���h����
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
    /// �O������p�[�e�B�N���̃o�b�t�@���擾���邽�߂̃��\�b�h
    /// </summary>
    public ComputeBuffer GetParticleBuffer()
    {
        return particleBuffer;
    }

    /// <summary>
    /// �������̉��
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
