using UnityEngine;

public class Particle
{
    public Vector3 pos = new Vector3(0, 0, 0);
    public Vector3 predictedPos = new Vector3(0, 0, 0);
    public Vector3 vel = new Vector3(0, 0, 0);

    // �e�N���X�^�ɂ�����ڕW�ʒu�������ɉ��Z����
    public Vector3 goalPos = new Vector3(0, 0, 0);
    // ���̃p�[�e�B�N�������蓖�Ă��Ă���N���X�^�̐�
    public int numClusters = 0;

    public Particle(Vector3 pos)
    {
        this.pos = pos;
        predictedPos = pos;
    }
}
