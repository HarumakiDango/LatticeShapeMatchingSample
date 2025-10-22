using UnityEngine;
using System.Collections.Generic;

public class Simulator : MonoBehaviour
{
    // �p�����[�^
    [Range(1, 20)] public int numIterations = 5;
    [Range(1, 20)] public int numSubsteps = 5;
    [Range(0, 1)] public float stiffness = 0.9f;
    [Range(0, 1)] public float dampCoeff = 0.98f;
    public Vector3 gravity = new Vector3(0, -9.8f, 0);
    public Vector3 colliderBoxSize = new Vector3(2, 2, 2);

    private Particle[] particles;
    private int numParticles;

    private ShapeMatchCluster[] shapeMatchClusters;

    bool isInitialized = false;

    private void Start()
    {
        

    }

    public void Initialize(Particle[] particles, ShapeMatchCluster[] clusters)
    {
        this.particles = new Particle[particles.Length];
        particles.CopyTo(this.particles, 0);

        shapeMatchClusters = new ShapeMatchCluster[clusters.Length];
        clusters.CopyTo(shapeMatchClusters, 0);

        numParticles = particles.Length; Debug.Log("numParticles: " + numParticles);

        
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;

        ExecuteSimulationStep(Time.deltaTime);

        // Debug.Log(particles[0].pos);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3(0, 0, 0), 2 * colliderBoxSize);

        Gizmos.color = Color.green;
        for (int i = 0; i < numParticles; i++)
        {
            Gizmos.DrawSphere(particles[i].pos, 0.02f);
        }
    }

    private void ExecuteSimulationStep(float dt)
    {
        float subDt = dt / numSubsteps;

        for (int i = 0; i < numSubsteps; i++)
        {
            SubStep(subDt);
        }

    }

    private void SubStep(float dt)
    {
        for (int i = 0; i < numParticles; i++)
        {
            AddExternalForce(particles[i], dt);

        }


        for (int i = 0; i < numParticles; i++)
        {
            PredictPosition(particles[i], dt);

        }

        for (int i = 0; i < numParticles; i++)
        {
            ApplyCollider(particles[i]);

        }

        float k = 1 - Mathf.Pow(1 - Mathf.Clamp01(stiffness), 1f / (numIterations * numSubsteps));
        for (int iterationCount = 0; iterationCount < numIterations; iterationCount++)
        {
            // �N���X�^���ƂɃp�[�e�B�N���̖ڕW�ʒu���v�Z
            for (int i = 0; i < shapeMatchClusters.Length; i++)
            {
                shapeMatchClusters[i].ConstrainPositions();
            }

            // �ڕW�ʒu�𕽋ς��Đ���ʒu���C��
            for (int i = 0; i < numParticles; i++)
            {
                Vector3 goal = particles[i].goalPos / particles[i].numClusters;
                particles[i].predictedPos += +k * goal;

                // ���̃��[�v�̂��߂ɖڕW�ʒu���[��������
                particles[i].goalPos = Vector3.zero;
            }
        }

        for (int i = 0; i < numParticles; i++)
        {
            UpdatePosVel(particles[i], dt);
        }
    }

    /// <summary>
    /// �O�͂̓K�p
    /// </summary>
    private void AddExternalForce(Particle particle, float dt)
    {
        particle.vel = particle.vel + dt * gravity;
    }

    /// <summary>
    /// ���݂̑��x���琄��ʒu���v�Z
    /// </summary>
    private void PredictPosition(Particle particle, float dt)
    {
        particle.predictedPos = particle.pos + dt * dampCoeff * particle.vel;
    }

    /// <summary>
    /// �Փ˔���
    /// </summary>
    private void ApplyCollider(Particle particle)
    {
        particle.predictedPos = new Vector3(
            Mathf.Clamp(particle.predictedPos.x, -colliderBoxSize.x, colliderBoxSize.x),
            Mathf.Clamp(particle.predictedPos.y, -colliderBoxSize.y, colliderBoxSize.y),
            Mathf.Clamp(particle.predictedPos.z, -colliderBoxSize.z, colliderBoxSize.z));
    }

    /// <summary>
    /// �C�������ʒu�����Ƃɑ��x���v�Z����
    /// </summary>
    private void UpdatePosVel(Particle particle, float dt)
    {
        particle.vel = (particle.predictedPos - particle.pos) / dt;
        particle.pos = particle.predictedPos;
    }
}
