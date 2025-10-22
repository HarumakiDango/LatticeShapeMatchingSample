using UnityEngine;

public class Particle
{
    public Vector3 pos = new Vector3(0, 0, 0);
    public Vector3 predictedPos = new Vector3(0, 0, 0);
    public Vector3 vel = new Vector3(0, 0, 0);

    // 各クラスタにおける目標位置をここに加算する
    public Vector3 goalPos = new Vector3(0, 0, 0);
    // このパーティクルが割り当てられているクラスタの数
    public int numClusters = 0;

    public Particle(Vector3 pos)
    {
        this.pos = pos;
        predictedPos = pos;
    }
}
