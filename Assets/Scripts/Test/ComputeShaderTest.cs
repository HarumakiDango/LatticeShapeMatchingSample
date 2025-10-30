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

        Debug.Log("計算前0行目：" + matrixArray[0].GetRow(0));
        Debug.Log("計算前1行目：" + matrixArray[0].GetRow(1));
        Debug.Log("計算前2行目：" + matrixArray[0].GetRow(2));
        Debug.Log("計算前3行目：" + matrixArray[0].GetRow(3));
        Debug.Log(matrixRowArray[0]);
        Debug.Log(matrixRowArray[1]);
        Debug.Log(matrixRowArray[2]);
        Debug.Log(matrixRowArray[3]);

        matrixBuffer = ComputeHelper.CreateStructuredBuffer(matrixArray);
        matrixRowBuffer = ComputeHelper.CreateStructuredBuffer(matrixRowArray);

        ComputeHelper.SetBuffer(compute, matrixBuffer, "MatrixBuffer", 0);
        ComputeHelper.SetBuffer(compute, matrixRowBuffer, "MatrixRowBuffer", 0);
        compute.SetMatrix("mat", matrix); // ここで列優先から行優先にしてるかも

        // 計算実行
        compute.Dispatch(0, Mathf.CeilToInt(matrixBuffer.count / 64f), 1, 1);

        // 結果を取得
        Matrix4x4[] matrixArrayOut = new Matrix4x4[matrixBuffer.count];
        matrixBuffer.GetData(matrixArrayOut);

        // Matrix4x4 mult = 

        Debug.Log("計算後0行目：" + matrixArrayOut[0].GetRow(0));
        Debug.Log("計算後1行目：" + matrixArrayOut[0].GetRow(1));
        Debug.Log("計算後2行目：" + matrixArrayOut[0].GetRow(2));
        Debug.Log("計算後3行目：" + matrixArrayOut[0].GetRow(3));

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
