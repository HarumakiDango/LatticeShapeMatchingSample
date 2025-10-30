struct Matrix3x3f
{
    float3 row0;
    float3 row1;
    float3 row2;
};

/// ===== コンストラクタ =====
Matrix3x3f MakeMatrix3x3f(float3 r0, float3 r1, float3 r2)
{
    Matrix3x3f m;
    m.row0 = r0;
    m.row1 = r1;
    m.row2 = r2;
    return m;
}

// ===== 単位行列 =====
Matrix3x3f Matrix3x3f_Identity()
{
    return MakeMatrix3x3f(
        float3(1, 0, 0),
        float3(0, 1, 0),
        float3(0, 0, 1)
    );
}

// ===== 転置 =====
Matrix3x3f Matrix3x3f_Transpose(Matrix3x3f m)
{
    return MakeMatrix3x3f(
        float3(m.row0.x, m.row1.x, m.row2.x),
        float3(m.row0.y, m.row1.y, m.row2.y),
        float3(m.row0.z, m.row1.z, m.row2.z)
    );
}

// ===== 行列 × ベクトル =====
float3 mul(Matrix3x3f m, float3 v)
{
    return float3(
        dot(m.row0, v),
        dot(m.row1, v),
        dot(m.row2, v)
    );
}

// ===== 行列 × 行列 =====
Matrix3x3f mul(Matrix3x3f a, Matrix3x3f b)
{
    Matrix3x3f bt = Matrix3x3f_Transpose(b);
    return MakeMatrix3x3f(
        float3(dot(a.row0, bt.row0), dot(a.row0, bt.row1), dot(a.row0, bt.row2)),
        float3(dot(a.row1, bt.row0), dot(a.row1, bt.row1), dot(a.row1, bt.row2)),
        float3(dot(a.row2, bt.row0), dot(a.row2, bt.row1), dot(a.row2, bt.row2))
    );
}

// ===== スカラー倍 =====
Matrix3x3f mul(Matrix3x3f m, float s)
{
    return MakeMatrix3x3f(m.row0 * s, m.row1 * s, m.row2 * s);
}

// ===== 加算・減算 =====
Matrix3x3f add(Matrix3x3f a, Matrix3x3f b)
{
    return MakeMatrix3x3f(a.row0 + b.row0, a.row1 + b.row1, a.row2 + b.row2);
}

Matrix3x3f sub(Matrix3x3f a, Matrix3x3f b)
{
    return MakeMatrix3x3f(a.row0 - b.row0, a.row1 - b.row1, a.row2 - b.row2);
}

// ===== 値の取得 (行・列指定) =====
float Matrix3x3f_Get(Matrix3x3f m, int row, int col)
{
    if (row == 0)
    {
        if (col == 0) return m.row0.x;
        if (col == 1) return m.row0.y;
        if (col == 2) return m.row0.z;
    }
    else if (row == 1)
    {
        if (col == 0) return m.row1.x;
        if (col == 1) return m.row1.y;
        if (col == 2) return m.row1.z;
    }
    else if (row == 2)
    {
        if (col == 0) return m.row2.x;
        if (col == 1) return m.row2.y;
        if (col == 2) return m.row2.z;
    }
    return 0.0; // 範囲外
}

// ===== 値の設定 (行・列指定) =====
void Matrix3x3f_Set(inout Matrix3x3f m, int row, int col, float value)
{
    if (row == 0)
    {
        if (col == 0) m.row0.x = value;
        else if (col == 1) m.row0.y = value;
        else if (col == 2) m.row0.z = value;
    }
    else if (row == 1)
    {
        if (col == 0) m.row1.x = value;
        else if (col == 1) m.row1.y = value;
        else if (col == 2) m.row1.z = value;
    }
    else if (row == 2)
    {
        if (col == 0) m.row2.x = value;
        else if (col == 1) m.row2.y = value;
        else if (col == 2) m.row2.z = value;
    }
}

// ============================================================
// Matrix3x3f Polar Decomposition (ComputeShader version)
// ============================================================

// --- One Norm ---
float Matrix3x3f_OneNorm(Matrix3x3f A)
{
    float sum1 = abs(A.row0.x) + abs(A.row1.x) + abs(A.row2.x);
    float sum2 = abs(A.row0.y) + abs(A.row1.y) + abs(A.row2.y);
    float sum3 = abs(A.row0.z) + abs(A.row1.z) + abs(A.row2.z);

    return max(sum1, max(sum2, sum3));
}

// --- Infinity Norm ---
float Matrix3x3f_InfNorm(Matrix3x3f A)
{
    float sum1 = abs(A.row0.x) + abs(A.row0.y) + abs(A.row0.z);
    float sum2 = abs(A.row1.x) + abs(A.row1.y) + abs(A.row1.z);
    float sum3 = abs(A.row2.x) + abs(A.row2.y) + abs(A.row2.z);

    return max(sum1, max(sum2, sum3));
}

// --- GetRow ---
float3 Matrix3x3f_GetRow(Matrix3x3f m, int row)
{
    if (row == 0) return m.row0;
    if (row == 1) return m.row1;
    return m.row2;
}

// --- SetRow ---
void Matrix3x3f_SetRow(inout Matrix3x3f m, int row, float3 v)
{
    if (row == 0) m.row0 = v;
    else if (row == 1) m.row1 = v;
    else m.row2 = v;
}

float SqrMagnitude(float3 v)
{
    return v.x * v.x + v.y * v.y + v.z * v.z;
}

// --- Polar Decomposition Stable ---
Matrix3x3f Matrix3x3f_PolarDecompositionStable(Matrix3x3f M, float tolerance)
{
    Matrix3x3f Mt = Matrix3x3f_Transpose(M);
    float Mone = Matrix3x3f_OneNorm(M);
    float Minf = Matrix3x3f_InfNorm(M);
    float Eone = 0;

    Matrix3x3f MadjTt = MakeMatrix3x3f(float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0));
    Matrix3x3f Et = MakeMatrix3x3f(float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0));

    [loop]
    do
    {
        // 各行を交差積で更新
        MadjTt.row0 = cross(Mt.row1, Mt.row2);
        MadjTt.row1 = cross(Mt.row2, Mt.row0);
        MadjTt.row2 = cross(Mt.row0, Mt.row1);

        float det = Mt.row0.x * MadjTt.row0.x + Mt.row0.y * MadjTt.row0.y + Mt.row0.z * MadjTt.row0.z;

        if (abs(det) < 1e-12f)
        {
            int index = 2147483647; // int.MaxValue の代用

            for (int i = 0; i < 3; i++)
            {
                float len = SqrMagnitude(Matrix3x3f_GetRow(MadjTt, i));
                if (len > 1e-12f)
                {
                    // index of valid cross product
                    // => is also the index of the vector in Mt that must be exchanged
                    index = i;
                    break;
                }
            }

            if (index == 2147483647)
            {
                return Matrix3x3f_Identity();
            }

            else
            {
                // Mt.SetRow(index, Mt.GetRow((index + 1) % 3).Cross(Mt.GetRow((index + 2) % 3))); 
                Matrix3x3f_SetRow(Mt, index, cross(Matrix3x3f_GetRow(Mt, (index + 1) % 3), Matrix3x3f_GetRow(Mt, (index + 2) % 3)));

                // MadjTt.SetRow((index + 1) % 3, Mt.GetRow((index + 2) % 3).Cross(Mt.GetRow(index))); 
                Matrix3x3f_SetRow(MadjTt, (index + 1) % 3, cross(Matrix3x3f_GetRow(Mt, (index + 2) % 3), Matrix3x3f_GetRow(Mt, index)));

                // MadjTt.SetRow((index + 2) % 3, Mt.GetRow(index).Cross(Mt.GetRow((index + 1) % 3)));
                Matrix3x3f_SetRow(MadjTt, (index + 2) % 3, cross(Matrix3x3f_GetRow(Mt, index), Matrix3x3f_GetRow(Mt, (index + 1) % 3)));

                Matrix3x3f M2 = Matrix3x3f_Transpose(Mt);

                Mone = Matrix3x3f_OneNorm(M2);
                Minf = Matrix3x3f_InfNorm(M2);

                det = det = Mt.row0.x * MadjTt.row0.x + Mt.row0.y * MadjTt.row0.y + Mt.row0.z * MadjTt.row0.z;
            }

        }

        float MadjTone = Matrix3x3f_OneNorm(MadjTt);
        float MadjTinf = Matrix3x3f_InfNorm(MadjTt);

        float gamma = sqrt(sqrt((MadjTone * MadjTinf) / (Mone * Minf)) / abs(det));

        float g1 = gamma * 0.5;
        float g2 = 0.5 / (gamma * det);

        for(int i = 0; i < 3; i++)
        {
            for(int j = 0; j < 3; j++)
            {
                // Et[i,j] = Mt[i,j];
                Matrix3x3f_Set(Et, i, j, Matrix3x3f_Get(Mt, i, j));

                // Mt[i,j] = g1*Mt[i,j] + g2*MadjTt[i,j];
                Matrix3x3f_Set(Mt, i, j, g1 * Matrix3x3f_Get(Mt, i, j) + g2 * Matrix3x3f_Get(MadjTt, i, j));

                // Et[i,j] -= Mt[i,j];
                Matrix3x3f_Set(Et, i, j, Matrix3x3f_Get(Et, i, j) - Matrix3x3f_Get(Mt, i, j));
            }
        }

        Eone = Matrix3x3f_OneNorm(Et);
        Mone = Matrix3x3f_OneNorm(Mt);
        Minf = Matrix3x3f_InfNorm(Mt);

    } while (Eone > Mone * tolerance);

    // Q = Mt^T
    return Matrix3x3f_Transpose(Mt);
}
