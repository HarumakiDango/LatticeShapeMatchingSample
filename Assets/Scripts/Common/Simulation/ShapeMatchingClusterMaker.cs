using UnityEngine;
using System.Collections.Generic;

public static class ShapeMatchingClusterMaker
{
    /// <summary>
    /// 各頂点を中心とした箱型のクラスタを割り当てる
    /// </summary>
    public static ShapeMatchCluster[] AssignGridClusters(PBDParticle[] particles, float boxSizeHalf)
    {
        // 誤差で取得できない場合があるので少しだけ余裕を持たせておく
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

            // シェイプマッチングの計算を安定させるため、パーティクル数が少なすぎる場合はクラスタを割り当てないようにする
            if (clusterParticles.Count <= 4) continue;

            Debug.Log("このクラスタに登録されたパーティクルの数：" + clusterParticles.Count);

            ShapeMatchCluster cluster = new ShapeMatchCluster(clusterParticles.ToArray(), clusterParticleIndices.ToArray());
            clusters.Add(cluster);
            
        }

        return clusters.ToArray();
    }
}
