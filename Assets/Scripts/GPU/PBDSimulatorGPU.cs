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
    public Matrix3x3f invRestMatrix;
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
    private ComputeShader compute;
    private ComputeBuffer particleBuffer;
    private ComputeBuffer shapeMatchingClusterParamBuffer;
    private ComputeBuffer shapeMatchingClusterParticleBuffer;
    private ComputeBuffer correctionReferenceBuffer;
    private ComputeBuffer correctionReferenceHelperBuffer;

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
                reference.index = myClusterIDListArray[i][j];
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
    }

    public void ExecuteStep(float dt)
    {
        UpdateSettings();

        // �O�͓K�p

        // ����ʒu�v�Z

        // �S����K�p���Ĉʒu���C��

        // �ʒu�̏C�����ʂ����ƂɁA���x�ƈʒu���X�V
    }

    /// <summary>
    /// ComputeShader�Ƀo�b�t�@�ƕϐ����o�C���h����
    /// </summary>
    private void UpdateSettings()
    {

    }
}
