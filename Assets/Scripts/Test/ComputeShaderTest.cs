using UnityEngine;

public class ComputeShaderTest : MonoBehaviour
{
    private ComputeShader compute;
    private ComputeBuffer matrixBuffer;
    private ComputeBuffer matrixRowBuffer;

    private Matrix4x4 matrix;

    private void Start()
    {
        matrix = new Matrix4x4();
        matrix.SetRow(0, new Vector4(0, 1, 2, 3));
        matrix.SetRow(1, new Vector4(10, 11, 12, 13));
        matrix.SetRow(2, new Vector4(20, 21, 22, 23));
        matrix.SetRow(3, new Vector4(30, 31, 32, 33));

        compute = Resources.Load< ComputeShader>("Test");

        Matrix4x4[] matrixArray = new Matrix4x4[1];
        Vector4[] matrixRowArray = new Vector4[4];

        Matrix4x4 m = new Matrix4x4();
        m.SetRow(0, new Vector4(0, 1, 2, 3));
        m.SetRow(1, new Vector4(10, 11, 12, 13));
        m.SetRow(2, new Vector4(20, 21, 22, 23));
        m.SetRow(3, new Vector4(30, 31, 32, 33));
        matrixArray[0] = m;

        matrixRowArray[0] = m.GetRow(0);
        matrixRowArray[1] = m.GetRow(1);
        matrixRowArray[2] = m.GetRow(2);
        matrixRowArray[3] = m.GetRow(3);

        Debug.Log("�v�Z�O0�s�ځF" + matrixArray[0].GetRow(0));
        Debug.Log("�v�Z�O1�s�ځF" + matrixArray[0].GetRow(1));
        Debug.Log("�v�Z�O2�s�ځF" + matrixArray[0].GetRow(2));
        Debug.Log("�v�Z�O3�s�ځF" + matrixArray[0].GetRow(3));
        Debug.Log(matrixRowArray[0]);
        Debug.Log(matrixRowArray[1]);
        Debug.Log(matrixRowArray[2]);
        Debug.Log(matrixRowArray[3]);

        matrixBuffer = ComputeHelper.CreateStructuredBuffer(matrixArray);
        matrixRowBuffer = ComputeHelper.CreateStructuredBuffer(matrixRowArray);

        ComputeHelper.SetBuffer(compute, matrixBuffer, "MatrixBuffer", 0);
        ComputeHelper.SetBuffer(compute, matrixRowBuffer, "MatrixRowBuffer", 0);
        compute.SetMatrix("mat", matrix); // �����ŗ�D�悩��s�D��ɂ��Ă邩��

        // �v�Z���s
        compute.Dispatch(0, Mathf.CeilToInt(matrixBuffer.count / 64f), 1, 1);

        // ���ʂ��擾
        Matrix4x4[] matrixArrayOut = new Matrix4x4[matrixBuffer.count];
        matrixBuffer.GetData(matrixArrayOut);

        // Matrix4x4 mult = 

        Debug.Log("�v�Z��0�s�ځF" + matrixArrayOut[0].GetRow(0));
        Debug.Log("�v�Z��1�s�ځF" + matrixArrayOut[0].GetRow(1));
        Debug.Log("�v�Z��2�s�ځF" + matrixArrayOut[0].GetRow(2));
        Debug.Log("�v�Z��3�s�ځF" + matrixArrayOut[0].GetRow(3));

        Vector4[] matrixRowArrayOut = new Vector4[matrixRowBuffer.count];
        matrixRowBuffer.GetData(matrixRowArrayOut);

        Debug.Log("matrixRowArrayOut0: " + matrixRowArrayOut[0]);
        Debug.Log("matrixRowArrayOut1: " + matrixRowArrayOut[1]);
        Debug.Log("matrixRowArrayOut2: " + matrixRowArrayOut[2]);
        Debug.Log("matrixRowArrayOut3: " + matrixRowArrayOut[3]);
    }

    private void OnDestroy()
    {
        matrixBuffer.Release();
        matrixRowBuffer.Release();
    }
}
