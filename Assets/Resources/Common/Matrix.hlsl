void Matrix3x3Set(inout float3x3 m, int rowIndex, int columnIndex, float value)
{
    if (rowIndex == 0)
    {
        if (columnIndex == 0) { m._m00 = value; }
        if (columnIndex == 1) { m._m01 = value; }
        if (columnIndex == 2) { m._m02 = value; }
    }
    if (rowIndex == 1)
    {
        if (columnIndex == 0) { m._m10 = value; }
        if (columnIndex == 1) { m._m11 = value; }
        if (columnIndex == 2) { m._m12 = value; }
    }
    if (rowIndex == 2)
    {
        if (columnIndex == 0) { m._m20 = value; }
        if (columnIndex == 1) { m._m21 = value; }
        if (columnIndex == 2) { m._m22 = value; }
    }
}

void Matrix3x3SetColumn(inout float3x3 m, int columnIndex, float3 column)
{
    m[columnIndex] = column;
}

void Matrix3x3SetRow(inout float3x3 m, int rowIndex, float3 row)
{
    if (rowIndex == 0)
    {
        m._m00 = row.x;
        m._m01 = row.y;
        m._m02 = row.z;
    }
    if (rowIndex == 1)
    {
        m._m10 = row.x;
        m._m11 = row.y;
        m._m12 = row.z;
    }
    if (rowIndex == 2)
    {
        m._m20 = row.x;
        m._m21 = row.y;
        m._m22 = row.z;
    }
}

float3 Matrix3x3GetColumn(float3x3 m, int columnIndex)
{
    return m[columnIndex];
}

float3 Matrix3x3GetRow(float3x3 m, int rowIndex)
{
    return float3(m[0][rowIndex], m[1][rowIndex], m[2][rowIndex]);
}

float3x3 Matrix3x3Inverse(float3x3 m)
{
    float a = m[0][0], b = m[0][1], c = m[0][2];
    float d = m[1][0], e = m[1][1], f = m[1][2];
    float g = m[2][0], h = m[2][1], i = m[2][2];

    float A =  e * i - f * h;
    float B = -(d * i - f * g);
    float C =  d * h - e * g;
    float D = -(b * i - c * h);
    float E =  a * i - c * g;
    float F = -(a * h - b * g);
    float G =  b * f - c * e;
    float H = -(a * f - c * d);
    float I =  a * e - b * d;

    float det = a * A + b * B + c * C;

    if (abs(det) < 1e-6)
    {
        // detが小さすぎる場合は単位行列を返す（またはエラー処理）
        return float3x3(1,0,0, 0,1,0, 0,0,1);
    }

    float invDet = 1.0 / det;

    return float3x3(
        A * invDet, D * invDet, G * invDet,
        B * invDet, E * invDet, H * invDet,
        C * invDet, F * invDet, I * invDet
    );
}

float Matrix3x3Determinant(float3x3 m)
{
    return
        m[0][0] * (m[1][1] * m[2][2] - m[1][2] * m[2][1]) -
        m[0][1] * (m[1][0] * m[2][2] - m[1][2] * m[2][0]) +
        m[0][2] * (m[1][0] * m[2][1] - m[1][1] * m[2][0]);
}

float Matrix3x3OneNorm(float3x3 m)
{
    // 各列の絶対値の合計を計算
    float sum0 = abs(m[0][0]) + abs(m[1][0]) + abs(m[2][0]); // 列0
    float sum1 = abs(m[0][1]) + abs(m[1][1]) + abs(m[2][1]); // 列1
    float sum2 = abs(m[0][2]) + abs(m[1][2]) + abs(m[2][2]); // 列2

    // 最大の列合計を返す
    return max(sum0, max(sum1, sum2));
}

float Matrix3x3InfNorm(float3x3 m)
{
    // 各行の絶対値の合計を計算
    float sum0 = abs(m[0][0]) + abs(m[0][1]) + abs(m[0][2]); // 行0
    float sum1 = abs(m[1][0]) + abs(m[1][1]) + abs(m[1][2]); // 行1
    float sum2 = abs(m[2][0]) + abs(m[2][1]) + abs(m[2][2]); // 行2

    // 最大の行合計を返す
    return max(sum0, max(sum1, sum2));
}

void Matrix3x3PolarDecompositionStable(float3x3 M, float tolerance, out float3x3 R, inout bool isNaN)
{
    float3x3 Mt = transpose(M);
    float Mone = Matrix3x3OneNorm(M);
    float Minf = Matrix3x3InfNorm(M);
    float Eone;
    float3x3 MadjTt = float3x3(0, 0, 0, 0, 0, 0, 0, 0, 0);
    float3x3 Et = float3x3(0, 0, 0, 0, 0, 0, 0, 0, 0);

    const float eps = 1e-12;

    // [loop]
    int loopCount = 0;
    while (true)
    {
        Matrix3x3SetRow(MadjTt, 0, cross(Matrix3x3GetRow(Mt, 1), Matrix3x3GetRow(Mt, 2)));
        Matrix3x3SetRow(MadjTt, 1, cross(Matrix3x3GetRow(Mt, 2), Matrix3x3GetRow(Mt, 0)));
        Matrix3x3SetRow(MadjTt, 2, cross(Matrix3x3GetRow(Mt, 0), Matrix3x3GetRow(Mt, 1)));

        float det = Mt[0][0] * MadjTt[0][0] + Mt[0][1] * MadjTt[0][1] + Mt[0][2] * MadjTt[0][2];

        if (abs(det) < eps)
        {
            
            
            int index = 1000000;
            for (int i = 0; i < 3; i++)
            {
                float3 row = Matrix3x3GetRow(MadjTt, i);
                float len = dot(row, row); // SqrMagnitude

                if (len > eps)
                {
                    index = i;
                    break;
                }
            }

            if (index == 1000000)
            {
                R = float3x3(1,0,0, 0,1,0, 0,0,1);
                return;
            }
            else
            {
                Matrix3x3SetRow(Mt, index, cross(Matrix3x3GetRow(Mt, (index + 1) % 3), Matrix3x3GetRow(Mt, (index + 2) % 3)));
                Matrix3x3SetRow(MadjTt, (index + 1) % 3, cross(Matrix3x3GetRow(Mt, (index + 2) % 3), Matrix3x3GetRow(Mt, index)));
                Matrix3x3SetRow(MadjTt, (index + 2) % 3, cross(Matrix3x3GetRow(Mt, index), Matrix3x3GetRow(Mt, (index + 1) % 3)));

                float3x3 M2 = transpose(Mt);
                Mone = Matrix3x3OneNorm(M2);
                Minf = Matrix3x3InfNorm(M2);

                det = Mt[0][0] * MadjTt[0][0] + Mt[0][1] * MadjTt[0][1] + Mt[0][2] * MadjTt[0][2];
            }
        }

        float MadjTone = Matrix3x3OneNorm(MadjTt);
        float MadjTinf = Matrix3x3InfNorm(MadjTt);

        if (Mone * Minf < 1e-6 || abs(det) < 1e-6) break;

        float gamma = sqrt(sqrt((MadjTone * MadjTinf) / (Mone * Minf)) / abs(det));
        float g1 = gamma * 0.5;
        float g2 = 0.5 / (gamma * det);

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Et[i][j] = Mt[i][j];
                Mt[i][j] = g1 * Mt[i][j] + g2 * MadjTt[i][j];
                Et[i][j] -= Mt[i][j];
            }
        }

        Eone = Matrix3x3OneNorm(Et);
        Mone = Matrix3x3OneNorm(Mt);
        Minf = Matrix3x3InfNorm(Mt);

        if (Eone <= Mone * tolerance)
            break;

        loopCount++;
        if (loopCount > 10000) break;
    }

    R = transpose(Mt);
}

// ヤコビ法による固有値分解
static const float3x3 P = float3x3(1,0,0, 0,1,0, 0,0,1);
static const float eps = 1e-8;
static const int maxIter = 20;
void Matrix3x3JacobiEigenDecomposition(inout float3x3 A, out float3x3 eigenVectors)
{
    eigenVectors = P; // 固有ベクトルを初期化

    for (int iter = 0; iter < maxIter; iter++)
    {
        // 非対角成分の中から絶対値が最大の成分を探す
        int p = 0;
        int q = 1;
        float maxValue = abs(A[0][1]);
        if (abs(A[0][2]) > maxValue) { maxValue = abs(A[0][2]); p = 0; q = 2; }
        if (abs(A[1][2]) > maxValue) { maxValue = abs(A[1][2]); p = 1; q = 2; }

        if (maxValue < eps) break; // 全ての非対角成分が十分小さくなったら終了する

        // [p, q]成分を0にする回転パラメータを求める
        float app = A[p][p];
        float aqq = A[q][q];
        float apq = A[p][q];

        float alpha = (app - aqq) * 0.5;
        float beta = -apq;
        float gamma = abs(alpha) / sqrt(alpha * alpha + beta * beta);

        float c = sqrt((1.0 + gamma) * 0.5);
        float s = sqrt((1.0 - gamma) * 0.5);
        if (alpha * beta < 0) s = -s;

        // 求めた回転を適用
        for (int i = 0; i < 3; i++)
        {
            float tmp_pi = c * A[p][i] - s * A[q][i];
            float tmp_qi = s * A[p][i] + c * A[q][i];
            Matrix3x3Set(A, p, i, tmp_pi);
            Matrix3x3Set(A, q, i, tmp_qi);
        }
        for (int i = 0; i < 3; i++)
        {
            Matrix3x3Set(A, i, p, A[p][i]);
            Matrix3x3Set(A, i, q, A[q][i]);
        }

        // 対角要素の更新
        Matrix3x3Set(A, p, p, c * c * app + s * s * aqq - 2.0 * s * c * apq);
        Matrix3x3Set(A, p, q, s * c * (app - aqq) + (c * c - s * s) * apq);
        Matrix3x3Set(A, q, p, A[p][q]);
        Matrix3x3Set(A, q, q, s * s * app + c * c * aqq + 2.0 * s * c * apq);

        // 固有ベクトルの更新
        for (int i = 0; i < 3; i++)
        {
            float tmp_ip = c * eigenVectors[i][p] - s * eigenVectors[i][q];
            float tmp_iq = s * eigenVectors[i][p] + c * eigenVectors[i][q];
            Matrix3x3Set(eigenVectors, i, p, tmp_ip);
            Matrix3x3Set(eigenVectors, i, q, tmp_iq);
        }
    }
}

// ヤコビ法で回転行列を求める
float3x3 Matrix3x3FindRotationMatrixJacobi(float3x3 M)
{
    // ヤコビ法で固有値分解を行い、固有値と固有ベクトルを求める
    float3x3 A = mul(transpose(M), M);
    float3x3 eigenVectors;
    Matrix3x3JacobiEigenDecomposition(A, eigenVectors);
    // S = Sqrt(M_t * M)を求める
    float3x3 Lambda_sqrt = float3x3(
        sqrt(A[0][0]), 0, 0,
        0, sqrt(A[1][1]), 0,
        0, 0, sqrt(A[2][2])
        );
    float3x3 S = mul(mul(eigenVectors, Lambda_sqrt), transpose(eigenVectors));
    
    // R = M * S_inv
    float3x3 R = mul(M, Matrix3x3Inverse(S));
    return R;
}