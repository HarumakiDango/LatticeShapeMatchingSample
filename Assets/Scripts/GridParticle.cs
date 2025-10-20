using UnityEngine;

public class GridParticle : MonoBehaviour
{
    // �V�~�����[�V�����p�p�����[�^
    public Vector3 pos { get; private set; }
    public Vector3 predictedPos { get; private set; }
    public Vector3 vel { get; private set; }

    public Vector3 goalPos { get; private set; }

    // ���̃p�[�e�B�N�������蓖�Ă��Ă���N���X�^�̐�
    public int numClusters;

    // Gizmos
    public float radius = 0.01f;
    public Color myColor = Color.green;

    private void Start()
    {
        Gizmos.color = myColor;
    }

    public void SetPosition(Vector3 pos)
    {
        this.pos = pos;
    }

    public void SetVelocity(Vector3 vel)
    {
        this.vel = vel;
    }

    public void SetPredictedPos(Vector3 predictedPos)
    {
        this.predictedPos = predictedPos;
    }
    public void AddPredictedPos(Vector3 v)
    {
        predictedPos += v;
    }

    public void SetGoalPos(Vector3 g)
    {
        goalPos = g;
    }
    public void AddGoalPos(Vector3 v)
    {
        goalPos += v;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(pos, radius);
    }
}
