using UnityEngine;

public class LatticeShapeMatchingGPU : MonoBehaviour
{
    [Range(0.1f, 1)] public float gridWidth = 0.2f;
    [Range(0, 9)] public int maxDiv = 5;

    private Grid grid;

    PBDParticle[] particles;
    int[,,] particleIDs;
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

        // メッシュ形状に合わせたグリッドを定義する
        grid = new Grid(verticesWorld, targetMesh.triangles, maxDiv);

        // グリッドの点の上にパーティクルを配置する
        (particles, particleIDs) = ParticleGenerator.GenerateGridParticles(grid);

        
        Debug.Log("パーティクル数：" + particles.Length);

        // パーティクルにシェイプマッチングのクラスタを割り当てる
        clusters = ShapeMatchingClusterMaker.AssignGridClusters(particles, gridWidth);
        Debug.Log("クラスタ数：" + clusters.Length);


        // 生成したパーティクルとクラスタを渡してシミュレータをインスタンス化
        simulator = new PBDSimulatorGPU(particles, clusters);

        // スキニングを実行するコンポーネントをインスタンス化
        // skinning = new LatticeSkinningGPU();

    }

    private void Update()
    {
        // シミュレーション実行
        simulator.ExecuteStep(Time.deltaTime);

        // シミュレーション結果を使用してスキニングを実行
    }

    private void OnDrawGizmos()
    {
        if (particles == null) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < particles.Length; i++)
        {
            Gizmos.DrawSphere(particles[i].pos, 0.009f);
        }

        if (simulator != null)
        {
            Gizmos.color = Color.red;
            ComputeBuffer particleBuffer = simulator.GetParticleBuffer();
            PBDParticleGPU[] particleArray = new PBDParticleGPU[particleBuffer.count];
            particleBuffer.GetData(particleArray);
            for (int i = 0; i < particleArray.Length; i++)
            {
                Gizmos.DrawSphere(particleArray[i].position, 0.01f);
            }
        }
    }

    private void OnDestroy()
    {
        if (simulator != null) simulator.ReleaseBuffers();
    }
}
