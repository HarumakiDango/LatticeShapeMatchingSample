using UnityEngine;

public class LatticeShapeMatchingGPU : MonoBehaviour
{
    [Range(0.1f, 1)] public float gridWidth = 0.2f;

    PBDParticle[] particles;
    ShapeMatchCluster[] clusters;

    PBDSimulatorGPU simulator;
    LatticeSkinningGPU skinning;

    private void Start()
    {
        // ターゲットのメッシュを取得して、ワールド空間の頂点座標を計算
        Mesh targetMesh = GetComponent<MeshFilter>().mesh;
        Vector3[] verticesWorld = new Vector3[targetMesh.vertices.Length];
        for (int i = 0; i < verticesWorld.Length; i++)
        {
            verticesWorld[i] = transform.TransformPoint(targetMesh.vertices[i]);
        }

        simulator = new PBDSimulatorGPU();

        // メッシュ形状に合わせてグリッド状にパーティクルを配置する
        particles = ParticleGenerator.GenerateGridParticles(verticesWorld, targetMesh.triangles, gridWidth);
        Debug.Log("パーティクル数：" + particles.Length);

        // パーティクルにシェイプマッチングのクラスタを割り当てる
        clusters = ShapeMatchingClusterMaker.AssignGridClusters(particles, gridWidth);
        Debug.Log("クラスタ数：" + clusters.Length);

        // 並列計算用にデータを変換する

    }

    private void Update()
    {
        // シミュレーション実行

        // シミュレーション結果を使用してスキニングを実行
    }

    private void OnDrawGizmos()
    {
        if (particles == null) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < particles.Length; i++)
        {
            Gizmos.DrawSphere(particles[i].pos, 0.01f);
        }
    }
}
