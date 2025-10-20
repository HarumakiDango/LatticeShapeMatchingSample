using UnityEngine;
using System.Collections.Generic;

public class Simulator : MonoBehaviour
{
    // グリッドメッシュ
    public Vector3 gridRange = new Vector3(1, 1, 1);
    public float gridUnitSize = 0.2f;

    public int clusterSize = 1;

    public int numIterations = 5;
    public float stiffness = 0.9f;
    public Vector3 gravity = new Vector3(0, -9.8f, 0);
    public Vector3 colliderBoxSize = new Vector3(2, 2, 2);

    public GameObject gridParticlePrefab;
    private GridParticle[,,] gridParticles;
    private ShapeMatchCluster[,,] shapeMatchClusters;

    private int numGridX;
    private int numGridY;
    private int numGridZ;

    private void Start()
    {
        // グリッド状にパーティクルを配置する
        numGridX = Mathf.CeilToInt(gridRange.x / gridUnitSize);
        numGridY = Mathf.CeilToInt(gridRange.y / gridUnitSize);
        numGridZ = Mathf.CeilToInt(gridRange.z / gridUnitSize);

        gridParticles = new GridParticle[numGridX, numGridY, numGridZ];

        for (int k = 0; k < numGridZ; k++)
        {
            for (int j = 0; j < numGridY; j++)
            {
                for (int i = 0; i < numGridX; i++)
                {
                    gridParticles[i, j, k] = Instantiate(gridParticlePrefab).GetComponent<GridParticle>();
                    gridParticles[i, j, k].SetPosition(new Vector3(i * gridUnitSize, j * gridUnitSize, k * gridUnitSize));
                    gridParticles[i, j, k].SetVelocity(new Vector3(0, 0, 0));
                }
            }
        }

        // 各パーティクルを中心に、与えられた大きさのクラスタを設定する
        shapeMatchClusters = new ShapeMatchCluster[numGridX, numGridY, numGridZ];

        for (int k = 0; k < numGridZ; k++)
        {
            for (int j = 0; j < numGridY; j++)
            {
                for (int i = 0; i < numGridX; i++)
                {
                    List<GridParticle> clusterParticleList = new List<GridParticle>();

                    for (int z = k - clusterSize; z <= k + clusterSize; z++)
                    {
                        for (int y = j - clusterSize; y <= j + clusterSize; y++)
                        {
                            for (int x = i - clusterSize; x <= i + clusterSize; x++)
                            {
                                if (x >= 0 && y >= 0 && z >= 0 && x < numGridX && y < numGridY && z < numGridZ)
                                {
                                    clusterParticleList.Add(gridParticles[x, y, z]);
                                }
                            }
                        }
                    }

                    shapeMatchClusters[i, j, k] = new ShapeMatchCluster(clusterParticleList.ToArray(), stiffness);
                }
            }
        }

    }

    private void Update()
    {
        ExecuteSimulationStep(Time.deltaTime);

        Debug.Log(gridParticles[0, 0, 0].pos);
    }

    private void ExecuteSimulationStep(float dt)
    {
        // 外力の適用
        for (int k = 0; k < numGridZ; k++)
        {
            for (int j = 0; j < numGridY; j++)
            {
                for (int i = 0; i < numGridX; i++)
                {
                    AddExternalForce(gridParticles[i, j, k], dt);
                }
            }
        
        }

        // 現在の速度から推定位置を計算
        for (int k = 0; k < numGridZ; k++)
        {
            for (int j = 0; j < numGridY; j++)
            {
                for (int i = 0; i < numGridX; i++)
                {
                    PredictPosition(gridParticles[i, j, k], dt);
                }
            }

        }

        // 衝突判定
        for (int k = 0; k < numGridZ; k++)
        {
            for (int j = 0; j < numGridY; j++)
            {
                for (int i = 0; i < numGridX; i++)
                {
                    ApplyCollider(gridParticles[i, j, k]);
                }
            }

        }

        for (int iterationCount = 0; iterationCount < numIterations; iterationCount++)
        {
            // クラスタごとにパーティクルの目標位置を計算して加算する
            for (int k = 0; k < numGridZ; k++)
            {
                for (int j = 0; j < numGridY; j++)
                {
                    for (int i = 0; i < numGridX; i++)
                    {
                        shapeMatchClusters[i, j, k].ConstraintPositions();
                    }
                }

            }

            // パーティクルを目標位置に移動
            for (int k = 0; k < numGridZ; k++)
            {
                for (int j = 0; j < numGridY; j++)
                {
                    for (int i = 0; i < numGridX; i++)
                    {
                        gridParticles[i, j, k].SetPredictedPos(gridParticles[i, j, k].predictedPos + stiffness * (gridParticles[i, j, k].goalPos / gridParticles[i, j, k].numClusters - gridParticles[i, j, k].predictedPos));
                        // 次のループのために目標位置をゼロ初期化
                        gridParticles[i, j, k].SetGoalPos(new Vector3(0, 0, 0));
                    }
                }

            }
        }

        

        // 修正した位置をもとに速度を計算する
        for (int k = 0; k < numGridZ; k++)
        {
            for (int j = 0; j < numGridY; j++)
            {
                for (int i = 0; i < numGridX; i++)
                {
                    UpdatePosVel(gridParticles[i, j, k], dt);
                }
            }

        }
    }

    private void AddExternalForce(GridParticle particle, float dt)
    {
        Vector3 vel = particle.vel + dt * gravity;
        particle.SetVelocity(vel);
    }

    private void PredictPosition(GridParticle particle, float dt)
    {
        Vector3 predictedPos = particle.pos + dt * particle.vel;
        particle.SetPredictedPos(predictedPos);
    }

    private void ApplyCollider(GridParticle particle)
    {
        Vector3 predictedPos = new Vector3(
            Mathf.Clamp(particle.predictedPos.x, -colliderBoxSize.x, colliderBoxSize.x),
            Mathf.Clamp(particle.predictedPos.y, -colliderBoxSize.y, colliderBoxSize.y),
            Mathf.Clamp(particle.predictedPos.z, -colliderBoxSize.z, colliderBoxSize.z));
        particle.SetPredictedPos(predictedPos);
    }

    private void UpdatePosVel(GridParticle particle, float dt)
    {
        Vector3 vel = (particle.predictedPos - particle.pos) / dt;
        particle.SetVelocity(vel);

        particle.SetPosition(particle.predictedPos);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, 2 * colliderBoxSize);
    }

}
