using UnityEngine;

public class LatticeShapeMatchingGPU : MonoBehaviour
{
    // [Range(0.1f, 1)] public float gridWidth = 0.2f;
    [Range(0, 20)] public int maxDiv = 5;
    [Range(1.0f, 1.1f)] public float gridScale = 1.01f;
    public bool drawGrid = false;

    private Grid grid;

    PBDParticle[] particles;
    int[,,] particleIDs;
    ShapeMatchCluster[] clusters;

    PBDSimulatorGPU simulator;
    LatticeSkinningGPU skinningSolver;

    private Material material;


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
        grid = new Grid(verticesWorld, targetMesh.triangles, maxDiv, gridScale);

        // グリッドの点の上にパーティクルを配置する
        (particles, particleIDs) = ParticleGenerator.GenerateGridParticles(grid);

        
        Debug.Log("パーティクル数：" + particles.Length);

        // パーティクルにシェイプマッチングのクラスタを割り当てる
        clusters = ShapeMatchingClusterMaker.AssignGridClusters(particles, grid.cellWidth);
        Debug.Log("クラスタ数：" + clusters.Length);


        // 生成したパーティクルとクラスタを渡してシミュレータをインスタンス化
        simulator = new PBDSimulatorGPU(particles, clusters);

        // スキニングを実行するコンポーネントをインスタンス化
        skinningSolver = new LatticeSkinningGPU(particles, particleIDs, grid, verticesWorld);

        // スキニング結果を渡すマテリアルを取得しておく
        material = GetComponent<MeshRenderer>().material;

    }

    private void Update()
    {
        // シミュレーション実行
        simulator.ExecuteStep(Time.deltaTime);

        // シミュレーション結果を使用してスキニングを実行
        skinningSolver.Execute(simulator.GetParticleBuffer());
    }

    private void OnRenderObject()
    {
        // 頂点位置のバッファを取得して、頂点シェーダーに渡してメッシュを描画する
        material.SetBuffer("_VertexBuffer", skinningSolver.GetVertexBuffer());
    }

    private void OnDrawGizmos()
    {
        if (!drawGrid) return;

        if (particles != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < particles.Length; i++)
            {
                Gizmos.DrawSphere(particles[i].pos, 0.009f);
            }
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
        if (skinningSolver != null) skinningSolver.ReleaseBuffers();
    }
}
