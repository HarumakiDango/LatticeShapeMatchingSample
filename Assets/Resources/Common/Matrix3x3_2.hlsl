/*float OneNorm(float3x3 M)
{
    float sum1 = abs(M[0][0]) + abs(M[1][0]) + abs(M[2][0]);
	float sum2 = abs(M[0][1]) + abs(M[1][1]) + abs(M[2][1]);
	float sum3 = abs(M[0][2]) + abs(M[1][2]) + abs(M[2][2]);

	float maxSum = sum1;
	if (sum2 > maxSum) maxSum = sum2;
	if (sum3 > maxSum) maxSum = sum3;

	return maxSum;
}

float InfNorm(float3x3 M)
{
    float sum1 = abs(M[0][0]) + abs(M[0][1]) + abs(M[0][2]);
	float sum2 = abs(M[1][0]) + abs(M[1][1]) + abs(M[1][2]);
	float sum3 = abs(M[2][0]) + abs(M[2][1]) + abs(M[2][2]);

	 float maxSum = sum1;
	if (sum2 > maxSum) maxSum = sum2;
	if (sum3 > maxSum) maxSum = sum3;

	return maxSum;
}

float SqrMag(float3 v)
{
    return v.x * v.x + v.y * v.y + v.z * v.z;
}

void SetRow(out float3x3 M, int index, float3 v)
{
    float3 row0 = M[0];
    float3 row1 = M[1];
    float3 row2 = M[2];

    if (index == 0) M = float3x3(v, row1, row2);
    if (index == 1) M = float3x3(row0, v, row2);
    if (index == 2) M = float3x3(row0, row1, v);
}

void PolarDecompositionStable(float3x3 M, float tolerance, out float3x3 R)
{
    float3x3 Mt = transpose(M);
    float Mone = OneNorm(M);
    float Minf = InfNorm(M);

    float Eone = 0;

    float3x3 MadjTt;
    float3x3 Et;

    int MaxValue = 999999999;

    int loopCount = 0;

    do
    {
        // Adjoint transpose (cross-products of rows)
        MadjTt[0] = cross(Mt[1], Mt[2]);
        MadjTt[1] = cross(Mt[2], Mt[0]);
        MadjTt[2] = cross(Mt[0], Mt[1]);

        float det = Mt[0][0] * MadjTt[0][0] +
                    Mt[0][1] * MadjTt[0][1] +
                    Mt[0][2] * MadjTt[0][2];

        if (abs(det) < 1e-12)
        {
            int index = MaxValue;
            for (int i = 0; i < 3; i++)
            {
                float len = SqrMag(MadjTt[i]);
                if (len > 1e-12)
                {
                    // index of valid cross product
                    // => is also the index of the vector in Mt that must be exchanged
                    index = i;
                    break;
                }
            }

            if (index == MaxValue)
            {
                R = float3x3(1,0,0, 0,1,0, 0,0,1);
                return;
            }
            else
            {
                float3 row0 = Mt[0];
                float3 row1 = Mt[1];
                float3 row2 = Mt[2];

                float3 crossProduct = cross(Mt[(index + 1) % 3], Mt[(index + 2) % 3]);
                // Mt[index] = crossProduct;

                if (index == 0) Mt = float3x3(crossProduct, row1, row2);
                if (index == 1) Mt = float3x3(row0, crossProduct, row2);
                if (index == 2) Mt = float3x3(row0, row1, crossProduct);

                SetRow(MadjTt, (index + 1) % 3, cross(Mt[(index + 2) % 3], Mt[index]));
                SetRow(MadjTt, (index + 2) % 3, cross(Mt[index], Mt[(index + 1) % 3]));
                
                float3x3 M2 = transpose(Mt);

                Mone = OneNorm(M2);
                Minf = InfNorm(M2);

                det = Mt[0][0] * MadjTt[0][0] + Mt[0][1] * MadjTt[0][1] + Mt[0][2] * MadjTt[0][2];
            }
        }

        float MadjTone = OneNorm(MadjTt);
        float MadjTinf = InfNorm(MadjTt);

        float gamma = sqrt(sqrt((MadjTone * MadjTinf) / (Mone * Minf)) / abs(det));

        float g1 = gamma * 0.5;
        float g2 = 0.5 / (gamma * det);

        // Newton update
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Et[i][j] = Mt[i][j];
                Mt[i][j] = g1 * Mt[i][j] + g2 * MadjTt[i][j];
                Et[i][j] -= Mt[i][j];
            }
        }

        Eone = OneNorm(Et);

        Mone = OneNorm(Mt);
        Minf = InfNorm(Mt);

        loopCount++;
    }
    while (Eone > Mone * tolerance && loopCount < 100);

    R = transpose(Mt);
}*/

float OneNorm(float3x3 M) {
    float sum1 = abs(M[0][0]) + abs(M[1][0]) + abs(M[2][0]);
    float sum2 = abs(M[0][1]) + abs(M[1][1]) + abs(M[2][1]);
    float sum3 = abs(M[0][2]) + abs(M[1][2]) + abs(M[2][2]);
    return max(sum1, max(sum2, sum3));
}

float InfNorm(float3x3 M) {
    float sum1 = abs(M[0][0]) + abs(M[0][1]) + abs(M[0][2]);
    float sum2 = abs(M[1][0]) + abs(M[1][1]) + abs(M[1][2]);
    float sum3 = abs(M[2][0]) + abs(M[2][1]) + abs(M[2][2]);
    return max(sum1, max(sum2, sum3));
}

float SqrMag(float3 v) {
    return dot(v, v);
}

void SetRow(out float3x3 M, int index, float3 v) {
    float3 row0 = M[0];
    float3 row1 = M[1];
    float3 row2 = M[2];
    if (index == 0) M = float3x3(v, row1, row2);
    if (index == 1) M = float3x3(row0, v, row2);
    if (index == 2) M = float3x3(row0, row1, v);
}

void PolarDecompositionStable(float3x3 M, float tolerance, out float3x3 R)
{
    float3x3 Mt = transpose(M);
    float Mone = OneNorm(M);
    float Minf = InfNorm(M);

    float3x3 MadjTt = float3x3(0.0,0.0,0.0, 0.0,0.0,0.0, 0.0,0.0,0.0);
    float3x3 Et = float3x3(0.0,0.0,0.0, 0.0,0.0,0.0, 0.0,0.0,0.0);

    float Eone = 0.0;
    int loopCount = 0;

    do {
        // Adjoint transpose (cross-products of rows)
        MadjTt[0] = cross(Mt[1], Mt[2]);
        MadjTt[1] = cross(Mt[2], Mt[0]);
        MadjTt[2] = cross(Mt[0], Mt[1]);

        float det = dot(Mt[0], MadjTt[0]);

        if (abs(det) < 1e-12) {
            int index = -1;
            for (int i = 0; i < 3; i++) {
                if (SqrMag(MadjTt[i]) > 1e-12) {
                    index = i;
                    break;
                }
            }

            if (index < 0) {
                R = float3x3(1,0,0, 0,1,0, 0,0,1);
                return;
            }

            float3 row0 = Mt[0];
            float3 row1 = Mt[1];
            float3 row2 = Mt[2];
            float3 crossProduct = cross(Mt[(index + 1) % 3], Mt[(index + 2) % 3]);
            if (index == 0) Mt = float3x3(crossProduct, row1, row2);
            if (index == 1) Mt = float3x3(row0, crossProduct, row2);
            if (index == 2) Mt = float3x3(row0, row1, crossProduct);
        }

        float MadjTone = OneNorm(MadjTt);
        float MadjTinf = InfNorm(MadjTt);
        float gamma = sqrt(sqrt((MadjTone * MadjTinf) / (Mone * Minf)) / abs(det));

        float g1 = 0.5 * gamma;
        float g2 = 0.5 / (gamma * det);

        for (int i = 0; i < 3; i++) {
            Et[i] = Mt[i];
            Mt[i] = g1 * Mt[i] + g2 * MadjTt[i];
            Et[i] -= Mt[i];
        }

        Eone = OneNorm(Et);
        Mone = OneNorm(Mt);
        Minf = InfNorm(Mt);
        loopCount++;
    }
    while (Eone > max(Mone * tolerance, 1e-12) && loopCount < 100);

    R = transpose(Mt);
}
